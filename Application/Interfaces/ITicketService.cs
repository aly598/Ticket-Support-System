using Application.DTOs.Common;
using Application.DTOs.Dashboard;
using Application.DTOs.History;
using Application.DTOs.Messages;
using Application.DTOs.Tickets;
using Domain.Enums;

namespace Application.Interfaces;

public interface ITicketService
{
    Task<TicketResponse> CreateTicketAsync(CreateTicketRequest request);
    Task<TicketResponse> GetTicketAsync(string ticketNumber);
    Task<PagedResult<TicketResponse>> ListTicketsAsync(TicketQueryParameters query);
    Task<TicketResponse> ClaimTicketAsync(string ticketNumber);
    Task<TicketResponse> ResolveTicketAsync(string ticketNumber, ResolveTicketRequest request);
    Task<TicketResponse> ReopenTicketAsync(string ticketNumber);
    Task<TicketResponse> CloseTicketAsync(string ticketNumber);
    Task<MessageResponse> AddMessageAsync(string ticketNumber, AddMessageRequest request);
    Task<List<HistoryResponse>> GetHistoryAsync(string ticketNumber);
    Task<DashboardViewModel> GetDashboardAsync(TicketStatus? statusFilter, TicketPriority? priorityFilter);
}
