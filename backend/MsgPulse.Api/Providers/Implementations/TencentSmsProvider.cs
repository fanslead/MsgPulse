using System.Text.Json;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers.Models;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Sms.V20210111;
using TencentCloud.Sms.V20210111.Models;

namespace MsgPulse.Api.Providers.Implementations;

/// <summary>
/// 腾讯云短信配置
/// </summary>
public class TencentSmsConfig
{
    public string? SecretId { get; set; }
    public string? SecretKey { get; set; }
    public string? SdkAppId { get; set; }
    public string? SignName { get; set; }
    public string? Region { get; set; }
}

/// <summary>
/// 腾讯云短信厂商实现
/// </summary>
public class TencentSmsProvider : BaseMessageProvider
{
    public override ProviderType ProviderType => ProviderType.TencentSms;
    public override MessageChannel[] SupportedChannels => new[] { MessageChannel.SMS };

    private TencentSmsConfig? _config;
    private SmsClient? _client;

    public override ConfigurationSchema GetConfigurationSchema()
    {
        return new ConfigurationSchema
        {
            ProviderName = "腾讯云短信",
            Description = "腾讯云短信服务(Tencent Cloud SMS)提供快速、稳定、优质的短信发送能力",
            DocumentationUrl = "https://cloud.tencent.com/document/product/382",
            Fields = new List<ConfigurationField>
            {
                new ConfigurationField
                {
                    Name = "secretId",
                    Label = "SecretId",
                    Type = "text",
                    Required = true,
                    Placeholder = "AKI***************",
                    HelpText = "腾讯云账号的SecretId，用于API认证",
                    ValidationPattern = "^[A-Za-z0-9]{32,256}$",
                    ValidationMessage = "请输入有效的SecretId(32-256位字母数字)",
                    IsSensitive = false,
                    Group = "认证信息",
                    Order = 1
                },
                new ConfigurationField
                {
                    Name = "secretKey",
                    Label = "SecretKey",
                    Type = "password",
                    Required = true,
                    Placeholder = "请输入SecretKey",
                    HelpText = "腾讯云账号的SecretKey，请妥善保管",
                    ValidationPattern = "^[A-Za-z0-9]{32,256}$",
                    ValidationMessage = "请输入有效的SecretKey(32-256位字母数字)",
                    IsSensitive = true,
                    Group = "认证信息",
                    Order = 2
                },
                new ConfigurationField
                {
                    Name = "sdkAppId",
                    Label = "SDK AppID",
                    Type = "text",
                    Required = true,
                    Placeholder = "1400******",
                    HelpText = "短信应用ID，在腾讯云短信控制台创建应用后获得",
                    ValidationPattern = "^[0-9]{10,20}$",
                    ValidationMessage = "请输入有效的SDK AppID(10-20位数字)",
                    IsSensitive = false,
                    Group = "短信配置",
                    Order = 3
                },
                new ConfigurationField
                {
                    Name = "signName",
                    Label = "短信签名",
                    Type = "text",
                    Required = true,
                    Placeholder = "您的应用名称",
                    HelpText = "短信签名内容，需在腾讯云控制台预先申请并审核通过",
                    ValidationMessage = "请输入已审核通过的短信签名",
                    IsSensitive = false,
                    Group = "短信配置",
                    Order = 4
                },
                new ConfigurationField
                {
                    Name = "region",
                    Label = "地域",
                    Type = "select",
                    Required = false,
                    DefaultValue = "ap-guangzhou",
                    HelpText = "腾讯云服务地域，默认为ap-guangzhou",
                    IsSensitive = false,
                    Group = "高级配置",
                    Order = 5,
                    Options = new List<SelectOption>
                    {
                        new SelectOption { Label = "广州", Value = "ap-guangzhou" },
                        new SelectOption { Label = "北京", Value = "ap-beijing" },
                        new SelectOption { Label = "上海", Value = "ap-shanghai" },
                        new SelectOption { Label = "成都", Value = "ap-chengdu" }
                    }
                }
            },
            Example = @"{
  ""secretId"": ""AKI***************"",
  ""secretKey"": ""您的SecretKey"",
  ""sdkAppId"": ""1400000000"",
  ""signName"": ""您的应用"",
  ""region"": ""ap-guangzhou""
}"
        };
    }

    public override void Initialize(string? configuration)
    {
        base.Initialize(configuration);
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            _config = JsonSerializer.Deserialize<TencentSmsConfig>(configuration);

            if (_config != null && !string.IsNullOrWhiteSpace(_config.SecretId) && !string.IsNullOrWhiteSpace(_config.SecretKey))
            {
                var credential = new Credential
                {
                    SecretId = _config.SecretId,
                    SecretKey = _config.SecretKey
                };

                var clientProfile = new ClientProfile
                {
                    HttpProfile = new HttpProfile
                    {
                        Endpoint = "sms.tencentcloudapi.com"
                    }
                };

                _client = new SmsClient(credential, _config.Region ?? "ap-guangzhou", clientProfile);
            }
        }
    }

    public override async Task<ProviderResult> SendSmsAsync(SmsRequest request, CancellationToken cancellationToken = default)
    {
        if (_config == null || _client == null || string.IsNullOrWhiteSpace(_config.SdkAppId))
        {
            return ProviderResult.Failure("腾讯云短信未配置或配置无效");
        }

        try
        {
            var sendRequest = new SendSmsRequest
            {
                SmsSdkAppId = _config.SdkAppId,
                SignName = request.SignName ?? _config.SignName,
                TemplateId = request.TemplateCode,
                PhoneNumberSet = new[] { request.PhoneNumber }
            };

            // 解析模板参数
            if (!string.IsNullOrWhiteSpace(request.TemplateParams))
            {
                var paramsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(request.TemplateParams);
                if (paramsDict != null)
                {
                    sendRequest.TemplateParamSet = paramsDict.Values.ToArray();
                }
            }

            var response = await _client.SendSms(sendRequest);

            if (response?.SendStatusSet != null && response.SendStatusSet.Length > 0)
            {
                var status = response.SendStatusSet[0];
                if (status.Code == "Ok")
                {
                    return ProviderResult.Success(
                        messageId: status.SerialNo ?? $"tencent-{Guid.NewGuid():N}",
                        rawResponse: JsonSerializer.Serialize(response)
                    );
                }
                else
                {
                    return ProviderResult.Failure(
                        errorMessage: $"腾讯云短信发送失败: {status.Message}",
                        rawResponse: JsonSerializer.Serialize(response)
                    );
                }
            }
            else
            {
                return ProviderResult.Failure(
                    errorMessage: "腾讯云短信发送失败: 未返回发送状态",
                    rawResponse: JsonSerializer.Serialize(response)
                );
            }
        }
        catch (TencentCloudSDKException ex)
        {
            return ProviderResult.Failure(
                errorMessage: $"腾讯云短信发送异常: {ex.Message}",
                rawResponse: JsonSerializer.Serialize(new { ex.ErrorCode, ex.Message, ex.RequestId })
            );
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"腾讯云短信发送失败: {ex.Message}");
        }
    }

    public override async Task<ProviderResult> TestConnectionAsync(MessageChannel channel, CancellationToken cancellationToken = default)
    {
        if (channel != MessageChannel.SMS)
        {
            return ProviderResult.Failure("腾讯云仅支持短信渠道");
        }

        if (_config == null || _client == null || string.IsNullOrWhiteSpace(_config.SdkAppId))
        {
            return ProviderResult.Failure("配置信息不完整");
        }

        try
        {
            // 使用PullSmsSendStatus接口测试连接
            var testRequest = new PullSmsSendStatusRequest
            {
                SmsSdkAppId = _config.SdkAppId,
                Limit = 1
            };

            var response = await _client.PullSmsSendStatus(testRequest);

            // 能够成功调用API即表示连接正常
            return ProviderResult.Success("连接测试成功", "腾讯云API调用正常");
        }
        catch (TencentCloudSDKException ex)
        {
            // 某些特定错误码也表示连接正常
            if (ex.ErrorCode == "ResourceNotFound.AppIdNotExist" || ex.ErrorCode == "InvalidParameterValue")
            {
                return ProviderResult.Success("连接测试成功", "腾讯云API可正常访问");
            }
            return ProviderResult.Failure($"连接测试失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"连接测试失败: {ex.Message}");
        }
    }

    public override async Task<TemplateSyncResult> SyncSmsTemplatesAsync(CancellationToken cancellationToken = default)
    {
        if (_config == null || _client == null || string.IsNullOrWhiteSpace(_config.SdkAppId))
        {
            return new TemplateSyncResult
            {
                IsSuccess = false,
                ErrorMessage = "配置信息不完整"
            };
        }

        try
        {
            // 调用DescribeSmsTemplateList接口查询模板列表
            var queryRequest = new DescribeSmsTemplateListRequest
            {
                TemplateIdSet = null, // null表示查询所有
                International = 0 // 0表示国内短信
            };

            var response = await _client.DescribeSmsTemplateList(queryRequest);

            if (response?.DescribeTemplateStatusSet != null && response.DescribeTemplateStatusSet.Length > 0)
            {
                var templates = new List<SyncedTemplate>();
                foreach (var template in response.DescribeTemplateStatusSet)
                {
                    templates.Add(new SyncedTemplate
                    {
                        Code = template.TemplateId?.ToString() ?? "",
                        Name = template.TemplateName ?? "",
                        Content = template.TemplateContent ?? "",
                        Status = ConvertTemplateStatus(template.StatusCode)
                    });
                }

                return new TemplateSyncResult
                {
                    IsSuccess = true,
                    Templates = templates
                };
            }
            else
            {
                return new TemplateSyncResult
                {
                    IsSuccess = false,
                    ErrorMessage = "模板同步失败: 未返回模板列表"
                };
            }
        }
        catch (Exception ex)
        {
            return new TemplateSyncResult
            {
                IsSuccess = false,
                ErrorMessage = $"模板同步失败: {ex.Message}"
            };
        }
    }

    private static string ConvertTemplateStatus(long? statusCode)
    {
        return statusCode switch
        {
            0 => "审核通过",
            1 => "待审核",
            -1 => "审核未通过",
            _ => "未知"
        };
    }
}
