namespace MsgPulse.Api.Models;

public class Manufacturer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
    public required string SupportedChannels { get; set; }
    public string? SmsConfig { get; set; }
    public string? EmailConfig { get; set; }
    public string? AppPushConfig { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
