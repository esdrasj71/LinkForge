using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using LinkForge.Shared.Data;
using LinkForge.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis singleton
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Redis connection string is missing.");
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddSingleton<RedisService>();
builder.Services.AddSingleton<ClickEventPublisher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }