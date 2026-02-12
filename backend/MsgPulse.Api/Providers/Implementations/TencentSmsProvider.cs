using System.Text.Json;
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
