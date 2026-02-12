namespace MsgPulse.Api.Providers;

/// <summary>
/// 预设的厂商类型枚举
/// </summary>
public enum ProviderType
{
    /// <summary>
    /// 阿里云
    /// </summary>
    AliyunSms = 1,

    /// <summary>
    /// 腾讯云
    /// </summary>
    TencentSms = 2,

    /// <summary>
    /// Azure通信服务
    /// </summary>
    AzureCommunication = 3,

    /// <summary>
    /// AWS SNS/SES
    /// </summary>
    AwsSnsses = 4,

    /// <summary>
    /// Google Firebase
    /// </summary>
    GoogleFirebase = 5,

    /// <summary>
    /// Apple APNs
    /// </summary>
    AppleApns = 6,

    /// <summary>
    /// 极光推送
    /// </summary>
    JpushProvider = 7,

    /// <summary>
    /// SendGrid (邮件)
    /// </summary>
    SendGrid = 8,

    /// <summary>
    /// Mailgun (邮件)
    /// </summary>
    Mailgun = 9,

    /// <summary>
    /// 网易云信
    /// </summary>
    NetEaseYunxin = 10,

    /// <summary>
    /// 自定义SMTP (邮件)
    /// </summary>
    CustomSmtp = 11
}

/// <summary>
/// 消息渠道类型
/// </summary>
public enum MessageChannel
{
    /// <summary>
    /// 短信
    /// </summary>
    SMS = 1,

    /// <summary>
    /// 邮件
    /// </summary>
    Email = 2,

    /// <summary>
    /// APP推送
    /// </summary>
    AppPush = 3
}
