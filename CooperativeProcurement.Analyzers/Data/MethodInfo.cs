using System.Reflection;

namespace CooperativeProcurement.Analyzers.Data
{
    /// <summary>
    /// Информация о методе
    /// </summary>
    public class MethodInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public List<string> Modifiers { get; set; } = new();
        public List<ParameterInfo> Parameters { get; set; } = new();
        public int Line { get; set; }
        public bool HasBody { get; set; }
        public bool IsAsync { get; set; }
        public bool IsStatic { get; set; }
        public bool IsPublic { get; set; }
        public List<string> Attributes { get; set; } = new();
        public int LineCount { get; set; }
        public int ComplexityScore { get; set; }
    }
}
