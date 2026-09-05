using System.Net;
using System.Text.Json;
using Application.DTOs.Common;
using Domain.Exceptions;

namespace API.Middleware;

/// <summary>
/// Global exception middleware. Catches all unhandled exceptions and returns
/// the standard error contract { code, message, correlationId }.
/// Never exposes stack traces, SQL details, or secrets.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (TicketDomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception: {Code} - {Message}", ex.Code, ex.Message);
            await WriteErrorResponse(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(context, (int)HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string code, string message)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        var error = new ErrorResponse
        {
            Code = code,
            Message = message,
            CorrelationId = correlationId
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(error, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
