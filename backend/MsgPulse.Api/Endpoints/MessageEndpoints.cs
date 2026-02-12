using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers;
using MsgPulse.Api.Providers.Models;
using MsgPulse.Api.Services;
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

        // 批量发送消息
        group.MapPost("/batch-send", BatchSendMessage)
            .WithName("BatchSendMessage")
            .WithSummary("批量发送消息")
            .WithDescription("批量导入接收方并发送消息");

        // 导出消息记录
        group.MapGet("/export", ExportMessages)
            .WithName("ExportMessages")
            .WithSummary("导出消息记录")
            .WithDescription("导出符合条件的消息记录为CSV格式");
    }

    private static async Task<IResult> SendMessage(
        MessageSendRequest request,
        MsgPulseDbContext db,
        CallbackService callbackService,
        ProviderFactory providerFactory)
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
            CallbackUrl = request.CallbackUrl,
            CreatedAt = DateTime.UtcNow
        };

        db.MessageRecords.Add(record);
        await db.SaveChangesAsync();

        // 调用厂商接口发送消息
        var manufacturer = await db.Manufacturers.FindAsync(selectedManufacturerId.Value);
        if (manufacturer == null || string.IsNullOrWhiteSpace(manufacturer.Configuration))
        {
            record.SendStatus = "失败";
            record.FailureReason = "厂商未配置";
            await db.SaveChangesAsync();
            return Results.Ok(ApiResponse.Error(500, "厂商未配置"));
        }

        var provider = providerFactory.GetProvider(manufacturer.ProviderType);
        if (provider == null)
        {
            record.SendStatus = "失败";
            record.FailureReason = "厂商实现不存在";
            await db.SaveChangesAsync();
            return Results.Ok(ApiResponse.Error(500, "厂商实现不存在"));
        }

        provider.Initialize(manufacturer.Configuration);

        ProviderResult? result = null;
        record.SendTime = DateTime.UtcNow;

        try
        {
            // 根据消息类型调用对应的发送方法
            if (request.MessageType == "SMS")
            {
                result = await provider.SendSmsAsync(new SmsRequest
                {
                    PhoneNumber = request.Recipient,
                    TemplateCode = request.TemplateCode,
                    TemplateParams = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null
                });
            }
            else if (request.MessageType == "Email")
            {
                // 获取邮件模板
                var emailTemplate = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Code == request.TemplateCode);
                if (emailTemplate == null)
                {
                    record.SendStatus = "失败";
                    record.FailureReason = "邮件模板不存在";
                    await db.SaveChangesAsync();
                    return Results.Ok(ApiResponse.Error(400, "邮件模板不存在"));
                }

                // 替换模板变量
                var subject = ReplaceVariables(emailTemplate.Subject, request.Variables);
                var content = ReplaceVariables(emailTemplate.Content, request.Variables);

                result = await provider.SendEmailAsync(new EmailRequest
                {
                    ToEmail = request.Recipient,
                    Subject = subject,
                    Content = content,
                    ContentType = emailTemplate.ContentType
                });
            }
            else if (request.MessageType == "AppPush")
            {
                result = await provider.SendPushAsync(new AppPushRequest
                {
                    Target = request.Recipient,
                    Title = request.Variables?.GetValueOrDefault("title") ?? "推送消息",
                    Content = request.Variables?.GetValueOrDefault("content") ?? ""
                });
            }

            if (result != null && result.IsSuccess)
            {
                record.SendStatus = "成功";
                record.CompleteTime = DateTime.UtcNow;
                record.ManufacturerResponse = result.RawResponse;
            }
            else
            {
                record.SendStatus = "失败";
                record.FailureReason = result?.ErrorMessage ?? "发送失败";
                record.ManufacturerResponse = result?.RawResponse;
            }
        }
        catch (Exception ex)
        {
            record.SendStatus = "失败";
            record.FailureReason = ex.Message;
            record.CompleteTime = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        // 推送状态变更回调
        await callbackService.PushCallbackAsync(record);

        return Results.Ok(ApiResponse.Success(new { taskId, status = record.SendStatus },
            record.SendStatus == "成功" ? "消息发送成功" : $"消息发送失败: {record.FailureReason}"));
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

    private static async Task<IResult> RetryMessage(int id, MsgPulseDbContext db, CallbackService callbackService, ProviderFactory providerFactory)
    {
        var record = await db.MessageRecords
            .Include(m => m.Manufacturer)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (record == null)
            return Results.Ok(ApiResponse.Error(404, "消息记录不存在"));

        if (record.SendStatus != "失败")
            return Results.Ok(ApiResponse.Error(400, "只能重试失败的消息"));

        if (record.Manufacturer == null || string.IsNullOrWhiteSpace(record.Manufacturer.Configuration))
            return Results.Ok(ApiResponse.Error(500, "厂商未配置"));

        var provider = providerFactory.GetProvider(record.Manufacturer.ProviderType);
        if (provider == null)
            return Results.Ok(ApiResponse.Error(500, "厂商实现不存在"));

        provider.Initialize(record.Manufacturer.Configuration);

        // 重新发送
        record.RetryCount += 1;
        record.SendTime = DateTime.UtcNow;
        record.SendStatus = "发送中";

        try
        {
            ProviderResult? result = null;

            if (record.MessageType == "SMS")
            {
                result = await provider.SendSmsAsync(new SmsRequest
                {
                    PhoneNumber = record.Recipient,
                    TemplateCode = record.TemplateCode,
                    TemplateParams = record.Variables
                });
            }
            else if (record.MessageType == "Email")
            {
                var vars = string.IsNullOrWhiteSpace(record.Variables)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(record.Variables);

                var emailTemplate = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Code == record.TemplateCode);
                if (emailTemplate != null)
                {
                    result = await provider.SendEmailAsync(new EmailRequest
                    {
                        ToEmail = record.Recipient,
                        Subject = ReplaceVariables(emailTemplate.Subject, vars),
                        Content = ReplaceVariables(emailTemplate.Content, vars),
                        ContentType = emailTemplate.ContentType
                    });
                }
            }
            else if (record.MessageType == "AppPush")
            {
                var vars = string.IsNullOrWhiteSpace(record.Variables)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(record.Variables);

                result = await provider.SendPushAsync(new AppPushRequest
                {
                    Target = record.Recipient,
                    Title = vars?.GetValueOrDefault("title") ?? "推送消息",
                    Content = vars?.GetValueOrDefault("content") ?? ""
                });
            }

            if (result != null && result.IsSuccess)
            {
                record.SendStatus = "成功";
                record.CompleteTime = DateTime.UtcNow;
                record.FailureReason = null;
                record.ManufacturerResponse = result.RawResponse;
            }
            else
            {
                record.SendStatus = "失败";
                record.FailureReason = result?.ErrorMessage ?? "重试发送失败";
                record.ManufacturerResponse = result?.RawResponse;
            }
        }
        catch (Exception ex)
        {
            record.SendStatus = "失败";
            record.FailureReason = ex.Message;
            record.CompleteTime = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        // 推送状态变更回调
        await callbackService.PushCallbackAsync(record);

        return Results.Ok(ApiResponse.Success(new { status = record.SendStatus },
            record.SendStatus == "成功" ? "消息重试成功" : $"消息重试失败: {record.FailureReason}"));
    }

    /// <summary>
    /// 批量发送消息
    /// </summary>
    private static async Task<IResult> BatchSendMessage(
        BatchSendRequest request,
        MsgPulseDbContext db,
        CallbackService callbackService,
        ProviderFactory providerFactory)
    {
        var successCount = 0;
        var failCount = 0;
        var taskIds = new List<string>();

        foreach (var recipient in request.Recipients)
        {
            try
            {
                var sendRequest = new MessageSendRequest(
                    request.MessageType,
                    request.TemplateCode,
                    recipient,
                    request.Variables,
                    request.CustomTag,
                    request.CallbackUrl
                );

                // 重用SendMessage逻辑
                var taskId = Guid.NewGuid().ToString();
                taskIds.Add(taskId);

                var rules = await db.RouteRules
                    .Include(r => r.TargetManufacturer)
                    .Where(r => r.MessageType == request.MessageType && r.IsActive)
                    .OrderBy(r => r.Priority)
                    .ToListAsync();

                int? selectedManufacturerId = null;
                int? selectedRuleId = null;

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
                {
                    failCount++;
                    continue;
                }

                var record = new MessageRecord
                {
                    TaskId = taskId,
                    MessageType = request.MessageType,
                    TemplateCode = request.TemplateCode,
                    Recipient = recipient,
                    Variables = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null,
                    ManufacturerId = selectedManufacturerId,
                    RouteRuleId = selectedRuleId,
                    SendStatus = "待发送",
                    CustomTag = request.CustomTag,
                    CallbackUrl = request.CallbackUrl,
                    CreatedAt = DateTime.UtcNow
                };

                db.MessageRecords.Add(record);
                await db.SaveChangesAsync();

                // 异步发送(简化版,实际项目应该用后台任务队列)
                var manufacturer = await db.Manufacturers.FindAsync(selectedManufacturerId.Value);
                if (manufacturer != null && !string.IsNullOrWhiteSpace(manufacturer.Configuration))
                {
                    var provider = providerFactory.GetProvider(manufacturer.ProviderType);
                    if (provider != null)
                    {
                        provider.Initialize(manufacturer.Configuration);
                        record.SendTime = DateTime.UtcNow;

                        try
                        {
                            ProviderResult? result = null;

                            if (request.MessageType == "SMS")
                            {
                                result = await provider.SendSmsAsync(new SmsRequest
                                {
                                    PhoneNumber = recipient,
                                    TemplateCode = request.TemplateCode,
                                    TemplateParams = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null
                                });
                            }
                            else if (request.MessageType == "Email")
                            {
                                var emailTemplate = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Code == request.TemplateCode);
                                if (emailTemplate != null)
                                {
                                    result = await provider.SendEmailAsync(new EmailRequest
                                    {
                                        ToEmail = recipient,
                                        Subject = ReplaceVariables(emailTemplate.Subject, request.Variables),
                                        Content = ReplaceVariables(emailTemplate.Content, request.Variables),
                                        ContentType = emailTemplate.ContentType
                                    });
                                }
                            }

                            if (result != null && result.IsSuccess)
                            {
                                record.SendStatus = "成功";
                                record.CompleteTime = DateTime.UtcNow;
                                record.ManufacturerResponse = result.RawResponse;
                                successCount++;
                            }
                            else
                            {
                                record.SendStatus = "失败";
                                record.FailureReason = result?.ErrorMessage ?? "发送失败";
                                failCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            record.SendStatus = "失败";
                            record.FailureReason = ex.Message;
                            failCount++;
                        }

                        await db.SaveChangesAsync();

                        // 推送状态变更回调
                        await callbackService.PushCallbackAsync(record);
                    }
                }
            }
            catch
            {
                failCount++;
            }
        }

        return Results.Ok(ApiResponse.Success(new
        {
            total = request.Recipients.Count,
            successCount,
            failCount,
            taskIds
        }, $"批量发送完成，成功{successCount}条，失败{failCount}条"));
    }

    /// <summary>
    /// 导出消息记录
    /// </summary>
    private static async Task<IResult> ExportMessages(
        MsgPulseDbContext db,
        string? messageType,
        string? sendStatus,
        int? manufacturerId,
        DateTime? startTime,
        DateTime? endTime)
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

        var records = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

        // 生成CSV
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("任务ID,消息类型,模板编码,接收方,厂商,发送状态,发送时间,完成时间,失败原因,创建时间");

        foreach (var record in records)
        {
            csv.AppendLine($"{record.TaskId},{record.MessageType},{record.TemplateCode},{record.Recipient}," +
                          $"{record.Manufacturer?.Name ?? "未知"},{record.SendStatus}," +
                          $"{record.SendTime:yyyy-MM-dd HH:mm:ss},{record.CompleteTime:yyyy-MM-dd HH:mm:ss}," +
                          $"{record.FailureReason},{record.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return Results.File(bytes, "text/csv", $"messages_{DateTime.Now:yyyyMMddHHmmss}.csv");
    }

    /// <summary>
    /// 替换模板变量
    /// </summary>
    private static string ReplaceVariables(string template, Dictionary<string, string>? variables)
    {
        if (string.IsNullOrWhiteSpace(template) || variables == null)
            return template;

        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{key}}}", value);
        }

        return result;
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
    string? CustomTag,
    string? CallbackUrl
);

/// <summary>
/// 批量发送请求模型
/// </summary>
public record BatchSendRequest(
    string MessageType,
    string TemplateCode,
    List<string> Recipients,
    Dictionary<string, string>? Variables,
    string? CustomTag,
    string? CallbackUrl
);
