namespace CooperativeProcurement.Analyzers.Analyzers;

/// <summary>
/// Режим анализа комментариев
/// </summary>
public enum AnalysisMode
{
    /// <summary>Анализ через синтаксическое дерево (тривии)</summary>
    Syntactic,
    /// <summary>Анализ через SemanticModel (символы)</summary>
    Semantic
}
