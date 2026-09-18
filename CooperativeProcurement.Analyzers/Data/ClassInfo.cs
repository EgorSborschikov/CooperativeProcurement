namespace CooperativeProcurement.Analyzers.Data;

/// <summary>
/// Информация о классе
/// </summary>
public class ClassInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<string> Modifiers { get; set; } = [];
    public string BaseType { get; set; } = "object";
    public List<string> Interfaces { get; set; } = [];
    public List<MethodInfo> Methods { get; set; } = [];
    public List<PropertyInfo> Properties { get; set; } = [];
    public List<FieldInfo> Fields { get; set; } = [];
    public int Line { get; set; }
    public int MemberCount => Methods.Count + Properties.Count + Fields.Count;
    public string ProjectName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}
