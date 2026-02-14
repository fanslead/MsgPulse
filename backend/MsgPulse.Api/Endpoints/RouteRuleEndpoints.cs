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

        // 测试路由规则
        group.MapPost("/test", TestRouteRule)
            .WithName("TestRouteRule")
            .WithSummary("测试路由规则")
            .WithDescription("输入测试条件,验证规则是否匹配成功,并返回匹配的目标厂商");
    }

    private static async Task<IResult> GetRouteRules(
        MsgPulseDbContext db,
        string? messageType,
        bool? isActive)
    {
        var query = db.RouteRules
            .Include(r => r.TargetManufacturer)
            .Include(r => r.TargetChannel)
            .AsQueryable();

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
            .Include(r => r.TargetChannel)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
            return Results.Ok(ApiResponse.Error(404, "路由规则不存在"));

        return Results.Ok(ApiResponse.Success(rule));
    }

    private static async Task<IResult> CreateRouteRule(
        RouteRule rule,
        MsgPulseDbContext db)
    {
        // 验证至少设置了一个目标（Manufacturer或Channel）
        if (!rule.TargetManufacturerId.HasValue && !rule.TargetChannelId.HasValue)
            return Results.Ok(ApiResponse.Error(400, "必须指定目标厂商或目标渠道"));

        // 验证目标Manufacturer存在且启用
        if (rule.TargetManufacturerId.HasValue &&
            !await db.Manufacturers.AnyAsync(m => m.Id == rule.TargetManufacturerId && m.IsActive))
            return Results.Ok(ApiResponse.Error(400, "目标厂商不存在或未启用"));

        // 验证目标Channel存在且启用
        if (rule.TargetChannelId.HasValue &&
            !await db.Channels.AnyAsync(c => c.Id == rule.TargetChannelId && c.IsActive))
            return Results.Ok(ApiResponse.Error(400, "目标渠道不存在或未启用"));

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

        // 验证至少设置了一个目标
        if (!updatedRule.TargetManufacturerId.HasValue && !updatedRule.TargetChannelId.HasValue)
            return Results.Ok(ApiResponse.Error(400, "必须指定目标厂商或目标渠道"));

        // 验证目标Manufacturer存在（如果设置了）
        if (updatedRule.TargetManufacturerId.HasValue &&
            !await db.Manufacturers.AnyAsync(m => m.Id == updatedRule.TargetManufacturerId))
            return Results.Ok(ApiResponse.Error(400, "目标厂商不存在"));

        // 验证目标Channel存在（如果设置了）
        if (updatedRule.TargetChannelId.HasValue &&
            !await db.Channels.AnyAsync(c => c.Id == updatedRule.TargetChannelId))
            return Results.Ok(ApiResponse.Error(400, "目标渠道不存在"));

        rule.Name = updatedRule.Name;
        rule.MessageType = updatedRule.MessageType;
        rule.MatchConditions = updatedRule.MatchConditions;
        rule.TargetManufacturerId = updatedRule.TargetManufacturerId;
        rule.TargetChannelId = updatedRule.TargetChannelId;
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

    /// <summary>
    /// 测试路由规则匹配
    /// </summary>
    private static async Task<IResult> TestRouteRule(
        TestRouteRuleRequest request,
        MsgPulseDbContext db)
    {
        // 获取所有启用的路由规则,按优先级排序
        var rules = await db.RouteRules
            .Include(r => r.TargetManufacturer)
            .Include(r => r.TargetChannel)
            .Where(r => r.MessageType == request.MessageType && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();

        if (rules.Count == 0)
        {
            return Results.Ok(ApiResponse.Error(404, $"没有找到适用于{request.MessageType}的路由规则"));
        }

        // 遍历规则,找到第一个匹配的
        foreach (var rule in rules)
        {
            // 优先检查Channel，其次检查Manufacturer
            if (rule.TargetChannel?.IsActive == true)
            {
                return Results.Ok(ApiResponse.Success(new
                {
                    matched = true,
                    ruleId = rule.Id,
                    ruleName = rule.Name,
                    priority = rule.Priority,
                    targetType = "Channel",
                    targetChannel = new
                    {
                        id = rule.TargetChannel.Id,
                        name = rule.TargetChannel.Name,
                        code = rule.TargetChannel.Code,
                        channelType = rule.TargetChannel.ChannelType.ToString()
                    },
                    matchConditions = rule.MatchConditions
                }, $"匹配成功:规则「{rule.Name}」,目标渠道「{rule.TargetChannel.Name}」"));
            }
            else if (rule.TargetManufacturer?.IsActive == true)
            {
                return Results.Ok(ApiResponse.Success(new
                {
                    matched = true,
                    ruleId = rule.Id,
                    ruleName = rule.Name,
                    priority = rule.Priority,
                    targetType = "Manufacturer",
                    targetManufacturer = new
                    {
                        id = rule.TargetManufacturer.Id,
                        name = rule.TargetManufacturer.Name,
                        code = rule.TargetManufacturer.Code,
                        providerType = rule.TargetManufacturer.ProviderType.ToString()
                    },
                    matchConditions = rule.MatchConditions
                }, $"匹配成功:规则「{rule.Name}」,目标厂商「{rule.TargetManufacturer.Name}」"));
            }
        }

        return Results.Ok(ApiResponse.Error(404, "未找到可用的匹配规则(所有规则的目标厂商均未启用)"));
    }
}

/// <summary>
/// 测试路由规则请求
/// </summary>
public record TestRouteRuleRequest(
    string MessageType,
    string? TemplateCode,
    string? Recipient,
    string? CustomTag
);
