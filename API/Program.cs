using System.Text.Json.Serialization;
using API.Middleware;
using Infrastructure;
using Infrastructure.Seeding;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (DB, Identity, JWT, DI)
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers + JSON enum serialization
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();

// Swagger with Bearer auth
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ticket Support System API",
        Version = "v1",
        Description = "Support ticket workflow API with JWT authentication"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed data
await DataSeeder.SeedAsync(app.Services);

// Middleware pipeline order matters:
// 1. Correlation ID first (so it's available to everything)
// 2. Exception middleware (catches all exceptions)
// 3. Swagger (dev only)
// 4. HTTPS
// 5. Auth
// 6. Controllers

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the Program class accessible for WebApplicationFactory in tests
public partial class Program { }
