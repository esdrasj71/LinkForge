using LinkForge.Api.Models;
using LinkForge.Shared.Data;
using LinkForge.Shared.Models;
using LinkForge.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace LinkForge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LinksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ShortCodeGenerator _codeGenerator;

    public LinksController(AppDbContext context)
    {
        _context = context;
        _codeGenerator = new ShortCodeGenerator();
    }

    [HttpPost]
    public async Task<ActionResult<Link>> CreateLink(CreateLinkRequest request)
    {
        if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return BadRequest(new { error = "invalid_url", message = "OriginalUrl must be a valid http or https URL." });
        }

        var shortCode = request.CustomCode ?? _codeGenerator.Generate();

        // Ensure uniqueness
        var exists = await _context.Links.AnyAsync(l => l.ShortCode == shortCode);
        if (exists)
        {
            if (request.CustomCode != null)
                return Conflict(new { error = "code_taken", message = $"Short code '{shortCode}' is already in use." });

            // Collision on generated code — extremely unlikely, retry once
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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Link>> GetLink(int id)
    {
        var link = await _context.Links.FindAsync(id);
        if (link == null || link.IsDeleted) return NotFound();
        return link;
    }

}