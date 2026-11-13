using System.Net;
using System.Text.Json;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var errorType = "InternalServerError";
        var message = exception.Message;

        switch (exception)
        {
            case UnbalancedLedgerException:
                statusCode = HttpStatusCode.UnprocessableEntity;
                errorType = "UnbalancedLedgerError";
                break;
            case InsufficientFundsException:
                statusCode = HttpStatusCode.Conflict;
                errorType = "InsufficientFundsError";
                break;
            case InactiveAccountException:
                statusCode = HttpStatusCode.Forbidden;
                errorType = "InactiveAccountError";
                break;
            case CurrencyMismatchException:
                statusCode = HttpStatusCode.BadRequest;
                errorType = "CurrencyMismatchError";
                break;
            case DuplicateIdempotencyException:
                statusCode = HttpStatusCode.Conflict;
                errorType = "DuplicateIdempotencyError";
                break;
            case InvalidAccountOperationException:
                statusCode = HttpStatusCode.BadRequest;
                errorType = "InvalidAccountOperationError";
                break;
            case ConcurrencyConflictException:
                statusCode = HttpStatusCode.Conflict;
                errorType = "ConcurrencyConflictError";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = errorType,
            status = (int)statusCode,
            message,
            timestamp = DateTimeOffset.UtcNow
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
