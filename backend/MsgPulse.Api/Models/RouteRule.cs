namespace MsgPulse.Api.Models;

public class RouteRule
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string MessageType { get; set; }
    public string? MatchConditions { get; set; }
    public int TargetManufacturerId { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Manufacturer? TargetManufacturer { get; set; }
}
