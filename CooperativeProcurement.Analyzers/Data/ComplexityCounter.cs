using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CooperativeProcurement.Analyzers.Data
{
    /// <summary>
    /// Обходчик для подсчета цикломатической сложности
    /// </summary>
    public class ComplexityCounter : CSharpSyntaxWalker
    {
        public int Complexity { get; private set; } = 1;

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            Complexity++;
            base.VisitIfStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            Complexity++;
            base.VisitForStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            Complexity++;
            base.VisitForEachStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            Complexity++;
            base.VisitWhileStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            Complexity++;
            base.VisitDoStatement(node);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            // Каждый case добавляет сложность
            Complexity += node.Sections.Count;
            base.VisitSwitchStatement(node);
        }

        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            Complexity++; // Тернарный оператор ?:
            base.VisitConditionalExpression(node);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // && и || увеличивают сложность
            if (node.OperatorToken.Kind() == SyntaxKind.AmpersandAmpersandToken ||
                node.OperatorToken.Kind() == SyntaxKind.BarBarToken)
            {
                Complexity++;
            }
            base.VisitBinaryExpression(node);
        }
    }
}
