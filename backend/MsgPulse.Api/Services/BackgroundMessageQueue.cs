using System.Threading.Channels;

namespace MsgPulse.Api.Services;

/// <summary>
/// 后台消息队列 - 使用 Channel<T> 实现高性能内存队列
/// </summary>
public class BackgroundMessageQueue
{
    private readonly Channel<QueuedMessage> _queue;
    private readonly ILogger<BackgroundMessageQueue> _logger;

    public BackgroundMessageQueue(ILogger<BackgroundMessageQueue> logger, int capacity = 1000)
    {
        _logger = logger;

        // 创建有界队列,当队列满时等待
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _queue = Channel.CreateBounded<QueuedMessage>(options);
    }

    /// <summary>
    /// 将消息加入队列
    /// </summary>
    public async ValueTask<bool> EnqueueAsync(QueuedMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _queue.Writer.WriteAsync(message, cancellationToken);
            _logger.LogDebug("消息已加入队列: TaskId={TaskId}, MessageType={MessageType}",
                message.TaskId, message.MessageType);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("消息入队被取消: TaskId={TaskId}", message.TaskId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息入队失败: TaskId={TaskId}", message.TaskId);
            return false;
        }
    }

    /// <summary>
    /// 从队列中取出消息
    /// </summary>
    public async ValueTask<QueuedMessage?> DequeueAsync(CancellationToken cancellationToken)
    {
        try
        {
            var message = await _queue.Reader.ReadAsync(cancellationToken);
            _logger.LogDebug("消息已从队列取出: TaskId={TaskId}", message.TaskId);
            return message;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// 获取队列中待处理的消息数量
    /// </summary>
    public int Count => _queue.Reader.Count;

    /// <summary>
    /// 标记队列完成(不再接受新消息)
    /// </summary>
    public void CompleteAdding()
    {
        _queue.Writer.Complete();
        _logger.LogInformation("消息队列已标记为完成,不再接受新消息");
    }
}

/// <summary>
/// 队列消息
/// </summary>
public class QueuedMessage
{
    /// <summary>
    /// 消息记录ID
    /// </summary>
    public int MessageRecordId { get; set; }

    /// <summary>
    /// 任务ID
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public required string MessageType { get; set; }

    /// <summary>
    /// 优先级 (数字越小优先级越高, 默认5为普通优先级)
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 入队时间
    /// </summary>
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 0;
}
