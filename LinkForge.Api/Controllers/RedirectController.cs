using LinkForge.Shared.Data;
using LinkForge.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LinkForge.Api.Controllers;

[ApiController]
public class RedirectController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RedisService _redis;
    private readonly ClickEventPublisher _publisher;
    private readonly ILogger<RedirectController> _logger;

    private const string CacheKeyPrefix = "link:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public RedirectController(
        AppDbContext context,
        RedisService redis,
        ClickEventPublisher publisher,
        ILogger<RedirectController> logger)
    {
        _context = context;
        _redis = redis;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpGet("/{code}")]
    public async Task<IActionResult> RedirectToOriginal(string code)
    {
        var db = _redis.GetDatabase();
        var cacheKey = $"{CacheKeyPrefix}{code}";

        // Try cache
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            _logger.LogInformation("Cache HIT for {Code}", code);
            var cachedLink = JsonSerializer.Deserialize<CachedLink>(cached!);

            if (cachedLink!.IsExpired)
                return StatusCode(410, new { error = "expired", message = "This link has expired." });

            await PublishClickSafeAsync(cachedLink.LinkId);
            return Redirect(cachedLink.OriginalUrl);
        }

        _logger.LogInformation("Cache MISS for {Code}", code);

        // Fall back to database
        var link = await _context.Links
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ShortCode == code && !l.IsDeleted);

        if (link == null)
            return NotFound(new { error = "not_found", message = $"No link found for code '{code}'." });

        // Populate cache — LinkId so cache hits can also publish events
        var toCache = new CachedLink(link.Id, link.OriginalUrl, link.ExpiresAt);

        await db.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(toCache),
            CacheTtl);

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTime.UtcNow)
            return StatusCode(410, new { error = "expired", message = "This link has expired." });

        await PublishClickSafeAsync(link.Id);
        return Redirect(link.OriginalUrl);
    }

    private async Task PublishClickSafeAsync(int linkId)
    {
        try
        {
            var referrer = Request.Headers.Referer.FirstOrDefault();
            var userAgent = Request.Headers.UserAgent.FirstOrDefault();
            await _publisher.PublishAsync(linkId, referrer, userAgent, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish click event for link {LinkId}", linkId);
        }
    }

    private record CachedLink(int LinkId, string OriginalUrl, DateTime? ExpiresAt)
    {
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    }
}