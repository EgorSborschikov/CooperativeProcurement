using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CooperativeProcurement.Analyzers.Services
{
    /// <summary>
    /// Обходчик для поиска if-конструкций
    /// </summary>
    public class IfStatementWalker : CSharpSyntaxWalker
    {
        public List<(string ClassName, string MethodName, int Line, string Condition)> IfStatements { get; } = new();
        private string? _currentClassName;
        private string? _currentMethodName;
        private readonly string _fileName;

        public IfStatementWalker(string fileName)
        {
            _fileName = fileName;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _currentClassName = node.Identifier.Text;
            base.VisitClassDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            _currentMethodName = node.Identifier.Text;
            base.VisitMethodDeclaration(node);
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            if (_currentClassName != null && _currentMethodName != null)
            {
                var location = node.GetLocation().GetLineSpan();
                var line = location.StartLinePosition.Line + 1;

                IfStatements.Add((
                    _currentClassName,
                    _currentMethodName,
                    line,
                    node.Condition.ToString()
                ));
            }
            base.VisitIfStatement(node);
        }
    }
}
