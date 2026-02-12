using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;
using MsgPulse.Api.Services;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 速率限制配置端点
/// </summary>
public static class RateLimitEndpoints
{
    public static void MapRateLimitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rate-limits").WithTags("速率限制配置");

        group.MapGet("/", GetRateLimits)
            .WithName("GetRateLimits")
            .WithSummary("获取所有速率限制配置")
            .WithDescription("返回全局和厂商级别的速率限制配置");

        group.MapGet("/{id}", GetRateLimit)
            .WithName("GetRateLimit")
            .WithSummary("获取单个速率限制配置")
            .WithDescription("根据ID获取速率限制配置详情");

        group.MapGet("/manufacturer/{manufacturerId}", GetManufacturerRateLimit)
            .WithName("GetManufacturerRateLimit")
            .WithSummary("获取厂商的速率限制配置")
            .WithDescription("根据厂商ID获取其速率限制配置");

        group.MapPost("/", CreateRateLimit)
            .WithName("CreateRateLimit")
            .WithSummary("创建速率限制配置")
            .WithDescription("为全局或特定厂商创建速率限制配置");

        group.MapPut("/{id}", UpdateRateLimit)
            .WithName("UpdateRateLimit")
            .WithSummary("更新速率限制配置")
            .WithDescription("更新现有的速率限制配置");

        group.MapDelete("/{id}", DeleteRateLimit)
            .WithName("DeleteRateLimit")
            .WithSummary("删除速率限制配置")
            .WithDescription("删除速率限制配置");
    }

    private static async Task<IResult> GetRateLimits(MsgPulseDbContext db)
    {
        var configs = await db.RateLimitConfigs
            .Include(c => c.Manufacturer)
            .ToListAsync();

        var result = configs.Select(c => new
        {
            c.Id,
            c.ManufacturerId,
            manufacturerName = c.Manufacturer?.Name ?? "全局限制",
            c.RequestsPerSecond,
            c.RequestsPerMinute,
            c.RequestsPerHour,
            c.IsEnabled,
            c.CreatedAt,
            c.UpdatedAt
        });

        return Results.Ok(ApiResponse.Success(result));
    }

    private static async Task<IResult> GetRateLimit(int id, MsgPulseDbContext db)
    {
        var config = await db.RateLimitConfigs
            .Include(c => c.Manufacturer)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (config == null)
            return Results.Ok(ApiResponse.Error(404, "速率限制配置不存在"));

        return Results.Ok(ApiResponse.Success(config));
    }

    private static async Task<IResult> GetManufacturerRateLimit(int manufacturerId, MsgPulseDbContext db)
    {
        var config = await db.RateLimitConfigs
            .FirstOrDefaultAsync(c => c.ManufacturerId == manufacturerId);

        if (config == null)
            return Results.Ok(ApiResponse.Error(404, "该厂商未配置速率限制"));

        return Results.Ok(ApiResponse.Success(config));
    }

    private static async Task<IResult> CreateRateLimit(
        CreateRateLimitRequest request,
        MsgPulseDbContext db,
        RateLimitingService rateLimitingService)
    {
        // 检查是否已存在配置
        if (request.ManufacturerId.HasValue)
        {
            var existing = await db.RateLimitConfigs
                .AnyAsync(c => c.ManufacturerId == request.ManufacturerId.Value);

            if (existing)
                return Results.Ok(ApiResponse.Error(400, "该厂商已存在速率限制配置"));
        }
        else
        {
            var existingGlobal = await db.RateLimitConfigs.AnyAsync(c => c.ManufacturerId == null);
            if (existingGlobal)
                return Results.Ok(ApiResponse.Error(400, "全局速率限制配置已存在"));
        }

        var config = new RateLimitConfig
        {
            ManufacturerId = request.ManufacturerId,
            RequestsPerSecond = request.RequestsPerSecond,
            RequestsPerMinute = request.RequestsPerMinute,
            RequestsPerHour = request.RequestsPerHour,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.RateLimitConfigs.Add(config);
        await db.SaveChangesAsync();

        // 使缓存失效
        rateLimitingService.InvalidateCache();

        return Results.Ok(ApiResponse.Success(config, "速率限制配置创建成功"));
    }

    private static async Task<IResult> UpdateRateLimit(
        int id,
        UpdateRateLimitRequest request,
        MsgPulseDbContext db,
        RateLimitingService rateLimitingService)
    {
        var config = await db.RateLimitConfigs.FindAsync(id);
        if (config == null)
            return Results.Ok(ApiResponse.Error(404, "速率限制配置不存在"));

        config.RequestsPerSecond = request.RequestsPerSecond;
        config.RequestsPerMinute = request.RequestsPerMinute;
        config.RequestsPerHour = request.RequestsPerHour;
        config.IsEnabled = request.IsEnabled;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // 使缓存失效
        rateLimitingService.InvalidateCache();

        return Results.Ok(ApiResponse.Success(config, "速率限制配置更新成功"));
    }

    private static async Task<IResult> DeleteRateLimit(
        int id,
        MsgPulseDbContext db,
        RateLimitingService rateLimitingService)
    {
        var config = await db.RateLimitConfigs.FindAsync(id);
        if (config == null)
            return Results.Ok(ApiResponse.Error(404, "速率限制配置不存在"));

        db.RateLimitConfigs.Remove(config);
        await db.SaveChangesAsync();

        // 使缓存失效
        rateLimitingService.InvalidateCache();

        return Results.Ok(ApiResponse.Success(message: "速率限制配置删除成功"));
    }
}

public record CreateRateLimitRequest(
    int? ManufacturerId,
    int RequestsPerSecond,
    int RequestsPerMinute,
    int RequestsPerHour,
    bool IsEnabled
);

public record UpdateRateLimitRequest(
    int RequestsPerSecond,
    int RequestsPerMinute,
    int RequestsPerHour,
    bool IsEnabled
);
