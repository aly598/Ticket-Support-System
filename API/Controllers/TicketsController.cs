using Application.DTOs.Tickets;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// Create a ticket for the authenticated customer.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var ticket = await _ticketService.CreateTicketAsync(request);
        return CreatedAtAction(nameof(GetTicket), new { ticketNumber = ticket.TicketNumber }, ticket);
    }

    /// <summary>
    /// Return one permitted ticket with visible messages.
    /// </summary>
    [HttpGet("{ticketNumber}")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicket(string ticketNumber)
    {
        var ticket = await _ticketService.GetTicketAsync(ticketNumber);
        return Ok(ticket);
    }

    /// <summary>
    /// Filter and page the permitted ticket list.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Application.DTOs.Common.PagedResult<TicketResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTickets([FromQuery] TicketQueryParameters query)
    {
        var result = await _ticketService.ListTicketsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Claim an Open ticket. Agent/Admin only.
    /// </summary>
    [HttpPost("{ticketNumber}/claim")]
    [Authorize(Roles = "SupportAgent,Admin")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClaimTicket(string ticketNumber)
    {
        var ticket = await _ticketService.ClaimTicketAsync(ticketNumber);
        return Ok(ticket);
    }

    /// <summary>
    /// Resolve an InProgress ticket with a public resolution message.
    /// Assigned agent or Admin only.
    /// </summary>
    [HttpPost("{ticketNumber}/resolve")]
    [Authorize(Roles = "SupportAgent,Admin")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveTicket(string ticketNumber, [FromBody] ResolveTicketRequest request)
    {
        var ticket = await _ticketService.ResolveTicketAsync(ticketNumber, request);
        return Ok(ticket);
    }

    /// <summary>
    /// Reopen a Resolved ticket within 48 hours. Owner customer only.
    /// </summary>
    [HttpPost("{ticketNumber}/reopen")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReopenTicket(string ticketNumber)
    {
        var ticket = await _ticketService.ReopenTicketAsync(ticketNumber);
        return Ok(ticket);
    }

    /// <summary>
    /// Close a Resolved ticket. Owner customer or Admin only.
    /// </summary>
    [HttpPost("{ticketNumber}/close")]
    [Authorize(Roles = "Customer,Admin")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CloseTicket(string ticketNumber)
    {
        var ticket = await _ticketService.CloseTicketAsync(ticketNumber);
        return Ok(ticket);
    }

    /// <summary>
    /// Add a public reply or permitted internal note.
    /// </summary>
    [HttpPost("{ticketNumber}/messages")]
    [ProducesResponseType(typeof(Application.DTOs.Messages.MessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMessage(string ticketNumber, [FromBody] Application.DTOs.Messages.AddMessageRequest request)
    {
        var message = await _ticketService.AddMessageAsync(ticketNumber, request);
        return CreatedAtAction(nameof(GetTicket), new { ticketNumber }, message);
    }

    /// <summary>
    /// Return the immutable workflow audit history. Agent/Admin only.
    /// </summary>
    [HttpGet("{ticketNumber}/history")]
    [Authorize(Roles = "SupportAgent,Admin")]
    [ProducesResponseType(typeof(List<Application.DTOs.History.HistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(string ticketNumber)
    {
        var history = await _ticketService.GetHistoryAsync(ticketNumber);
        return Ok(history);
    }
}
