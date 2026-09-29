using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using LinkForge.Shared.Data;
using LinkForge.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Redis connection string is missing.");
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddSingleton<RedisService>();
builder.Services.AddSingleton<ClickEventPublisher>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://linkforge-web.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

// Root endpoint for the API
app.MapGet("/", () => Results.Json(new
{
    name = "LinkForge API",
    description = "Distributed URL shortener with event-driven analytics",
    endpoints = new[]
    {
        "GET    /api/links",
        "POST   /api/links",
        "GET    /api/links/{id}",
        "PUT    /api/links/{id}",
        "DELETE /api/links/{id}",
        "GET    /api/links/{id}/analytics",
        "GET    /{code}          (redirect)"
    },
    source = "https://github.com/esdrasj71/LinkForge"
}));

app.Run();

public partial class Program { }