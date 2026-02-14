using Microsoft.EntityFrameworkCore;
using MsgPulse.Api.Models;

namespace MsgPulse.Api.Data;

public class MsgPulseDbContext : DbContext
{
    public MsgPulseDbContext(DbContextOptions<MsgPulseDbContext> options) : base(options)
    {
    }

    public DbSet<Manufacturer> Manufacturers { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<SmsTemplate> SmsTemplates { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<RouteRule> RouteRules { get; set; }
    public DbSet<MessageRecord> MessageRecords { get; set; }
    public DbSet<RateLimitConfig> RateLimitConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // 使用ProviderType枚举值作为主键
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SupportedChannels).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProviderType).IsRequired();
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SupportedChannels).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ChannelType).IsRequired();
        });

        modelBuilder.Entity<SmsTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Manufacturer)
                .WithMany()
                .HasForeignKey(e => e.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Channel)
                .WithMany()
                .HasForeignKey(e => e.ChannelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<RouteRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.MessageType).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.TargetManufacturer)
                .WithMany()
                .HasForeignKey(e => e.TargetManufacturerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TargetChannel)
                .WithMany()
                .HasForeignKey(e => e.TargetChannelId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.MessageType, e.Priority });
        });

        modelBuilder.Entity<MessageRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaskId).IsUnique();
            entity.Property(e => e.TaskId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MessageType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TemplateCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Recipient).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SendStatus).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Manufacturer)
                .WithMany()
                .HasForeignKey(e => e.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Channel)
                .WithMany()
                .HasForeignKey(e => e.ChannelId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RouteRule)
                .WithMany()
                .HasForeignKey(e => e.RouteRuleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SendStatus);
            entity.HasIndex(e => e.ManufacturerId);
            entity.HasIndex(e => e.ChannelId);
        });

        modelBuilder.Entity<RateLimitConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Manufacturer)
                .WithMany()
                .HasForeignKey(e => e.ManufacturerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Channel)
                .WithMany()
                .HasForeignKey(e => e.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ManufacturerId);
            entity.HasIndex(e => e.ChannelId);
        });
    }
}
