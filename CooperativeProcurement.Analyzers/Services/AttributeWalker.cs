using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CooperativeProcurement.Analyzers.Services
{
    /// <summary>
    /// Обходчик для поиска методов с атрибутами
    /// </summary>
    public class AttributeWalker : CSharpSyntaxWalker
    {
        public List<(string ClassName, string MethodName, int Line, string AttributeName)> AttributedMethods { get; } = new();
        private string? _currentClassName;
        private readonly string _fileName;

        public AttributeWalker(string fileName)
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
            if (_currentClassName == null) return;

            foreach (var attributeList in node.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attrName = attribute.Name.ToString();
                    var location = node.GetLocation().GetLineSpan();
                    var line = location.StartLinePosition.Line + 1;

                    AttributedMethods.Add((_currentClassName, node.Identifier.Text, line, attrName));
                }
            }
            base.VisitMethodDeclaration(node);
        }
    }
}
