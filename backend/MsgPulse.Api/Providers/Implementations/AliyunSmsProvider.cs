using System.Text.Json;
using MsgPulse.Api.Providers.Models;

namespace MsgPulse.Api.Providers.Implementations;

/// <summary>
/// 阿里云短信配置
/// </summary>
public class AliyunSmsConfig
{
    public string? AccessKeyId { get; set; }
    public string? AccessKeySecret { get; set; }
    public string? RegionId { get; set; }
    public string? SignName { get; set; }
}

/// <summary>
/// 阿里云短信厂商实现
/// </summary>
public class AliyunSmsProvider : BaseMessageProvider
{
    public override ProviderType ProviderType => ProviderType.AliyunSms;
    public override MessageChannel[] SupportedChannels => new[] { MessageChannel.SMS };

    private AliyunSmsConfig? _config;

    public override void Initialize(string? configuration)
    {
        base.Initialize(configuration);
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            _config = JsonSerializer.Deserialize<AliyunSmsConfig>(configuration);
        }
    }

    public override async Task<ProviderResult> SendSmsAsync(SmsRequest request, CancellationToken cancellationToken = default)
    {
        if (_config == null || string.IsNullOrWhiteSpace(_config.AccessKeyId))
        {
            return ProviderResult.Failure("阿里云短信未配置或配置无效");
        }

        try
        {
            // TODO: 集成阿里云SDK
            // 这里是示例代码，实际应该调用阿里云SDK
            await Task.Delay(100, cancellationToken); // 模拟API调用

            // 模拟成功响应
            return ProviderResult.Success(
                messageId: $"aliyun-{Guid.NewGuid():N}",
                rawResponse: "阿里云短信发送成功（示例）"
            );
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"阿里云短信发送失败: {ex.Message}");
        }
    }

    public override async Task<ProviderResult> TestConnectionAsync(MessageChannel channel, CancellationToken cancellationToken = default)
    {
        if (channel != MessageChannel.SMS)
        {
            return ProviderResult.Failure("阿里云仅支持短信渠道");
        }

        if (_config == null || string.IsNullOrWhiteSpace(_config.AccessKeyId))
        {
            return ProviderResult.Failure("配置信息不完整");
        }

        try
        {
            // TODO: 实际应该调用阿里云的测试接口
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
        if (_config == null || string.IsNullOrWhiteSpace(_config.AccessKeyId))
        {
            return new TemplateSyncResult
            {
                IsSuccess = false,
                ErrorMessage = "配置信息不完整"
            };
        }

        try
        {
            // TODO: 调用阿里云QuerySmsTemplate接口获取模板列表
            await Task.Delay(100, cancellationToken);

            // 模拟返回数据
            return new TemplateSyncResult
            {
                IsSuccess = true,
                Templates = new List<SyncedTemplate>
                {
                    new SyncedTemplate
                    {
                        Code = "SMS_123456789",
                        Name = "验证码模板",
                        Content = "您的验证码是${code},5分钟内有效",
                        Status = "已审核"
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
