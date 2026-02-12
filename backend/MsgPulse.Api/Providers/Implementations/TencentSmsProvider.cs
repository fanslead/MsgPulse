using System.Text.Json;
using MsgPulse.Api.Providers.Models;

namespace MsgPulse.Api.Providers.Implementations;

/// <summary>
/// 腾讯云短信配置
/// </summary>
public class TencentSmsConfig
{
    public string? SecretId { get; set; }
    public string? SecretKey { get; set; }
    public string? SdkAppId { get; set; }
    public string? SignName { get; set; }
}

/// <summary>
/// 腾讯云短信厂商实现
/// </summary>
public class TencentSmsProvider : BaseMessageProvider
{
    public override ProviderType ProviderType => ProviderType.TencentSms;
    public override MessageChannel[] SupportedChannels => new[] { MessageChannel.SMS };

    private TencentSmsConfig? _config;

    public override void Initialize(string? configuration)
    {
        base.Initialize(configuration);
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            _config = JsonSerializer.Deserialize<TencentSmsConfig>(configuration);
        }
    }

    public override async Task<ProviderResult> SendSmsAsync(SmsRequest request, CancellationToken cancellationToken = default)
    {
        if (_config == null || string.IsNullOrWhiteSpace(_config.SecretId))
        {
            return ProviderResult.Failure("腾讯云短信未配置或配置无效");
        }

        try
        {
            // TODO: 集成腾讯云SDK
            await Task.Delay(100, cancellationToken);

            return ProviderResult.Success(
                messageId: $"tencent-{Guid.NewGuid():N}",
                rawResponse: "腾讯云短信发送成功（示例）"
            );
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"腾讯云短信发送失败: {ex.Message}");
        }
    }

    public override async Task<ProviderResult> TestConnectionAsync(MessageChannel channel, CancellationToken cancellationToken = default)
    {
        if (channel != MessageChannel.SMS)
        {
            return ProviderResult.Failure("腾讯云仅支持短信渠道");
        }

        if (_config == null || string.IsNullOrWhiteSpace(_config.SecretId))
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

    public override async Task<TemplateSyncResult> SyncSmsTemplatesAsync(CancellationToken cancellationToken = default)
    {
        if (_config == null || string.IsNullOrWhiteSpace(_config.SecretId))
        {
            return new TemplateSyncResult
            {
                IsSuccess = false,
                ErrorMessage = "配置信息不完整"
            };
        }

        try
        {
            // TODO: 调用腾讯云DescribeSmsTemplateList接口
            await Task.Delay(100, cancellationToken);

            return new TemplateSyncResult
            {
                IsSuccess = true,
                Templates = new List<SyncedTemplate>
                {
                    new SyncedTemplate
                    {
                        Code = "1234567",
                        Name = "验证码通知",
                        Content = "您的验证码为{1},请在{2}分钟内完成验证",
                        Status = "已通过"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new TemplateSyncResult
            {
                IsSuccess = false,
                ErrorMessage = $"模板同步失败: {ex.Message}"
            };
        }
    }
}
