using MsgPulse.Api.Providers.Implementations;

namespace MsgPulse.Api.Providers;

/// <summary>
/// 厂商工厂 - 用于创建和获取厂商实例
/// </summary>
public class ProviderFactory
{
    private readonly Dictionary<ProviderType, IMessageProvider> _providers = new();

    public ProviderFactory()
    {
        // 注册所有预设厂商
        RegisterProvider(new AliyunSmsProvider());
        RegisterProvider(new TencentSmsProvider());
        RegisterProvider(new SendGridProvider());
        RegisterProvider(new JpushProvider());
    }

    /// <summary>
    /// 注册厂商
    /// </summary>
    private void RegisterProvider(IMessageProvider provider)
    {
        _providers[provider.ProviderType] = provider;
    }

    /// <summary>
    /// 获取厂商实例
    /// </summary>
    public IMessageProvider? GetProvider(ProviderType providerType)
    {
        return _providers.GetValueOrDefault(providerType);
    }

    /// <summary>
    /// 获取所有已注册的厂商
    /// </summary>
    public IEnumerable<IMessageProvider> GetAllProviders()
    {
        return _providers.Values;
    }

    /// <summary>
    /// 获取厂商信息列表
    /// </summary>
    public List<ProviderInfo> GetProviderInfos()
    {
        return _providers.Select(p => new ProviderInfo
        {
            ProviderType = p.Key,
            Name = GetProviderName(p.Key),
            Code = p.Key.ToString(),
            SupportedChannels = p.Value.SupportedChannels
        }).ToList();
    }

    /// <summary>
    /// 获取厂商中文名称
    /// </summary>
    private static string GetProviderName(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.AliyunSms => "阿里云短信",
            ProviderType.TencentSms => "腾讯云短信",
            ProviderType.AzureCommunication => "Azure通信服务",
            ProviderType.AwsSnsses => "AWS SNS/SES",
            ProviderType.GoogleFirebase => "Google Firebase",
            ProviderType.AppleApns => "Apple APNs",
            ProviderType.JpushProvider => "极光推送",
            ProviderType.SendGrid => "SendGrid",
            ProviderType.Mailgun => "Mailgun",
            ProviderType.NetEaseYunxin => "网易云信",
            _ => providerType.ToString()
        };
    }
}

/// <summary>
/// 厂商信息
/// </summary>
public class ProviderInfo
{
    public ProviderType ProviderType { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required MessageChannel[] SupportedChannels { get; set; }
}
