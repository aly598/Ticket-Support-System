using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;

namespace Tests;

/// <summary>
/// Helper methods for integration tests — login, create ticket, etc.
/// </summary>
public static class TestHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return loginResponse!.AccessToken;
    }

    public static void SetToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<string> LoginAndSetTokenAsync(HttpClient client, string email, string password)
    {
        var token = await LoginAsync(client, email, password);
        SetToken(client, token);
        return token;
    }

    public static void ClearToken(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }
}
