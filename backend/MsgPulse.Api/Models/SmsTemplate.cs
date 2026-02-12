namespace MsgPulse.Api.Models;

public class SmsTemplate
{
    public int Id { get; set; }
    public int ManufacturerId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string Content { get; set; }
    public string? Variables { get; set; }
    public string? AuditStatus { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Manufacturer? Manufacturer { get; set; }
}
