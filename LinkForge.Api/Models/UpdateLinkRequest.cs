namespace LinkForge.Api.Models;

public class UpdateLinkRequest
{
    public string? OriginalUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
}