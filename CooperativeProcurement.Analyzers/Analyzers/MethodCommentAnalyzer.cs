using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CooperativeProcurement.Analyzers.Analyzers;

/// <summary>
/// Анализатор, проверяющий наличие XML-комментариев у публичных методов
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MethodCommentAnalyzer : DiagnosticAnalyzer
{
    // Константы для диагностики
    private const string DiagnosticId = "CP0001";
    private const string Category = "Documentation";
    public static AnalysisMode Mode { get; set; } = AnalysisMode.Syntactic;

    // Описание диагностического правила (без использовани ресурсов)
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Публичные методы должны иметь XML-комментарии",
        messageFormat: "Публичный метод '{0}' не содержит XML-комментарий (/// <summary>...)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Все публичные методы должны быть задокументированы с помощью XML-комментариев для улучшения читаемости и поддержки кода."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        // Настройка контекста
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Регистрация действий для методов
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    ///<summary>
    /// Точка входа анализатора (выбор подхода в зависимости от режима)
    ///</summary>
    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        switch (Mode)
        {
            case AnalysisMode.Syntactic:
                AnalyzeMethodSyntactic(context);
                break;
            case AnalysisMode.Semantic:
                AnalyzeMethodSemantic(context);
                break;
            default:
                AnalyzeMethodSyntactic(context);
                break;
        }
    }

    /// <summary>
    /// Семантический анализ метода на наличие комментария
    /// </summary>
    private void AnalyzeMethodSyntactic(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Проверка публичности метода
        bool isPublic = methodDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword);
        bool isProtected = methodDeclaration.Modifiers.Any(SyntaxKind.ProtectedKeyword);
        bool isInternal = methodDeclaration.Modifiers.Any(SyntaxKind.InternalKeyword);

        // Пропуск приватных методов
        if (!isPublic && !isProtected && !isInternal)
        {
            return;
        }

        // Пропуск переопределений
        if (methodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
        {
            return;
        }

        // Проверка наличия XML-комментария
        bool hasComment = HasXmlComment(methodDeclaration);

        if (!hasComment)
        {
            // Создаем диагностику
            var diagnostic = Diagnostic.Create(
                Rule,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text
            );

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Проверка наличия XML-комментария перед методом
    /// </summary>
    private bool HasXmlComment(MethodDeclarationSyntax method)
    {
        // Получение всех тривии перед методом
        SyntaxTriviaList leadingTrivia = method.GetLeadingTrivia();

        foreach (SyntaxTrivia trivia in leadingTrivia)
        {
            // Проверка наличия комментария документации
            if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                // Проверка содержания тега <summary>
                SyntaxNode? structure = trivia.GetStructure();
                if (structure != null)
                {
                    if (structure is DocumentationCommentTriviaSyntax documentation)
                    {
                        // Поиск тега summary
                        bool hasSummary = documentation.Content
                            .OfType<XmlElementSyntax>()
                            .Any(e => e.StartTag.Name.ToString() == "summary");

                        if (hasSummary)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Анализ с использованием SemanticModel
    /// </summary>
    private void AnalyzeMethodSemantic(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;

        // Получение символа метода
        IMethodSymbol? methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol == null)
        {
            return;
        }

        // Проверяем доступность
        if (methodSymbol.DeclaredAccessibility == Accessibility.Private)
        {
            return;
        }

        // Проверка переопределения
        if (methodSymbol.IsOverride)
        {
            return;
        }

        // Проверка наличия документации через символ
        string? documentation = methodSymbol.GetDocumentationCommentXml();
        if (string.IsNullOrEmpty(documentation))
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text
            );
            context.ReportDiagnostic(diagnostic);
        }
    }
}
