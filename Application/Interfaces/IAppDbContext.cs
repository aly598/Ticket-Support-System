using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Database context abstraction so the Application layer doesn't depend on EF Core directly.
/// Implemented by AppDbContext in Infrastructure.
/// </summary>
public interface IAppDbContext
{
    IQueryable<Ticket> Tickets { get; }
    IQueryable<TicketMessage> TicketMessages { get; }
    IQueryable<TicketHistory> TicketHistories { get; }

    void AddTicket(Ticket ticket);
    void AddMessage(TicketMessage message);
    void AddHistory(TicketHistory history);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
