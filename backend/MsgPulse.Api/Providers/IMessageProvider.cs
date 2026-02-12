using MsgPulse.Api.Providers.Models;

namespace MsgPulse.Api.Providers;

/// <summary>
/// 消息厂商接口抽象层
/// </summary>
public interface IMessageProvider
{
    /// <summary>
    /// 厂商类型
    /// </summary>
    ProviderType ProviderType { get; }

    /// <summary>
    /// 支持的渠道
    /// </summary>
    MessageChannel[] SupportedChannels { get; }

    /// <summary>
    /// 初始化配置
    /// </summary>
    void Initialize(string? configuration);

    /// <summary>
    /// 发送短信
    /// </summary>
    Task<ProviderResult> SendSmsAsync(SmsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送邮件
    /// </summary>
    Task<ProviderResult> SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送APP推送
    /// </summary>
    Task<ProviderResult> SendPushAsync(AppPushRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试连接
    /// </summary>
    Task<ProviderResult> TestConnectionAsync(MessageChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步短信模板（从厂商侧拉取）
    /// </summary>
    Task<TemplateSyncResult> SyncSmsTemplatesAsync(CancellationToken cancellationToken = default);
}
