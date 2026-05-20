using Anyware.TaskManagement.API.Tests.Fixtures;
using Anyware.TaskManagement.API.Tests.Helpers;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.API.Tests.Integration.Tasks
{
    public sealed class TasksIntegrationTests : IClassFixture<ApiTestFixture>
    {
        private readonly HttpClient _client;
        private readonly ApiTestFixture _factory;
        private sealed record TaskBody(
            Guid Id,
            string Title,
            string Description,
            string Status,
            string Priority,
            Guid UserId,
            DateTime CreatedAt,
            DateTime? UpdatedAt);

        private sealed record ErrorBody(
            int StatusCode,
            string Message,
            IDictionary<string, string[]>? Errors);

        public TasksIntegrationTests(ApiTestFixture factory)
        {
            _factory = factory;
            _client = factory.CreateApiClient();
        }


        [Fact]
        public async Task CreateTask_AsAuthenticatedUser_Returns201WithLocationHeader()
        {
            await AuthHelper.AsNewUserAsync(_client);

            var response = await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = "Integration test task",
                description = "Created in an integration test",
                priority = "high"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();

            var body = await response.Content
                .ReadFromJsonAsync<TaskBody>(TestJsonOptions.Default);

            body!.Title.Should().Be("Integration test task");
            body.Status.Should().Be("Pending");
            body.Priority.Should().Be("High");
            body.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task CreateTask_LocationHeaderPointsToGetEndpoint()
        {
            await AuthHelper.AsNewUserAsync(_client);

            var createResponse = await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = "Location header test",
                description = "Verifying the Location header",
                priority = "low"
            });

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var location = createResponse.Headers.Location!.ToString();
            var getResponse = await _client.GetAsync(location);

            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await getResponse.Content
                .ReadFromJsonAsync<TaskBody>(TestJsonOptions.Default);

            body!.Title.Should().Be("Location header test");
        }

        [Fact]
        public async Task CreateTask_WithoutToken_Returns401()
        {
            AuthHelper.Deauthorize(_client);

            var response = await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = "Unauthorized task",
                description = "Should fail",
                priority = "low"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateTask_MissingTitle_Returns422WithFieldErrors()
        {
            await AuthHelper.AsNewUserAsync(_client);

            var response = await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = "",    
                description = "desc",
                priority = "low"
            });

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var body = await response.Content
                .ReadFromJsonAsync<ErrorBody>(TestJsonOptions.Default);

            body!.Errors.Should().ContainKey("Title");
        }
        [Fact]
        public async Task CreateTask_DuplicateTitleSameDay_Returns409()
        {
            await AuthHelper.AsNewUserAsync(_client);

            const string title = "Duplicate task title";
            var first = await _client.PostAsJsonAsync("/api/tasks", new
            {
                title,
                description = "First one",
                priority = "medium"
            });
            first.StatusCode.Should().Be(HttpStatusCode.Created);

            var second = await _client.PostAsJsonAsync("/api/tasks", new
            {
                title,
                description = "Second one — duplicate",
                priority = "low"
            });

            second.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var body = await second.Content
                .ReadFromJsonAsync<ErrorBody>(TestJsonOptions.Default);

            body!.Message.Should().Contain(title);
        }

        [Fact]
        public async Task CreateTask_DifferentUsers_SameTitleSameDay_BothSucceed()
        {            await AuthHelper.AsNewUserAsync(_client);
            var firstCreate = await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = "Shared title between users",
                description = "User A task",
                priority = "low"
            });
            firstCreate.StatusCode.Should().Be(HttpStatusCode.Created);
            await AuthHelper.AsNewUserAsync(_client, $"userb_{Guid.NewGuid():N}@test.com");
            var secondCreate = await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = "Shared title between users",
                description = "User B task",
                priority = "high"
            });

            secondCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        [Fact]
        public async Task GetTaskById_FirstRequest_Returns200AndPopulatesCache()
        {
            await AuthHelper.AsNewUserAsync(_client);

            var taskId = await CreateTaskAndGetId("Cache population test");
            var response = await _client.GetAsync($"/api/tasks/{taskId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var scope = _factory.Services.CreateScope();
            var cache = (InMemoryCacheService)scope.ServiceProvider
                .GetRequiredService<ICacheService>();

            cache.Contains($"task:{taskId}").Should().BeTrue(
                "first GET should populate Redis cache");
        }

        [Fact]
        public async Task GetTaskById_SecondRequest_ServedFromCache()
        {
            await AuthHelper.AsNewUserAsync(_client);
            var taskId = await CreateTaskAndGetId("Cache hit test");
            await _client.GetAsync($"/api/tasks/{taskId}");
            using var scope = _factory.Services.CreateScope();
            var cache = (InMemoryCacheService)scope.ServiceProvider
                .GetRequiredService<ICacheService>();
            var response = await _client.GetAsync($"/api/tasks/{taskId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            cache.Contains($"task:{taskId}").Should().BeTrue();
        }

        [Fact]
        public async Task UpdateTaskStatus_InvalidatesCache()
        {
            await AuthHelper.AsNewUserAsync(_client);
            var taskId = await CreateTaskAndGetId("Cache invalidation test");
            await _client.GetAsync($"/api/tasks/{taskId}");

            using var scope = _factory.Services.CreateScope();
            var cache = (InMemoryCacheService)scope.ServiceProvider
                .GetRequiredService<ICacheService>();

            cache.Contains($"task:{taskId}").Should().BeTrue("pre-condition: cache must be populated");
            var patchResponse = await _client.PatchAsJsonAsync(
                $"/api/tasks/{taskId}/status",
                new { newStatus = "inProgress" });

            patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            cache.Contains($"task:{taskId}").Should().BeFalse(
                "PATCH must remove the cache entry");
        }

        [Fact]
        public async Task GetTaskById_NotFound_Returns404()
        {
            await AuthHelper.AsNewUserAsync(_client);
            var response = await _client.GetAsync($"/api/tasks/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTaskById_OtherUsersTask_Returns403()
        {
            await AuthHelper.AsNewUserAsync(_client, $"owner_{Guid.NewGuid():N}@test.com");
            var taskId = await CreateTaskAndGetId("Private task");
            await AuthHelper.AsNewUserAsync(_client, $"other_{Guid.NewGuid():N}@test.com");
            var response = await _client.GetAsync($"/api/tasks/{taskId}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateTaskStatus_OtherUsersTask_Returns403()
        {
            await AuthHelper.AsNewUserAsync(_client, $"creator_{Guid.NewGuid():N}@test.com");
            var taskId = await CreateTaskAndGetId("Protected task");
            await AuthHelper.AsNewUserAsync(_client, $"attacker_{Guid.NewGuid():N}@test.com");
            var response = await _client.PatchAsJsonAsync(
                $"/api/tasks/{taskId}/status",
                new { newStatus = "done" });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        [Fact]
        public async Task GetAllTasks_ReturnsSortedByPriorityDescThenCreatedAtAsc()
        {
            await AuthHelper.AsNewUserAsync(_client);
            await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = "Low priority task",
                description = "D",
                priority = "low"
            });
            await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = "Critical priority task",
                description = "D",
                priority = "critical"
            });
            await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = "High priority task",
                description = "D",
                priority = "high"
            });

            var response = await _client.GetAsync("/api/tasks");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var tasks = await response.Content
                .ReadFromJsonAsync<List<TaskBody>>(TestJsonOptions.Default);

            tasks.Should().NotBeNull();
            tasks!.Count.Should().BeGreaterThanOrEqualTo(3);
            tasks[0].Priority.Should().Be("Critical");
            tasks[1].Priority.Should().Be("High");
            tasks[^1].Priority.Should().Be("Low");
        }

        [Fact]
        public async Task GetAllTasks_ReturnsOnlyCurrentUsersOwnTasks()
        {
            await AuthHelper.AsNewUserAsync(_client, $"isolated_a_{Guid.NewGuid():N}@test.com");
            await _client.PostAsJsonAsync("/api/tasks", new
            {
                title = "User A exclusive task",
                description = "D",
                priority = "low"
            });

            await AuthHelper.AsNewUserAsync(_client, $"isolated_b_{Guid.NewGuid():N}@test.com");
            var response = await _client.GetAsync("/api/tasks");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var tasks = await response.Content
                .ReadFromJsonAsync<List<TaskBody>>(TestJsonOptions.Default);

            tasks.Should().NotContain(t => t.Title == "User A exclusive task",
                "users must never see each other's tasks");
        }
        [Fact]
        public async Task UpdateTaskStatus_ValidTransition_Returns204AndStatusIsUpdated()
        {
            await AuthHelper.AsNewUserAsync(_client);
            var taskId = await CreateTaskAndGetId("Status update test");

            var patchResponse = await _client.PatchAsJsonAsync(
                $"/api/tasks/{taskId}/status",
                new { newStatus = "inProgress" });

            patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/tasks/{taskId}");
            var task = await getResponse.Content
                .ReadFromJsonAsync<TaskBody>(TestJsonOptions.Default);

            task!.Status.Should().Be("InProgress");
            task.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateTaskStatus_DoneToInProgress_Returns400()
        {
            await AuthHelper.AsNewUserAsync(_client);
            var taskId = await CreateTaskAndGetId("Completed task");
            await _client.PatchAsJsonAsync(
                $"/api/tasks/{taskId}/status",
                new { newStatus = "done" });
            var response = await _client.PatchAsJsonAsync(
                $"/api/tasks/{taskId}/status",
                new { newStatus = "inProgress" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        private async Task<Guid> CreateTaskAndGetId(string title)
        {
            var response = await _client.PostAsJsonAsync("/api/tasks", new
            {
                title,
                description = "Integration test description",
                priority = "medium"
            });

            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<TaskBody>(TestJsonOptions.Default);

            return body!.Id;
        }
    }
}