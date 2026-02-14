using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 模板预览端点
/// </summary>
public static class TemplatePreviewEndpoints
{
    /// <summary>
    /// 注册模板预览相关的所有端点
    /// </summary>
    public static void MapTemplatePreviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/template-preview").WithTags("模板预览");

        // 短信模板预览
        group.MapPost("/sms", PreviewSmsTemplate)
            .WithName("PreviewSmsTemplate")
            .WithSummary("短信模板预览")
            .WithDescription("使用提供的变量值预览短信模板内容");

        // 邮件模板预览
        group.MapPost("/email", PreviewEmailTemplate)
            .WithName("PreviewEmailTemplate")
            .WithSummary("邮件模板预览")
            .WithDescription("使用提供的变量值预览邮件模板内容");
    }

    private static async Task<IResult> PreviewSmsTemplate(
        TemplatePreviewRequest request,
        MsgPulseDbContext db)
    {
        // 根据模板ID或Code查找模板
        SmsTemplate? template = null;

        if (request.TemplateId.HasValue)
        {
            template = await db.SmsTemplates.FindAsync(request.TemplateId.Value);
        }
        else if (!string.IsNullOrEmpty(request.TemplateCode))
        {
            template = await db.SmsTemplates
                .FirstOrDefaultAsync(t => t.Code == request.TemplateCode);
        }

        if (template == null)
            return Results.Ok(ApiResponse.Error(404, "模板不存在"));

        // 替换变量
        var previewContent = ReplaceVariables(template.Content, request.Variables);

        // 提取模板中的变量
        var templateVariables = ExtractVariables(template.Content);

        var result = new
        {
            templateId = template.Id,
            templateName = template.Name,
            templateCode = template.Code,
            originalContent = template.Content,
            previewContent,
            variables = templateVariables,
            providedVariables = request.Variables,
            missingVariables = templateVariables.Except(request.Variables?.Keys.ToList() ?? new List<string>()).ToList()
        };

        return Results.Ok(ApiResponse.Success(result));
    }

    private static async Task<IResult> PreviewEmailTemplate(
        TemplatePreviewRequest request,
        MsgPulseDbContext db)
    {
        // 根据模板ID或Code查找模板
        EmailTemplate? template = null;

        if (request.TemplateId.HasValue)
        {
            template = await db.EmailTemplates.FindAsync(request.TemplateId.Value);
        }
        else if (!string.IsNullOrEmpty(request.TemplateCode))
        {
            template = await db.EmailTemplates
                .FirstOrDefaultAsync(t => t.Code == request.TemplateCode);
        }

        if (template == null)
            return Results.Ok(ApiResponse.Error(404, "模板不存在"));

        // 替换变量
        var previewSubject = ReplaceVariables(template.Subject, request.Variables);
        var previewContent = ReplaceVariables(template.Content, request.Variables);

        // 提取模板中的变量
        var subjectVariables = ExtractVariables(template.Subject);
        var contentVariables = ExtractVariables(template.Content);
        var allVariables = subjectVariables.Union(contentVariables).ToList();

        var result = new
        {
            templateId = template.Id,
            templateName = template.Name,
            templateCode = template.Code,
            contentType = template.ContentType,
            originalSubject = template.Subject,
            originalContent = template.Content,
            previewSubject,
            previewContent,
            variables = allVariables,
            providedVariables = request.Variables,
            missingVariables = allVariables.Except(request.Variables?.Keys.ToList() ?? new List<string>()).ToList()
        };

        return Results.Ok(ApiResponse.Success(result));
    }

    /// <summary>
    /// 替换模板中的变量
    /// </summary>
    private static string ReplaceVariables(string template, Dictionary<string, string>? variables)
    {
        if (string.IsNullOrEmpty(template) || variables == null || variables.Count == 0)
            return template;

        var result = template;
        foreach (var (key, value) in variables)
        {
            // 支持 {key} 和 ${key} 两种格式
            result = result.Replace($"{{{key}}}", value);
            result = result.Replace($"${{{key}}}", value);
        }

        return result;
    }

    /// <summary>
    /// 提取模板中的变量名列表
    /// </summary>
    private static List<string> ExtractVariables(string template)
    {
        if (string.IsNullOrEmpty(template))
            return new List<string>();

        var variables = new HashSet<string>();

        // 匹配 {variable} 格式
        var matches1 = Regex.Matches(template, @"\{(\w+)\}");
        foreach (Match match in matches1)
        {
            variables.Add(match.Groups[1].Value);
        }

        // 匹配 ${variable} 格式
        var matches2 = Regex.Matches(template, @"\$\{(\w+)\}");
        foreach (Match match in matches2)
        {
            variables.Add(match.Groups[1].Value);
        }

        return variables.ToList();
    }
}

/// <summary>
/// 模板预览请求
/// </summary>
public class TemplatePreviewRequest
{
    /// <summary>
    /// 模板ID（与TemplateCode二选一）
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// 模板编码（与TemplateId二选一）
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 变量值
    /// </summary>
    public Dictionary<string, string>? Variables { get; set; }
}
