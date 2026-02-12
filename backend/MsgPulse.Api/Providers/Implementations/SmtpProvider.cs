using System.Net;
using System.Net.Mail;
using System.Text.Json;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers.Models;

namespace MsgPulse.Api.Providers.Implementations;

/// <summary>
/// 自定义SMTP邮件配置
/// </summary>
public class SmtpConfig
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public bool EnableSsl { get; set; } = true;
    public int Timeout { get; set; } = 30000;
}

/// <summary>
/// 自定义SMTP邮件提供商实现
/// </summary>
public class SmtpProvider : BaseMessageProvider
{
    public override ProviderType ProviderType => ProviderType.CustomSmtp;
    public override MessageChannel[] SupportedChannels => new[] { MessageChannel.Email };

    private SmtpConfig? _config;

    public override ConfigurationSchema GetConfigurationSchema()
    {
        return new ConfigurationSchema
        {
            ProviderName = "自定义SMTP",
            Description = "使用标准SMTP协议发送邮件，支持任何SMTP服务器(如企业邮箱、腾讯企业邮、阿里企业邮等)",
            DocumentationUrl = "https://en.wikipedia.org/wiki/Simple_Mail_Transfer_Protocol",
            Fields = new List<ConfigurationField>
            {
                new ConfigurationField
                {
                    Name = "host",
                    Label = "SMTP服务器地址",
                    Type = "text",
                    Required = true,
                    Placeholder = "smtp.example.com",
                    HelpText = "SMTP服务器的主机名或IP地址",
                    ValidationMessage = "请输入有效的服务器地址",
                    IsSensitive = false,
                    Group = "服务器配置",
                    Order = 1
                },
                new ConfigurationField
                {
                    Name = "port",
                    Label = "端口",
                    Type = "number",
                    Required = true,
                    DefaultValue = "587",
                    Placeholder = "587",
                    HelpText = "SMTP端口，常用: 587(TLS), 465(SSL), 25(非加密)",
                    ValidationPattern = "^[0-9]{1,5}$",
                    ValidationMessage = "请输入有效的端口号(1-65535)",
                    IsSensitive = false,
                    Group = "服务器配置",
                    Order = 2
                },
                new ConfigurationField
                {
                    Name = "username",
                    Label = "用户名",
                    Type = "text",
                    Required = true,
                    Placeholder = "user@example.com",
                    HelpText = "SMTP认证用户名，通常是完整邮箱地址",
                    IsSensitive = false,
                    Group = "认证信息",
                    Order = 3
                },
                new ConfigurationField
                {
                    Name = "password",
                    Label = "密码",
                    Type = "password",
                    Required = true,
                    Placeholder = "请输入SMTP密码或授权码",
                    HelpText = "SMTP认证密码或授权码，某些邮箱服务需要使用专用授权码",
                    IsSensitive = true,
                    Group = "认证信息",
                    Order = 4
                },
                new ConfigurationField
                {
                    Name = "fromEmail",
                    Label = "发件人邮箱",
                    Type = "text",
                    Required = true,
                    Placeholder = "noreply@example.com",
                    HelpText = "默认发件人邮箱地址",
                    ValidationPattern = "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$",
                    ValidationMessage = "请输入有效的邮箱地址",
                    IsSensitive = false,
                    Group = "发件人配置",
                    Order = 5
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
                    Order = 6
                },
                new ConfigurationField
                {
                    Name = "enableSsl",
                    Label = "启用SSL/TLS",
                    Type = "select",
                    Required = false,
                    DefaultValue = "true",
                    HelpText = "是否启用SSL/TLS加密传输，推荐启用以提高安全性",
                    IsSensitive = false,
                    Group = "高级配置",
                    Order = 7,
                    Options = new List<SelectOption>
                    {
                        new SelectOption { Label = "启用(推荐)", Value = "true" },
                        new SelectOption { Label = "禁用", Value = "false" }
                    }
                },
                new ConfigurationField
                {
                    Name = "timeout",
                    Label = "超时时间(毫秒)",
                    Type = "number",
                    Required = false,
                    DefaultValue = "30000",
                    Placeholder = "30000",
                    HelpText = "邮件发送超时时间，默认30秒",
                    IsSensitive = false,
                    Group = "高级配置",
                    Order = 8
                }
            },
            Example = @"{
  ""host"": ""smtp.example.com"",
  ""port"": 587,
  ""username"": ""user@example.com"",
  ""password"": ""your-password-or-auth-code"",
  ""fromEmail"": ""noreply@example.com"",
  ""fromName"": ""您的应用"",
  ""enableSsl"": true,
  ""timeout"": 30000
}"
        };
    }

    public override void Initialize(string? configuration)
    {
        base.Initialize(configuration);
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            _config = JsonSerializer.Deserialize<SmtpConfig>(configuration);
        }
    }

    public override async Task<ProviderResult> SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        if (_config == null || string.IsNullOrWhiteSpace(_config.Host) ||
            string.IsNullOrWhiteSpace(_config.Username) || string.IsNullOrWhiteSpace(_config.FromEmail))
        {
            return ProviderResult.Failure("SMTP未配置或配置无效");
        }

        try
        {
            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_config.FromEmail, request.FromName ?? _config.FromName ?? _config.FromEmail);
            mailMessage.To.Add(request.ToEmail);
            mailMessage.Subject = request.Subject;
            mailMessage.Body = request.Content;
            mailMessage.IsBodyHtml = request.ContentType?.ToLower() == "html";

            using var smtpClient = new SmtpClient(_config.Host, _config.Port);
            smtpClient.Credentials = new NetworkCredential(_config.Username, _config.Password);
            smtpClient.EnableSsl = _config.EnableSsl;
            smtpClient.Timeout = _config.Timeout;

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            return ProviderResult.Success(
                messageId: $"smtp-{Guid.NewGuid():N}",
                rawResponse: $"邮件发送成功 - {_config.Host}:{_config.Port}"
            );
        }
        catch (SmtpException ex)
        {
            return ProviderResult.Failure(
                errorMessage: $"SMTP发送失败: {ex.Message} (状态码: {ex.StatusCode})",
                rawResponse: JsonSerializer.Serialize(new { ex.StatusCode, ex.Message })
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
            return ProviderResult.Failure("自定义SMTP仅支持邮件渠道");
        }

        if (_config == null || string.IsNullOrWhiteSpace(_config.Host))
        {
            return ProviderResult.Failure("SMTP配置不完整");
        }

        try
        {
            using var smtpClient = new SmtpClient(_config.Host, _config.Port);
            smtpClient.Credentials = new NetworkCredential(_config.Username, _config.Password);
            smtpClient.EnableSsl = _config.EnableSsl;
            smtpClient.Timeout = 10000; // 测试时使用较短超时

            // 发送测试邮件到发件人自己
            using var testMessage = new MailMessage();
            testMessage.From = new MailAddress(_config.FromEmail!, _config.FromName ?? "测试");
            testMessage.To.Add(_config.FromEmail!);
            testMessage.Subject = "MsgPulse SMTP连接测试";
            testMessage.Body = $"这是一封SMTP连接测试邮件，发送时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            await smtpClient.SendMailAsync(testMessage, cancellationToken);

            return ProviderResult.Success(
                messageId: "test-success",
                rawResponse: $"SMTP连接测试成功，已发送测试邮件到 {_config.FromEmail}"
            );
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"SMTP连接测试失败: {ex.Message}");
        }
    }
}
