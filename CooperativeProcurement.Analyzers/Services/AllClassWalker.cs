using CooperativeProcurement.Analyzers.Data;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CooperativeProcurement.Analyzers.Services
{
    /// <summary>
    /// Обходчик для сбора информации о всех классах
    /// </summary>
    public class AllClassesWalker : CSharpSyntaxWalker
    {
        public List<ClassInfo> Classes { get; } = new();
        private readonly string _projectName;
        private readonly string _fileName;
        private readonly string _filePath;
        private string? _currentNamespace;

        public AllClassesWalker(string projectName, string fileName, string filePath)
        {
            _projectName = projectName;
            _fileName = fileName;
            _filePath = filePath;
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            _currentNamespace = node.Name.ToString();
            base.VisitNamespaceDeclaration(node);
        }

        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            _currentNamespace = node.Name.ToString();
            base.VisitFileScopedNamespaceDeclaration(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var classInfo = new ClassInfo
            {
                Name = node.Identifier.Text,
                FullName = string.IsNullOrEmpty(_currentNamespace)
                    ? node.Identifier.Text
                    : $"{_currentNamespace}.{node.Identifier.Text}",
                Namespace = _currentNamespace ?? "global",
                Modifiers = node.Modifiers.Select(m => m.Text).ToList(),
                BaseType = node.BaseList?.Types.FirstOrDefault()?.ToString() ?? "object",
                Interfaces = node.BaseList?.Types.Skip(1).Select(t => t.ToString()).ToList() ?? new List<string>(),
                Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                ProjectName = _projectName,
                FilePath = _filePath
            };

            // Собираем методы
            foreach (var member in node.Members.OfType<MethodDeclarationSyntax>())
            {
                var methodInfo = ExtractMethodInfo(member);
                classInfo.Methods.Add(methodInfo);
            }

            // Собираем свойства
            foreach (var member in node.Members.OfType<PropertyDeclarationSyntax>())
            {
                var propInfo = ExtractPropertyInfo(member);
                classInfo.Properties.Add(propInfo);
            }

            // Собираем поля
            foreach (var member in node.Members.OfType<FieldDeclarationSyntax>())
            {
                foreach (var variable in member.Declaration.Variables)
                {
                    var fieldInfo = new FieldInfo
                    {
                        Name = variable.Identifier.Text,
                        Type = member.Declaration.Type.ToString(),
                        Modifiers = member.Modifiers.Select(m => m.Text).ToList(),
                        Line = member.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    };
                    classInfo.Fields.Add(fieldInfo);
                }
            }

            Classes.Add(classInfo);
            base.VisitClassDeclaration(node);
        }

        private MethodInfo ExtractMethodInfo(MethodDeclarationSyntax node)
        {
            var info = new MethodInfo
            {
                Name = node.Identifier.Text,
                ReturnType = node.ReturnType.ToString(),
                Modifiers = node.Modifiers.Select(m => m.Text).ToList(),
                Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                HasBody = node.Body != null,
                IsAsync = node.Modifiers.Any(m => m.Text == "async"),
                IsStatic = node.Modifiers.Any(m => m.Text == "static"),
                IsPublic = node.Modifiers.Any(m => m.Text == "public"),
                Attributes = node.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Select(a => a.Name.ToString())
                    .ToList()
            };

            // Параметры
            foreach (var param in node.ParameterList.Parameters)
            {
                var paramInfo = new ParameterInfo
                {
                    Name = param.Identifier.Text,
                    Type = param.Type?.ToString() ?? "var",
                    HasDefaultValue = param.Default != null
                };
                if (param.Default != null)
                {
                    paramInfo.DefaultValue = param.Default.Value.ToString();
                }
                info.Parameters.Add(paramInfo);
            }

            // Количество строк
            if (node.Body != null)
            {
                var lines = node.Body.Statements.Count;
                info.LineCount = lines;
            }

            // Цикломатическая сложность
            var complexityWalker = new ComplexityCounter();
            complexityWalker.Visit(node);
            info.ComplexityScore = complexityWalker.Complexity;

            return info;
        }

        private PropertyInfo ExtractPropertyInfo(PropertyDeclarationSyntax node)
        {
            var info = new PropertyInfo
            {
                Name = node.Identifier.Text,
                Type = node.Type.ToString(),
                Modifiers = node.Modifiers.Select(m => m.Text).ToList(),
                Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                HasGetter = node.AccessorList?.Accessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration) ?? false,
                HasSetter = node.AccessorList?.Accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration) ?? false,
                IsAutoProperty = node.AccessorList?.Accessors.Any(a => a.Body == null && a.ExpressionBody == null) ?? false
            };
            return info;
        }
    }
}
