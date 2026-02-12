using System.Text;
using System.Text.Json;
using MsgPulse.Api.Models;

namespace MsgPulse.Api.Services;

/// <summary>
/// 消息状态变更回调服务
/// </summary>
public class CallbackService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CallbackService> _logger;

    public CallbackService(IHttpClientFactory httpClientFactory, ILogger<CallbackService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 推送消息状态变更回调
    /// </summary>
    public async Task PushCallbackAsync(MessageRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.CallbackUrl))
        {
            return;
        }

        try
        {
            var callbackData = new
            {
                taskId = record.TaskId,
                messageType = record.MessageType,
                templateCode = record.TemplateCode,
                recipient = record.Recipient,
                sendStatus = record.SendStatus,
                sendTime = record.SendTime,
                completeTime = record.CompleteTime,
                failureReason = record.FailureReason,
                retryCount = record.RetryCount,
                manufacturerId = record.ManufacturerId,
                timestamp = DateTime.UtcNow
            };

            var jsonContent = JsonSerializer.Serialize(callbackData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.PostAsync(record.CallbackUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("回调推送成功: TaskId={TaskId}, CallbackUrl={CallbackUrl}",
                    record.TaskId, record.CallbackUrl);
            }
            else
            {
                _logger.LogWarning("回调推送失败: TaskId={TaskId}, StatusCode={StatusCode}",
                    record.TaskId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回调推送异常: TaskId={TaskId}, CallbackUrl={CallbackUrl}",
                record.TaskId, record.CallbackUrl);
        }
    }
}
