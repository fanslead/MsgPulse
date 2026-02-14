using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Data;
using MsgPulse.Api.Endpoints;
using MsgPulse.Api.Providers;
using MsgPulse.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 配置数据库
builder.Services.AddDbContext<MsgPulseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=msgpulse.db"));

// 注册服务
builder.Services.AddHttpClient();
builder.Services.AddScoped<CallbackService>();
builder.Services.AddSingleton<ProviderFactory>();

// 注册速率限制服务
builder.Services.AddSingleton<RateLimitingService>();

// 注册消息去重服务
builder.Services.AddSingleton<MessageDeduplicationService>();

// 注册配置加密服务
builder.Services.AddSingleton<ConfigurationEncryptionService>();

// 注册消息队列和后台工作服务
builder.Services.AddSingleton<BackgroundMessageQueue>();
builder.Services.AddHostedService<MessageProcessingWorker>();

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加OpenAPI支持
builder.Services.AddOpenApi();

var app = builder.Build();

// 确保数据库已创建并初始化种子数据
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MsgPulseDbContext>();
    db.Database.EnsureCreated();

    // 初始化预设厂商数据
    DbInitializer.SeedManufacturers(db);
}

// 开发环境配置
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 启用CORS
app.UseCors();

// 注册所有端点
app.MapManufacturerEndpoints();
app.MapChannelEndpoints();
app.MapSmsTemplateEndpoints();
app.MapEmailTemplateEndpoints();
app.MapRouteRuleEndpoints();
app.MapMessageEndpoints();
app.MapRateLimitEndpoints();
app.MapDashboardEndpoints();
app.MapHealthEndpoints();
app.MapTemplatePreviewEndpoints();

app.Run();
