using System.Text.Json;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

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
    private SendGridClient? _client;

    public override ConfigurationSchema GetConfigurationSchema()
    {
        return new ConfigurationSchema
        {
            ProviderName = "SendGrid",
            Description = "SendGrid是全球领先的邮件发送服务平台，提供高送达率的邮件发送能力",
            DocumentationUrl = "https://docs.sendgrid.com/",
            Fields = new List<ConfigurationField>
            {
                new ConfigurationField
                {
                    Name = "apiKey",
                    Label = "API Key",
                    Type = "password",
                    Required = true,
                    Placeholder = "SG.***************",
                    HelpText = "SendGrid API密钥，在SendGrid控制台Settings > API Keys中创建",
                    ValidationPattern = "^SG\\.[A-Za-z0-9_\\-]{22,}\\.[A-Za-z0-9_\\-]{43,}$",
                    ValidationMessage = "请输入有效的SendGrid API Key(格式: SG.xxx.xxx)",
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
                    Placeholder = "noreply@example.com",
                    HelpText = "默认发件人邮箱地址，需要在SendGrid中验证该域名或邮箱",
                    ValidationPattern = "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$",
                    ValidationMessage = "请输入有效的邮箱地址",
                    IsSensitive = false,
                    Group = "邮件配置",
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
                    Group = "邮件配置",
                    Order = 3
                }
            },
            Example = @"{
  ""apiKey"": ""SG.***************.*************"",
  ""fromEmail"": ""noreply@example.com"",
  ""fromName"": ""您的应用""
}"
        };
    }

    public override void Initialize(string? configuration)
    {
        base.Initialize(configuration);
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            _config = JsonSerializer.Deserialize<SendGridConfig>(configuration);

            if (_config != null && !string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                _client = new SendGridClient(_config.ApiKey);
            }
        }
    }

    public override async Task<ProviderResult> SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        if (_config == null || _client == null || string.IsNullOrWhiteSpace(_config.FromEmail))
        {
            return ProviderResult.Failure("SendGrid未配置或配置无效");
        }

        try
        {
            var from = new EmailAddress(_config.FromEmail, request.FromName ?? _config.FromName);
            var to = new EmailAddress(request.ToEmail);

            var message = MailHelper.CreateSingleEmail(
                from,
                to,
                request.Subject,
                request.ContentType == "Text" ? request.Content : null,
                request.ContentType == "HTML" ? request.Content : null
            );

            var response = await _client.SendEmailAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // SendGrid 返回的 X-Message-Id header 包含消息ID
                var messageId = response.Headers.GetValues("X-Message-Id").FirstOrDefault() ?? $"sendgrid-{Guid.NewGuid():N}";

                return ProviderResult.Success(
                    messageId: messageId,
                    rawResponse: $"StatusCode: {response.StatusCode}"
                );
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                return ProviderResult.Failure(
                    errorMessage: $"SendGrid邮件发送失败: {response.StatusCode}",
                    rawResponse: responseBody
                );
            }
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

        if (_config == null || _client == null)
        {
            return ProviderResult.Failure("配置信息不完整");
        }

        try
        {
            // 使用 API Key 验证接口测试连接
            var response = await _client.RequestAsync(
                method: SendGridClient.Method.GET,
                urlPath: "scopes",
                cancellationToken: cancellationToken
            );

            if (response.IsSuccessStatusCode)
            {
                return ProviderResult.Success("连接测试成功", "SendGrid API密钥有效");
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                return ProviderResult.Failure($"连接测试失败: {response.StatusCode} - {responseBody}");
            }
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"连接测试失败: {ex.Message}");
        }
    }
}
