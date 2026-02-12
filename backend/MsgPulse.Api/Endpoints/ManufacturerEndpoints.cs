using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;

namespace MsgPulse.Api.Endpoints;

/// <summary>
/// 厂商管理端点
/// </summary>
public static class ManufacturerEndpoints
{
    /// <summary>
    /// 注册厂商相关的所有端点
    /// </summary>
    public static void MapManufacturerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/manufacturers").WithTags("厂商管理");

        // 获取厂商列表
        group.MapGet("/", GetManufacturers)
            .WithName("GetManufacturers")
            .WithSummary("获取厂商列表")
            .WithDescription("支持按名称和渠道类型筛选厂商");

        // 获取单个厂商
        group.MapGet("/{id}", GetManufacturer)
            .WithName("GetManufacturer")
            .WithSummary("获取厂商详情")
            .WithDescription("根据ID获取单个厂商的详细信息");

        // 创建厂商
        group.MapPost("/", CreateManufacturer)
            .WithName("CreateManufacturer")
            .WithSummary("创建厂商")
            .WithDescription("创建新的消息厂商配置");

        // 更新厂商
        group.MapPut("/{id}", UpdateManufacturer)
            .WithName("UpdateManufacturer")
            .WithSummary("更新厂商")
            .WithDescription("更新现有厂商的配置信息");

        // 删除厂商
        group.MapDelete("/{id}", DeleteManufacturer)
            .WithName("DeleteManufacturer")
            .WithSummary("删除厂商")
            .WithDescription("删除指定的厂商（需确保没有关联数据）");
    }

    private static async Task<IResult> GetManufacturers(
        MsgPulseDbContext db,
        string? name,
        string? channel)
    {
        var query = db.Manufacturers.AsQueryable();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(m => m.Name.Contains(name));

        if (!string.IsNullOrEmpty(channel))
            query = query.Where(m => m.SupportedChannels.Contains(channel));

        var manufacturers = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
        return Results.Ok(ApiResponse.Success(manufacturers));
    }

    private static async Task<IResult> GetManufacturer(int id, MsgPulseDbContext db)
    {
        var manufacturer = await db.Manufacturers.FindAsync(id);
        if (manufacturer == null)
            return Results.Ok(ApiResponse.Error(404, "厂商不存在"));

        return Results.Ok(ApiResponse.Success(manufacturer));
    }

    private static async Task<IResult> CreateManufacturer(
        Manufacturer manufacturer,
        MsgPulseDbContext db)
    {
        if (await db.Manufacturers.AnyAsync(m => m.Code == manufacturer.Code))
            return Results.Ok(ApiResponse.Error(400, "厂商编码已存在"));

        manufacturer.CreatedAt = DateTime.UtcNow;
        manufacturer.UpdatedAt = DateTime.UtcNow;
        db.Manufacturers.Add(manufacturer);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(new { id = manufacturer.Id }, "厂商创建成功"));
    }

    private static async Task<IResult> UpdateManufacturer(
        int id,
        Manufacturer updatedManufacturer,
        MsgPulseDbContext db)
    {
        var manufacturer = await db.Manufacturers.FindAsync(id);
        if (manufacturer == null)
            return Results.Ok(ApiResponse.Error(404, "厂商不存在"));

        if (updatedManufacturer.Code != manufacturer.Code &&
            await db.Manufacturers.AnyAsync(m => m.Code == updatedManufacturer.Code))
            return Results.Ok(ApiResponse.Error(400, "厂商编码已存在"));

        manufacturer.Name = updatedManufacturer.Name;
        manufacturer.Code = updatedManufacturer.Code;
        manufacturer.Description = updatedManufacturer.Description;
        manufacturer.SupportedChannels = updatedManufacturer.SupportedChannels;
        manufacturer.SmsConfig = updatedManufacturer.SmsConfig;
        manufacturer.EmailConfig = updatedManufacturer.EmailConfig;
        manufacturer.AppPushConfig = updatedManufacturer.AppPushConfig;
        manufacturer.IsActive = updatedManufacturer.IsActive;
        manufacturer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "厂商更新成功"));
    }

    private static async Task<IResult> DeleteManufacturer(int id, MsgPulseDbContext db)
    {
        var manufacturer = await db.Manufacturers.FindAsync(id);
        if (manufacturer == null)
            return Results.Ok(ApiResponse.Error(404, "厂商不存在"));

        if (await db.SmsTemplates.AnyAsync(t => t.ManufacturerId == id) ||
            await db.RouteRules.AnyAsync(r => r.TargetManufacturerId == id) ||
            await db.MessageRecords.AnyAsync(m => m.ManufacturerId == id))
            return Results.Ok(ApiResponse.Error(400, "存在关联数据，无法删除"));

        db.Manufacturers.Remove(manufacturer);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success(message: "厂商删除成功"));
    }
}

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
