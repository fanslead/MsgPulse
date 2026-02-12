using MsgPulse.Api.Models;
using MsgPulse.Api.Providers.Models;

namespace MsgPulse.Api.Providers;

/// <summary>
/// 抽象基类厂商实现
/// </summary>
public abstract class BaseMessageProvider : IMessageProvider
{
    public abstract ProviderType ProviderType { get; }
    public abstract MessageChannel[] SupportedChannels { get; }

    /// <summary>
    /// 获取配置Schema (子类必须实现)
    /// </summary>
    public abstract ConfigurationSchema GetConfigurationSchema();

    /// <summary>
    /// 厂商配置（JSON格式）
    /// </summary>
    protected string? Configuration { get; set; }

    public virtual Task<ProviderResult> SendSmsAsync(SmsRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProviderResult.Failure("此厂商不支持短信发送"));
    }

    public virtual Task<ProviderResult> SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProviderResult.Failure("此厂商不支持邮件发送"));
    }

    public virtual Task<ProviderResult> SendPushAsync(AppPushRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProviderResult.Failure("此厂商不支持APP推送"));
    }

    public virtual Task<ProviderResult> TestConnectionAsync(MessageChannel channel, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProviderResult.Failure("测试连接功能未实现"));
    }

    public virtual Task<TemplateSyncResult> SyncSmsTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TemplateSyncResult
        {
            IsSuccess = false,
            ErrorMessage = "模板同步功能未实现"
        });
    }

    /// <summary>
    /// 初始化配置
    /// </summary>
    public virtual void Initialize(string? configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    protected bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(Configuration);
    }
}
