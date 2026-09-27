using LinkForge.Shared.Streams;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkForge.Shared.Services;

public class StreamInitializer
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StreamInitializer> _logger;

    public StreamInitializer(IConnectionMultiplexer redis, ILogger<StreamInitializer> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task EnsureGroupExistsAsync()
    {
        var db = _redis.GetDatabase();

        try
        {
            await db.StreamCreateConsumerGroupAsync(
                LinkForge.Shared.Streams.Streams.ClickEvents,
                LinkForge.Shared.Streams.Streams.ConsumerGroup,
                StreamPosition.NewMessages,
                createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            _logger.LogInformation("Consumer group {Group} already exists.", LinkForge.Shared.Streams.Streams.ConsumerGroup);
        }

    }
}