using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 路由规则管理端点
/// </summary>
public static class RouteRuleEndpoints
{
    /// <summary>
    /// 注册路由规则相关的所有端点
    /// </summary>
    public static void MapRouteRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/route-rules").WithTags("路由规则管理");

        // 获取路由规则列表
        group.MapGet("/", GetRouteRules)
            .WithName("GetRouteRules")
            .WithSummary("获取路由规则列表")
            .WithDescription("支持按消息类型、状态筛选路由规则");

        // 获取单个路由规则
        group.MapGet("/{id}", GetRouteRule)
            .WithName("GetRouteRule")
            .WithSummary("获取路由规则详情")
            .WithDescription("根据ID获取单个路由规则的详细信息");

        // 创建路由规则
        group.MapPost("/", CreateRouteRule)
            .WithName("CreateRouteRule")
            .WithSummary("创建路由规则")
            .WithDescription("创建新的路由规则配置");

        // 更新路由规则
        group.MapPut("/{id}", UpdateRouteRule)
            .WithName("UpdateRouteRule")
            .WithSummary("更新路由规则")
            .WithDescription("更新现有路由规则的配置信息");

        // 删除路由规则
        group.MapDelete("/{id}", DeleteRouteRule)
            .WithName("DeleteRouteRule")
            .WithSummary("删除路由规则")
            .WithDescription("删除指定的路由规则（需确保没有关联记录）");
    }

    private static async Task<IResult> GetRouteRules(
        MsgPulseDbContext db,
        string? messageType,
        bool? isActive)
    {
        var query = db.RouteRules.Include(r => r.TargetManufacturer).AsQueryable();

        if (!string.IsNullOrEmpty(messageType))
            query = query.Where(r => r.MessageType == messageType);

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        var rules = await query.OrderBy(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Results.Ok(ApiResponse.Success(rules));
    }

    private static async Task<IResult> GetRouteRule(int id, MsgPulseDbContext db)
    {
        var rule = await db.RouteRules
            .Include(r => r.TargetManufacturer)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
            return Results.Ok(ApiResponse.Error(404, "路由规则不存在"));

        return Results.Ok(ApiResponse.Success(rule));
    }

    private static async Task<IResult> CreateRouteRule(
        RouteRule rule,
        MsgPulseDbContext db)
    {
        if (!await db.Manufacturers.AnyAsync(m => m.Id == rule.TargetManufacturerId && m.IsActive))
            return Results.Ok(ApiResponse.Error(400, "目标厂商不存在或未启用"));

        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;
        db.RouteRules.Add(rule);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(new { id = rule.Id }, "路由规则创建成功"));
    }

    private static async Task<IResult> UpdateRouteRule(
        int id,
        RouteRule updatedRule,
        MsgPulseDbContext db)
    {
        var rule = await db.RouteRules.FindAsync(id);
        if (rule == null)
            return Results.Ok(ApiResponse.Error(404, "路由规则不存在"));

        if (!await db.Manufacturers.AnyAsync(m => m.Id == updatedRule.TargetManufacturerId))
            return Results.Ok(ApiResponse.Error(400, "目标厂商不存在"));

        rule.Name = updatedRule.Name;
        rule.MessageType = updatedRule.MessageType;
        rule.MatchConditions = updatedRule.MatchConditions;
        rule.TargetManufacturerId = updatedRule.TargetManufacturerId;
        rule.Priority = updatedRule.Priority;
        rule.IsActive = updatedRule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "路由规则更新成功"));
    }

    private static async Task<IResult> DeleteRouteRule(int id, MsgPulseDbContext db)
    {
        var rule = await db.RouteRules.FindAsync(id);
        if (rule == null)
            return Results.Ok(ApiResponse.Error(404, "路由规则不存在"));

        if (await db.MessageRecords.AnyAsync(m => m.RouteRuleId == id))
            return Results.Ok(ApiResponse.Error(400, "存在关联记录，无法删除"));

        db.RouteRules.Remove(rule);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "路由规则删除成功"));
    }
}
