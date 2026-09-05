using System.Net;
using System.Net.Http.Json;
using Application.DTOs.History;
using Application.DTOs.Tickets;

namespace Tests;

/// <summary>
/// Scenarios F and G: Concurrent claim and idempotent claim tests.
/// F: Agent1 and Agent2 claim TCK-000001 concurrently → one 200, one 409.
/// G: Winning agent repeats claim → 200 unchanged, no new history.
/// </summary>
public class ClaimTests : IAsyncLifetime
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

    // === Scenario F ===

    [Fact]
    public async Task ConcurrentClaim_OneSucceedsOneGets409()
    {
        // Get tokens for both agents
        var agent1Token = await TestHelpers.LoginAsync(_client, "agent1@demo.local", "Demo!Agent1");
        var agent2Token = await TestHelpers.LoginAsync(_client, "agent2@demo.local", "Demo!Agent2");

        // Create two separate clients to simulate concurrent claims
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        TestHelpers.SetToken(client1, agent1Token);
        TestHelpers.SetToken(client2, agent2Token);

        // Fire both claims simultaneously
        var task1 = client1.PostAsync("/api/tickets/TCK-000001/claim", null);
        var task2 = client2.PostAsync("/api/tickets/TCK-000001/claim", null);

        var results = await Task.WhenAll(task1, task2);

        var statusCodes = results.Select(r => r.StatusCode).ToList();

        // Exactly one should be 200 OK and one should be 409 Conflict
        Assert.Contains(HttpStatusCode.OK, statusCodes);
        Assert.Contains(HttpStatusCode.Conflict, statusCodes);

        // Verify only one Claimed history row
        TestHelpers.SetToken(_client, agent1Token);
        var historyResponse = await _client.GetAsync("/api/tickets/TCK-000001/history");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<HistoryResponse>>(TestHelpers.JsonOptions);
        Assert.NotNull(history);

        var claimedCount = history.Count(h => h.EventType.ToString() == "Claimed");
        Assert.Equal(1, claimedCount);

        client1.Dispose();
        client2.Dispose();
    }

    // === Scenario G ===

    [Fact]
    public async Task IdempotentClaim_SameAgent_Returns200Unchanged()
    {
        await TestHelpers.LoginAndSetTokenAsync(_client, "agent1@demo.local", "Demo!Agent1");

        // First claim
        var response1 = await _client.PostAsync("/api/tickets/TCK-000001/claim", null);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var ticket1 = await response1.Content.ReadFromJsonAsync<TicketResponse>(TestHelpers.JsonOptions);

        // Get history count after first claim
        var historyResponse1 = await _client.GetAsync("/api/tickets/TCK-000001/history");
        var history1 = await historyResponse1.Content.ReadFromJsonAsync<List<HistoryResponse>>(TestHelpers.JsonOptions);
        var claimedCount1 = history1!.Count(h => h.EventType.ToString() == "Claimed");

        // Second claim by same agent
        var response2 = await _client.PostAsync("/api/tickets/TCK-000001/claim", null);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var ticket2 = await response2.Content.ReadFromJsonAsync<TicketResponse>(TestHelpers.JsonOptions);

        // Version and state should be unchanged
        Assert.Equal(ticket1!.Version, ticket2!.Version);

        // History count should not increase
        var historyResponse2 = await _client.GetAsync("/api/tickets/TCK-000001/history");
        var history2 = await historyResponse2.Content.ReadFromJsonAsync<List<HistoryResponse>>(TestHelpers.JsonOptions);
        var claimedCount2 = history2!.Count(h => h.EventType.ToString() == "Claimed");
        Assert.Equal(claimedCount1, claimedCount2);
    }

    [Fact]
    public async Task Claim_ByDifferentAgent_AfterClaimed_Returns409()
    {
        // Agent1 claims first
        await TestHelpers.LoginAndSetTokenAsync(_client, "agent1@demo.local", "Demo!Agent1");
        var response1 = await _client.PostAsync("/api/tickets/TCK-000001/claim", null);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        // Agent2 tries to claim the same ticket
        TestHelpers.ClearToken(_client);
        await TestHelpers.LoginAndSetTokenAsync(_client, "agent2@demo.local", "Demo!Agent2");
        var response2 = await _client.PostAsync("/api/tickets/TCK-000001/claim", null);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }
}
