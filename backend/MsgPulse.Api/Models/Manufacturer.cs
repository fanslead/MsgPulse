using MsgPulse.Api.Providers;

namespace MsgPulse.Api.Models;

/// <summary>
/// 厂商配置 - 存储预设厂商的配置信息
/// </summary>
public class Manufacturer
{
    /// <summary>
    /// 主键（对应ProviderType枚举值）
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 厂商类型（枚举）
    /// </summary>
    public ProviderType ProviderType { get; set; }

    /// <summary>
    /// 厂商名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 厂商编码
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// 厂商描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 支持的渠道（逗号分隔：SMS,Email,AppPush）
    /// </summary>
    public required string SupportedChannels { get; set; }

    /// <summary>
    /// 厂商配置（JSON格式，包含AccessKey等敏感信息）
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
