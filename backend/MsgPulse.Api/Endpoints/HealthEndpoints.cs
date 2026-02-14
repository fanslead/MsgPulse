using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;
using MsgPulse.Api.Services;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 系统健康检查端点
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// 注册健康检查相关的所有端点
    /// </summary>
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health").WithTags("系统健康检查");

        // 基础健康检查
        group.MapGet("/", GetHealth)
            .WithName("GetHealth")
            .WithSummary("基础健康检查")
            .WithDescription("检查API服务是否正常运行");

        // 详细健康检查
        group.MapGet("/detailed", GetDetailedHealth)
            .WithName("GetDetailedHealth")
            .WithSummary("详细健康检查")
            .WithDescription("检查数据库、队列、厂商连接等系统组件状态");
    }

    private static IResult GetHealth()
    {
        var data = new
        {
            status = "healthy",
            timestamp = DateTime.Now,
            version = "1.0.0"
        };

        return Results.Ok(ApiResponse.Success(data));
    }

    private static async Task<IResult> GetDetailedHealth(
        MsgPulseDbContext db,
        BackgroundMessageQueue queue)
    {
        var checks = new Dictionary<string, object>();

        // 数据库健康检查
        try
        {
            var canConnect = await db.Database.CanConnectAsync();
            var recordCount = await db.MessageRecords.CountAsync();

            checks["database"] = new
            {
                status = canConnect ? "healthy" : "unhealthy",
                canConnect,
                recordCount,
                message = canConnect ? "数据库连接正常" : "数据库连接失败"
            };
        }
        catch (Exception ex)
        {
            checks["database"] = new
            {
                status = "unhealthy",
                message = $"数据库检查失败: {ex.Message}"
            };
        }

        // 消息队列健康检查
        try
        {
            var queueCount = queue.Count;
            var queueHealth = queueCount < 900 ? "healthy" : "warning"; // 容量90%时警告

            checks["messageQueue"] = new
            {
                status = queueHealth,
                queuedCount = queueCount,
                capacity = 1000,
                message = queueHealth == "healthy" ? "队列运行正常" : "队列接近容量上限"
            };
        }
        catch (Exception ex)
        {
            checks["messageQueue"] = new
            {
                status = "unhealthy",
                message = $"队列检查失败: {ex.Message}"
            };
        }

        // 厂商连接状态检查
        try
        {
            var manufacturers = await db.Manufacturers
                .Where(m => m.IsActive)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.ProviderType,
                    m.IsActive
                })
                .ToListAsync();

            checks["manufacturers"] = new
            {
                status = "healthy",
                totalActive = manufacturers.Count,
                manufacturers = manufacturers.Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.ProviderType,
                    status = m.IsActive ? "active" : "inactive"
                })
            };
        }
        catch (Exception ex)
        {
            checks["manufacturers"] = new
            {
                status = "unhealthy",
                message = $"厂商检查失败: {ex.Message}"
            };
        }

        // 最近消息处理情况
        try
        {
            var recentMinutes = 5;
            var recentTime = DateTime.Now.AddMinutes(-recentMinutes);
            var recentMessages = await db.MessageRecords
                .Where(m => m.CreatedAt >= recentTime)
                .GroupBy(m => m.SendStatus)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();

            checks["recentMessages"] = new
            {
                status = "healthy",
                timeRange = $"最近{recentMinutes}分钟",
                messages = recentMessages
            };
        }
        catch (Exception ex)
        {
            checks["recentMessages"] = new
            {
                status = "unhealthy",
                message = $"消息统计失败: {ex.Message}"
            };
        }

        // 整体健康状态
        var overallHealthy = checks.Values
            .OfType<dynamic>()
            .All(c => c.GetType().GetProperty("status")?.GetValue(c)?.ToString() != "unhealthy");

        var result = new
        {
            status = overallHealthy ? "healthy" : "unhealthy",
            timestamp = DateTime.Now,
            checks
        };

        return Results.Ok(ApiResponse.Success(result));
    }
}
