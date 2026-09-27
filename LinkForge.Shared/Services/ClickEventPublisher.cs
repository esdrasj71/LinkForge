using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using LinkForge.Shared.Streams;

namespace LinkForge.Shared.Services;

public class ClickEventPublisher
{
    private readonly IConnectionMultiplexer _redis;

    public ClickEventPublisher(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task PublishAsync(int linkId, string? referrer, string? userAgent, string? country)
    {
        var db = _redis.GetDatabase();
        var entries = new NameValueEntry[]
        {
            new("linkId", linkId),
            new("referrer", referrer ?? ""),
            new("userAgent", userAgent ?? ""),
            new("country", country ?? ""),
            new("clickedAt", DateTime.UtcNow.ToString("O"))
        };

        await db.StreamAddAsync(LinkForge.Shared.Streams.Streams.ClickEvents, entries);
    }
}