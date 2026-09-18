namespace CooperativeProcurement.Analyzers.Data;

/// <summary>
/// Информация о поле
/// </summary>
public class FieldInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Modifiers { get; set; } = [];
    public int Line { get; set; }
}
