namespace MsgPulse.Api.Models;

public class EmailTemplate
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string Subject { get; set; }
    public required string ContentType { get; set; }
    public required string Content { get; set; }
    public string? Variables { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
