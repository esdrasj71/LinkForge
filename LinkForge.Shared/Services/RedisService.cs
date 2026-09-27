using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace LinkForge.Shared.Services;
public class RedisService
{
    private readonly IConnectionMultiplexer _redis;
    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public IDatabase GetDatabase() => _redis.GetDatabase();
}