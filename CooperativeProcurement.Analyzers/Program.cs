using CooperativeProcurement.Analyzers.Analyzers;
using CooperativeProcurement.Analyzers.Data;
using CooperativeProcurement.Analyzers.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;

namespace CooperativeProcurement.Analyzers;

internal class Program
{
    private static readonly List<ClassInfo> _allClasses = [];
    private static readonly Dictionary<string, int> _namespaceStats = [];
    private static int _totalLinesOfCode = 0;
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Путь к файлу решения относительно текущей папки
        string solutionPath = Path.Combine("..", "..", "..", "..", "CooperativeProcurement.sln");

        // Проверка существования файла
        if (!File.Exists(solutionPath))
        {
            Console.WriteLine($"Файл решения не найден по пути: {solutionPath}");
            Console.WriteLine("Пожалуйста, укажите правильный путь к решению.");
            return;
        }

        Console.WriteLine("Синтаксический анализ кода с помощью Roslyn\n");
        Console.WriteLine($"Загрузка решения: {solutionPath}");

        // Создание рабочего пространства для загрузки решения
        var workspace = MSBuildWorkspace.Create();

        // Отключение логирования предупреждений о загрузке проектов
        workspace.WorkspaceFailed += (sender, e) =>
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Warning)
            {
                Console.WriteLine($"Предупреждение: {e.Diagnostic.Message}");
            }
        };

        // Загрузка решения
        Solution solution = await workspace.OpenSolutionAsync(solutionPath);
        Console.WriteLine($"Загружено проектов: {solution.Projects.Count()}\n");

        // Запрос режима анализатора
        Console.WriteLine("Выберите режим анализа комментариев:");
        Console.WriteLine("  1 - Синтаксический (через тривии)");
        Console.WriteLine("  2 - Семантический (через SemanticModel)");
        Console.Write("Ваш выбор [1]: ");

        string? choice = Console.ReadLine();
        MethodCommentAnalyzer.Mode = choice == "2"
            ? AnalysisMode.Semantic
            : AnalysisMode.Syntactic;

        Console.WriteLine($"Режим: {MethodCommentAnalyzer.Mode}\n");

        // Запуск проверки комментариев
        await CheckMethodComments(solution);

        // Анализ всех классов
        await AnalyzeAllClasses(solution);

        // Вывод общей статистики
        PrintStatistics();

        // Вывод детальной информации о классах
        PrintClassDetails();

        // Поиск if-конструкций
        await FindIfStatements(solution);

        // Поик методов с атрибутами
        await FindMethodsWithAttributes(solution);

        // Анализ сложности методов
        await AnalyzeMethodComplexity(solution);

        // Визуализация структуры
        VisualizeClassHierarchy(solution);

        // Экспорт результатов
        await ExportResults();

        // Обход всех проектов и документов
        foreach (Project project in solution.Projects)
        {
            Console.WriteLine($"\nПроект: {project.Name} ---");
            Console.WriteLine($"Документов: {project.Documents.Count()}");

            foreach (Document document in project.Documents)
            {
                Console.WriteLine($"\nДокумент: {document.Name}");

                // Синтаксическое дерево
                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null)
                {
                    continue;
                }

                SyntaxNode root = await syntaxTree.GetRootAsync();

                // Создание и запуска обходчика
                var walker = new SyntaxTreeWalker();
                walker.Visit(root);

                Console.WriteLine($"Всего узлов: {walker.NodeCount}");
                Console.WriteLine($"Всего токенов: {walker.TokenCount}");
            }
        }

        Console.WriteLine("\nАнализ завершен");
    }

    private static async Task CheckMethodComments(Solution solution)
    {
        Console.WriteLine("Проверка XML-комментариев у публичных методов");

        // Получение дескриптора правила
        var analyzer = new MethodCommentAnalyzer();
        DiagnosticDescriptor rule = analyzer.SupportedDiagnostics.First();

        var problems = new List<(string File, string Class, string Method, int Line)>();

        foreach (Project project in solution.Projects)
        {
            foreach (Document document in project.Documents)
            {
                if (document.Name.EndsWith(".g.cs") || document.Name.EndsWith(".designer.cs"))
                {
                    continue;
                }

                SyntaxTree? root = await document.GetSyntaxTreeAsync();
                if (root == null)
                {
                    continue;
                }

                SyntaxNode syntaxRoot = await root.GetRootAsync();
                SemanticModel? semanticModel = await document.GetSemanticModelAsync();

                IEnumerable<MethodDeclarationSyntax> methods = syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (MethodDeclarationSyntax method in methods)
                {
                    bool shouldReport = false;

                    // Выбираем подход в зависимости от режима
                    if (MethodCommentAnalyzer.Mode == AnalysisMode.Semantic && semanticModel != null)
                    {
                        // Семантический подход
                        IMethodSymbol? symbol = semanticModel.GetDeclaredSymbol(method);
                        if (symbol == null)
                        {
                            continue;
                        }

                        if (symbol.DeclaredAccessibility == Accessibility.Private)
                        {
                            continue;
                        }

                        if (symbol.IsOverride)
                        {
                            continue;
                        }

                        string? documentation = symbol.GetDocumentationCommentXml();
                        shouldReport = string.IsNullOrEmpty(documentation);
                    }
                    else
                    {
                        // Синтаксический подход
                        bool isPublic = method.Modifiers.Any(SyntaxKind.PublicKeyword);
                        bool isProtected = method.Modifiers.Any(SyntaxKind.ProtectedKeyword);
                        bool isInternal = method.Modifiers.Any(SyntaxKind.InternalKeyword);
                        if (!isPublic && !isProtected && !isInternal)
                        {
                            continue;
                        }

                        if (method.Modifiers.Any(SyntaxKind.OverrideKeyword))
                        {
                            continue;
                        }

                        shouldReport = !HasXmlComment(method);
                    }

                    if (shouldReport)
                    {
                        int line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        problems.Add((
                            document.Name,
                            GetClassName(method),
                            method.Identifier.Text,
                            line
                        ));
                    }
                }
            }
        }

        Console.WriteLine();
        if (problems.Any())
        {
            Console.WriteLine($"Найдено методов без комментариев: {problems.Count}\n");

            IEnumerable<IGrouping<string, (string File, string Class, string Method, int Line)>> grouped = problems.GroupBy(p => p.File);
            foreach (IGrouping<string, (string File, string Class, string Method, int Line)> group in grouped)
            {
                Console.WriteLine($"{group.Key}:");
                foreach ((string File, string Class, string Method, int Line) problem in group)
                {
                    Console.WriteLine($"   • {problem.Class}.{problem.Method}() → строка {problem.Line}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("Все публичные методы имеют XML-комментарии!");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Проверка наличия XML-комментария (синтаксический подход)
    /// </summary>
    private static bool HasXmlComment(MethodDeclarationSyntax method)
    {
        foreach (SyntaxTrivia trivia in method.GetLeadingTrivia())
        {
            if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                if (trivia.GetStructure() is DocumentationCommentTriviaSyntax doc)
                {
                    if (doc.Content.OfType<XmlElementSyntax>()
                        .Any(e => e.StartTag.Name.ToString() == "summary"))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Получение имени класса, в котором объявлен метод
    /// </summary>
    private static string GetClassName(MethodDeclarationSyntax method)
    {
        SyntaxNode? parent = method.Parent;
        while (parent != null)
        {
            if (parent is ClassDeclarationSyntax classDecl)
            {
                return classDecl.Identifier.Text;
            }

            parent = parent.Parent;
        }
        return "Unknown";
    }

    private static async Task AnalyzeAllClasses(Solution solution)
    {
        Console.WriteLine("Анализ всех классов в проекте");

        _allClasses.Clear();

        foreach (Project project in solution.Projects)
        {
            foreach (Document document in project.Documents)
            {
                // Пропускаем автоматически сгенерированные файлы
                if (document.Name.EndsWith(".g.cs") || document.Name.EndsWith(".designer.cs"))
                {
                    continue;
                }

                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null)
                {
                    continue;
                }

                SyntaxNode root = await syntaxTree.GetRootAsync();

                // Считаем строки кода
                Microsoft.CodeAnalysis.Text.TextLineCollection lines = (await syntaxTree.GetTextAsync()).Lines;
                _totalLinesOfCode += lines.Count;

                // Создаем обходчик для классов
                var walker = new AllClassesWalker(project.Name, document.Name, document.FilePath);
                walker.Visit(root);

                _allClasses.AddRange(walker.Classes);

                // Собираем статистику по пространствам имен
                foreach (ClassInfo classInfo in walker.Classes)
                {
                    if (!string.IsNullOrEmpty(classInfo.Namespace))
                    {
                        if (!_namespaceStats.ContainsKey(classInfo.Namespace))
                        {
                            _namespaceStats[classInfo.Namespace] = 0;
                        }

                        _namespaceStats[classInfo.Namespace]++;
                    }
                }
            }
        }

        Console.WriteLine($"Всего найдено классов: {_allClasses.Count}");
        Console.WriteLine($"Всего строк кода: {_totalLinesOfCode}");
        Console.WriteLine($"Среднее количество строк на класс: {_totalLinesOfCode / (_allClasses.Count > 0 ? _allClasses.Count : 1):F0}");
    }

    private static void PrintStatistics()
    {
        Console.WriteLine("Общая статистика проекта");

        int totalMethods = _allClasses.Sum(c => c.Methods.Count);
        int totalProperties = _allClasses.Sum(c => c.Properties.Count);
        int totalFields = _allClasses.Sum(c => c.Fields.Count);
        int totalMembers = _allClasses.Sum(c => c.MemberCount);

        Console.WriteLine($"Статистика по классам:");
        Console.WriteLine($"   Всего классов: {_allClasses.Count}");
        Console.WriteLine($"   Всего методов: {totalMethods}");
        Console.WriteLine($"   Всего свойств: {totalProperties}");
        Console.WriteLine($"   Всего полей: {totalFields}");
        Console.WriteLine($"   Всего членов классов: {totalMembers}");
        Console.WriteLine($"   Среднее количество членов на класс: {(double)totalMembers / _allClasses.Count:F2}");

        Console.WriteLine($"\nСтатистика по пространствам имен:");
        foreach (KeyValuePair<string, int> ns in _namespaceStats.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"   • {ns.Key}: {ns.Value} классов");
        }

        ClassInfo? largestClass = _allClasses.OrderByDescending(c => c.MemberCount).FirstOrDefault();
        if (largestClass != null)
        {
            Console.WriteLine($"\nСамый большой класс: {largestClass.FullName}");
            Console.WriteLine($"   Членов: {largestClass.MemberCount}");
            Console.WriteLine($"   Методов: {largestClass.Methods.Count}");
            Console.WriteLine($"   Свойств: {largestClass.Properties.Count}");
            Console.WriteLine($"   Поля: {largestClass.Fields.Count}");
        }
    }

    private static void PrintClassDetails()
    {
        Console.WriteLine("Детальная информация о классах");

        IOrderedEnumerable<IGrouping<string, ClassInfo>> groupedClasses = _allClasses
            .GroupBy(c => c.ProjectName)
            .OrderBy(g => g.Key);

        foreach (IGrouping<string, ClassInfo>? group in groupedClasses)
        {
            Console.WriteLine($"\nПроект: {group.Key}");
            Console.WriteLine($"   Классов: {group.Count()}");
            Console.WriteLine("   " + new string('─', 60));

            foreach (ClassInfo? classInfo in group.OrderBy(c => c.Namespace).ThenBy(c => c.Name))
            {
                Console.WriteLine($"\n   {classInfo.FullName}");
                Console.WriteLine($"      Модификаторы: {string.Join(" ", classInfo.Modifiers)}");
                Console.WriteLine($"      Базовый класс: {classInfo.BaseType}");
                Console.WriteLine($"      Интерфейсы: {(classInfo.Interfaces.Any() ? string.Join(", ", classInfo.Interfaces) : "нет")}");
                Console.WriteLine($"      Методов: {classInfo.Methods.Count}");
                Console.WriteLine($"      Свойств: {classInfo.Properties.Count}");
                Console.WriteLine($"      Поля: {classInfo.Fields.Count}");
                Console.WriteLine($"      Строка: {classInfo.Line}");

                // Показываем первые 3 метода
                if (classInfo.Methods.Any())
                {
                    Console.WriteLine($"      Методы (первые 3):");
                    foreach (MethodInfo? method in classInfo.Methods.Take(3))
                    {
                        string modifiers = string.Join(" ", method.Modifiers);
                        Console.WriteLine($"        - {modifiers} {method.ReturnType} {method.Name}()");
                    }
                    if (classInfo.Methods.Count > 3)
                    {
                        Console.WriteLine($"        ... и еще {classInfo.Methods.Count - 3} методов");
                    }
                }

                // Показываем свойства
                if (classInfo.Properties.Any())
                {
                    Console.WriteLine($"      Свойства:");
                    foreach (PropertyInfo? prop in classInfo.Properties.Take(5))
                    {
                        string modifiers = string.Join(" ", prop.Modifiers);
                        string accessors = "";
                        if (prop.HasGetter && prop.HasSetter)
                        {
                            accessors = "{ get; set; }";
                        }
                        else if (prop.HasGetter)
                        {
                            accessors = "{ get; }";
                        }
                        else if (prop.HasSetter)
                        {
                            accessors = "{ set; }";
                        }

                        Console.WriteLine($"        - {modifiers} {prop.Type} {prop.Name} {accessors}");
                    }
                    if (classInfo.Properties.Count > 5)
                    {
                        Console.WriteLine($"        ... и еще {classInfo.Properties.Count - 5} свойств");
                    }
                }
            }
        }
    }

    private static async Task FindIfStatements(Solution solution)
    {
        Console.WriteLine("Поиск if-конструкций в коде");

        var ifStatements = new List<(string Class, string Method, string File, int Line, string Condition)>();

        foreach (Project project in solution.Projects)
        {
            foreach (Document document in project.Documents)
            {
                if (document.Name.EndsWith(".g.cs") || document.Name.EndsWith(".designer.cs"))
                {
                    continue;
                }

                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null)
                {
                    continue;
                }

                SyntaxNode root = await syntaxTree.GetRootAsync();
                var walker = new IfStatementWalker(document.Name);
                walker.Visit(root);

                foreach ((string ClassName, string MethodName, int Line, string Condition) in walker.IfStatements)
                {
                    ifStatements.Add((ClassName, MethodName, document.Name, Line, Condition));
                }
            }
        }

        Console.WriteLine($"Всего найдено if-конструкций: {ifStatements.Count}\n");

        if (ifStatements.Any())
        {
            IEnumerable<IGrouping<string, (string Class, string Method, string File, int Line, string Condition)>> byClass = ifStatements.GroupBy(x => x.Class);
            Console.WriteLine("Распределение по классам:");
            foreach (IGrouping<string, (string Class, string Method, string File, int Line, string Condition)>? group in byClass.OrderByDescending(g => g.Count()).Take(10))
            {
                Console.WriteLine($"   • {group.Key}: {group.Count()} if-конструкций");
            }

            Console.WriteLine("\nДетализация (первые 10):");
            foreach ((string className, string methodName, string file, int line, string condition) in ifStatements.Take(10))
            {
                string conditionPreview = condition.Length > 40
                    ? condition[..40] + "..."
                    : condition;
                Console.WriteLine($"   • {className}.{methodName}() → {file}:{line}");
                Console.WriteLine($"     if ({conditionPreview})");
            }

            if (ifStatements.Count > 10)
            {
                Console.WriteLine($"   ... и еще {ifStatements.Count - 10} конструкций");
            }
        }
    }

    private static async Task FindMethodsWithAttributes(Solution solution)
    {
        Console.WriteLine("Поиск методов с атрибутами");

        var attributedMethods = new List<(string Class, string Method, string File, int Line, string Attribute)>();

        foreach (Project project in solution.Projects)
        {
            foreach (Document document in project.Documents)
            {
                if (document.Name.EndsWith(".g.cs") || document.Name.EndsWith(".designer.cs"))
                {
                    continue;
                }

                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null)
                {
                    continue;
                }

                SyntaxNode root = await syntaxTree.GetRootAsync();
                var walker = new AttributeWalker(document.Name);
                walker.Visit(root);

                foreach ((string ClassName, string MethodName, int Line, string AttributeName) in walker.AttributedMethods)
                {
                    attributedMethods.Add((ClassName, MethodName, document.Name, Line, AttributeName));
                }
            }
        }

        Console.WriteLine($"Всего найдено методов с атрибутами: {attributedMethods.Count}\n");

        if (attributedMethods.Any())
        {
            IEnumerable<IGrouping<string, (string Class, string Method, string File, int Line, string Attribute)>> byAttribute = attributedMethods.GroupBy(x => x.Attribute);
            Console.WriteLine("Распределение по атрибутам:");
            foreach (IGrouping<string, (string Class, string Method, string File, int Line, string Attribute)>? group in byAttribute.OrderByDescending(g => g.Count()))
            {
                Console.WriteLine($"   • {group.Key}: {group.Count()} методов");
            }

            Console.WriteLine("\nДетализация:");
            foreach ((string className, string methodName, string file, int line, string attribute) in attributedMethods)
            {
                Console.WriteLine($"   • [{attribute}] {className}.{methodName}() → {file}:{line}");
            }
        }
    }

    private static async Task AnalyzeMethodComplexity(Solution solution)
    {
        Console.WriteLine("Анализ цикломатической сложности методов ");
        var methodComplexity = new List<(string Class, string Method, int Complexity, int Lines, string File)>();

        foreach (Project project in solution.Projects)
        {
            foreach (Document document in project.Documents)
            {
                if (document.Name.EndsWith(".g.cs") || document.Name.EndsWith(".designer.cs"))
                {
                    continue;
                }

                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null)
                {
                    continue;
                }

                SyntaxNode root = await syntaxTree.GetRootAsync();
                var walker = new ComplexityWalker(); // Исправлено: без аргументов
                walker.Visit(root);

                foreach ((string ClassName, string MethodName, int Complexity, int LineCount) in walker.MethodComplexity)
                {
                    methodComplexity.Add((ClassName, MethodName, Complexity, LineCount, document.Name));
                }
            }
        }

        Console.WriteLine($"Проанализировано методов: {methodComplexity.Count}\n");

        if (methodComplexity.Any())
        {
            double avgComplexity = methodComplexity.Average(m => m.Complexity);
            Console.WriteLine($"Средняя цикломатическая сложность: {avgComplexity:F2}");

            (string Class, string Method, int Complexity, int Lines, string File) maxComplexity = methodComplexity.OrderByDescending(m => m.Complexity).First();
            Console.WriteLine($"\nСамый сложный метод: {maxComplexity.Class}.{maxComplexity.Method}");
            Console.WriteLine($"   Сложность: {maxComplexity.Complexity}");
            Console.WriteLine($"   Строк: {maxComplexity.Lines}");
            Console.WriteLine($"   Файл: {maxComplexity.File}");

            Console.WriteLine("\nМетоды с высокой сложностью (> 5):");
            foreach ((string Class, string Method, int Complexity, int Lines, string File) item in methodComplexity.Where(m => m.Complexity > 5).OrderByDescending(m => m.Complexity))
            {
                Console.WriteLine($"   • {item.Class}.{item.Method}() → сложность {item.Complexity} (строк {item.Lines})");
            }
        }
    }

    private static void VisualizeClassHierarchy(Solution solution)
    {
        Console.WriteLine("Визуализация иерархии классов");

        Console.WriteLine("Иерархия наследования:");
        Console.WriteLine(new string('─', 60));

        var classesWithBase = _allClasses.Where(c => c.BaseType != "object").ToList();

        if (classesWithBase.Any())
        {
            foreach (ClassInfo? classInfo in classesWithBase.OrderBy(c => c.BaseType))
            {
                Console.WriteLine($"   {classInfo.FullName} → наследует: {classInfo.BaseType}");
            }
        }
        else
        {
            Console.WriteLine("Нет классов с наследованием (кроме object)");
        }

        Console.WriteLine("\nИнтерфейсы:");
        var classesWithInterfaces = _allClasses.Where(c => c.Interfaces.Any()).ToList();

        if (classesWithInterfaces.Any())
        {
            foreach (ClassInfo? classInfo in classesWithInterfaces.OrderBy(c => c.Name))
            {
                Console.WriteLine($"   {classInfo.FullName} → реализует: {string.Join(", ", classInfo.Interfaces)}");
            }
        }
        else
        {
            Console.WriteLine("Нет классов, реализующих интерфейсы");
        }
    }

    private static async Task ExportResults()
    {
        Console.WriteLine("Экспорт результатов");

        var exportData = new
        {
            TotalClasses = _allClasses.Count,
            TotalLines = _totalLinesOfCode,
            NamespaceStats = _namespaceStats,
            Classes = _allClasses.Select(c => new
            {
                c.Name,
                c.FullName,
                c.Namespace,
                c.Modifiers,
                c.BaseType,
                c.Interfaces,
                MethodCount = c.Methods.Count,
                PropertyCount = c.Properties.Count,
                FieldCount = c.Fields.Count,
                c.Line,
                c.ProjectName,
                Methods = c.Methods.Select(m => new
                {
                    m.Name,
                    m.ReturnType,
                    m.Modifiers,
                    m.Line,
                    m.IsAsync,
                    m.IsStatic,
                    m.IsPublic,
                    m.Attributes,
                    m.ComplexityScore,
                    ParameterCount = m.Parameters.Count
                }),
                Properties = c.Properties.Select(p => new
                {
                    p.Name,
                    p.Type,
                    p.Modifiers,
                    p.HasGetter,
                    p.HasSetter,
                    p.IsAutoProperty
                })
            })
        };

        string json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            MaxDepth = 64
        });

        string outputPath = "analysis_results.json";
        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine($"Результаты экспортированы в файл: {outputPath}");
        Console.WriteLine($"Размер файла: {new FileInfo(outputPath).Length / 1024.0:F2} KB");
    }
}
