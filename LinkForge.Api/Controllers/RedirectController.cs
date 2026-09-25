using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinkForge.Shared.Data;

namespace LinkForge.Api.Controllers;

[ApiController]
public class RedirectController : ControllerBase
{
    private readonly AppDbContext _context;

    public RedirectController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("/{code}")]
    public async Task<IActionResult> RedirectToOriginal(string code)
    {
        var link = await _context.Links
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ShortCode == code && !l.IsDeleted);

        if (link == null)
        {
            return NotFound(new { error = "not_found", message = $"No link found for code '{code}'." });
        }

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTime.UtcNow)
        {
            return StatusCode(410, new { error = "expired", message = "This link has expired." });
        }

        return Redirect(link.OriginalUrl);
    }
}