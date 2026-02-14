using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 渠道管理端点（CRUD）
/// </summary>
public static class ChannelEndpoints
{
    /// <summary>
    /// 注册渠道相关的所有端点
    /// </summary>
    public static void MapChannelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/channels").WithTags("渠道管理");

        // 获取所有渠道
        group.MapGet("/", GetChannels)
            .WithName("GetChannels")
            .WithSummary("获取所有渠道")
            .WithDescription("返回用户配置的所有消息发送渠道");

        // 获取渠道类型（模板）列表
        group.MapGet("/types", GetChannelTypes)
            .WithName("GetChannelTypes")
            .WithSummary("获取渠道类型列表")
            .WithDescription("返回可用的渠道类型（Provider模板）列表");

        // 获取渠道类型配置Schema
        group.MapGet("/types/{type}/config-schema", GetChannelTypeSchema)
            .WithName("GetChannelTypeSchema")
            .WithSummary("获取渠道类型配置Schema")
            .WithDescription("获取指定渠道类型的配置表单结构");

        // 获取单个渠道
        group.MapGet("/{id:int}", GetChannel)
            .WithName("GetChannel")
            .WithSummary("获取渠道详情")
            .WithDescription("根据ID获取渠道详细信息");

        // 创建渠道
        group.MapPost("/", CreateChannel)
            .WithName("CreateChannel")
            .WithSummary("创建新渠道")
            .WithDescription("创建一个新的消息发送渠道");

        // 更新渠道
        group.MapPut("/{id:int}", UpdateChannel)
            .WithName("UpdateChannel")
            .WithSummary("更新渠道")
            .WithDescription("更新渠道配置和状态");

        // 删除渠道
        group.MapDelete("/{id:int}", DeleteChannel)
            .WithName("DeleteChannel")
            .WithSummary("删除渠道")
            .WithDescription("删除指定的渠道");

        // 测试渠道连接
        group.MapPost("/{id:int}/test", TestChannelConnection)
            .WithName("TestChannelConnection")
            .WithSummary("测试渠道连接")
            .WithDescription("测试指定渠道的连通性");

        // 同步短信模板
        group.MapPost("/{id:int}/sync-templates", SyncChannelTemplates)
            .WithName("SyncChannelTemplates")
            .WithSummary("同步短信模板")
            .WithDescription("从渠道厂商侧拉取并同步短信模板列表");
    }

    /// <summary>
    /// 获取所有渠道
    /// </summary>
    private static async Task<IResult> GetChannels(MsgPulseDbContext db, string? messageType)
    {
        var query = db.Channels.AsQueryable();

        // 按消息类型筛选
        if (!string.IsNullOrEmpty(messageType))
        {
            query = query.Where(c => c.SupportedChannels.Contains(messageType));
        }

        var channels = await query
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();

        var result = channels.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            code = c.Code,
            channelType = c.ChannelType.ToString(),
            description = c.Description,
            supportedChannels = c.SupportedChannels,
            isActive = c.IsActive,
            isConfigured = !string.IsNullOrWhiteSpace(c.Configuration),
            createdAt = c.CreatedAt,
            updatedAt = c.UpdatedAt
        });

        return Results.Ok(ApiResponse.Success(result));
    }

    /// <summary>
    /// 获取可用的渠道类型（模板）
    /// </summary>
    private static IResult GetChannelTypes(ProviderFactory providerFactory, string? messageChannel)
    {
        var types = providerFactory.GetProviderInfos();

        // 按消息渠道筛选
        if (!string.IsNullOrEmpty(messageChannel))
        {
            types = types.Where(t => t.SupportedChannels.Any(sc =>
                sc.ToString().Equals(messageChannel, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        var result = types.Select(t => new
        {
            providerType = (int)t.ProviderType,
            name = t.Name,
            code = t.Code,
            supportedChannels = string.Join(",", t.SupportedChannels.Select(c => c.ToString()))
        });

        return Results.Ok(ApiResponse.Success(result));
    }

    /// <summary>
    /// 获取渠道类型配置Schema
    /// </summary>
    private static IResult GetChannelTypeSchema(int type, ProviderFactory providerFactory)
    {
        var providerType = (ProviderType)type;
        var provider = providerFactory.GetProvider(providerType);

        if (provider == null)
            return Results.Ok(ApiResponse.Error(404, "渠道类型不存在"));

        var schema = provider.GetConfigurationSchema();
        return Results.Ok(ApiResponse.Success(schema));
    }

    /// <summary>
    /// 获取单个渠道
    /// </summary>
    private static async Task<IResult> GetChannel(int id, MsgPulseDbContext db)
    {
        var channel = await db.Channels.FindAsync(id);

        if (channel == null)
            return Results.Ok(ApiResponse.Error(404, "渠道不存在"));

        var result = new
        {
            id = channel.Id,
            name = channel.Name,
            code = channel.Code,
            channelType = channel.ChannelType.ToString(),
            channelTypeValue = (int)channel.ChannelType,
            description = channel.Description,
            supportedChannels = channel.SupportedChannels,
            configuration = channel.Configuration,
            isActive = channel.IsActive,
            createdAt = channel.CreatedAt,
            updatedAt = channel.UpdatedAt
        };

        return Results.Ok(ApiResponse.Success(result));
    }

    /// <summary>
    /// 创建渠道
    /// </summary>
    private static async Task<IResult> CreateChannel(
        CreateChannelRequest request,
        MsgPulseDbContext db,
        ProviderFactory providerFactory)
    {
        // 验证Code唯一性
        if (await db.Channels.AnyAsync(c => c.Code == request.Code))
            return Results.Ok(ApiResponse.Error(400, "渠道编码已存在"));

        // 验证ChannelType是否有效
        var provider = providerFactory.GetProvider(request.ChannelType);
        if (provider == null)
            return Results.Ok(ApiResponse.Error(400, "无效的渠道类型"));

        var providerInfo = providerFactory.GetProviderInfos()
            .FirstOrDefault(p => p.ProviderType == request.ChannelType);

        if (providerInfo == null)
            return Results.Ok(ApiResponse.Error(400, "渠道类型不存在"));

        var channel = new Channel
        {
            Name = request.Name,
            Code = request.Code,
            ChannelType = request.ChannelType,
            Description = request.Description,
            SupportedChannels = string.Join(",", providerInfo.SupportedChannels.Select(c => c.ToString())),
            Configuration = request.Configuration,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Channels.Add(channel);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(new { id = channel.Id }, "渠道创建成功"));
    }

    /// <summary>
    /// 更新渠道
    /// </summary>
    private static async Task<IResult> UpdateChannel(
        int id,
        UpdateChannelRequest request,
        MsgPulseDbContext db)
    {
        var channel = await db.Channels.FindAsync(id);

        if (channel == null)
            return Results.Ok(ApiResponse.Error(404, "渠道不存在"));

        // 检查Code唯一性（排除自己）
        if (request.Code != channel.Code &&
            await db.Channels.AnyAsync(c => c.Code == request.Code))
            return Results.Ok(ApiResponse.Error(400, "渠道编码已存在"));

        channel.Name = request.Name;
        channel.Code = request.Code;
        channel.Description = request.Description;
        channel.Configuration = request.Configuration;
        channel.IsActive = request.IsActive;
        channel.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "渠道更新成功"));
    }

    /// <summary>
    /// 删除渠道
    /// </summary>
    private static async Task<IResult> DeleteChannel(int id, MsgPulseDbContext db)
    {
        var channel = await db.Channels.FindAsync(id);

        if (channel == null)
            return Results.Ok(ApiResponse.Error(404, "渠道不存在"));

        // 检查是否有关联的路由规则
        if (await db.RouteRules.AnyAsync(r => r.TargetChannelId == id))
            return Results.Ok(ApiResponse.Error(400, "该渠道已被路由规则使用，无法删除"));

        // 检查是否有关联的消息记录
        if (await db.MessageRecords.AnyAsync(m => m.ChannelId == id))
            return Results.Ok(ApiResponse.Error(400, "该渠道已有消息记录，无法删除"));

        db.Channels.Remove(channel);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "渠道删除成功"));
    }

    /// <summary>
    /// 测试渠道连接
    /// </summary>
    private static async Task<IResult> TestChannelConnection(
        int id,
        TestConnectionRequest request,
        MsgPulseDbContext db,
        ProviderFactory providerFactory)
    {
        var channel = await db.Channels.FindAsync(id);

        if (channel == null || string.IsNullOrWhiteSpace(channel.Configuration))
            return Results.Ok(ApiResponse.Error(400, "渠道未配置"));

        var provider = providerFactory.GetProvider(channel.ChannelType);
        if (provider == null)
            return Results.Ok(ApiResponse.Error(500, "渠道实现不存在"));

        provider.Initialize(channel.Configuration);
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
    private static async Task<IResult> SyncChannelTemplates(
        int id,
        MsgPulseDbContext db,
        ProviderFactory providerFactory)
    {
        var channel = await db.Channels.FindAsync(id);

        if (channel == null || string.IsNullOrWhiteSpace(channel.Configuration))
            return Results.Ok(ApiResponse.Error(400, "渠道未配置"));

        var provider = providerFactory.GetProvider(channel.ChannelType);
        if (provider == null)
            return Results.Ok(ApiResponse.Error(500, "渠道实现不存在"));

        provider.Initialize(channel.Configuration);
        var syncResult = await provider.SyncSmsTemplatesAsync();

        if (!syncResult.IsSuccess)
        {
            return Results.Ok(ApiResponse.Error(500, syncResult.ErrorMessage ?? "模板同步失败"));
        }

        // 将同步的模板保存到数据库（关联到Channel）
        foreach (var template in syncResult.Templates)
        {
            var existing = await db.SmsTemplates
                .FirstOrDefaultAsync(t => t.Code == template.Code && t.ChannelId == id);

            if (existing == null)
            {
                db.SmsTemplates.Add(new SmsTemplate
                {
                    ChannelId = id,
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
/// 创建渠道请求
/// </summary>
public record CreateChannelRequest(
    string Name,
    string Code,
    ProviderType ChannelType,
    string? Description,
    string? Configuration,
    bool IsActive
);

/// <summary>
/// 更新渠道请求
/// </summary>
public record UpdateChannelRequest(
    string Name,
    string Code,
    string? Description,
    string? Configuration,
    bool IsActive
);
