using Microsoft.EntityFrameworkCore;
using PayLedger.Api.Middleware;
using PayLedger.Application.Interfaces;
using PayLedger.Application.Queries;
using PayLedger.Infrastructure.Data;
using PayLedger.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=payledger.db";

builder.Services.AddDbContext<PayLedgerDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IPayLedgerDbContext>(sp => sp.GetRequiredService<PayLedgerDbContext>());

// Add MediatR (scan Application layer)
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetLedgerSummaryQuery).Assembly));

// Add Domain & Infrastructure Services
builder.Services.AddSingleton<IExchangeRateService, ExchangeRateService>();

// Add Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PayLedger API",
        Version = "v1",
        Description = "Double-Entry Financial Ledger & Multi-Currency Settlement Engine"
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Cache-Lookup", "X-Idempotent-Replay", "X-Idempotency-Key");
    });
});

var app = builder.Build();

// Auto-migrate and seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PayLedgerDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(db);
}

// Pipeline configuration
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PayLedger API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
