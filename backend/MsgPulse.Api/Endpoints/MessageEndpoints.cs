using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;
using System.Text.Json;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 消息管理端点
/// </summary>
public static class MessageEndpoints
{
    /// <summary>
    /// 注册消息相关的所有端点
    /// </summary>
    public static void MapMessageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/messages").WithTags("消息管理");

        // 发送消息
        group.MapPost("/send", SendMessage)
            .WithName("SendMessage")
            .WithSummary("发送消息")
            .WithDescription("根据路由规则发送消息（短信/邮件/APP推送）");

        // 获取消息记录列表
        group.MapGet("/", GetMessages)
            .WithName("GetMessages")
            .WithSummary("获取消息记录列表")
            .WithDescription("支持多条件筛选和分页查询消息发送记录");

        // 获取单个消息记录
        group.MapGet("/{id}", GetMessage)
            .WithName("GetMessage")
            .WithSummary("获取消息记录详情")
            .WithDescription("根据ID获取单个消息记录的详细信息");

        // 重试失败消息
        group.MapPost("/{id}/retry", RetryMessage)
            .WithName("RetryMessage")
            .WithSummary("重试失败消息")
            .WithDescription("重新发送失败状态的消息");
    }

    private static async Task<IResult> SendMessage(
        MessageSendRequest request,
        MsgPulseDbContext db)
    {
        var taskId = Guid.NewGuid().ToString();

        // 根据消息类型查找匹配的路由规则
        var rules = await db.RouteRules
            .Include(r => r.TargetManufacturer)
            .Where(r => r.MessageType == request.MessageType && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();

        int? selectedManufacturerId = null;
        int? selectedRuleId = null;

        // 选择第一个有效的路由规则
        foreach (var rule in rules)
        {
            if (rule.TargetManufacturer?.IsActive == true)
            {
                selectedManufacturerId = rule.TargetManufacturerId;
                selectedRuleId = rule.Id;
                break;
            }
        }

        if (!selectedManufacturerId.HasValue)
            return Results.Ok(ApiResponse.Error(400, "没有找到匹配的路由规则"));

        // 创建消息记录
        var record = new MessageRecord
        {
            TaskId = taskId,
            MessageType = request.MessageType,
            TemplateCode = request.TemplateCode,
            Recipient = request.Recipient,
            Variables = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null,
            ManufacturerId = selectedManufacturerId,
            RouteRuleId = selectedRuleId,
            SendStatus = "待发送",
            CustomTag = request.CustomTag,
            CreatedAt = DateTime.UtcNow
        };

        db.MessageRecords.Add(record);
        await db.SaveChangesAsync();

        // 模拟发送消息（实际项目中应调用厂商接口）
        record.SendStatus = "成功";
        record.SendTime = DateTime.UtcNow;
        record.CompleteTime = DateTime.UtcNow;
        record.ManufacturerResponse = JsonSerializer.Serialize(new { result = "模拟发送成功" });
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(new { taskId }, "消息发送成功"));
    }

    private static async Task<IResult> GetMessages(
        MsgPulseDbContext db,
        string? messageType,
        string? sendStatus,
        int? manufacturerId,
        DateTime? startTime,
        DateTime? endTime,
        int page = 1,
        int pageSize = 20)
    {
        var query = db.MessageRecords
            .Include(m => m.Manufacturer)
            .Include(m => m.RouteRule)
            .AsQueryable();

        if (!string.IsNullOrEmpty(messageType))
            query = query.Where(m => m.MessageType == messageType);

        if (!string.IsNullOrEmpty(sendStatus))
            query = query.Where(m => m.SendStatus == sendStatus);

        if (manufacturerId.HasValue)
            query = query.Where(m => m.ManufacturerId == manufacturerId.Value);

        if (startTime.HasValue)
            query = query.Where(m => m.CreatedAt >= startTime.Value);

        if (endTime.HasValue)
            query = query.Where(m => m.CreatedAt <= endTime.Value);

        var total = await query.CountAsync();
        var records = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(ApiResponse.Success(new { total, page, pageSize, records }));
    }

    private static async Task<IResult> GetMessage(int id, MsgPulseDbContext db)
    {
        var record = await db.MessageRecords
            .Include(m => m.Manufacturer)
            .Include(m => m.RouteRule)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (record == null)
            return Results.Ok(ApiResponse.Error(404, "消息记录不存在"));

        return Results.Ok(ApiResponse.Success(record));
    }

    private static async Task<IResult> RetryMessage(int id, MsgPulseDbContext db)
    {
        var record = await db.MessageRecords.FindAsync(id);
        if (record == null)
            return Results.Ok(ApiResponse.Error(404, "消息记录不存在"));

        if (record.SendStatus != "失败")
            return Results.Ok(ApiResponse.Error(400, "只能重试失败的消息"));

        // 模拟重试发送
        record.SendStatus = "成功";
        record.SendTime = DateTime.UtcNow;
        record.CompleteTime = DateTime.UtcNow;
        record.RetryCount += 1;
        record.FailureReason = null;
        record.ManufacturerResponse = JsonSerializer.Serialize(new { result = "模拟重试成功" });

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "消息重试成功"));
    }
}

/// <summary>
/// 消息发送请求模型
/// </summary>
public record MessageSendRequest(
    string MessageType,
    string TemplateCode,
    string Recipient,
    Dictionary<string, string>? Variables,
    string? CustomTag
);
