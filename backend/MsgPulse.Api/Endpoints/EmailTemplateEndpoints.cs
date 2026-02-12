using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 邮件模板管理端点
/// </summary>
public static class EmailTemplateEndpoints
{
    /// <summary>
    /// 注册邮件模板相关的所有端点
    /// </summary>
    public static void MapEmailTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/email-templates").WithTags("邮件模板管理");

        // 获取邮件模板列表
        group.MapGet("/", GetEmailTemplates)
            .WithName("GetEmailTemplates")
            .WithSummary("获取邮件模板列表")
            .WithDescription("支持按模板编码、状态筛选邮件模板");

        // 获取单个邮件模板
        group.MapGet("/{id}", GetEmailTemplate)
            .WithName("GetEmailTemplate")
            .WithSummary("获取邮件模板详情")
            .WithDescription("根据ID获取单个邮件模板的详细信息");

        // 创建邮件模板
        group.MapPost("/", CreateEmailTemplate)
            .WithName("CreateEmailTemplate")
            .WithSummary("创建邮件模板")
            .WithDescription("创建新的邮件模板配置");

        // 更新邮件模板
        group.MapPut("/{id}", UpdateEmailTemplate)
            .WithName("UpdateEmailTemplate")
            .WithSummary("更新邮件模板")
            .WithDescription("更新现有邮件模板的配置信息");

        // 删除邮件模板
        group.MapDelete("/{id}", DeleteEmailTemplate)
            .WithName("DeleteEmailTemplate")
            .WithSummary("删除邮件模板")
            .WithDescription("删除指定的邮件模板（需确保没有关联记录）");
    }

    private static async Task<IResult> GetEmailTemplates(
        MsgPulseDbContext db,
        string? code,
        bool? isActive)
    {
        var query = db.EmailTemplates.AsQueryable();

        if (!string.IsNullOrEmpty(code))
            query = query.Where(t => t.Code.Contains(code));

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        var templates = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Results.Ok(ApiResponse.Success(templates));
    }

    private static async Task<IResult> GetEmailTemplate(int id, MsgPulseDbContext db)
    {
        var template = await db.EmailTemplates.FindAsync(id);
        if (template == null)
            return Results.Ok(ApiResponse.Error(404, "模板不存在"));

        return Results.Ok(ApiResponse.Success(template));
    }

    private static async Task<IResult> CreateEmailTemplate(
        EmailTemplate template,
        MsgPulseDbContext db)
    {
        if (await db.EmailTemplates.AnyAsync(t => t.Code == template.Code))
            return Results.Ok(ApiResponse.Error(400, "模板编码已存在"));

        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(new { id = template.Id }, "模板创建成功"));
    }

    private static async Task<IResult> UpdateEmailTemplate(
        int id,
        EmailTemplate updatedTemplate,
        MsgPulseDbContext db)
    {
        var template = await db.EmailTemplates.FindAsync(id);
        if (template == null)
            return Results.Ok(ApiResponse.Error(404, "模板不存在"));

        var hasRecords = await db.MessageRecords.AnyAsync(m => m.TemplateCode == template.Code);
        if (hasRecords && updatedTemplate.Code != template.Code)
            return Results.Ok(ApiResponse.Error(400, "存在关联记录，无法修改模板编码"));

        template.Name = updatedTemplate.Name;
        template.Subject = updatedTemplate.Subject;
        template.ContentType = updatedTemplate.ContentType;
        template.Content = updatedTemplate.Content;
        template.Variables = updatedTemplate.Variables;
        template.IsActive = updatedTemplate.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "模板更新成功"));
    }

    private static async Task<IResult> DeleteEmailTemplate(int id, MsgPulseDbContext db)
    {
        var template = await db.EmailTemplates.FindAsync(id);
        if (template == null)
            return Results.Ok(ApiResponse.Error(404, "模板不存在"));

        if (await db.MessageRecords.AnyAsync(m => m.TemplateCode == template.Code))
            return Results.Ok(ApiResponse.Error(400, "存在关联记录，无法删除"));

        db.EmailTemplates.Remove(template);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "模板删除成功"));
    }
}
