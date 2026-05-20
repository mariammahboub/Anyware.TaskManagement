using System.Net.Http.Headers;
using System.Net.Http.Json;
using Anyware.TaskManagement.API.Tests.Helpers;

namespace Anyware.TaskManagement.API.Tests.Helpers;

public static class AuthHelper
{
    public const string AdminEmail    = "admin@anyware.com";
    public const string AdminPassword = "Admin@123";
    public const string UserName      = "Demo Integration User";
    public const string UserPassword  = "Demo@Integration1!";

    public sealed record TokenPair(
        string   AccessToken,
        string   RefreshToken,
        DateTime AccessTokenExpiry,
        Guid     UserId,
        string   Name,
        string   Email,
        string   Role);

    public static async Task<TokenPair> RegisterAsync(
        HttpClient client, string name, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { name, email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenPair>(TestJsonOptions.Default))!;
    }

    public static async Task<TokenPair> LoginAsync(
        HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenPair>(TestJsonOptions.Default))!;
    }

    public static void Authorize(HttpClient client, string accessToken)
        => client.DefaultRequestHeaders.Authorization =
               new AuthenticationHeaderValue("Bearer", accessToken);

    public static void Deauthorize(HttpClient client)
        => client.DefaultRequestHeaders.Authorization = null;

    public static async Task<(HttpClient Client, TokenPair Tokens)> AsAdminAsync(
        HttpClient client)
    {
        var tokens = await LoginAsync(client, AdminEmail, AdminPassword);
        Authorize(client, tokens.AccessToken);
        return (client, tokens);
    }

    public static async Task<(HttpClient Client, TokenPair Tokens)> AsNewUserAsync(
        HttpClient client, string? email = null)
    {
        var resolvedEmail = email ?? $"user_{Guid.NewGuid():N}@test.com";
        var tokens        = await RegisterAsync(client, UserName, resolvedEmail, UserPassword);
        Authorize(client, tokens.AccessToken);
        return (client, tokens);
    }
}
