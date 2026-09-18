namespace CooperativeProcurement.Analyzers.Data;

/// <summary>
/// Информация о свойстве
/// </summary>
public class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Modifiers { get; set; } = [];
    public bool HasGetter { get; set; }
    public bool HasSetter { get; set; }
    public bool IsAutoProperty { get; set; }
    public int Line { get; set; }
}
