using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinkForge.Api.Models;
using LinkForge.Shared.Data;
using LinkForge.Shared.Models;
using LinkForge.Shared.Services;

namespace LinkForge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LinksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ShortCodeGenerator _codeGenerator;
    private readonly RedisService _redis;

    public LinksController(AppDbContext context, RedisService redis)
    {
        _context = context;
        _codeGenerator = new ShortCodeGenerator();
        _redis = redis;
    }

    // Creates a new short link
    [HttpPost]
    public async Task<ActionResult<Link>> CreateLink(CreateLinkRequest request)
    {
        if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return BadRequest(new { error = "invalid_url", message = "OriginalUrl must be a valid http or https URL." });
        }

        var shortCode = request.CustomCode ?? _codeGenerator.Generate();

        var exists = await _context.Links.AnyAsync(l => l.ShortCode == shortCode);
        if (exists)
        {
            if (request.CustomCode != null)
                return Conflict(new { error = "code_taken", message = $"Short code '{shortCode}' is already in use." });

            shortCode = _codeGenerator.Generate();
        }

        var link = new Link
        {
            ShortCode = shortCode,
            OriginalUrl = request.OriginalUrl,
            ExpiresAt = request.ExpiresAt
        };

        _context.Links.Add(link);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLink), new { id = link.Id }, link);
    }

    // All links with pagination
    [HttpGet]
    public async Task<ActionResult> GetLinks([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var total = await _context.Links.CountAsync(l => !l.IsDeleted);

        var links = await _context.Links
            .AsNoTracking()
            .Where(l => !l.IsDeleted)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            items = links
        });
    }

    // Single link
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Link>> GetLink(int id)
    {
        var link = await _context.Links.FindAsync(id);
        if (link == null || link.IsDeleted) return NotFound();
        return link;
    }

    // Update URL or expiration, invalidate cache
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Link>> UpdateLink(int id, UpdateLinkRequest request)
    {
        var link = await _context.Links.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
        if (link == null) return NotFound();

        if (request.OriginalUrl != null)
        {
            if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                return BadRequest(new { error = "invalid_url", message = "OriginalUrl must be a valid http or https URL." });
            }
            link.OriginalUrl = request.OriginalUrl;
        }

        if (request.ExpiresAt.HasValue)
            link.ExpiresAt = request.ExpiresAt;

        await _context.SaveChangesAsync();

        await _redis.GetDatabase().KeyDeleteAsync(CacheKeys.Link(link.ShortCode));

        return Ok(link);
    }

    // Soft delete, invalidate cache
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLink(int id)
    {
        var link = await _context.Links.FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
        if (link == null) return NotFound();

        link.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _redis.GetDatabase().KeyDeleteAsync(CacheKeys.Link(link.ShortCode));

        return NoContent();
    }

    // Aggregated click stats
    [HttpGet("{id:int}/analytics")]
    public async Task<ActionResult> GetAnalytics(int id)
    {
        var link = await _context.Links.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
        if (link == null) return NotFound();

        var totalClicks = link.TotalClicks;

        var clicksByDay = await _context.ClickEvents
            .Where(c => c.LinkId == id)
            .GroupBy(c => c.ClickedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .OrderByDescending(x => x.date)
            .Take(30)
            .ToListAsync();

        var topReferrers = await _context.ClickEvents
            .Where(c => c.LinkId == id && c.Referrer != null && c.Referrer != "")
            .GroupBy(c => c.Referrer)
            .Select(g => new { referrer = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(5)
            .ToListAsync();

        return Ok(new
        {
            linkId = id,
            shortCode = link.ShortCode,
            totalClicks,
            clicksByDay,
            topReferrers
        });
    }
}