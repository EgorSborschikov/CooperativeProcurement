using CooperativeProcurement.Analyzers.Data;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CooperativeProcurement.Analyzers.Services
{
    /// <summary>
    /// Обходчик для анализа сложности методов
    /// </summary>
    public class ComplexityWalker : CSharpSyntaxWalker
    {
        public List<(string ClassName, string MethodName, int Complexity, int LineCount)> MethodComplexity { get; } = new();
        private string? _currentClassName;

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _currentClassName = node.Identifier.Text;
            base.VisitClassDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (_currentClassName == null) return;

            var counter = new ComplexityCounter();
            counter.Visit(node);

            var lineCount = node.Body?.Statements.Count ?? 0;

            MethodComplexity.Add((
                _currentClassName,
                node.Identifier.Text,
                counter.Complexity,
                lineCount
            ));
            base.VisitMethodDeclaration(node);
        }
    }
}
