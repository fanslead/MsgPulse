namespace MsgPulse.Api.Models;

public class MessageRecord
{
    public int Id { get; set; }
    public required string TaskId { get; set; }
    public required string MessageType { get; set; }
    public required string TemplateCode { get; set; }
    public required string Recipient { get; set; }
    public string? Variables { get; set; }
    public int? ManufacturerId { get; set; }
    public int? RouteRuleId { get; set; }
    public required string SendStatus { get; set; }
    public DateTime? SendTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string? ManufacturerResponse { get; set; }
    public string? FailureReason { get; set; }
    public string? CustomTag { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Manufacturer? Manufacturer { get; set; }
    public RouteRule? RouteRule { get; set; }
}
