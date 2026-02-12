using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers;
using MsgPulse.Api.Providers.Models;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 厂商管理端点（配置管理，非CRUD）
/// </summary>
public static class ManufacturerEndpoints
{
    /// <summary>
    /// 注册厂商相关的所有端点
    /// </summary>
    public static void MapManufacturerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/manufacturers").WithTags("厂商配置管理");

        // 获取所有预设厂商列表
        group.MapGet("/", GetManufacturers)
            .WithName("GetManufacturers")
            .WithSummary("获取所有预设厂商列表")
            .WithDescription("返回系统预设的所有厂商及其配置状态");

        // 获取单个厂商配置
        group.MapGet("/{id}", GetManufacturer)
            .WithName("GetManufacturer")
            .WithSummary("获取厂商配置详情")
            .WithDescription("根据厂商ID获取详细配置信息");

        // 获取厂商配置Schema
        group.MapGet("/{id}/config-schema", GetConfigurationSchema)
            .WithName("GetConfigurationSchema")
            .WithSummary("获取厂商配置Schema")
            .WithDescription("获取厂商配置的结构化字段定义,用于生成配置表单");

        // 更新厂商配置（非新增/删除）
        group.MapPut("/{id}/config", UpdateManufacturerConfig)
            .WithName("UpdateManufacturerConfig")
            .WithSummary("更新厂商配置")
            .WithDescription("配置厂商的AccessKey等参数，并启用/禁用厂商");

        // 测试厂商连接
        group.MapPost("/{id}/test", TestManufacturerConnection)
            .WithName("TestManufacturerConnection")
            .WithSummary("测试厂商连接")
            .WithDescription("测试指定渠道的连通性");

        // 同步短信模板
        group.MapPost("/{id}/sync-templates", SyncSmsTemplates)
            .WithName("SyncSmsTemplates")
            .WithSummary("同步短信模板")
            .WithDescription("从厂商侧拉取并同步短信模板列表");
    }

    /// <summary>
    /// 获取所有厂商（包括已配置和未配置的）
    /// </summary>
    private static async Task<IResult> GetManufacturers(
        MsgPulseDbContext db,
        ProviderFactory providerFactory,
        string? channel)
    {
        // 从数据库获取已保存的配置
        var dbManufacturers = await db.Manufacturers.ToListAsync();

        // 获取所有预设厂商
        var allProviders = providerFactory.GetProviderInfos();

        // 合并数据：预设厂商 + 数据库配置状态
        var result = allProviders.Select(p =>
        {
            var dbConfig = dbManufacturers.FirstOrDefault(m => m.Id == (int)p.ProviderType);
            return new
            {
                id = (int)p.ProviderType,
                providerType = p.ProviderType.ToString(),
                name = p.Name,
                code = p.Code,
                supportedChannels = string.Join(",", p.SupportedChannels.Select(c => c.ToString())),
                isActive = dbConfig?.IsActive ?? false,
                isConfigured = !string.IsNullOrWhiteSpace(dbConfig?.Configuration),
                description = dbConfig?.Description ?? $"{p.Name}服务",
                updatedAt = dbConfig?.UpdatedAt
            };
        }).AsEnumerable();

        // 按渠道筛选
        if (!string.IsNullOrEmpty(channel))
        {
            result = result.Where(m => m.supportedChannels.Contains(channel, StringComparison.OrdinalIgnoreCase));
        }

        return Results.Ok(ApiResponse.Success(result.ToList()));
    }

    /// <summary>
    /// 获取单个厂商配置
    /// </summary>
    private static async Task<IResult> GetManufacturer(int id, MsgPulseDbContext db, ProviderFactory providerFactory)
    {
        var manufacturer = await db.Manufacturers.FindAsync(id);

        // 即使数据库中没有配置，也返回预设信息
        var providerInfo = providerFactory.GetProviderInfos()
            .FirstOrDefault(p => (int)p.ProviderType == id);

        if (providerInfo == null)
            return Results.Ok(ApiResponse.Error(404, "厂商不存在"));

        var result = new
        {
            id,
            providerType = providerInfo.ProviderType.ToString(),
            name = providerInfo.Name,
            code = providerInfo.Code,
            supportedChannels = string.Join(",", providerInfo.SupportedChannels.Select(c => c.ToString())),
            isActive = manufacturer?.IsActive ?? false,
            isConfigured = !string.IsNullOrWhiteSpace(manufacturer?.Configuration),
            configuration = manufacturer?.Configuration, // 返回配置供编辑
            description = manufacturer?.Description ?? $"{providerInfo.Name}服务",
            createdAt = manufacturer?.CreatedAt,
            updatedAt = manufacturer?.UpdatedAt
        };

        return Results.Ok(ApiResponse.Success(result));
    }

    /// <summary>
    /// 获取配置Schema
    /// </summary>
    private static IResult GetConfigurationSchema(int id, ProviderFactory providerFactory)
    {
        var providerInfo = providerFactory.GetProviderInfos()
            .FirstOrDefault(p => (int)p.ProviderType == id);

        if (providerInfo == null)
            return Results.Ok(ApiResponse.Error(404, "厂商不存在"));

        var provider = providerFactory.GetProvider(providerInfo.ProviderType);
        if (provider == null)
            return Results.Ok(ApiResponse.Error(500, "厂商实现不存在"));

        var schema = provider.GetConfigurationSchema();
        return Results.Ok(ApiResponse.Success(schema));
    }

    /// <summary>
    /// 更新厂商配置
    /// </summary>
    private static async Task<IResult> UpdateManufacturerConfig(
        int id,
        UpdateConfigRequest request,
        MsgPulseDbContext db,
        ProviderFactory providerFactory)
    {
        var providerInfo = providerFactory.GetProviderInfos()
            .FirstOrDefault(p => (int)p.ProviderType == id);

        if (providerInfo == null)
            return Results.Ok(ApiResponse.Error(404, "厂商不存在"));

        var manufacturer = await db.Manufacturers.FindAsync(id);

        if (manufacturer == null)
        {
            // 首次配置，创建记录
            manufacturer = new Manufacturer
            {
                Id = id,
                ProviderType = providerInfo.ProviderType,
                Name = providerInfo.Name,
                Code = providerInfo.Code,
                SupportedChannels = string.Join(",", providerInfo.SupportedChannels.Select(c => c.ToString())),
                Description = request.Description ?? $"{providerInfo.Name}服务",
                Configuration = request.Configuration,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Manufacturers.Add(manufacturer);
        }
        else
        {
            // 更新配置
            manufacturer.Configuration = request.Configuration;
            manufacturer.IsActive = request.IsActive;
            manufacturer.Description = request.Description ?? manufacturer.Description;
            manufacturer.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "厂商配置更新成功"));
    }

    /// <summary>
    /// 测试厂商连接
    /// </summary>
    private static async Task<IResult> TestManufacturerConnection(
        int id,
        TestConnectionRequest request,
        MsgPulseDbContext db,
        ProviderFactory providerFactory)
    {
        var manufacturer = await db.Manufacturers.FindAsync(id);
        if (manufacturer == null || string.IsNullOrWhiteSpace(manufacturer.Configuration))
            return Results.Ok(ApiResponse.Error(400, "厂商未配置"));

        var provider = providerFactory.GetProvider(manufacturer.ProviderType);
        if (provider == null)
            return Results.Ok(ApiResponse.Error(500, "厂商实现不存在"));

        // 初始化配置
        provider.Initialize(manufacturer.Configuration);

        // 测试连接
        var testResult = await provider.TestConnectionAsync(request.Channel);

        if (testResult.IsSuccess)
        {
            return Results.Ok(ApiResponse.Success(new
            {
                isSuccess = true,
                message = testResult.MessageId ?? "连接测试成功",
                rawResponse = testResult.RawResponse
            }));
        }
        else
        {
            return Results.Ok(ApiResponse.Error(500, testResult.ErrorMessage ?? "连接测试失败"));
        }
    }

    /// <summary>
    /// 同步短信模板
    /// </summary>
    private static async Task<IResult> SyncSmsTemplates(int id, MsgPulseDbContext db, ProviderFactory providerFactory)
    {
        var manufacturer = await db.Manufacturers.FindAsync(id);
        if (manufacturer == null || string.IsNullOrWhiteSpace(manufacturer.Configuration))
            return Results.Ok(ApiResponse.Error(400, "厂商未配置"));

        var provider = providerFactory.GetProvider(manufacturer.ProviderType);
        if (provider == null)
            return Results.Ok(ApiResponse.Error(500, "厂商实现不存在"));

        provider.Initialize(manufacturer.Configuration);

        var syncResult = await provider.SyncSmsTemplatesAsync();

        if (!syncResult.IsSuccess)
        {
            return Results.Ok(ApiResponse.Error(500, syncResult.ErrorMessage ?? "模板同步失败"));
        }

        // 将同步的模板保存到数据库
        foreach (var template in syncResult.Templates)
        {
            var existing = await db.SmsTemplates
                .FirstOrDefaultAsync(t => t.Code == template.Code && t.ManufacturerId == id);

            if (existing == null)
            {
                db.SmsTemplates.Add(new SmsTemplate
                {
                    ManufacturerId = id,
                    Code = template.Code,
                    Name = template.Name,
                    Content = template.Content,
                    IsActive = template.Status == "已审核" || template.Status == "已通过",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Name = template.Name;
                existing.Content = template.Content;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(new
        {
            syncCount = syncResult.Templates.Count,
            templates = syncResult.Templates
        }, $"成功同步{syncResult.Templates.Count}个模板"));
    }
}

/// <summary>
/// 更新配置请求
/// </summary>
public record UpdateConfigRequest(
    string? Configuration,
    bool IsActive,
    string? Description
);

/// <summary>
/// 测试连接请求
/// </summary>
public record TestConnectionRequest(
    MessageChannel Channel
);

/// <summary>
/// API响应辅助类
/// </summary>
public static class ApiResponse
{
    public static object Success(object? data = null, string message = "操作成功")
    {
        return new { code = 200, msg = message, data };
    }

    public static object Error(int code, string message)
    {
        return new { code, msg = message, data = (object?)null };
    }
}
