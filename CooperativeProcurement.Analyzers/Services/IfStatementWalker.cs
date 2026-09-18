using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CooperativeProcurement.Analyzers.Services;

/// <summary>
/// Обходчик для поиска if-конструкций
/// </summary>
public class IfStatementWalker : CSharpSyntaxWalker
{
    public List<(string ClassName, string MethodName, int Line, string Condition)> IfStatements { get; } = [];
    private string? _currentClassName;
    private string? _currentMethodName;
    private readonly string _fileName;

    public IfStatementWalker(string fileName)
    {
        this._fileName = fileName;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        this._currentClassName = node.Identifier.Text;
        base.VisitClassDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        this._currentMethodName = node.Identifier.Text;
        base.VisitMethodDeclaration(node);
    }

    public override void VisitIfStatement(IfStatementSyntax node)
    {
        if (this._currentClassName != null && this._currentMethodName != null)
        {
            Microsoft.CodeAnalysis.FileLinePositionSpan location = node.GetLocation().GetLineSpan();
            int line = location.StartLinePosition.Line + 1;

            IfStatements.Add((
                this._currentClassName,
                this._currentMethodName,
                line,
                node.Condition.ToString()
            ));
        }
        base.VisitIfStatement(node);
    }
}
