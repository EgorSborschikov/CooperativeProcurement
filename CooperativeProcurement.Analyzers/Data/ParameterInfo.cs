namespace CooperativeProcurement.Analyzers.Data;

/// <summary>
/// Информация о параметре
/// </summary>
public class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool HasDefaultValue { get; set; }
    public string? DefaultValue { get; set; }
}
