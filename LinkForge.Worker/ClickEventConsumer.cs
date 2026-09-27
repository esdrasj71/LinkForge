using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using LinkForge.Shared.Data;
using LinkForge.Shared.Models;
using LinkForge.Shared.Services;
using LinkForge.Shared.Streams;

namespace LinkForge.Worker;

public class ClickEventConsumer : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StreamInitializer _initializer;
    private readonly ILogger<ClickEventConsumer> _logger;

    private const string ConsumerName = "worker-1";
    private static readonly TimeSpan BlockTimeout = TimeSpan.FromSeconds(5);

    public ClickEventConsumer(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        StreamInitializer initializer,
        ILogger<ClickEventConsumer> logger)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _initializer.EnsureGroupExistsAsync();

        var db = _redis.GetDatabase();

        _logger.LogInformation("ClickEventConsumer started. Listening for events.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var entries = await db.StreamReadGroupAsync(
                    Streams.ClickEvents,
                    Streams.ConsumerGroup,
                    ConsumerName,
                    count: 10,
                    noAck: false);

                if (entries.Length == 0)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                foreach (var entry in entries)
                {
                    await ProcessEntryAsync(db, entry, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming click events.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessEntryAsync(IDatabase db, StreamEntry entry, CancellationToken ct)
    {
        var values = entry.Values.ToDictionary(v => v.Name.ToString(), v => v.Value.ToString());

        var linkId = int.Parse(values["linkId"]);
        var referrer = string.IsNullOrEmpty(values["referrer"]) ? null : values["referrer"];
        var userAgent = string.IsNullOrEmpty(values["userAgent"]) ? null : values["userAgent"];
        var country = string.IsNullOrEmpty(values["country"]) ? null : values["country"];
        var clickedAt = DateTime.Parse(values["clickedAt"]).ToUniversalTime();

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var clickEvent = new ClickEvent
        {
            LinkId = linkId,
            Referrer = referrer,
            UserAgent = userAgent,
            Country = country,
            ClickedAt = clickedAt
        };
        context.ClickEvents.Add(clickEvent);

        await context.Links
            .Where(l => l.Id == linkId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.TotalClicks, l => l.TotalClicks + 1), ct);

        await context.SaveChangesAsync(ct);

        // Acknowledge the event so it isn't re-processed
        await db.StreamAcknowledgeAsync(Streams.ClickEvents, Streams.ConsumerGroup, entry.Id);

        _logger.LogInformation("Processed click event {EntryId} for link {LinkId}", entry.Id, linkId);
    }
}