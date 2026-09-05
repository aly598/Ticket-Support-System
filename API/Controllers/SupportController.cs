using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// MVC controller for the staff dashboard. Uses the same DI service as the API.
/// </summary>
[Authorize(Roles = "SupportAgent,Admin")]
public class SupportController : Controller
{
    private readonly ITicketService _ticketService;

    public SupportController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet("/support/dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority)
    {
        var viewModel = await _ticketService.GetDashboardAsync(status, priority);
        return View("Dashboard", viewModel);
    }
}
