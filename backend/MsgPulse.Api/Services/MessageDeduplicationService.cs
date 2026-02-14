using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MsgPulse.Api.Services;

/// <summary>
/// 消息去重服务 - 防止短时间内重复发送相同消息
/// </summary>
public class MessageDeduplicationService
{
    private readonly ConcurrentDictionary<string, DateTime> _dedupeCache;
    private readonly ILogger<MessageDeduplicationService> _logger;
    private readonly TimeSpan _defaultWindow;

    public MessageDeduplicationService(
        ILogger<MessageDeduplicationService> logger,
        int defaultWindowMinutes = 5)
    {
        _logger = logger;
        _dedupeCache = new ConcurrentDictionary<string, DateTime>();
        _defaultWindow = TimeSpan.FromMinutes(defaultWindowMinutes);

        // 启动后台清理任务
        _ = StartCleanupTask();
    }

    /// <summary>
    /// 检查消息是否重复
    /// </summary>
    /// <param name="messageType">消息类型</param>
    /// <param name="recipient">接收方</param>
    /// <param name="templateCode">模板编码</param>
    /// <param name="variables">变量值</param>
    /// <param name="window">去重时间窗口（可选）</param>
    /// <returns>是否重复</returns>
    public bool IsDuplicate(
        string messageType,
        string recipient,
        string templateCode,
        Dictionary<string, string>? variables = null,
        TimeSpan? window = null)
    {
        var dedupeKey = GenerateDedupeKey(messageType, recipient, templateCode, variables);
        var timeWindow = window ?? _defaultWindow;

        if (_dedupeCache.TryGetValue(dedupeKey, out var lastSentTime))
        {
            var elapsed = DateTime.Now - lastSentTime;
            if (elapsed < timeWindow)
            {
                _logger.LogWarning(
                    "检测到重复消息: Type={MessageType}, Recipient={Recipient}, Template={TemplateCode}, TimeElapsed={Elapsed}s",
                    messageType, recipient, templateCode, elapsed.TotalSeconds);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 记录消息发送（用于去重）
    /// </summary>
    public void RecordMessage(
        string messageType,
        string recipient,
        string templateCode,
        Dictionary<string, string>? variables = null)
    {
        var dedupeKey = GenerateDedupeKey(messageType, recipient, templateCode, variables);
        _dedupeCache[dedupeKey] = DateTime.Now;

        _logger.LogDebug(
            "记录消息去重键: Type={MessageType}, Recipient={Recipient}, Template={TemplateCode}",
            messageType, recipient, templateCode);
    }

    /// <summary>
    /// 清除指定消息的去重记录（用于手动重试）
    /// </summary>
    public void ClearDedupeRecord(
        string messageType,
        string recipient,
        string templateCode,
        Dictionary<string, string>? variables = null)
    {
        var dedupeKey = GenerateDedupeKey(messageType, recipient, templateCode, variables);
        _dedupeCache.TryRemove(dedupeKey, out _);

        _logger.LogInformation(
            "清除消息去重记录: Type={MessageType}, Recipient={Recipient}, Template={TemplateCode}",
            messageType, recipient, templateCode);
    }

    /// <summary>
    /// 获取去重缓存统计信息
    /// </summary>
    public DeduplicationStats GetStats()
    {
        var now = DateTime.Now;
        var activeCount = _dedupeCache.Count(kv => now - kv.Value < _defaultWindow);

        return new DeduplicationStats
        {
            TotalKeys = _dedupeCache.Count,
            ActiveKeys = activeCount,
            ExpiredKeys = _dedupeCache.Count - activeCount,
            DefaultWindowMinutes = (int)_defaultWindow.TotalMinutes
        };
    }

    /// <summary>
    /// 生成去重键
    /// </summary>
    private string GenerateDedupeKey(
        string messageType,
        string recipient,
        string templateCode,
        Dictionary<string, string>? variables)
    {
        // 构建去重键内容
        var keyContent = new
        {
            MessageType = messageType.ToLowerInvariant(),
            Recipient = recipient.ToLowerInvariant(),
            TemplateCode = templateCode.ToLowerInvariant(),
            Variables = variables?.OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        var json = JsonSerializer.Serialize(keyContent);

        // 使用SHA256生成哈希键
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// 后台清理过期的去重记录
    /// </summary>
    private async Task StartCleanupTask()
    {
        await Task.Yield(); // 确保异步启动

        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1)); // 每分钟清理一次

                var now = DateTime.Now;
                var expiredKeys = _dedupeCache
                    .Where(kv => now - kv.Value > _defaultWindow * 2) // 保留2倍窗口时间
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _dedupeCache.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("清理了 {Count} 个过期的去重记录", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理去重记录时发生错误");
            }
        }
    }
}

/// <summary>
/// 去重统计信息
/// </summary>
public class DeduplicationStats
{
    /// <summary>
    /// 缓存中的总键数
    /// </summary>
    public int TotalKeys { get; set; }

    /// <summary>
    /// 活跃的键数（未过期）
    /// </summary>
    public int ActiveKeys { get; set; }

    /// <summary>
    /// 已过期的键数
    /// </summary>
    public int ExpiredKeys { get; set; }

    /// <summary>
    /// 默认时间窗口（分钟）
    /// </summary>
    public int DefaultWindowMinutes { get; set; }
}
