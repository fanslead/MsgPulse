namespace MsgPulse.Api.Providers.Models;

/// <summary>
/// 厂商接口调用结果
/// </summary>
public class ProviderResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 消息ID（厂商返回）
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 厂商原始响应
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ProviderResult Success(string messageId, string? rawResponse = null)
    {
        return new ProviderResult
        {
            IsSuccess = true,
            MessageId = messageId,
            RawResponse = rawResponse
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ProviderResult Failure(string errorMessage, string? rawResponse = null)
    {
        return new ProviderResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            RawResponse = rawResponse
        };
    }
}

/// <summary>
/// 短信发送请求
/// </summary>
public class SmsRequest
{
    /// <summary>
    /// 手机号
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// 模板编码
    /// </summary>
    public required string TemplateCode { get; set; }

    /// <summary>
    /// 模板参数（JSON格式）
    /// </summary>
    public string? TemplateParams { get; set; }

    /// <summary>
    /// 签名
    /// </summary>
    public string? SignName { get; set; }
}

/// <summary>
/// 邮件发送请求
/// </summary>
public class EmailRequest
{
    /// <summary>
    /// 收件人邮箱
    /// </summary>
    public required string ToEmail { get; set; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// 邮件内容
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 内容类型（HTML/Text）
    /// </summary>
    public string ContentType { get; set; } = "HTML";

    /// <summary>
    /// 发件人名称
    /// </summary>
    public string? FromName { get; set; }
}

/// <summary>
/// APP推送请求
/// </summary>
public class AppPushRequest
{
    /// <summary>
    /// 设备Token或用户ID
    /// </summary>
    public required string Target { get; set; }

    /// <summary>
    /// 推送标题
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// 推送内容
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 额外数据（JSON格式）
    /// </summary>
    public string? ExtraData { get; set; }

    /// <summary>
    /// 平台类型（iOS/Android）
    /// </summary>
    public string? Platform { get; set; }
}

/// <summary>
/// 模板同步结果
/// </summary>
public class TemplateSyncResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 同步的模板列表
    /// </summary>
    public List<SyncedTemplate> Templates { get; set; } = new();

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 已同步的模板
/// </summary>
public class SyncedTemplate
{
    /// <summary>
    /// 模板编码
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 模板内容
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 审核状态
    /// </summary>
    public string? Status { get; set; }
}
