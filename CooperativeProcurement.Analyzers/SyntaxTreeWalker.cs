using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CooperativeProcurement.Analyzers
{
    /// <summary>
    /// Обходчик синтаксического дерева для сбора статистики
    /// </summary>
    public class SyntaxTreeWalker : CSharpSyntaxWalker
    {
        public int NodeCount { get; private set; }
        public int TokenCount { get; private set; }

        public SyntaxTreeWalker() : base(SyntaxWalkerDepth.Token)
        {
            NodeCount = 0;
            TokenCount = 0;
        }

        // Счетчик для всех типов узлов
        public override void Visit(SyntaxNode? node)
        {
            if (node != null)
            {
                NodeCount++;
                base.Visit(node);
            }
        }

        // Счетчик для токенов
        public override void VisitToken(SyntaxToken token)
        {
            TokenCount++;
            base.VisitToken(token);
        }

        // Вывод информации о классе
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            Console.WriteLine($"  Класс: {node.Identifier.Text} (строка {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1})");
            base.VisitClassDeclaration(node);
        }

        // Вывод информации о методе
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            string modifiers = string.Join(" ", node.Modifiers.Select(m => m.Text));
            Console.WriteLine($"    Метод: {modifiers} {node.ReturnType} {node.Identifier.Text}()");
            base.VisitMethodDeclaration(node);
        }

        // Вывод информации о свойстве
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            Console.WriteLine($"    Свойство: {node.Type} {node.Identifier.Text}");
            base.VisitPropertyDeclaration(node);
        }
    }
}
