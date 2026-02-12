using System.Text.Json;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers.Models;
using AlibabaCloud.SDK.Dysmsapi20170525;
using AlibabaCloud.SDK.Dysmsapi20170525.Models;
using Tea;

namespace MsgPulse.Api.Providers.Implementations;

/// <summary>
/// 阿里云短信配置
/// </summary>
public class AliyunSmsConfig
{
    public string? AccessKeyId { get; set; }
    public string? AccessKeySecret { get; set; }
    public string? RegionId { get; set; }
    public string? SignName { get; set; }
    public string? Endpoint { get; set; }
}

/// <summary>
/// 阿里云短信厂商实现
/// </summary>
public class AliyunSmsProvider : BaseMessageProvider
{
    public override ProviderType ProviderType => ProviderType.AliyunSms;
    public override MessageChannel[] SupportedChannels => new[] { MessageChannel.SMS };

    private AliyunSmsConfig? _config;
    private Client? _client;

    public override ConfigurationSchema GetConfigurationSchema()
    {
        return new ConfigurationSchema
        {
            ProviderName = "阿里云短信",
            Description = "阿里云短信服务(Alibaba Cloud SMS)提供快速、稳定、安全的短信发送能力",
            DocumentationUrl = "https://help.aliyun.com/product/44282.html",
            Fields = new List<ConfigurationField>
            {
                new ConfigurationField
                {
                    Name = "accessKeyId",
                    Label = "AccessKey ID",
                    Type = "text",
                    Required = true,
                    Placeholder = "LTA***************",
                    HelpText = "阿里云账号的AccessKey ID，用于API认证",
                    ValidationPattern = "^[A-Za-z0-9]{16,128}$",
                    ValidationMessage = "请输入有效的AccessKey ID(16-128位字母数字)",
                    IsSensitive = false,
                    Group = "认证信息",
                    Order = 1
                },
                new ConfigurationField
                {
                    Name = "accessKeySecret",
                    Label = "AccessKey Secret",
                    Type = "password",
                    Required = true,
                    Placeholder = "请输入AccessKey Secret",
                    HelpText = "阿里云账号的AccessKey Secret，请妥善保管",
                    ValidationPattern = "^[A-Za-z0-9]{30,128}$",
                    ValidationMessage = "请输入有效的AccessKey Secret(30-128位字母数字)",
                    IsSensitive = true,
                    Group = "认证信息",
                    Order = 2
                },
                new ConfigurationField
                {
                    Name = "signName",
                    Label = "短信签名",
                    Type = "text",
                    Required = true,
                    Placeholder = "您的应用名称",
                    HelpText = "短信签名名称，需在阿里云控制台预先申请并审核通过",
                    ValidationMessage = "请输入已审核通过的短信签名",
                    IsSensitive = false,
                    Group = "短信配置",
                    Order = 3
                },
                new ConfigurationField
                {
                    Name = "regionId",
                    Label = "地域ID",
                    Type = "select",
                    Required = false,
                    DefaultValue = "cn-hangzhou",
                    HelpText = "阿里云服务地域，默认为cn-hangzhou",
                    IsSensitive = false,
                    Group = "高级配置",
                    Order = 4,
                    Options = new List<SelectOption>
                    {
                        new SelectOption { Label = "华东1(杭州)", Value = "cn-hangzhou" },
                        new SelectOption { Label = "华北2(北京)", Value = "cn-beijing" },
                        new SelectOption { Label = "华南1(深圳)", Value = "cn-shenzhen" },
                        new SelectOption { Label = "华东2(上海)", Value = "cn-shanghai" }
                    }
                },
                new ConfigurationField
                {
                    Name = "endpoint",
                    Label = "服务端点",
                    Type = "text",
                    Required = false,
                    DefaultValue = "dysmsapi.aliyuncs.com",
                    Placeholder = "dysmsapi.aliyuncs.com",
                    HelpText = "API服务端点地址，一般无需修改",
                    IsSensitive = false,
                    Group = "高级配置",
                    Order = 5
                }
            },
            Example = @"{
  ""accessKeyId"": ""LTAI***************"",
  ""accessKeySecret"": ""您的AccessKey Secret"",
  ""signName"": ""您的应用"",
  ""regionId"": ""cn-hangzhou"",
  ""endpoint"": ""dysmsapi.aliyuncs.com""
}"
        };
    }

    public override void Initialize(string? configuration)
    {
        base.Initialize(configuration);
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            _config = JsonSerializer.Deserialize<AliyunSmsConfig>(configuration);

            if (_config != null && !string.IsNullOrWhiteSpace(_config.AccessKeyId) && !string.IsNullOrWhiteSpace(_config.AccessKeySecret))
            {
                var config = new AlibabaCloud.OpenApiClient.Models.Config
                {
                    AccessKeyId = _config.AccessKeyId,
                    AccessKeySecret = _config.AccessKeySecret,
                    Endpoint = _config.Endpoint ?? "dysmsapi.aliyuncs.com"
                };
                _client = new Client(config);
            }
        }
    }

    public override async Task<ProviderResult> SendSmsAsync(SmsRequest request, CancellationToken cancellationToken = default)
    {
        if (_config == null || _client == null)
        {
            return ProviderResult.Failure("阿里云短信未配置或配置无效");
        }

        try
        {
            var sendRequest = new SendSmsRequest
            {
                PhoneNumbers = request.PhoneNumber,
                SignName = request.SignName ?? _config.SignName,
                TemplateCode = request.TemplateCode,
                TemplateParam = request.TemplateParams
            };

            var response = await _client.SendSmsAsync(sendRequest);

            if (response?.Body?.Code == "OK")
            {
                return ProviderResult.Success(
                    messageId: response.Body.BizId ?? $"aliyun-{Guid.NewGuid():N}",
                    rawResponse: JsonSerializer.Serialize(response.Body)
                );
            }
            else
            {
                return ProviderResult.Failure(
                    errorMessage: $"阿里云短信发送失败: {response?.Body?.Message ?? "未知错误"}",
                    rawResponse: JsonSerializer.Serialize(response?.Body)
                );
            }
        }
        catch (TeaException ex)
        {
            return ProviderResult.Failure(
                errorMessage: $"阿里云短信发送异常: {ex.Message}",
                rawResponse: JsonSerializer.Serialize(new { ex.Code, ex.Message, ex.Data })
            );
        }
        catch (Exception ex)
        {
            return ProviderResult.Failure($"阿里云短信发送失败: {ex.Message}");
        }
    }

    public override async Task<ProviderResult> TestConnectionAsync(MessageChannel channel, CancellationToken cancellationToken = default)
    {
        if (channel != MessageChannel.SMS)
        {
            return ProviderResult.Failure("阿里云仅支持短信渠道");
        }

        if (_config == null || _client == null)
        {
            return ProviderResult.Failure("配置信息不完整");
        }

        try
        {
            // 使用QuerySendDetails接口测试连接（查询最近一条记录）
            var testRequest = new QuerySendDetailsRequest
            {
                PhoneNumber = "13800138000", // 测试手机号
                SendDate = DateTime.Now.ToString("yyyyMMdd"),
                PageSize = 1,
                CurrentPage = 1
            };

            var response = await _client.QuerySendDetailsAsync(testRequest);

            if (response?.Body?.Code == "OK" || response?.Body?.Code == "isv.BUSINESS_LIMIT_CONTROL")
            {
                return ProviderResult.Success("连接测试成功", "阿里云API调用正常");
            }
            else
            {
                return ProviderResult.Failure($"连接测试失败: {response?.Body?.Message ?? "未知错误"}");
            }
        }
        catch (TeaException ex)
        {
            // 某些错误码表示连接正常但参数问题，这也算测试成功
            if (ex.Code == "isv.BUSINESS_LIMIT_CONTROL" || ex.Code == "isv.MOBILE_NUMBER_ILLEGAL")
            {
                return ProviderResult.Success("连接测试成功", "阿里云API可正常访问");
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
        if (_config == null || _client == null)
        {
            return new TemplateSyncResult
            {
                IsSuccess = false,
                ErrorMessage = "配置信息不完整"
            };
        }

        try
        {
            // 调用QuerySmsTemplateList查询模板列表
            var queryRequest = new QuerySmsTemplateListRequest
            {
                PageIndex = 1,
                PageSize = 50
            };

            var response = await _client.QuerySmsTemplateListAsync(queryRequest);

            if (response?.Body?.Code == "OK" && response.Body.SmsTemplateList != null)
            {
                var templates = new List<SyncedTemplate>();
                foreach (var template in response.Body.SmsTemplateList)
                {
                    templates.Add(new SyncedTemplate
                    {
                        Code = template.TemplateCode ?? "",
                        Name = template.TemplateName ?? "",
                        Content = template.TemplateContent ?? "",
                        Status = ConvertTemplateStatus(template.TemplateType)
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
                    ErrorMessage = $"模板同步失败: {response?.Body?.Message ?? "未知错误"}"
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

    private static string ConvertTemplateStatus(int? status)
    {
        return status switch
        {
            0 => "审核中",
            1 => "已审核",
            2 => "审核失败",
            _ => "未知"
        };
    }
}
