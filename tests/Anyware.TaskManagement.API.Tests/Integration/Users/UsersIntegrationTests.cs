using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Anyware.TaskManagement.API.Tests.Fixtures;
using Anyware.TaskManagement.API.Tests.Helpers;

namespace Anyware.TaskManagement.API.Tests.Integration.Users;

public sealed class UsersIntegrationTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;

    private sealed record UserBody(
        Guid Id, string Name, string Email, string Role,
        DateTime CreatedAt, DateTime? UpdatedAt)
    {
        public Guid UserId => Id;
    }

    private sealed record ErrorBody(int StatusCode, string Message);

    public UsersIntegrationTests(ApiTestFixture factory)
        => _client = factory.CreateApiClient();

    [Fact]
    public async Task GetCurrentUser_AuthenticatedUser_ReturnsOwnProfile()
    {
        var (_, tokens) = await AuthHelper.AsNewUserAsync(_client);
        var response    = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserBody>(TestJsonOptions.Default);
        body!.UserId.Should().Be(tokens.UserId);
        body.Role.Should().Be("User");
    }

    [Fact]
    public async Task GetCurrentUser_Unauthenticated_Returns401()
    {
        AuthHelper.Deauthorize(_client);
        var response = await _client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllUsers_AsAdmin_Returns200WithUserList()
    {
        await AuthHelper.AsAdminAsync(_client);
        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserBody>>(TestJsonOptions.Default);
        users.Should().NotBeNullOrEmpty();
        users.Should().Contain(u => u.Email == AuthHelper.AdminEmail.ToLowerInvariant());
    }

    [Fact]
    public async Task GetAllUsers_AsRegularUser_Returns403()
    {
        await AuthHelper.AsNewUserAsync(_client);
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllUsers_Unauthenticated_Returns401()
    {
        AuthHelper.Deauthorize(_client);
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_AsAdmin_Returns201()
    {
        await AuthHelper.AsAdminAsync(_client);
        var email    = $"newstaff_{Guid.NewGuid():N}@anyware.com";
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Staff Member", email, password = "Staff@Pass1!", role = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserBody>(TestJsonOptions.Default);
        body!.Email.Should().Be(email.ToLowerInvariant());
        body.Role.Should().Be("User");
    }

    [Fact]
    public async Task CreateUser_AsAdmin_DuplicateEmail_Returns409()
    {
        await AuthHelper.AsAdminAsync(_client);
        var email = $"dup_{Guid.NewGuid():N}@anyware.com";

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "First", email, password = "Pass@1234!", role = "User"
        });

        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Second", email, password = "Pass@1234!", role = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUser_AsRegularUser_Returns403()
    {
        await AuthHelper.AsNewUserAsync(_client);
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Fail", email = "fail@test.com", password = "Pass@1234!", role = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_AsAdmin_Returns204AndUserRemoved()
    {
        await AuthHelper.AsAdminAsync(_client);
        var email   = $"todelete_{Guid.NewGuid():N}@anyware.com";
        var created = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "To Delete", email, password = "Pass@1234!", role = "User"
        });
        var user = await created.Content.ReadFromJsonAsync<UserBody>(TestJsonOptions.Default);

        var deleteResponse = await _client.DeleteAsync($"/api/users/{user!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await _client.GetAsync("/api/users");
        var users        = await listResponse.Content.ReadFromJsonAsync<List<UserBody>>(TestJsonOptions.Default);
        users.Should().NotContain(u => u.Id == user.Id);
    }

    [Fact]
    public async Task DeleteUser_OwnAdminAccount_Returns403()
    {
        var (_, tokens) = await AuthHelper.AsAdminAsync(_client);
        var response    = await _client.DeleteAsync($"/api/users/{tokens.UserId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_AsRegularUser_Returns403()
    {
        await AuthHelper.AsNewUserAsync(_client);
        var response = await _client.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_NonExistentId_Returns404()
    {
        await AuthHelper.AsAdminAsync(_client);
        var response = await _client.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
