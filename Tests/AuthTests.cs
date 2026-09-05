using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Auth;

namespace Tests;

/// <summary>
/// Scenarios A and B: Authentication and Authorization tests.
/// A: Login with seeded credentials returns 200 + JWT. Invalid returns 401.
/// B: Anonymous → 401, wrong role → 403.
/// </summary>
public class AuthTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.CleanupDatabaseAsync();
        await _factory.DisposeAsync();
    }

    // === Scenario A ===

    [Fact]
    public async Task Login_WithValidAgentCredentials_Returns200WithJwt()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "agent1@demo.local", password = "Demo!Agent1" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(login);
        Assert.NotEmpty(login.AccessToken);
        Assert.Equal("agent1@demo.local", login.User.Email);
        Assert.Equal("SupportAgent", login.User.Role);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "agent1@demo.local", password = "WrongPassword" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@demo.local", password = "Demo!Agent1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // === Scenario B ===

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StaffEndpoint_WithCustomerToken_Returns403()
    {
        await TestHelpers.LoginAndSetTokenAsync(_client, "customer1@demo.local", "Demo!Customer1");

        // Customer tries to claim a ticket (staff-only operation)
        var response = await _client.PostAsync("/api/tickets/TCK-000001/claim", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_NewCustomer_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { email = "newcustomer@test.local", displayName = "New Customer", password = "Test!Pass1" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
