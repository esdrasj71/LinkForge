using LinkForge.Worker;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using LinkForge.Shared.Data;
using LinkForge.Shared.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Redis connection string is missing.");
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddSingleton<StreamInitializer>();
builder.Services.AddHostedService<ClickEventConsumer>();

var host = builder.Build();
host.Run();