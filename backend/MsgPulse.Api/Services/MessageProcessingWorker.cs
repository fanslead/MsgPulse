using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers;
using MsgPulse.Api.Providers.Models;
using System.Text.Json;

namespace MsgPulse.Api.Services;

/// <summary>
/// 消息处理后台工作服务 - 从队列消费消息并调用厂商API发送
/// </summary>
public class MessageProcessingWorker : BackgroundService
{
    private readonly BackgroundMessageQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageProcessingWorker> _logger;
    private readonly int _workerCount;

    // 自动重试配置
    private readonly int _maxRetryAttempts = 3;
    private readonly TimeSpan[] _retryDelays = new[]
    {
        TimeSpan.FromSeconds(5),   // 第1次重试: 5秒后
        TimeSpan.FromSeconds(30),  // 第2次重试: 30秒后
        TimeSpan.FromMinutes(5)    // 第3次重试: 5分钟后
    };

    public MessageProcessingWorker(
        BackgroundMessageQueue queue,
        IServiceProvider serviceProvider,
        ILogger<MessageProcessingWorker> logger,
        IConfiguration configuration)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _workerCount = configuration.GetValue<int>("MessageQueue:WorkerCount", 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("消息处理工作服务启动，工作线程数: {WorkerCount}", _workerCount);

        // 启动多个并发工作线程
        var workers = Enumerable.Range(0, _workerCount)
            .Select(i => ProcessMessagesAsync(i, stoppingToken))
            .ToArray();

        await Task.WhenAll(workers);

        _logger.LogInformation("消息处理工作服务已停止");
    }

    /// <summary>
    /// 工作线程 - 持续从队列取消息并处理
    /// </summary>
    private async Task ProcessMessagesAsync(int workerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("工作线程 {WorkerId} 已启动", workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queuedMessage = await _queue.DequeueAsync(stoppingToken);
                if (queuedMessage == null)
                {
                    continue;
                }

                _logger.LogDebug("工作线程 {WorkerId} 开始处理消息: TaskId={TaskId}",
                    workerId, queuedMessage.TaskId);

                await ProcessSingleMessageAsync(queuedMessage, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工作线程 {WorkerId} 处理消息时发生异常", workerId);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        _logger.LogInformation("工作线程 {WorkerId} 已停止", workerId);
    }

    /// <summary>
    /// 处理单个消息
    /// </summary>
    private async Task ProcessSingleMessageAsync(QueuedMessage queuedMessage, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MsgPulseDbContext>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<ProviderFactory>();
        var callbackService = scope.ServiceProvider.GetRequiredService<CallbackService>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<ConfigurationEncryptionService>();

        // 加载消息记录
        var record = await db.MessageRecords
            .Include(m => m.Manufacturer)
            .FirstOrDefaultAsync(m => m.Id == queuedMessage.MessageRecordId, cancellationToken);

        if (record == null)
        {
            _logger.LogWarning("消息记录不存在: MessageRecordId={MessageRecordId}", queuedMessage.MessageRecordId);
            return;
        }

        // 检查是否已发送成功(防止重复处理)
        if (record.SendStatus == "成功")
        {
            _logger.LogDebug("消息已成功发送，跳过: TaskId={TaskId}", record.TaskId);
            return;
        }

        // 更新状态为"发送中"
        record.SendStatus = "发送中";
        record.SendTime = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            // 调用厂商API发送
            var result = await SendMessageViaProviderAsync(record, providerFactory, db, encryptionService, cancellationToken);

            // 更新发送结果
            if (result.IsSuccess)
            {
                record.SendStatus = "成功";
                record.CompleteTime = DateTime.UtcNow;
                record.ManufacturerResponse = result.RawResponse;
                _logger.LogInformation("消息发送成功: TaskId={TaskId}, MessageType={MessageType}",
                    record.TaskId, record.MessageType);
            }
            else
            {
                await HandleSendFailureAsync(record, queuedMessage, result.ErrorMessage ?? "发送失败", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息发送异常: TaskId={TaskId}", record.TaskId);
            await HandleSendFailureAsync(record, queuedMessage, $"发送异常: {ex.Message}", cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        // 触发回调
        try
        {
            await callbackService.PushCallbackAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回调推送失败: TaskId={TaskId}", record.TaskId);
        }
    }

    /// <summary>
    /// 处理发送失败 - 自动重试或标记为失败
    /// </summary>
    private async Task HandleSendFailureAsync(
        MessageRecord record,
        QueuedMessage queuedMessage,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        record.RetryCount++;
        record.FailureReason = errorMessage;

        // 判断是否需要重试
        if (record.RetryCount <= _maxRetryAttempts)
        {
            var delayIndex = Math.Min(record.RetryCount - 1, _retryDelays.Length - 1);
            var delay = _retryDelays[delayIndex];

            _logger.LogWarning("消息发送失败，将在 {Delay} 后重试 (第 {RetryCount}/{MaxRetry} 次): TaskId={TaskId}, 原因={Reason}",
                delay, record.RetryCount, _maxRetryAttempts, record.TaskId, errorMessage);

            // 更新状态为"等待重试"
            record.SendStatus = "等待重试";

            // 延迟后重新入队
            _ = Task.Run(async () =>
            {
                await Task.Delay(delay, cancellationToken);
                queuedMessage.RetryCount = record.RetryCount;
                await _queue.EnqueueAsync(queuedMessage, cancellationToken);
            }, cancellationToken);
        }
        else
        {
            // 超过最大重试次数，标记为失败
            record.SendStatus = "失败";
            _logger.LogError("消息发送失败，已达最大重试次数: TaskId={TaskId}, 原因={Reason}",
                record.TaskId, errorMessage);
        }
    }

    /// <summary>
    /// 通过厂商Provider发送消息
    /// </summary>
    private async Task<ProviderResult> SendMessageViaProviderAsync(
        MessageRecord record,
        ProviderFactory providerFactory,
        MsgPulseDbContext db,
        ConfigurationEncryptionService encryptionService,
        CancellationToken cancellationToken)
    {
        if (record.Manufacturer == null || string.IsNullOrWhiteSpace(record.Manufacturer.Configuration))
        {
            return ProviderResult.Failure("厂商未配置");
        }

        var provider = providerFactory.GetProvider(record.Manufacturer.ProviderType);
        if (provider == null)
        {
            return ProviderResult.Failure("厂商实现不存在");
        }

        // 解密配置后初始化
        var decryptedConfiguration = encryptionService.DecryptIfNeeded(record.Manufacturer.Configuration);
        provider.Initialize(decryptedConfiguration);

        // 根据消息类型调用不同的发送方法
        if (record.MessageType == "SMS")
        {
            return await provider.SendSmsAsync(new SmsRequest
            {
                PhoneNumber = record.Recipient,
                TemplateCode = record.TemplateCode,
                TemplateParams = record.Variables
            }, cancellationToken);
        }
        else if (record.MessageType == "Email")
        {
            // 查询邮件模板
            var template = await db.EmailTemplates
                .FirstOrDefaultAsync(t => t.Code == record.TemplateCode, cancellationToken);

            if (template == null)
            {
                return ProviderResult.Failure($"邮件模板不存在: {record.TemplateCode}");
            }

            // 替换模板变量
            var content = template.Content;
            if (!string.IsNullOrWhiteSpace(record.Variables))
            {
                var variables = JsonSerializer.Deserialize<Dictionary<string, string>>(record.Variables);
                if (variables != null)
                {
                    foreach (var kv in variables)
                    {
                        content = content.Replace($"{{{{{kv.Key}}}}}", kv.Value);
                    }
                }
            }

            return await provider.SendEmailAsync(new EmailRequest
            {
                ToEmail = record.Recipient,
                Subject = template.Subject,
                Content = content,
                ContentType = template.ContentType
            }, cancellationToken);
        }
        else if (record.MessageType == "AppPush")
        {
            // 解析变量获取title和content
            var title = "通知";
            var content = "";

            if (!string.IsNullOrWhiteSpace(record.Variables))
            {
                var variables = JsonSerializer.Deserialize<Dictionary<string, string>>(record.Variables);
                if (variables != null)
                {
                    title = variables.GetValueOrDefault("title", "通知");
                    content = variables.GetValueOrDefault("content", "");
                }
            }

            return await provider.SendPushAsync(new AppPushRequest
            {
                Target = record.Recipient,
                Title = title,
                Content = content
            }, cancellationToken);
        }
        else
        {
            return ProviderResult.Failure($"不支持的消息类型: {record.MessageType}");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止消息处理工作服务...");

        // 标记队列不再接受新消息
        _queue.CompleteAdding();

        await base.StopAsync(cancellationToken);
    }
}
