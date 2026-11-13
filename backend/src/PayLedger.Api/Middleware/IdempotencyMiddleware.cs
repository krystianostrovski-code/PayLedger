using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Entities;

namespace PayLedger.Api.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    public const string IdempotencyHeader = "Idempotency-Key";

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        // Only enforce idempotency on state-mutating requests (POST, PUT, PATCH)
        if (!HttpMethods.IsPost(context.Request.Method) && !HttpMethods.IsPut(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(IdempotencyHeader, out var keyHeader) || string.IsNullOrWhiteSpace(keyHeader))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = keyHeader.ToString().Trim();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IPayLedgerDbContext>();

        var existingRecord = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == idempotencyKey);

        if (existingRecord != null && !existingRecord.IsExpired())
        {
            context.Response.StatusCode = existingRecord.ResponseStatusCode;
            context.Response.ContentType = "application/json";
            context.Response.Headers["X-Cache-Lookup"] = "HIT";
            context.Response.Headers["X-Idempotent-Replay"] = "true";
            await context.Response.WriteAsync(existingRecord.ResponseBody);
            return;
        }

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBodyText = await new StreamReader(memoryStream).ReadToEndAsync();
            memoryStream.Seek(0, SeekOrigin.Begin);

            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                var record = new IdempotencyRecord(
                    idempotencyKey,
                    context.Request.Path,
                    "SHA256",
                    context.Response.StatusCode,
                    responseBodyText,
                    TimeSpan.FromHours(24)
                );

                dbContext.IdempotencyRecords.Add(record);
                await dbContext.SaveChangesAsync();
            }

            context.Response.Headers["X-Cache-Lookup"] = "MISS";
            context.Response.Headers["X-Idempotency-Key"] = idempotencyKey;

            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
