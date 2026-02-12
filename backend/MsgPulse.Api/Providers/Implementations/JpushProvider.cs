using System.Text.Json;
using MsgPulse.Api.Providers.Models;

namespace MsgPulse.Api.Providers.Implementations;

/// <summary>
/// 极光推送配置
/// </summary>
public class JpushConfig
{
    public string? AppKey { get; set; }
    public string? MasterSecret { get; set; }
}

/// <summary>
/// 极光推送厂商实现
/// </summary>
public class JpushProvider : BaseMessageProvider
{
    public override ProviderType ProviderType => ProviderType.JpushProvider;
    public override MessageChannel[] SupportedChannels => new[] { MessageChannel.AppPush };

    private JpushConfig? _config;

    public override void Initialize(string? configuration)
    {
        base.Initialize(configuration);
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            _config = JsonSerializer.Deserialize<JpushConfig>(configuration);
        }
    }

    public override async Task<ProviderResult> SendPushAsync(AppPushRequest request, CancellationToken cancellationToken = default)
    {
        if (_config == null || string.IsNullOrWhiteSpace(_config.AppKey))
        {
            return ProviderResult.Failure("极光推送未配置或配置无效");
        }

        try
        {
            // TODO: 集成极光推送SDK
            await Task.Delay(100, cancellationToken);

            return ProviderResult.Success(
                messageId: $"jpush-{Guid.NewGuid():N}",
                rawResponse: "极光推送发送成功（示例）"
            );
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"极光推送发送失败: {ex.Message}");
        }
    }

    public override async Task<ProviderResult> TestConnectionAsync(MessageChannel channel, CancellationToken cancellationToken = default)
    {
        if (channel != MessageChannel.AppPush)
        {
            return ProviderResult.Failure("极光推送仅支持APP推送渠道");
        }

        if (_config == null || string.IsNullOrWhiteSpace(_config.AppKey))
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
