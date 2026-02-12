using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Models;
using MsgPulse.Api.Providers;

namespace MsgPulse.Api.Data;

/// <summary>
/// 数据库种子数据初始化
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// 初始化预设厂商数据
    /// </summary>
    public static void SeedManufacturers(MsgPulseDbContext context)
    {
        // 如果已有数据，跳过初始化
        if (context.Manufacturers.Any())
        {
            return;
        }

        var manufacturers = new[]
        {
            new Manufacturer
            {
                Id = (int)ProviderType.AliyunSms,
                ProviderType = ProviderType.AliyunSms,
                Name = "阿里云短信",
                Code = "AliyunSms",
                Description = "阿里云短信服务，支持国内外短信发送",
                SupportedChannels = "SMS",
                IsActive = false, // 默认未启用，需要配置后启用
            },
            new Manufacturer
            {
                Id = (int)ProviderType.TencentSms,
                ProviderType = ProviderType.TencentSms,
                Name = "腾讯云短信",
                Code = "TencentSms",
                Description = "腾讯云短信服务，快速稳定的短信发送能力",
                SupportedChannels = "SMS",
                IsActive = false,
            },
            new Manufacturer
            {
                Id = (int)ProviderType.AzureCommunication,
                ProviderType = ProviderType.AzureCommunication,
                Name = "Azure通信服务",
                Code = "AzureCommunication",
                Description = "Microsoft Azure通信服务，支持短信和邮件",
                SupportedChannels = "SMS,Email",
                IsActive = false,
            },
            new Manufacturer
            {
                Id = (int)ProviderType.AwsSnsses,
                ProviderType = ProviderType.AwsSnsses,
                Name = "AWS SNS/SES",
                Code = "AwsSnsses",
                Description = "Amazon Web Services的SNS短信和SES邮件服务",
                SupportedChannels = "SMS,Email",
                IsActive = false,
            },
            new Manufacturer
            {
                Id = (int)ProviderType.GoogleFirebase,
                ProviderType = ProviderType.GoogleFirebase,
                Name = "Google Firebase",
                Code = "GoogleFirebase",
                Description = "Google Firebase云消息推送服务",
                SupportedChannels = "AppPush",
                IsActive = false,
            },
            new Manufacturer
            {
                Id = (int)ProviderType.AppleApns,
                ProviderType = ProviderType.AppleApns,
                Name = "Apple APNs",
                Code = "AppleApns",
                Description = "Apple推送通知服务",
                SupportedChannels = "AppPush",
                IsActive = false,
            },
            new Manufacturer
            {
                Id = (int)ProviderType.JpushProvider,
                ProviderType = ProviderType.JpushProvider,
                Name = "极光推送",
                Code = "JpushProvider",
                Description = "极光推送服务，支持Android和iOS推送",
                SupportedChannels = "AppPush",
                IsActive = false,
            },
            new Manufacturer
            {
                Id = (int)ProviderType.SendGrid,
                ProviderType = ProviderType.SendGrid,
                Name = "SendGrid",
                Code = "SendGrid",
                Description = "SendGrid邮件服务，全球领先的邮件发送平台",
                SupportedChannels = "Email",
                IsActive = false,
            },
            new Manufacturer
            {
                Id = (int)ProviderType.Mailgun,
                ProviderType = ProviderType.Mailgun,
                Name = "Mailgun",
                Code = "Mailgun",
                Description = "Mailgun邮件API服务",
                SupportedChannels = "Email",
                IsActive = false,
            },
            new Manufacturer
            {
                Id = (int)ProviderType.NetEaseYunxin,
                ProviderType = ProviderType.NetEaseYunxin,
                Name = "网易云信",
                Code = "NetEaseYunxin",
                Description = "网易云信即时通讯和推送服务",
                SupportedChannels = "SMS,AppPush",
                IsActive = false,
            }
        };

        context.Manufacturers.AddRange(manufacturers);
        context.SaveChanges();
    }
}
