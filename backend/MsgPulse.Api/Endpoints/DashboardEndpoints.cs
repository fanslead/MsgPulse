using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 仪表盘统计端点
/// </summary>
public static class DashboardEndpoints
{
    /// <summary>
    /// 注册仪表盘相关的所有端点
    /// </summary>
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("仪表盘统计");

        // 获取总体统计数据
        group.MapGet("/overview", GetOverview)
            .WithName("GetDashboardOverview")
            .WithSummary("获取仪表盘总览数据")
            .WithDescription("获取消息发送总量、成功率、失败率等统计数据");

        // 获取按时间维度的统计
        group.MapGet("/timeline", GetTimeline)
            .WithName("GetDashboardTimeline")
            .WithSummary("获取时间维度统计")
            .WithDescription("获取指定时间范围内的消息发送趋势数据");

        // 获取按厂商维度的统计
        group.MapGet("/manufacturers", GetManufacturerStats)
            .WithName("GetManufacturerStats")
            .WithSummary("获取厂商维度统计")
            .WithDescription("获取各厂商的消息发送量和成功率统计");

        // 获取按消息类型维度的统计
        group.MapGet("/message-types", GetMessageTypeStats)
            .WithName("GetMessageTypeStats")
            .WithSummary("获取消息类型统计")
            .WithDescription("获取各类型消息（SMS/Email/AppPush）的发送统计");
    }

    private static async Task<IResult> GetOverview(
        MsgPulseDbContext db,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        var query = db.MessageRecords.AsQueryable();

        // 默认查询最近7天
        if (!startTime.HasValue)
            startTime = DateTime.Now.AddDays(-7);
        if (!endTime.HasValue)
            endTime = DateTime.Now;

        query = query.Where(m => m.CreatedAt >= startTime && m.CreatedAt <= endTime);

        var total = await query.CountAsync();
        var successCount = await query.CountAsync(m => m.SendStatus == "成功");
        var failureCount = await query.CountAsync(m => m.SendStatus == "失败");
        var pendingCount = await query.CountAsync(m => m.SendStatus == "队列中" || m.SendStatus == "发送中");

        var successRate = total > 0 ? (double)successCount / total * 100 : 0;
        var failureRate = total > 0 ? (double)failureCount / total * 100 : 0;

        // 今日统计
        var todayStart = DateTime.Today;
        var todayQuery = db.MessageRecords.Where(m => m.CreatedAt >= todayStart);
        var todayTotal = await todayQuery.CountAsync();
        var todaySuccess = await todayQuery.CountAsync(m => m.SendStatus == "成功");

        var data = new
        {
            total,
            successCount,
            failureCount,
            pendingCount,
            successRate = Math.Round(successRate, 2),
            failureRate = Math.Round(failureRate, 2),
            todayTotal,
            todaySuccess,
            todaySuccessRate = todayTotal > 0 ? Math.Round((double)todaySuccess / todayTotal * 100, 2) : 0,
            timeRange = new
            {
                start = startTime,
                end = endTime
            }
        };

        return Results.Ok(ApiResponse.Success(data));
    }

    private static async Task<IResult> GetTimeline(
        MsgPulseDbContext db,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string granularity = "day")
    {
        // 默认查询最近7天
        if (!startTime.HasValue)
            startTime = DateTime.Now.AddDays(-7);
        if (!endTime.HasValue)
            endTime = DateTime.Now;

        var query = db.MessageRecords
            .Where(m => m.CreatedAt >= startTime && m.CreatedAt <= endTime)
            .AsQueryable();

        var messages = await query.ToListAsync();

        // 按照粒度分组
        var timeline = granularity.ToLower() switch
        {
            "hour" => messages
                .GroupBy(m => new { m.CreatedAt.Date, m.CreatedAt.Hour })
                .Select(g => new
                {
                    time = new DateTime(g.Key.Date.Year, g.Key.Date.Month, g.Key.Date.Day, g.Key.Hour, 0, 0),
                    total = g.Count(),
                    success = g.Count(m => m.SendStatus == "成功"),
                    failure = g.Count(m => m.SendStatus == "失败"),
                    pending = g.Count(m => m.SendStatus == "队列中" || m.SendStatus == "发送中")
                })
                .OrderBy(x => x.time)
                .ToList(),
            _ => messages
                .GroupBy(m => m.CreatedAt.Date)
                .Select(g => new
                {
                    time = g.Key,
                    total = g.Count(),
                    success = g.Count(m => m.SendStatus == "成功"),
                    failure = g.Count(m => m.SendStatus == "失败"),
                    pending = g.Count(m => m.SendStatus == "队列中" || m.SendStatus == "发送中")
                })
                .OrderBy(x => x.time)
                .ToList()
        };

        return Results.Ok(ApiResponse.Success(timeline));
    }

    private static async Task<IResult> GetManufacturerStats(
        MsgPulseDbContext db,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        // 默认查询最近7天
        if (!startTime.HasValue)
            startTime = DateTime.Now.AddDays(-7);
        if (!endTime.HasValue)
            endTime = DateTime.Now;

        var stats = await db.MessageRecords
            .Where(m => m.CreatedAt >= startTime && m.CreatedAt <= endTime)
            .GroupBy(m => new { m.ManufacturerId, m.Manufacturer!.Name })
            .Select(g => new
            {
                manufacturerId = g.Key.ManufacturerId,
                manufacturerName = g.Key.Name,
                total = g.Count(),
                success = g.Count(m => m.SendStatus == "成功"),
                failure = g.Count(m => m.SendStatus == "失败"),
                pending = g.Count(m => m.SendStatus == "队列中" || m.SendStatus == "发送中"),
                successRate = g.Count() > 0 ? Math.Round((double)g.Count(m => m.SendStatus == "成功") / g.Count() * 100, 2) : 0
            })
            .OrderByDescending(x => x.total)
            .ToListAsync();

        return Results.Ok(ApiResponse.Success(stats));
    }

    private static async Task<IResult> GetMessageTypeStats(
        MsgPulseDbContext db,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        // 默认查询最近7天
        if (!startTime.HasValue)
            startTime = DateTime.Now.AddDays(-7);
        if (!endTime.HasValue)
            endTime = DateTime.Now;

        var stats = await db.MessageRecords
            .Where(m => m.CreatedAt >= startTime && m.CreatedAt <= endTime)
            .GroupBy(m => m.MessageType)
            .Select(g => new
            {
                messageType = g.Key,
                total = g.Count(),
                success = g.Count(m => m.SendStatus == "成功"),
                failure = g.Count(m => m.SendStatus == "失败"),
                pending = g.Count(m => m.SendStatus == "队列中" || m.SendStatus == "发送中"),
                successRate = g.Count() > 0 ? Math.Round((double)g.Count(m => m.SendStatus == "成功") / g.Count() * 100, 2) : 0
            })
            .OrderByDescending(x => x.total)
            .ToListAsync();

        return Results.Ok(ApiResponse.Success(stats));
    }
}
