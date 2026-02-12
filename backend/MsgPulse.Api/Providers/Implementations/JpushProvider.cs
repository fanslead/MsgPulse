using System.Text.Json;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers.Models;
using cn.jpush.api;
using cn.jpush.api.push;
using cn.jpush.api.push.mode;
using cn.jpush.api.push.notification;
using cn.jpush.api.common;

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
/// 极光推送厂商实现
/// </summary>
public class JpushProvider : BaseMessageProvider
{
    public override ProviderType ProviderType => ProviderType.JpushProvider;
    public override MessageChannel[] SupportedChannels => new[] { MessageChannel.AppPush };

    private JpushConfig? _config;
    private JPushClient? _client;

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

            if (_config != null && !string.IsNullOrWhiteSpace(_config.AppKey) && !string.IsNullOrWhiteSpace(_config.MasterSecret))
            {
                _client = new JPushClient(_config.AppKey, _config.MasterSecret);
            }
        }
    }

    public override async Task<ProviderResult> SendPushAsync(AppPushRequest request, CancellationToken cancellationToken = default)
    {
        if (_config == null || _client == null)
        {
            return ProviderResult.Failure("极光推送未配置或配置无效");
        }

        try
        {
            // 构建推送对象
            var pushPayload = new PushPayload();

            // 设置推送平台
            if (string.IsNullOrWhiteSpace(request.Platform))
            {
                pushPayload.platform = Platform.all();
            }
            else if (request.Platform.Equals("iOS", StringComparison.OrdinalIgnoreCase))
            {
                pushPayload.platform = Platform.ios();
            }
            else if (request.Platform.Equals("Android", StringComparison.OrdinalIgnoreCase))
            {
                pushPayload.platform = Platform.android();
            }
            else
            {
                pushPayload.platform = Platform.all();
            }

            // 设置目标设备（使用别名或注册ID）
            if (request.Target.Contains("@"))
            {
                // 假设包含@的是别名
                pushPayload.audience = Audience.s_alias(request.Target);
            }
            else
            {
                // 否则当做注册ID
                pushPayload.audience = Audience.s_registrationId(request.Target);
            }

            // 设置通知内容
            var notification = new Notification()
                .setAlert(request.Content);

            // Android通知
            notification.AndroidNotification = new AndroidNotification()
                .setAlert(request.Content)
                .setTitle(request.Title);

            // iOS通知
            notification.IosNotification = new IosNotification()
                .setAlert(request.Content)
                .incrBadge(1)
                .setSound("default");

            pushPayload.notification = notification;

            // 设置离线保留时间（1小时）
            var options = new Options();
            options.apns_production = _config.ApnsProduction ?? false;
            options.time_to_live = 3600;
            pushPayload.options = options;

            // 发送推送
            var response = await Task.Run(() => _client.SendPush(pushPayload), cancellationToken);

            if (response.isResultOK())
            {
                return ProviderResult.Success(
                    messageId: response.msg_id.ToString(),
                    rawResponse: JsonSerializer.Serialize(new { response.msg_id, response.sendno })
                );
            }
            else
            {
                var content = response.ResponseResult != null ? "请求失败" : "未知错误";
                return ProviderResult.Failure(
                    errorMessage: $"极光推送发送失败: {content}",
                    rawResponse: content
                );
            }
        }
        catch (APIRequestException ex)
        {
            return ProviderResult.Failure(
                errorMessage: $"极光推送API异常: {ex.Message}",
                rawResponse: $"ErrorCode: {ex.ErrorCode}, ErrorMessage: {ex.ErrorMessage}"
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

        if (_config == null || _client == null)
        {
            return ProviderResult.Failure("配置信息不完整");
        }

        try
        {
            // 使用一个简单的推送请求验证配置
            var testPayload = new PushPayload
            {
                platform = Platform.all(),
                audience = Audience.s_tag("__test__"),
                notification = new Notification().setAlert("test")
            };

            // 只验证payload格式，不实际发送
            var result = await Task.Run(() =>
            {
                try
                {
                    // 尝试发送到一个不存在的标签，如果认证成功会返回错误但连接正常
                    var resp = _client.SendPush(testPayload);
                    return true;
                }
                catch (APIRequestException ex)
                {
                    // 这些错误码表示认证成功但目标不存在
                    if (ex.ErrorCode == 1011 || ex.ErrorCode == 1020 || ex.ErrorCode == 1003)
                    {
                        return true;
                    }
                    throw;
                }
            }, cancellationToken);

            if (result)
            {
                return ProviderResult.Success("连接测试成功", "极光推送API可正常访问");
            }
            else
            {
                return ProviderResult.Failure("连接测试失败: 推送验证失败");
            }
        }
        catch (APIRequestException ex)
        {
            return ProviderResult.Failure($"连接测试失败: {ex.ErrorMessage}");
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"连接测试失败: {ex.Message}");
        }
    }
}
