using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Seeding;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        // Ensure database is created and migrated
        await context.Database.MigrateAsync();

        // Seed roles
        string[] roles = { "Customer", "SupportAgent", "Admin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed users (deterministic — only if they don't exist)
        var users = new[]
        {
            new { Email = "customer1@demo.local", DisplayName = "Customer One", Role = "Customer", Password = "Demo!Customer1" },
            new { Email = "customer2@demo.local", DisplayName = "Customer Two", Role = "Customer", Password = "Demo!Customer2" },
            new { Email = "agent1@demo.local", DisplayName = "Agent One", Role = "SupportAgent", Password = "Demo!Agent1" },
            new { Email = "agent2@demo.local", DisplayName = "Agent Two", Role = "SupportAgent", Password = "Demo!Agent2" },
            new { Email = "admin@demo.local", DisplayName = "Support Admin", Role = "Admin", Password = "Demo!Admin1" },
        };

        foreach (var userData in users)
        {
            if (await userManager.FindByEmailAsync(userData.Email) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    DisplayName = userData.DisplayName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, userData.Role);
                    logger.LogInformation("Seeded user {Email} with role {Role}", userData.Email, userData.Role);
                }
                else
                {
                    logger.LogError("Failed to seed user {Email}: {Errors}", userData.Email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // Seed tickets (only if no tickets exist)
        if (!await context.TicketsSet.AnyAsync())
        {
            var customer1 = await userManager.FindByEmailAsync("customer1@demo.local");
            var customer2 = await userManager.FindByEmailAsync("customer2@demo.local");
            var agent1 = await userManager.FindByEmailAsync("agent1@demo.local");
            var agent2 = await userManager.FindByEmailAsync("agent2@demo.local");

            if (customer1 == null || customer2 == null || agent1 == null || agent2 == null)
            {
                logger.LogError("Cannot seed tickets — required users not found.");
                return;
            }

            var now = DateTime.UtcNow;

            // TCK-000001: customer1, Open, unassigned
            var ticket1 = new Ticket
            {
                TicketNumber = "TCK-000001",
                CreatedByUserId = customer1.Id,
                Title = "Cannot access email on mobile device",
                Description = "My email app stopped syncing on my phone since yesterday morning.",
                Priority = TicketPriority.High,
                Status = TicketStatus.Open,
                CreatedAtUtc = now.AddHours(-24),
                UpdatedAtUtc = now.AddHours(-24)
            };

            // TCK-000002: customer2, InProgress, assigned to agent1
            var ticket2 = new Ticket
            {
                TicketNumber = "TCK-000002",
                CreatedByUserId = customer2.Id,
                AssignedAgentUserId = agent1.Id,
                Title = "Printer not responding to print jobs",
                Description = "The shared office printer is not responding to any print jobs sent from my workstation.",
                Priority = TicketPriority.Medium,
                Status = TicketStatus.InProgress,
                CreatedAtUtc = now.AddHours(-48),
                UpdatedAtUtc = now.AddHours(-12)
            };

            // TCK-000003: customer1, Resolved, assigned to agent2
            var ticket3 = new Ticket
            {
                TicketNumber = "TCK-000003",
                CreatedByUserId = customer1.Id,
                AssignedAgentUserId = agent2.Id,
                Title = "Software license expired warning",
                Description = "I am getting a license expired warning when opening the design application.",
                Priority = TicketPriority.Low,
                Status = TicketStatus.Resolved,
                CreatedAtUtc = now.AddHours(-72),
                UpdatedAtUtc = now.AddHours(-2),
                ResolvedAtUtc = now.AddHours(-2)
            };

            context.TicketsSet.AddRange(ticket1, ticket2, ticket3);
            await context.SaveChangesAsync();

            // Add history rows for seeded tickets
            var historyEntries = new List<TicketHistory>
            {
                // Ticket 1: Created
                new()
                {
                    TicketId = ticket1.Id,
                    ActorUserId = customer1.Id,
                    EventType = EventType.Created,
                    FromStatus = null,
                    ToStatus = TicketStatus.Open,
                    CreatedAtUtc = ticket1.CreatedAtUtc
                },
                // Ticket 2: Created, then Claimed
                new()
                {
                    TicketId = ticket2.Id,
                    ActorUserId = customer2.Id,
                    EventType = EventType.Created,
                    FromStatus = null,
                    ToStatus = TicketStatus.Open,
                    CreatedAtUtc = ticket2.CreatedAtUtc
                },
                new()
                {
                    TicketId = ticket2.Id,
                    ActorUserId = agent1.Id,
                    EventType = EventType.Claimed,
                    FromStatus = TicketStatus.Open,
                    ToStatus = TicketStatus.InProgress,
                    CreatedAtUtc = ticket2.UpdatedAtUtc
                },
                // Ticket 3: Created, Claimed, Resolved
                new()
                {
                    TicketId = ticket3.Id,
                    ActorUserId = customer1.Id,
                    EventType = EventType.Created,
                    FromStatus = null,
                    ToStatus = TicketStatus.Open,
                    CreatedAtUtc = ticket3.CreatedAtUtc
                },
                new()
                {
                    TicketId = ticket3.Id,
                    ActorUserId = agent2.Id,
                    EventType = EventType.Claimed,
                    FromStatus = TicketStatus.Open,
                    ToStatus = TicketStatus.InProgress,
                    CreatedAtUtc = ticket3.CreatedAtUtc.AddHours(12)
                },
                new()
                {
                    TicketId = ticket3.Id,
                    ActorUserId = agent2.Id,
                    EventType = EventType.Resolved,
                    FromStatus = TicketStatus.InProgress,
                    ToStatus = TicketStatus.Resolved,
                    CreatedAtUtc = ticket3.ResolvedAtUtc!.Value
                }
            };

            context.TicketHistoriesSet.AddRange(historyEntries);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} tickets with history.", 3);
        }
    }
}
