using System.Text.Json;
using Azure;
using Azure.Communication.Email;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers.Models;

namespace MsgPulse.Api.Providers.Implementations;

/// <summary>
/// Azure通信服务邮件配置
/// </summary>
public class AzureEmailConfig
{
    public string? ConnectionString { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
}

/// <summary>
/// Azure通信服务邮件提供商实现
/// </summary>
public class AzureEmailProvider : BaseMessageProvider
{
    public override ProviderType ProviderType => ProviderType.AzureCommunication;
    public override MessageChannel[] SupportedChannels => new[] { MessageChannel.Email };

    private AzureEmailConfig? _config;
    private EmailClient? _client;

    public override ConfigurationSchema GetConfigurationSchema()
    {
        return new ConfigurationSchema
        {
            ProviderName = "Azure通信服务",
            Description = "Azure Communication Services提供企业级电子邮件发送能力，支持高可用性和全球覆盖",
            DocumentationUrl = "https://learn.microsoft.com/zh-cn/azure/communication-services/quickstarts/email/send-email",
            Fields = new List<ConfigurationField>
            {
                new ConfigurationField
                {
                    Name = "connectionString",
                    Label = "连接字符串",
                    Type = "password",
                    Required = true,
                    Placeholder = "endpoint=https://...;accesskey=...",
                    HelpText = "Azure通信服务资源的连接字符串，可在Azure门户中的资源页面找到",
                    ValidationMessage = "请输入有效的Azure连接字符串",
                    IsSensitive = true,
                    Group = "认证信息",
                    Order = 1
                },
                new ConfigurationField
                {
                    Name = "fromEmail",
                    Label = "发件人邮箱",
                    Type = "text",
                    Required = true,
                    Placeholder = "donotreply@yourdomain.com",
                    HelpText = "已验证的发件人邮箱地址，需要在Azure通信服务中预先配置",
                    ValidationPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
                    ValidationMessage = "请输入有效的邮箱地址",
                    IsSensitive = false,
                    Group = "发件人配置",
                    Order = 2
                },
                new ConfigurationField
                {
                    Name = "fromName",
                    Label = "发件人名称",
                    Type = "text",
                    Required = false,
                    Placeholder = "您的应用名称",
                    HelpText = "邮件发件人显示名称，留空则只显示邮箱地址",
                    IsSensitive = false,
                    Group = "发件人配置",
                    Order = 3
                }
            },
            Example = @"{
  ""connectionString"": ""endpoint=https://your-resource.communication.azure.com/;accesskey=your-access-key"",
  ""fromEmail"": ""donotreply@yourdomain.com"",
  ""fromName"": ""您的应用""
}"
        };
    }

    public override void Initialize(string? configuration)
    {
        base.Initialize(configuration);
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            _config = JsonSerializer.Deserialize<AzureEmailConfig>(configuration);

            if (_config != null && !string.IsNullOrWhiteSpace(_config.ConnectionString))
            {
                _client = new EmailClient(_config.ConnectionString);
            }
        }
    }

    public override async Task<ProviderResult> SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        if (_config == null || _client == null || string.IsNullOrWhiteSpace(_config.FromEmail))
        {
            return ProviderResult.Failure("Azure通信服务未配置或配置无效");
        }

        try
        {
            var emailContent = new EmailContent(request.Subject)
            {
                PlainText = request.ContentType?.ToLower() != "html" ? request.Content : null,
                Html = request.ContentType?.ToLower() == "html" ? request.Content : null
            };

            // 构建发件人地址，包含显示名称
            var senderAddress = _config.FromEmail;
            var displayName = request.FromName ?? _config.FromName;

            var emailMessage = new EmailMessage(
                senderAddress: senderAddress,
                recipientAddress: request.ToEmail,
                content: emailContent
            );

            var emailSendOperation = await _client.SendAsync(
                WaitUntil.Started,
                emailMessage,
                cancellationToken
            );

            return ProviderResult.Success(
                messageId: emailSendOperation.Id,
                rawResponse: JsonSerializer.Serialize(new {
                    OperationId = emailSendOperation.Id,
                    Status = emailSendOperation.HasCompleted ? "Completed" : "Started"
                })
            );
        }
        catch (RequestFailedException ex)
        {
            return ProviderResult.Failure(
                errorMessage: $"Azure邮件发送失败: {ex.Message} (错误代码: {ex.ErrorCode})",
                rawResponse: JsonSerializer.Serialize(new { ex.ErrorCode, ex.Message, ex.Status })
            );
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"邮件发送失败: {ex.Message}");
        }
    }

    public override async Task<ProviderResult> TestConnectionAsync(MessageChannel channel, CancellationToken cancellationToken = default)
    {
        if (channel != MessageChannel.Email)
        {
            return ProviderResult.Failure("Azure通信服务当前仅支持邮件渠道");
        }

        if (_config == null || _client == null || string.IsNullOrWhiteSpace(_config.FromEmail))
        {
            return ProviderResult.Failure("配置信息不完整");
        }

        try
        {
            // 发送测试邮件到发件人自己
            var emailContent = new EmailContent("MsgPulse Azure通信服务连接测试")
            {
                PlainText = $"这是一封Azure通信服务连接测试邮件，发送时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            };

            var emailMessage = new EmailMessage(
                senderAddress: _config.FromEmail,
                recipientAddress: _config.FromEmail,
                content: emailContent
            );

            var emailSendOperation = await _client.SendAsync(
                WaitUntil.Started,
                emailMessage,
                cancellationToken
            );

            return ProviderResult.Success(
                messageId: "test-success",
                rawResponse: $"Azure通信服务连接测试成功，已发送测试邮件到 {_config.FromEmail}，操作ID: {emailSendOperation.Id}"
            );
        }
        catch (RequestFailedException ex)
        {
            return ProviderResult.Failure($"连接测试失败: {ex.Message} (错误代码: {ex.ErrorCode})");
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"连接测试失败: {ex.Message}");
        }
    }
}
