using Anyware.TaskManagement.API.Tests.Fixtures;
using Anyware.TaskManagement.API.Tests.Helpers;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.API.Tests.Integration.Auth
{
    public sealed class AuthIntegrationTests : IClassFixture<ApiTestFixture>
    {
        private readonly HttpClient _client;

        public AuthIntegrationTests(ApiTestFixture factory)
            => _client = factory.CreateApiClient();
        [Fact]
        public async Task Register_ValidRequest_Returns201AndTokenPair()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "New User",
                email = $"new_{Guid.NewGuid():N}@test.com",
                password = "Pass@1234!"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content
                .ReadFromJsonAsync<AuthHelper.TokenPair>(TestJsonOptions.Default);

            body.Should().NotBeNull();
            body!.AccessToken.Should().NotBeNullOrWhiteSpace();
            body.RefreshToken.Should().NotBeNullOrWhiteSpace();
            body.Role.Should().Be("User");
            body.AccessTokenExpiry.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns409()
        {
            var email = $"dup_{Guid.NewGuid():N}@test.com";
            await _client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "First",
                email,
                password = "Pass@1234!"
            });
            var response = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "Second",
                email,
                password = "Pass@1234!"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_InvalidPassword_Returns422WithFieldErrors()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "Bad Pass",
                email = "badpass@test.com",
                password = "weak"
            });

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var body = await response.Content
                .ReadFromJsonAsync<ErrorBody>(TestJsonOptions.Default);
            body!.Errors.Should().NotBeNull();
            body.Errors!.Should().ContainKey("Password");
        }
        [Fact]
        public async Task Login_ValidAdminCredentials_Returns200AndAdminRole()
        {
            var tokens = await AuthHelper.LoginAsync(
                _client, AuthHelper.AdminEmail, AuthHelper.AdminPassword);

            tokens.Should().NotBeNull();
            tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
            tokens.Role.Should().Be("Admin");
            tokens.Email.Should().Be(AuthHelper.AdminEmail);
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = AuthHelper.AdminEmail,
                password = "WrongPassword999!"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_UnknownEmail_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "ghost@nobody.com",
                password = "Anything@123"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ErrorMessageDoesNotRevealWhichFieldWasWrong()
        {
            var wrongEmailResp = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "notexist@test.com",
                password = "Pass@1234!"
            });
            var wrongPasswordResp = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = AuthHelper.AdminEmail,
                password = "Wrong@1234!"
            });

            var msgA = (await wrongEmailResp.Content
                .ReadFromJsonAsync<ErrorBody>(TestJsonOptions.Default))!.Message;
            var msgB = (await wrongPasswordResp.Content
                .ReadFromJsonAsync<ErrorBody>(TestJsonOptions.Default))!.Message;

            msgA.Should().Be(msgB, "error messages must be identical to prevent email enumeration");
        }
        [Fact]
        public async Task Refresh_WithValidTokenPair_Returns200AndNewTokenPair()
        {
            var original = await AuthHelper.LoginAsync(
                _client, AuthHelper.AdminEmail, AuthHelper.AdminPassword);

            var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
            {
                accessToken = original.AccessToken,
                refreshToken = original.RefreshToken
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var refreshed = await response.Content
                .ReadFromJsonAsync<AuthHelper.TokenPair>(TestJsonOptions.Default);

            refreshed!.AccessToken.Should().NotBe(original.AccessToken,
                "token rotation must issue a new access token");
            refreshed.RefreshToken.Should().NotBe(original.RefreshToken,
                "token rotation must issue a new refresh token");
        }

        [Fact]
        public async Task Refresh_WithOldRefreshTokenAfterRotation_Returns401()
        {
            var original = await AuthHelper.LoginAsync(
            _client, AuthHelper.AdminEmail, AuthHelper.AdminPassword);
            var firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new
            {
                accessToken = original.AccessToken,
                refreshToken = original.RefreshToken
            });
            firstRefresh.EnsureSuccessStatusCode();
            var secondRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new
            {
                accessToken = original.AccessToken,
                refreshToken = original.RefreshToken
            });

            secondRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        [Fact]
        public async Task Revoke_AsAuthenticatedUser_Returns204()
        {
            var tokens = await AuthHelper.LoginAsync(
                _client, AuthHelper.AdminEmail, AuthHelper.AdminPassword);
            AuthHelper.Authorize(_client, tokens.AccessToken);

            var response = await _client.PostAsync("/api/auth/revoke", null);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Revoke_ThenRefresh_Returns401()
        {
            var tokens = await AuthHelper.LoginAsync(
                _client, AuthHelper.AdminEmail, AuthHelper.AdminPassword);
            AuthHelper.Authorize(_client, tokens.AccessToken);
            await _client.PostAsync("/api/auth/revoke", null);
            AuthHelper.Deauthorize(_client);
            var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
            {
                accessToken = tokens.AccessToken,
                refreshToken = tokens.RefreshToken
            });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Revoke_WithoutToken_Returns401()
        {
            AuthHelper.Deauthorize(_client);
            var response = await _client.PostAsync("/api/auth/revoke", null);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        [Fact]
        public async Task ProtectedEndpoint_WithoutToken_Returns401()
        {
            AuthHelper.Deauthorize(_client);
            var response = await _client.GetAsync("/api/users/me");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        private sealed record ErrorBody(
            int StatusCode,
            string Message,
            IDictionary<string, string[]>? Errors);
    }
}