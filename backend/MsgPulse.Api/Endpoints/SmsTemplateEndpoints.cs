using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 短信模板管理端点
/// </summary>
public static class SmsTemplateEndpoints
{
    /// <summary>
    /// 注册短信模板相关的所有端点
    /// </summary>
    public static void MapSmsTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sms-templates").WithTags("短信模板管理");

        // 获取短信模板列表
        group.MapGet("/", GetSmsTemplates)
            .WithName("GetSmsTemplates")
            .WithSummary("获取短信模板列表")
            .WithDescription("支持按厂商ID、模板编码、状态筛选短信模板");

        // 获取单个短信模板
        group.MapGet("/{id}", GetSmsTemplate)
            .WithName("GetSmsTemplate")
            .WithSummary("获取短信模板详情")
            .WithDescription("根据ID获取单个短信模板的详细信息");

        // 创建短信模板
        group.MapPost("/", CreateSmsTemplate)
            .WithName("CreateSmsTemplate")
            .WithSummary("创建短信模板")
            .WithDescription("创建新的短信模板配置");

        // 更新短信模板
        group.MapPut("/{id}", UpdateSmsTemplate)
            .WithName("UpdateSmsTemplate")
            .WithSummary("更新短信模板")
            .WithDescription("更新现有短信模板的配置信息");

        // 删除短信模板
        group.MapDelete("/{id}", DeleteSmsTemplate)
            .WithName("DeleteSmsTemplate")
            .WithSummary("删除短信模板")
            .WithDescription("删除指定的短信模板（需确保没有关联记录）");
    }

    private static async Task<IResult> GetSmsTemplates(
        MsgPulseDbContext db,
        int? manufacturerId,
        string? code,
        bool? isActive)
    {
        var query = db.SmsTemplates.Include(t => t.Manufacturer).AsQueryable();

        if (manufacturerId.HasValue)
            query = query.Where(t => t.ManufacturerId == manufacturerId.Value);

        if (!string.IsNullOrEmpty(code))
            query = query.Where(t => t.Code.Contains(code));

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        var templates = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Results.Ok(ApiResponse.Success(templates));
    }

    private static async Task<IResult> GetSmsTemplate(int id, MsgPulseDbContext db)
    {
        var template = await db.SmsTemplates
            .Include(t => t.Manufacturer)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
            return Results.Ok(ApiResponse.Error(404, "模板不存在"));

        return Results.Ok(ApiResponse.Success(template));
    }

    private static async Task<IResult> CreateSmsTemplate(
        SmsTemplate template,
        MsgPulseDbContext db)
    {
        if (await db.SmsTemplates.AnyAsync(t => t.Code == template.Code))
            return Results.Ok(ApiResponse.Error(400, "模板编码已存在"));

        if (!await db.Manufacturers.AnyAsync(m => m.Id == template.ManufacturerId))
            return Results.Ok(ApiResponse.Error(400, "厂商不存在"));

        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        db.SmsTemplates.Add(template);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(new { id = template.Id }, "模板创建成功"));
    }

    private static async Task<IResult> UpdateSmsTemplate(
        int id,
        SmsTemplate updatedTemplate,
        MsgPulseDbContext db)
    {
        var template = await db.SmsTemplates.FindAsync(id);
        if (template == null)
            return Results.Ok(ApiResponse.Error(404, "模板不存在"));

        var hasRecords = await db.MessageRecords.AnyAsync(m => m.TemplateCode == template.Code);
        if (hasRecords && (updatedTemplate.Code != template.Code ||
                            updatedTemplate.Content != template.Content ||
                            updatedTemplate.ManufacturerId != template.ManufacturerId))
            return Results.Ok(ApiResponse.Error(400, "存在关联记录，无法修改核心字段"));

        template.Name = updatedTemplate.Name;
        template.Content = updatedTemplate.Content;
        template.Variables = updatedTemplate.Variables;
        template.AuditStatus = updatedTemplate.AuditStatus;
        template.IsActive = updatedTemplate.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "模板更新成功"));
    }

    private static async Task<IResult> DeleteSmsTemplate(int id, MsgPulseDbContext db)
    {
        var template = await db.SmsTemplates.FindAsync(id);
        if (template == null)
            return Results.Ok(ApiResponse.Error(404, "模板不存在"));

        if (await db.MessageRecords.AnyAsync(m => m.TemplateCode == template.Code))
            return Results.Ok(ApiResponse.Error(400, "存在关联记录，无法删除"));

        db.SmsTemplates.Remove(template);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "模板删除成功"));
    }
}
