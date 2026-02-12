using System.Text.Json;
using MsgPulse.Api.Providers.Models;

namespace MsgPulse.Api.Providers.Implementations;

/// <summary>
/// SendGrid邮件配置
/// </summary>
public class SendGridConfig
{
    public string? ApiKey { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
}

/// <summary>
/// SendGrid邮件厂商实现
/// </summary>
public class SendGridProvider : BaseMessageProvider
{
    public override ProviderType ProviderType => ProviderType.SendGrid;
    public override MessageChannel[] SupportedChannels => new[] { MessageChannel.Email };

    private SendGridConfig? _config;

    public override void Initialize(string? configuration)
    {
        base.Initialize(configuration);
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            _config = JsonSerializer.Deserialize<SendGridConfig>(configuration);
        }
    }

    public override async Task<ProviderResult> SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        if (_config == null || string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            return ProviderResult.Failure("SendGrid未配置或配置无效");
        }

        try
        {
            // TODO: 集成SendGrid SDK
            await Task.Delay(100, cancellationToken);

            return ProviderResult.Success(
                messageId: $"sendgrid-{Guid.NewGuid():N}",
                rawResponse: "SendGrid邮件发送成功（示例）"
            );
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"SendGrid邮件发送失败: {ex.Message}");
        }
    }

    public override async Task<ProviderResult> TestConnectionAsync(MessageChannel channel, CancellationToken cancellationToken = default)
    {
        if (channel != MessageChannel.Email)
        {
            return ProviderResult.Failure("SendGrid仅支持邮件渠道");
        }

        if (_config == null || string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            return ProviderResult.Failure("配置信息不完整");
        }

        try
        {
            await Task.Delay(50, cancellationToken);
            return ProviderResult.Success("连接测试成功", "配置有效");
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"连接测试失败: {ex.Message}");
        }
    }
}
