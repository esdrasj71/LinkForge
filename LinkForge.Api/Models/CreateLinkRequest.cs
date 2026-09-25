namespace LinkForge.Api.Models;

public class CreateLinkRequest
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string? CustomCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
}