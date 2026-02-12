using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;

namespace MsgPulse.Api.Services;

/// <summary>
/// 速率限制服务 - 使用滑动窗口算法
/// </summary>
public class RateLimitingService
{
    private readonly MsgPulseDbContext _db;
    private readonly ILogger<RateLimitingService> _logger;

    // 内存中的请求时间戳队列 (ManufacturerId -> 请求时间戳列表)
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requestTimestamps = new();

    // 速率限制配置缓存 (避免频繁查询数据库)
    private readonly ConcurrentDictionary<int, RateLimitConfig> _configCache = new();
    private RateLimitConfig? _globalConfig;
    private DateTime _lastCacheRefresh = DateTime.MinValue;
    private readonly TimeSpan _cacheRefreshInterval = TimeSpan.FromMinutes(1);

    // 锁对象
    private readonly object _cacheLock = new();

    public RateLimitingService(MsgPulseDbContext db, ILogger<RateLimitingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 检查是否允许发送消息 (考虑全局和厂商级别的限流)
    /// </summary>
    public async Task<RateLimitResult> CheckRateLimitAsync(int manufacturerId)
    {
        await RefreshConfigCacheIfNeededAsync();

        var now = DateTime.UtcNow;

        // 1. 检查全局限流
        if (_globalConfig != null && _globalConfig.IsEnabled)
        {
            var globalResult = CheckLimit("global", _globalConfig, now);
            if (!globalResult.IsAllowed)
            {
                return globalResult;
            }
        }

        // 2. 检查厂商级别限流
        if (_configCache.TryGetValue(manufacturerId, out var config) && config.IsEnabled)
        {
            var manufacturerResult = CheckLimit($"manufacturer_{manufacturerId}", config, now);
            if (!manufacturerResult.IsAllowed)
            {
                return manufacturerResult;
            }
        }

        // 3. 记录本次请求
        RecordRequest("global", now);
        RecordRequest($"manufacturer_{manufacturerId}", now);

        return new RateLimitResult { IsAllowed = true };
    }

    /// <summary>
    /// 检查特定限流配置
    /// </summary>
    private RateLimitResult CheckLimit(string key, RateLimitConfig config, DateTime now)
    {
        var queue = _requestTimestamps.GetOrAdd(key, _ => new ConcurrentQueue<DateTime>());

        // 清理过期的时间戳
        CleanupExpiredTimestamps(queue, now, TimeSpan.FromHours(1));

        // 检查每秒限制
        if (config.RequestsPerSecond > 0)
        {
            var countPerSecond = queue.Count(t => (now - t).TotalSeconds < 1);
            if (countPerSecond >= config.RequestsPerSecond)
            {
                return new RateLimitResult
                {
                    IsAllowed = false,
                    Reason = $"超出每秒请求限制 ({config.RequestsPerSecond})",
                    RetryAfterSeconds = 1
                };
            }
        }

        // 检查每分钟限制
        if (config.RequestsPerMinute > 0)
        {
            var countPerMinute = queue.Count(t => (now - t).TotalMinutes < 1);
            if (countPerMinute >= config.RequestsPerMinute)
            {
                var oldestInWindow = queue.Where(t => (now - t).TotalMinutes < 1).Min();
                var retryAfter = (int)Math.Ceiling(60 - (now - oldestInWindow).TotalSeconds);
                return new RateLimitResult
                {
                    IsAllowed = false,
                    Reason = $"超出每分钟请求限制 ({config.RequestsPerMinute})",
                    RetryAfterSeconds = retryAfter
                };
            }
        }

        // 检查每小时限制
        if (config.RequestsPerHour > 0)
        {
            var countPerHour = queue.Count(t => (now - t).TotalHours < 1);
            if (countPerHour >= config.RequestsPerHour)
            {
                var oldestInWindow = queue.Where(t => (now - t).TotalHours < 1).Min();
                var retryAfter = (int)Math.Ceiling(3600 - (now - oldestInWindow).TotalSeconds);
                return new RateLimitResult
                {
                    IsAllowed = false,
                    Reason = $"超出每小时请求限制 ({config.RequestsPerHour})",
                    RetryAfterSeconds = retryAfter
                };
            }
        }

        return new RateLimitResult { IsAllowed = true };
    }

    /// <summary>
    /// 记录请求时间戳
    /// </summary>
    private void RecordRequest(string key, DateTime timestamp)
    {
        var queue = _requestTimestamps.GetOrAdd(key, _ => new ConcurrentQueue<DateTime>());
        queue.Enqueue(timestamp);
    }

    /// <summary>
    /// 清理过期的时间戳
    /// </summary>
    private void CleanupExpiredTimestamps(ConcurrentQueue<DateTime> queue, DateTime now, TimeSpan maxAge)
    {
        while (queue.TryPeek(out var oldest) && (now - oldest) > maxAge)
        {
            queue.TryDequeue(out _);
        }
    }

    /// <summary>
    /// 刷新配置缓存
    /// </summary>
    private async Task RefreshConfigCacheIfNeededAsync()
    {
        if ((DateTime.UtcNow - _lastCacheRefresh) < _cacheRefreshInterval)
        {
            return;
        }

        lock (_cacheLock)
        {
            // 双重检查锁定
            if ((DateTime.UtcNow - _lastCacheRefresh) < _cacheRefreshInterval)
            {
                return;
            }

            try
            {
                var configs = _db.RateLimitConfigs.ToList();

                _configCache.Clear();
                _globalConfig = null;

                foreach (var config in configs)
                {
                    if (config.ManufacturerId.HasValue)
                    {
                        _configCache[config.ManufacturerId.Value] = config;
                    }
                    else
                    {
                        _globalConfig = config;
                    }
                }

                _lastCacheRefresh = DateTime.UtcNow;
                _logger.LogInformation("速率限制配置缓存已刷新，共加载 {Count} 条配置", configs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新速率限制配置缓存失败");
            }
        }
    }

    /// <summary>
    /// 手动刷新配置缓存
    /// </summary>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _lastCacheRefresh = DateTime.MinValue;
        }
    }
}

/// <summary>
/// 速率限制检查结果
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// 是否允许
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// 拒绝原因
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 建议重试延迟(秒)
    /// </summary>
    public int RetryAfterSeconds { get; set; }
}
