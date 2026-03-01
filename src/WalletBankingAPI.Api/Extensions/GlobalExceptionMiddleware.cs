using System.Net;
using System.Text.Json;
using WalletBankingAPI.Domain.Common;

namespace WalletBankingAPI.Api.Extensions;

/// <summary>
/// Catches unhandled exceptions and returns a consistent JSON error response.
/// PRD: "Error responses should be standardized (GlobalExceptionMiddleware)".
/// Production: never expose stack traces or internal details to clients.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new ApiResponse
        {
            IsSuccessful = false,
            Message = "An error occurred while processing your request.",
            ResponseCode = "INTERNAL_ERROR",
            StatusCode = 500,
            ResponseTime = DateTime.UtcNow
        };

        // In Development you could set response.Message = ex.Message; avoid in Production.
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
