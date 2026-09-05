using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

/// <summary>
/// Custom WebApplicationFactory that creates an isolated SQL Server database per test class.
/// Uses a unique database name to ensure test isolation.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName;
    private readonly TimeProvider? _timeProvider;

    public CustomWebApplicationFactory(string? dbName = null, TimeProvider? timeProvider = null)
    {
        _dbName = dbName ?? $"TicketSupportSystem_Test_{Guid.NewGuid():N}";
        _timeProvider = timeProvider;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    $"Server=(localdb)\\mssqllocaldb;Database={_dbName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"));

            // Replace TimeProvider if provided
            if (_timeProvider != null)
            {
                var timeDescriptors = services.Where(d =>
                    d.ServiceType == typeof(TimeProvider)).ToList();
                foreach (var d in timeDescriptors)
                    services.Remove(d);
                services.AddSingleton(_timeProvider);
            }
        });

        builder.UseEnvironment("Development");
    }

    public async Task CleanupDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
    }
}
