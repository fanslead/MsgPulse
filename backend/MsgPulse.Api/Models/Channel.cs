using MsgPulse.Api.Providers;

namespace MsgPulse.Api.Models;

/// <summary>
/// 渠道配置 - 用户自定义的消息发送渠道
/// </summary>
public class Channel
{
    /// <summary>
    /// 主键（自增）
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 渠道名称（用户自定义）
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 渠道编码（唯一）
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// 渠道类型（对应ProviderType，作为模板）
    /// </summary>
    public ProviderType ChannelType { get; set; }

    /// <summary>
    /// 渠道描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 支持的消息渠道（SMS/Email/AppPush）
    /// </summary>
    public required string SupportedChannels { get; set; }

    /// <summary>
    /// 渠道配置（JSON格式，包含AccessKey等敏感信息）
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
