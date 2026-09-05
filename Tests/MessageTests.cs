using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Messages;
using Application.DTOs.Tickets;

namespace Tests;

/// <summary>
/// Scenario J: Internal notes visibility test.
/// Agent adds internal note and public reply.
/// Staff sees both. Owner customer sees only public. Other customer sees 404.
/// </summary>
public class MessageTests : IAsyncLifetime
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

    // === Scenario J ===

    [Fact]
    public async Task InternalNote_VisibleToStaff_HiddenFromCustomer()
    {
        // Agent1 adds an internal note to TCK-000002
        await TestHelpers.LoginAndSetTokenAsync(_client, "agent1@demo.local", "Demo!Agent1");

        var internalResponse = await _client.PostAsJsonAsync("/api/tickets/TCK-000002/messages",
            new { body = "Internal note: checking with engineering team.", isInternal = true });
        Assert.Equal(HttpStatusCode.Created, internalResponse.StatusCode);

        // Agent1 adds a public reply
        var publicResponse = await _client.PostAsJsonAsync("/api/tickets/TCK-000002/messages",
            new { body = "We are working on your issue.", isInternal = false });
        Assert.Equal(HttpStatusCode.Created, publicResponse.StatusCode);

        // Staff sees both messages
        var staffGetResponse = await _client.GetAsync("/api/tickets/TCK-000002");
        var staffTicket = await staffGetResponse.Content.ReadFromJsonAsync<TicketResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(staffTicket?.Messages);
        Assert.Contains(staffTicket.Messages, m => m.IsInternal);
        Assert.Contains(staffTicket.Messages, m => !m.IsInternal);

        // Owner customer sees only public reply
        TestHelpers.ClearToken(_client);
        await TestHelpers.LoginAndSetTokenAsync(_client, "customer2@demo.local", "Demo!Customer2");

        var customerGetResponse = await _client.GetAsync("/api/tickets/TCK-000002");
        var customerTicket = await customerGetResponse.Content.ReadFromJsonAsync<TicketResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(customerTicket?.Messages);
        Assert.DoesNotContain(customerTicket.Messages, m => m.IsInternal);
        Assert.Contains(customerTicket.Messages, m => m.Body == "We are working on your issue.");

        // Another customer sees 404
        TestHelpers.ClearToken(_client);
        await TestHelpers.LoginAndSetTokenAsync(_client, "customer1@demo.local", "Demo!Customer1");
        var otherCustomerResponse = await _client.GetAsync("/api/tickets/TCK-000002");
        Assert.Equal(HttpStatusCode.NotFound, otherCustomerResponse.StatusCode);
    }

    [Fact]
    public async Task Message_OnClosedTicket_IsRejected()
    {
        // First close TCK-000003 (currently Resolved)
        await TestHelpers.LoginAndSetTokenAsync(_client, "customer1@demo.local", "Demo!Customer1");
        var closeResponse = await _client.PostAsync("/api/tickets/TCK-000003/close", null);

        if (closeResponse.StatusCode == HttpStatusCode.OK)
        {
            // Now try to add a message
            var messageResponse = await _client.PostAsJsonAsync("/api/tickets/TCK-000003/messages",
                new { body = "Trying to add a message to closed ticket.", isInternal = false });
            Assert.Equal(HttpStatusCode.Conflict, messageResponse.StatusCode);
        }
    }
}
