using System.Text.Json;
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
