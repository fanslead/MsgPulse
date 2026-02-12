namespace MsgPulse.Api.Models;

/// <summary>
/// 配置字段定义
/// </summary>
public class ConfigurationField
{
    /// <summary>
    /// 字段名称(英文,用于JSON Key)
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 字段标签(中文,用于界面显示)
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// 字段类型
    /// </summary>
    public required string Type { get; set; } // "text", "password", "select", "number"

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 占位符文本
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// 帮助文本
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// 验证规则(正则表达式)
    /// </summary>
    public string? ValidationPattern { get; set; }

    /// <summary>
    /// 验证失败提示
    /// </summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// 选项列表(用于select类型)
    /// </summary>
    public List<SelectOption>? Options { get; set; }

    /// <summary>
    /// 是否为敏感信息
    /// </summary>
    public bool IsSensitive { get; set; }

    /// <summary>
    /// 字段分组
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// 下拉选项
/// </summary>
public class SelectOption
{
    public required string Label { get; set; }
    public required string Value { get; set; }
}

/// <summary>
/// 配置Schema
/// </summary>
public class ConfigurationSchema
{
    /// <summary>
    /// 厂商名称
    /// </summary>
    public required string ProviderName { get; set; }

    /// <summary>
    /// 配置说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 官方文档链接
    /// </summary>
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// 配置字段列表
    /// </summary>
    public required List<ConfigurationField> Fields { get; set; }

    /// <summary>
    /// 配置示例
    /// </summary>
    public string? Example { get; set; }
}
