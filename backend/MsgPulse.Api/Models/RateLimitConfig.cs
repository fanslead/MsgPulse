namespace MsgPulse.Api.Models;

/// <summary>
/// 速率限制配置
/// </summary>
public class RateLimitConfig
{
    public int Id { get; set; }

    /// <summary>
    /// 厂商ID (null表示全局限制)
    /// </summary>
    public int? ManufacturerId { get; set; }

    /// <summary>
    /// 每秒请求数限制 (0表示不限制)
    /// </summary>
    public int RequestsPerSecond { get; set; } = 0;

    /// <summary>
    /// 每分钟请求数限制 (0表示不限制)
    /// </summary>
    public int RequestsPerMinute { get; set; } = 0;

    /// <summary>
    /// 每小时请求数限制 (0表示不限制)
    /// </summary>
    public int RequestsPerHour { get; set; } = 0;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 关联的厂商
    /// </summary>
    public Manufacturer? Manufacturer { get; set; }
}
