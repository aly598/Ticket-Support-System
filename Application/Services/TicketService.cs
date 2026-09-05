using Application.DTOs.Common;
using Application.DTOs.Dashboard;
using Application.DTOs.History;
using Application.DTOs.Messages;
using Application.DTOs.Tickets;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Services;

public class TicketService : ITicketService
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public TicketService(IAppDbContext context, ICurrentUserService currentUser, TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<TicketResponse> CreateTicketAsync(CreateTicketRequest request)
    {
        if (!_currentUser.IsCustomer)
            throw new ForbiddenException("Only customers can create tickets.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Generate unique ticket number
        var lastTicketNumber = _context.Tickets
            .OrderByDescending(t => t.Id)
            .Select(t => t.Id)
            .FirstOrDefault();

        var nextNumber = lastTicketNumber + 1;
        var ticketNumber = $"TCK-{nextNumber:D6}";

        var ticket = new Ticket
        {
            TicketNumber = ticketNumber,
            CreatedByUserId = _currentUser.UserId,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Status = TicketStatus.Open,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.AddTicket(ticket);

        var history = new TicketHistory
        {
            Ticket = ticket,
            ActorUserId = _currentUser.UserId,
            EventType = EventType.Created,
            FromStatus = null,
            ToStatus = TicketStatus.Open,
            CreatedAtUtc = now
        };

        _context.AddHistory(history);
        await _context.SaveChangesAsync();

        return await MapToResponseAsync(ticket);
    }

    public async Task<TicketResponse> GetTicketAsync(string ticketNumber)
    {
        var ticket = await FindTicketWithAccessCheckAsync(ticketNumber);

        var response = await MapToResponseAsync(ticket);

        // Load messages — filter internal notes for customers
        IQueryable<TicketMessage> messagesQuery = _context.TicketMessages
            .Where(m => m.TicketId == ticket.Id)
            .OrderBy(m => m.CreatedAtUtc);

        if (_currentUser.IsCustomer)
        {
            messagesQuery = messagesQuery.Where(m => !m.IsInternal);
        }

        var messages = messagesQuery.ToList();
        response.Messages = messages.Select(m => MapToMessageResponse(m)).ToList();
        return response;
    }

    public async Task<PagedResult<TicketResponse>> ListTicketsAsync(TicketQueryParameters query)
    {
        var ticketsQuery = _context.Tickets.AsQueryable();

        // Customers see only their own tickets
        if (_currentUser.IsCustomer)
        {
            ticketsQuery = ticketsQuery.Where(t => t.CreatedByUserId == _currentUser.UserId);
        }

        if (query.Status.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.Status == query.Status.Value);

        if (query.Priority.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.Priority == query.Priority.Value);

        var totalCount = ticketsQuery.Count();

        var tickets = ticketsQuery
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var items = new List<TicketResponse>();
        foreach (var t in tickets)
        {
            items.Add(await MapToResponseAsync(t));
        }

        return new PagedResult<TicketResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TicketResponse> ClaimTicketAsync(string ticketNumber)
    {
        if (!_currentUser.IsStaff)
            throw new ForbiddenException("Only agents and admins can claim tickets.");

        var ticket = _context.Tickets
            .FirstOrDefault(t => t.TicketNumber == ticketNumber);

        if (ticket == null)
            throw new TicketNotFoundException();

        // Idempotent claim: same agent claiming again
        if (ticket.AssignedAgentUserId == _currentUser.UserId && ticket.Status == TicketStatus.InProgress)
            return await MapToResponseAsync(ticket);

        // Already claimed by another agent
        if (ticket.AssignedAgentUserId != null && ticket.AssignedAgentUserId != _currentUser.UserId)
            throw new TicketAlreadyClaimedException(ticketNumber);

        // Must be Open to claim
        if (ticket.Status != TicketStatus.Open)
            throw new InvalidTransitionException($"Only Open tickets can be claimed. Current status: {ticket.Status}.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        ticket.AssignedAgentUserId = _currentUser.UserId;
        ticket.Status = TicketStatus.InProgress;
        ticket.UpdatedAtUtc = now;

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            ActorUserId = _currentUser.UserId,
            EventType = EventType.Claimed,
            FromStatus = TicketStatus.Open,
            ToStatus = TicketStatus.InProgress,
            CreatedAtUtc = now
        };

        _context.AddHistory(history);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            throw new TicketAlreadyClaimedException(ticketNumber);
        }

        return await MapToResponseAsync(ticket);
    }

    public async Task<TicketResponse> ResolveTicketAsync(string ticketNumber, ResolveTicketRequest request)
    {
        var ticket = _context.Tickets
            .FirstOrDefault(t => t.TicketNumber == ticketNumber);

        if (ticket == null)
            throw new TicketNotFoundException();

        // Only the assigned agent or Admin
        if (!_currentUser.IsAdmin && ticket.AssignedAgentUserId != _currentUser.UserId)
            throw new ForbiddenException("Only the assigned agent or an admin can resolve this ticket.");

        if (ticket.Status != TicketStatus.InProgress)
            throw new InvalidTransitionException($"Only InProgress tickets can be resolved. Current status: {ticket.Status}.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        ticket.Status = TicketStatus.Resolved;
        ticket.ResolvedAtUtc = now;
        ticket.UpdatedAtUtc = now;

        var message = new TicketMessage
        {
            TicketId = ticket.Id,
            AuthorUserId = _currentUser.UserId,
            Body = request.ResolutionMessage,
            IsInternal = false,
            CreatedAtUtc = now
        };

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            ActorUserId = _currentUser.UserId,
            EventType = EventType.Resolved,
            FromStatus = TicketStatus.InProgress,
            ToStatus = TicketStatus.Resolved,
            CreatedAtUtc = now
        };

        _context.AddMessage(message);
        _context.AddHistory(history);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            throw new ConcurrencyConflictException();
        }

        return await MapToResponseAsync(ticket);
    }

    public async Task<TicketResponse> ReopenTicketAsync(string ticketNumber)
    {
        var ticket = await FindTicketWithAccessCheckAsync(ticketNumber);

        if (!_currentUser.IsCustomer || ticket.CreatedByUserId != _currentUser.UserId)
            throw new ForbiddenException("Only the ticket owner can reopen it.");

        if (ticket.Status != TicketStatus.Resolved)
            throw new InvalidTransitionException($"Only Resolved tickets can be reopened. Current status: {ticket.Status}.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (ticket.ResolvedAtUtc.HasValue && (now - ticket.ResolvedAtUtc.Value).TotalHours > 48)
            throw new ReopenWindowExpiredException();

        ticket.Status = TicketStatus.InProgress;
        ticket.ResolvedAtUtc = null;
        ticket.UpdatedAtUtc = now;

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            ActorUserId = _currentUser.UserId,
            EventType = EventType.Reopened,
            FromStatus = TicketStatus.Resolved,
            ToStatus = TicketStatus.InProgress,
            CreatedAtUtc = now
        };

        _context.AddHistory(history);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            throw new ConcurrencyConflictException();
        }

        return await MapToResponseAsync(ticket);
    }

    public async Task<TicketResponse> CloseTicketAsync(string ticketNumber)
    {
        var ticket = await FindTicketWithAccessCheckAsync(ticketNumber);

        bool isOwner = _currentUser.IsCustomer && ticket.CreatedByUserId == _currentUser.UserId;
        if (!isOwner && !_currentUser.IsAdmin)
            throw new ForbiddenException("Only the ticket owner or an admin can close this ticket.");

        if (ticket.Status != TicketStatus.Resolved)
            throw new InvalidTransitionException($"Only Resolved tickets can be closed. Current status: {ticket.Status}.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAtUtc = now;
        ticket.UpdatedAtUtc = now;

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            ActorUserId = _currentUser.UserId,
            EventType = EventType.Closed,
            FromStatus = TicketStatus.Resolved,
            ToStatus = TicketStatus.Closed,
            CreatedAtUtc = now
        };

        _context.AddHistory(history);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            throw new ConcurrencyConflictException();
        }

        return await MapToResponseAsync(ticket);
    }

    public async Task<MessageResponse> AddMessageAsync(string ticketNumber, AddMessageRequest request)
    {
        var ticket = await FindTicketWithAccessCheckAsync(ticketNumber);

        if (ticket.Status == TicketStatus.Closed)
            throw new TicketClosedException();

        // Only staff can create internal notes; silently downgrade for customers
        if (request.IsInternal && !_currentUser.IsStaff)
            request.IsInternal = false;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var message = new TicketMessage
        {
            TicketId = ticket.Id,
            AuthorUserId = _currentUser.UserId,
            Body = request.Body,
            IsInternal = request.IsInternal,
            CreatedAtUtc = now
        };

        _context.AddMessage(message);
        await _context.SaveChangesAsync();

        return MapToMessageResponse(message);
    }

    public async Task<List<HistoryResponse>> GetHistoryAsync(string ticketNumber)
    {
        if (!_currentUser.IsStaff)
            throw new ForbiddenException("Only agents and admins can view ticket history.");

        var ticket = _context.Tickets
            .FirstOrDefault(t => t.TicketNumber == ticketNumber);

        if (ticket == null)
            throw new TicketNotFoundException();

        var history = _context.TicketHistories
            .Where(h => h.TicketId == ticket.Id)
            .OrderBy(h => h.CreatedAtUtc)
            .ToList();

        return history.Select(h => new HistoryResponse
        {
            Id = h.Id,
            ActorEmail = h.Actor?.Email ?? string.Empty,
            EventType = h.EventType,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            CreatedAtUtc = h.CreatedAtUtc
        }).ToList();
    }

    public async Task<DashboardViewModel> GetDashboardAsync(TicketStatus? statusFilter, TicketPriority? priorityFilter)
    {
        var allTickets = _context.Tickets;

        var viewModel = new DashboardViewModel
        {
            TotalTickets = allTickets.Count(),
            OpenTickets = allTickets.Count(t => t.Status == TicketStatus.Open),
            InProgressTickets = allTickets.Count(t => t.Status == TicketStatus.InProgress),
            ResolvedTickets = allTickets.Count(t => t.Status == TicketStatus.Resolved),
            ClosedTickets = allTickets.Count(t => t.Status == TicketStatus.Closed),
            FilterStatus = statusFilter,
            FilterPriority = priorityFilter
        };

        var filteredQuery = allTickets.AsQueryable();
        if (statusFilter.HasValue)
            filteredQuery = filteredQuery.Where(t => t.Status == statusFilter.Value);
        if (priorityFilter.HasValue)
            filteredQuery = filteredQuery.Where(t => t.Priority == priorityFilter.Value);

        var tickets = filteredQuery
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Take(100)
            .ToList();

        viewModel.Tickets = tickets.Select(t => new DashboardTicketRow
        {
            TicketNumber = t.TicketNumber,
            Title = t.Title,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString(),
            CreatedBy = t.CreatedBy?.Email ?? string.Empty,
            AssignedAgent = t.AssignedAgent?.Email,
            CreatedAtUtc = t.CreatedAtUtc,
            UpdatedAtUtc = t.UpdatedAtUtc
        }).ToList();

        return viewModel;
    }

    // === Private helpers ===

    private async Task<Ticket> FindTicketWithAccessCheckAsync(string ticketNumber)
    {
        var ticket = _context.Tickets
            .FirstOrDefault(t => t.TicketNumber == ticketNumber);

        if (ticket == null)
            throw new TicketNotFoundException();

        // Ownership privacy: customer sees 404 for other customers' tickets
        if (_currentUser.IsCustomer && ticket.CreatedByUserId != _currentUser.UserId)
            throw new TicketNotFoundException();

        return ticket;
    }

    private async Task<TicketResponse> MapToResponseAsync(Ticket ticket)
    {
        // Get user emails for response
        string createdByEmail = string.Empty;
        string? assignedAgentEmail = null;

        if (ticket.CreatedBy != null)
        {
            createdByEmail = ticket.CreatedBy.Email ?? string.Empty;
        }
        else if (ticket.CreatedByUserId == _currentUser.UserId)
        {
            createdByEmail = _currentUser.Email;
        }

        if (ticket.AssignedAgent != null)
        {
            assignedAgentEmail = ticket.AssignedAgent.Email;
        }
        else if (ticket.AssignedAgentUserId == _currentUser.UserId)
        {
            assignedAgentEmail = _currentUser.Email;
        }

        return new TicketResponse
        {
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CreatedBy = createdByEmail,
            AssignedAgent = assignedAgentEmail,
            CreatedAtUtc = ticket.CreatedAtUtc,
            UpdatedAtUtc = ticket.UpdatedAtUtc,
            ResolvedAtUtc = ticket.ResolvedAtUtc,
            ClosedAtUtc = ticket.ClosedAtUtc,
            Version = ticket.Version != null ? Convert.ToBase64String(ticket.Version) : string.Empty
        };
    }

    private static MessageResponse MapToMessageResponse(TicketMessage message)
    {
        return new MessageResponse
        {
            Id = message.Id,
            AuthorEmail = message.Author?.Email ?? string.Empty,
            AuthorDisplayName = message.Author?.DisplayName ?? string.Empty,
            Body = message.Body,
            IsInternal = message.IsInternal,
            CreatedAtUtc = message.CreatedAtUtc
        };
    }

    private static bool IsConcurrencyException(Exception ex)
    {
        // Check for DbUpdateConcurrencyException without directly referencing EF Core
        return ex.GetType().Name == "DbUpdateConcurrencyException"
            || ex.InnerException?.GetType().Name == "DbUpdateConcurrencyException";
    }
}
