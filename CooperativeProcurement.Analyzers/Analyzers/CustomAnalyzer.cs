using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CooperativeProcurement.Analyzers.Analyzers;

/// <summary>
/// Шаблон собственного анализатора.
/// 
/// Инструкция:
/// 1. Измените DiagnosticId на уникальный (например, "CP0003")
/// 2. Измените заголовок, сообщение и описание
/// 3. В методе Initialize зарегистрируйте нужный тип узла или символа
/// 4. В методе Analyze реализуйте логику проверки
/// 5. Раскомментируйте и заполните нужные части кода
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CustomAnalyzer : DiagnosticAnalyzer
{
    // Определение диагностики
    private const string _diagnosticId = "CP0003";
    private const string Category = "CustomRules";

    private static readonly DiagnosticDescriptor Rule = new(
        id: _diagnosticId,
        title: "Заголовок правила",
        messageFormat: "Сообщение для '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Описание правила."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    // Регистрация действий
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Анализ синтаксических узлов
        // context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);

        // Анализ символов (SemanticModel)
        // context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);

        // Анализ компиляции целиком
        // context.RegisterCompilationStartAction(AnalyzeCompilation);
    }

    // Логика анализа

    /// <summary>
    /// Анализ синтаксического узла
    /// </summary>
    private void AnalyzeMode(SyntaxNodeAnalysisContext context)
    {
        SyntaxNode node = context.Node;
        _ = context.SemanticModel;

        // Пример: проверка, что имя метода начинается с заглавной буквы
        if (node is MethodDeclarationSyntax method)
        {
            string name = method.Identifier.Text;
            if (!string.IsNullOrEmpty(name) && !char.IsUpper(name[0]))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    method.Identifier.GetLocation(),
                    name
                );
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Анализ символа
    /// </summary>
    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        ISymbol symbol = context.Symbol;

        // Пример: проверка, что интерфейсы начинаются с 'I'
        if (symbol is INamedTypeSymbol namedType &&
            namedType.TypeKind == TypeKind.Interface)
        {
            if (!namedType.Name.StartsWith("I"))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    namedType.Locations.FirstOrDefault(),
                    namedType.Name
                );
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Анализ компиляции
    /// </summary>
    private void AnalyzeCompilation(CompilationStartAnalysisContext context)
    {
        // Здесь можно зарегистрировать дополнительные действия
        // context.RegisterSyntaxNodeAction(...);
    }
}
