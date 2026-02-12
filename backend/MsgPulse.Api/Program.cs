using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MsgPulseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=msgpulse.db"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MsgPulseDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

var apiResponse = (int code, string msg, object? data = null) => new { code, msg, data };

app.MapGet("/api/manufacturers", async (MsgPulseDbContext db, string? name, string? channel) =>
{
    var query = db.Manufacturers.AsQueryable();

    if (!string.IsNullOrEmpty(name))
        query = query.Where(m => m.Name.Contains(name));

    if (!string.IsNullOrEmpty(channel))
        query = query.Where(m => m.SupportedChannels.Contains(channel));

    var manufacturers = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
    return Results.Ok(apiResponse(200, "Success", manufacturers));
});

app.MapGet("/api/manufacturers/{id}", async (int id, MsgPulseDbContext db) =>
{
    var manufacturer = await db.Manufacturers.FindAsync(id);
    if (manufacturer == null)
        return Results.Ok(apiResponse(404, "Manufacturer not found"));

    return Results.Ok(apiResponse(200, "Success", manufacturer));
});

app.MapPost("/api/manufacturers", async (Manufacturer manufacturer, MsgPulseDbContext db) =>
{
    if (await db.Manufacturers.AnyAsync(m => m.Code == manufacturer.Code))
        return Results.Ok(apiResponse(400, "Manufacturer code already exists"));

    manufacturer.CreatedAt = DateTime.UtcNow;
    manufacturer.UpdatedAt = DateTime.UtcNow;
    db.Manufacturers.Add(manufacturer);
    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Manufacturer created successfully", new { id = manufacturer.Id }));
});

app.MapPut("/api/manufacturers/{id}", async (int id, Manufacturer updatedManufacturer, MsgPulseDbContext db) =>
{
    var manufacturer = await db.Manufacturers.FindAsync(id);
    if (manufacturer == null)
        return Results.Ok(apiResponse(404, "Manufacturer not found"));

    if (updatedManufacturer.Code != manufacturer.Code &&
        await db.Manufacturers.AnyAsync(m => m.Code == updatedManufacturer.Code))
        return Results.Ok(apiResponse(400, "Manufacturer code already exists"));

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

    return Results.Ok(apiResponse(200, "Manufacturer updated successfully"));
});

app.MapDelete("/api/manufacturers/{id}", async (int id, MsgPulseDbContext db) =>
{
    var manufacturer = await db.Manufacturers.FindAsync(id);
    if (manufacturer == null)
        return Results.Ok(apiResponse(404, "Manufacturer not found"));

    if (await db.SmsTemplates.AnyAsync(t => t.ManufacturerId == id) ||
        await db.RouteRules.AnyAsync(r => r.TargetManufacturerId == id) ||
        await db.MessageRecords.AnyAsync(m => m.ManufacturerId == id))
        return Results.Ok(apiResponse(400, "Cannot delete manufacturer with associated data"));

    db.Manufacturers.Remove(manufacturer);
    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Manufacturer deleted successfully"));
});

app.MapGet("/api/sms-templates", async (MsgPulseDbContext db, int? manufacturerId, string? code, bool? isActive) =>
{
    var query = db.SmsTemplates.Include(t => t.Manufacturer).AsQueryable();

    if (manufacturerId.HasValue)
        query = query.Where(t => t.ManufacturerId == manufacturerId.Value);

    if (!string.IsNullOrEmpty(code))
        query = query.Where(t => t.Code.Contains(code));

    if (isActive.HasValue)
        query = query.Where(t => t.IsActive == isActive.Value);

    var templates = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    return Results.Ok(apiResponse(200, "Success", templates));
});

app.MapGet("/api/sms-templates/{id}", async (int id, MsgPulseDbContext db) =>
{
    var template = await db.SmsTemplates.Include(t => t.Manufacturer).FirstOrDefaultAsync(t => t.Id == id);
    if (template == null)
        return Results.Ok(apiResponse(404, "Template not found"));

    return Results.Ok(apiResponse(200, "Success", template));
});

app.MapPost("/api/sms-templates", async (SmsTemplate template, MsgPulseDbContext db) =>
{
    if (await db.SmsTemplates.AnyAsync(t => t.Code == template.Code))
        return Results.Ok(apiResponse(400, "Template code already exists"));

    if (!await db.Manufacturers.AnyAsync(m => m.Id == template.ManufacturerId))
        return Results.Ok(apiResponse(400, "Manufacturer not found"));

    template.CreatedAt = DateTime.UtcNow;
    template.UpdatedAt = DateTime.UtcNow;
    db.SmsTemplates.Add(template);
    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Template created successfully", new { id = template.Id }));
});

app.MapPut("/api/sms-templates/{id}", async (int id, SmsTemplate updatedTemplate, MsgPulseDbContext db) =>
{
    var template = await db.SmsTemplates.FindAsync(id);
    if (template == null)
        return Results.Ok(apiResponse(404, "Template not found"));

    var hasRecords = await db.MessageRecords.AnyAsync(m => m.TemplateCode == template.Code);
    if (hasRecords && (updatedTemplate.Code != template.Code ||
                        updatedTemplate.Content != template.Content ||
                        updatedTemplate.ManufacturerId != template.ManufacturerId))
        return Results.Ok(apiResponse(400, "Cannot modify core fields of template with associated records"));

    template.Name = updatedTemplate.Name;
    template.Content = updatedTemplate.Content;
    template.Variables = updatedTemplate.Variables;
    template.AuditStatus = updatedTemplate.AuditStatus;
    template.IsActive = updatedTemplate.IsActive;
    template.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Template updated successfully"));
});

app.MapDelete("/api/sms-templates/{id}", async (int id, MsgPulseDbContext db) =>
{
    var template = await db.SmsTemplates.FindAsync(id);
    if (template == null)
        return Results.Ok(apiResponse(404, "Template not found"));

    if (await db.MessageRecords.AnyAsync(m => m.TemplateCode == template.Code))
        return Results.Ok(apiResponse(400, "Cannot delete template with associated records"));

    db.SmsTemplates.Remove(template);
    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Template deleted successfully"));
});

app.MapGet("/api/email-templates", async (MsgPulseDbContext db, string? code, bool? isActive) =>
{
    var query = db.EmailTemplates.AsQueryable();

    if (!string.IsNullOrEmpty(code))
        query = query.Where(t => t.Code.Contains(code));

    if (isActive.HasValue)
        query = query.Where(t => t.IsActive == isActive.Value);

    var templates = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    return Results.Ok(apiResponse(200, "Success", templates));
});

app.MapGet("/api/email-templates/{id}", async (int id, MsgPulseDbContext db) =>
{
    var template = await db.EmailTemplates.FindAsync(id);
    if (template == null)
        return Results.Ok(apiResponse(404, "Template not found"));

    return Results.Ok(apiResponse(200, "Success", template));
});

app.MapPost("/api/email-templates", async (EmailTemplate template, MsgPulseDbContext db) =>
{
    if (await db.EmailTemplates.AnyAsync(t => t.Code == template.Code))
        return Results.Ok(apiResponse(400, "Template code already exists"));

    template.CreatedAt = DateTime.UtcNow;
    template.UpdatedAt = DateTime.UtcNow;
    db.EmailTemplates.Add(template);
    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Template created successfully", new { id = template.Id }));
});

app.MapPut("/api/email-templates/{id}", async (int id, EmailTemplate updatedTemplate, MsgPulseDbContext db) =>
{
    var template = await db.EmailTemplates.FindAsync(id);
    if (template == null)
        return Results.Ok(apiResponse(404, "Template not found"));

    var hasRecords = await db.MessageRecords.AnyAsync(m => m.TemplateCode == template.Code);
    if (hasRecords && updatedTemplate.Code != template.Code)
        return Results.Ok(apiResponse(400, "Cannot modify template code with associated records"));

    template.Name = updatedTemplate.Name;
    template.Subject = updatedTemplate.Subject;
    template.ContentType = updatedTemplate.ContentType;
    template.Content = updatedTemplate.Content;
    template.Variables = updatedTemplate.Variables;
    template.IsActive = updatedTemplate.IsActive;
    template.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Template updated successfully"));
});

app.MapDelete("/api/email-templates/{id}", async (int id, MsgPulseDbContext db) =>
{
    var template = await db.EmailTemplates.FindAsync(id);
    if (template == null)
        return Results.Ok(apiResponse(404, "Template not found"));

    if (await db.MessageRecords.AnyAsync(m => m.TemplateCode == template.Code))
        return Results.Ok(apiResponse(400, "Cannot delete template with associated records"));

    db.EmailTemplates.Remove(template);
    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Template deleted successfully"));
});

app.MapGet("/api/route-rules", async (MsgPulseDbContext db, string? messageType, bool? isActive) =>
{
    var query = db.RouteRules.Include(r => r.TargetManufacturer).AsQueryable();

    if (!string.IsNullOrEmpty(messageType))
        query = query.Where(r => r.MessageType == messageType);

    if (isActive.HasValue)
        query = query.Where(r => r.IsActive == isActive.Value);

    var rules = await query.OrderBy(r => r.Priority).ThenByDescending(r => r.CreatedAt).ToListAsync();
    return Results.Ok(apiResponse(200, "Success", rules));
});

app.MapGet("/api/route-rules/{id}", async (int id, MsgPulseDbContext db) =>
{
    var rule = await db.RouteRules.Include(r => r.TargetManufacturer).FirstOrDefaultAsync(r => r.Id == id);
    if (rule == null)
        return Results.Ok(apiResponse(404, "Route rule not found"));

    return Results.Ok(apiResponse(200, "Success", rule));
});

app.MapPost("/api/route-rules", async (RouteRule rule, MsgPulseDbContext db) =>
{
    if (!await db.Manufacturers.AnyAsync(m => m.Id == rule.TargetManufacturerId && m.IsActive))
        return Results.Ok(apiResponse(400, "Target manufacturer not found or inactive"));

    rule.CreatedAt = DateTime.UtcNow;
    rule.UpdatedAt = DateTime.UtcNow;
    db.RouteRules.Add(rule);
    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Route rule created successfully", new { id = rule.Id }));
});

app.MapPut("/api/route-rules/{id}", async (int id, RouteRule updatedRule, MsgPulseDbContext db) =>
{
    var rule = await db.RouteRules.FindAsync(id);
    if (rule == null)
        return Results.Ok(apiResponse(404, "Route rule not found"));

    if (!await db.Manufacturers.AnyAsync(m => m.Id == updatedRule.TargetManufacturerId))
        return Results.Ok(apiResponse(400, "Target manufacturer not found"));

    rule.Name = updatedRule.Name;
    rule.MessageType = updatedRule.MessageType;
    rule.MatchConditions = updatedRule.MatchConditions;
    rule.TargetManufacturerId = updatedRule.TargetManufacturerId;
    rule.Priority = updatedRule.Priority;
    rule.IsActive = updatedRule.IsActive;
    rule.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Route rule updated successfully"));
});

app.MapDelete("/api/route-rules/{id}", async (int id, MsgPulseDbContext db) =>
{
    var rule = await db.RouteRules.FindAsync(id);
    if (rule == null)
        return Results.Ok(apiResponse(404, "Route rule not found"));

    if (await db.MessageRecords.AnyAsync(m => m.RouteRuleId == id))
        return Results.Ok(apiResponse(400, "Cannot delete route rule with associated records"));

    db.RouteRules.Remove(rule);
    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Route rule deleted successfully"));
});

app.MapPost("/api/messages/send", async (MessageSendRequest request, MsgPulseDbContext db) =>
{
    var taskId = Guid.NewGuid().ToString();

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
        return Results.Ok(apiResponse(400, "No active route rule matched for the message type"));

    var record = new MessageRecord
    {
        TaskId = taskId,
        MessageType = request.MessageType,
        TemplateCode = request.TemplateCode,
        Recipient = request.Recipient,
        Variables = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null,
        ManufacturerId = selectedManufacturerId,
        RouteRuleId = selectedRuleId,
        SendStatus = "Pending",
        CustomTag = request.CustomTag,
        CreatedAt = DateTime.UtcNow
    };

    db.MessageRecords.Add(record);
    await db.SaveChangesAsync();

    record.SendStatus = "Success";
    record.SendTime = DateTime.UtcNow;
    record.CompleteTime = DateTime.UtcNow;
    record.ManufacturerResponse = JsonSerializer.Serialize(new { result = "simulated_success" });
    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Message sent successfully", new { taskId }));
});

app.MapGet("/api/messages", async (MsgPulseDbContext db, string? messageType, string? sendStatus,
    int? manufacturerId, DateTime? startTime, DateTime? endTime, int page = 1, int pageSize = 20) =>
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

    return Results.Ok(apiResponse(200, "Success", new { total, page, pageSize, records }));
});

app.MapGet("/api/messages/{id}", async (int id, MsgPulseDbContext db) =>
{
    var record = await db.MessageRecords
        .Include(m => m.Manufacturer)
        .Include(m => m.RouteRule)
        .FirstOrDefaultAsync(m => m.Id == id);

    if (record == null)
        return Results.Ok(apiResponse(404, "Message record not found"));

    return Results.Ok(apiResponse(200, "Success", record));
});

app.MapPost("/api/messages/{id}/retry", async (int id, MsgPulseDbContext db) =>
{
    var record = await db.MessageRecords.FindAsync(id);
    if (record == null)
        return Results.Ok(apiResponse(404, "Message record not found"));

    if (record.SendStatus != "Failed")
        return Results.Ok(apiResponse(400, "Only failed messages can be retried"));

    record.SendStatus = "Success";
    record.SendTime = DateTime.UtcNow;
    record.CompleteTime = DateTime.UtcNow;
    record.RetryCount += 1;
    record.FailureReason = null;
    record.ManufacturerResponse = JsonSerializer.Serialize(new { result = "simulated_retry_success" });

    await db.SaveChangesAsync();

    return Results.Ok(apiResponse(200, "Message retried successfully"));
});

app.Run();

public record MessageSendRequest(
    string MessageType,
    string TemplateCode,
    string Recipient,
    Dictionary<string, string>? Variables,
    string? CustomTag
);
