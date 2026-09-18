using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CooperativeProcurement.Analyzers.Services;

/// <summary>
/// Обходчик для поиска методов с атрибутами
/// </summary>
public class AttributeWalker : CSharpSyntaxWalker
{
    public List<(string ClassName, string MethodName, int Line, string AttributeName)> AttributedMethods { get; } = [];
    private string? _currentClassName;
    private readonly string _fileName;

    public AttributeWalker(string fileName)
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
        if (this._currentClassName == null)
        {
            return;
        }

        foreach (AttributeListSyntax attributeList in node.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                string attrName = attribute.Name.ToString();
                Microsoft.CodeAnalysis.FileLinePositionSpan location = node.GetLocation().GetLineSpan();
                int line = location.StartLinePosition.Line + 1;

                AttributedMethods.Add((this._currentClassName, node.Identifier.Text, line, attrName));
            }
        }
        base.VisitMethodDeclaration(node);
    }
}
