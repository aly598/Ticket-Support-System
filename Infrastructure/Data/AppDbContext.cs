using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ticket> TicketsSet => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessagesSet => Set<TicketMessage>();
    public DbSet<TicketHistory> TicketHistoriesSet => Set<TicketHistory>();

    // IAppDbContext implementation - return IQueryable with eager loading
    IQueryable<Ticket> IAppDbContext.Tickets => TicketsSet
        .Include(t => t.CreatedBy)
        .Include(t => t.AssignedAgent);

    IQueryable<TicketMessage> IAppDbContext.TicketMessages => TicketMessagesSet
        .Include(m => m.Author);

    IQueryable<TicketHistory> IAppDbContext.TicketHistories => TicketHistoriesSet
        .Include(h => h.Actor);

    public void AddTicket(Ticket ticket) => TicketsSet.Add(ticket);
    public void AddMessage(TicketMessage message) => TicketMessagesSet.Add(message);
    public void AddHistory(TicketHistory history) => TicketHistoriesSet.Add(history);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // === ApplicationUser ===
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName)
                .IsRequired()
                .HasMaxLength(100);
        });

        // === Ticket ===
        builder.Entity<Ticket>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.TicketNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(t => t.TicketNumber)
                .IsUnique();

            entity.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(t => t.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(t => t.Priority)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(t => t.CreatedByUserId)
                .IsRequired();

            // SQL Server rowversion concurrency token
            entity.Property(t => t.Version)
                .IsRowVersion();

            entity.HasOne(t => t.CreatedBy)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AssignedAgent)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedAgentUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // === TicketMessage ===
        builder.Entity<TicketMessage>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Body)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(m => m.AuthorUserId)
                .IsRequired();

            entity.HasOne(m => m.Ticket)
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Author)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // === TicketHistory ===
        builder.Entity<TicketHistory>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.ActorUserId)
                .IsRequired();

            entity.Property(h => h.EventType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(h => h.FromStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(h => h.ToStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(h => h.Ticket)
                .WithMany(t => t.History)
                .HasForeignKey(h => h.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.Actor)
                .WithMany(u => u.HistoryEntries)
                .HasForeignKey(h => h.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
