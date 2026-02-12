using System.Text;
using System.Text.Json;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers.Models;

namespace MsgPulse.Api.Providers.Implementations;

/// <summary>
/// 极光推送配置
/// </summary>
public class JpushConfig
{
    public string? AppKey { get; set; }
    public string? MasterSecret { get; set; }
    public bool? ApnsProduction { get; set; }
}

/// <summary>
/// 极光推送厂商实现 (使用REST API)
/// </summary>
public class JpushProvider : BaseMessageProvider
{
    public override ProviderType ProviderType => ProviderType.JpushProvider;
    public override MessageChannel[] SupportedChannels => new[] { MessageChannel.AppPush };

    private JpushConfig? _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private const string JpushApiUrl = "https://api.jpush.cn/v3/push";

    public JpushProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override ConfigurationSchema GetConfigurationSchema()
    {
        return new ConfigurationSchema
        {
            ProviderName = "极光推送",
            Description = "极光推送(JPush)是领先的第三方消息推送服务商，支持Android和iOS双平台",
            DocumentationUrl = "https://docs.jiguang.cn/jpush/server/push/rest_api_v3_push/",
            Fields = new List<ConfigurationField>
            {
                new ConfigurationField
                {
                    Name = "appKey",
                    Label = "AppKey",
                    Type = "text",
                    Required = true,
                    Placeholder = "1234567890abcdef",
                    HelpText = "极光推送应用的AppKey，在极光开发者平台创建应用后获得",
                    ValidationPattern = "^[a-zA-Z0-9]{24}$",
                    ValidationMessage = "请输入有效的AppKey(24位字母数字)",
                    IsSensitive = false,
                    Group = "认证信息",
                    Order = 1
                },
                new ConfigurationField
                {
                    Name = "masterSecret",
                    Label = "Master Secret",
                    Type = "password",
                    Required = true,
                    Placeholder = "请输入Master Secret",
                    HelpText = "极光推送应用的Master Secret，请妥善保管",
                    ValidationPattern = "^[a-zA-Z0-9]{24}$",
                    ValidationMessage = "请输入有效的Master Secret(24位字母数字)",
                    IsSensitive = true,
                    Group = "认证信息",
                    Order = 2
                },
                new ConfigurationField
                {
                    Name = "apnsProduction",
                    Label = "iOS生产环境",
                    Type = "select",
                    Required = false,
                    DefaultValue = "false",
                    HelpText = "iOS推送环境，开发环境选择false，生产环境选择true",
                    IsSensitive = false,
                    Group = "推送配置",
                    Order = 3,
                    Options = new List<SelectOption>
                    {
                        new SelectOption { Label = "开发环境(Sandbox)", Value = "false" },
                        new SelectOption { Label = "生产环境(Production)", Value = "true" }
                    }
                }
            },
            Example = @"{
  ""appKey"": ""1234567890abcdef1234"",
  ""masterSecret"": ""abcdef1234567890abcd"",
  ""apnsProduction"": false
}"
        };
    }

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
        if (_config == null || string.IsNullOrWhiteSpace(_config.AppKey) || string.IsNullOrWhiteSpace(_config.MasterSecret))
        {
            return ProviderResult.Failure("极光推送未配置或配置无效");
        }

        try
        {
            // 构建推送请求体
            var pushPayload = new
            {
                platform = DeterminePlatform(request.Platform),
                audience = DetermineAudience(request.Target),
                notification = new
                {
                    alert = request.Content,
                    android = new
                    {
                        alert = request.Content,
                        title = request.Title
                    },
                    ios = new
                    {
                        alert = request.Content,
                        badge = "+1",
                        sound = "default"
                    }
                },
                options = new
                {
                    apns_production = _config.ApnsProduction ?? false,
                    time_to_live = 3600
                }
            };

            var jsonPayload = JsonSerializer.Serialize(pushPayload);
            var httpClient = _httpClientFactory.CreateClient();

            // 设置Basic认证
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.AppKey}:{_config.MasterSecret}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(JpushApiUrl, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var msgId = result.GetProperty("msg_id").GetString() ?? Guid.NewGuid().ToString("N");

                return ProviderResult.Success(
                    messageId: msgId,
                    rawResponse: responseBody
                );
            }
            else
            {
                return ProviderResult.Failure(
                    errorMessage: $"极光推送发送失败: HTTP {response.StatusCode}",
                    rawResponse: responseBody
                );
            }
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

        if (_config == null || string.IsNullOrWhiteSpace(_config.AppKey) || string.IsNullOrWhiteSpace(_config.MasterSecret))
        {
            return ProviderResult.Failure("配置信息不完整");
        }

        try
        {
            // 使用不存在的标签测试连接
            var testPayload = new
            {
                platform = "all",
                audience = new { tag = new[] { "__test__" } },
                notification = new { alert = "test" }
            };

            var jsonPayload = JsonSerializer.Serialize(testPayload);
            var httpClient = _httpClientFactory.CreateClient();

            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.AppKey}:{_config.MasterSecret}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(JpushApiUrl, content, cancellationToken);

            // 认证成功即表示连接正常，即使标签不存在也没关系
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return ProviderResult.Success("连接测试成功", "极光推送API可正常访问");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return ProviderResult.Failure("连接测试失败: AppKey或Master Secret错误");
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return ProviderResult.Failure($"连接测试失败: HTTP {response.StatusCode}, {responseBody}");
            }
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"连接测试失败: {ex.Message}");
        }
    }

    private static object DeterminePlatform(string? platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            return "all";
        }
        else if (platform.Equals("iOS", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "ios" };
        }
        else if (platform.Equals("Android", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "android" };
        }
        else
        {
            return "all";
        }
    }

    private static object DetermineAudience(string target)
    {
        // 假设包含@的是别名，否则是注册ID
        if (target.Contains('@'))
        {
            return new { alias = new[] { target } };
        }
        else
        {
            return new { registration_id = new[] { target } };
        }
    }
}
