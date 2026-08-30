using CooperativeProcurement.Analyzers.Data;
using CooperativeProcurement.Analyzers.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;

namespace CooperativeProcurement.Analyzers
{
    class Program
    {
        private static List<ClassInfo> _allClasses = new();
        private static Dictionary<string, int> _namespaceStats = new();
        private static int _totalLinesOfCode = 0;
        static async Task Main(string[] args)
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
            var solution = await workspace.OpenSolutionAsync(solutionPath);
            Console.WriteLine($"Загружено {solution.Projects.Count()} проектов\n");

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
            await VisualizeClassHierarchy(solution);

            // Экспорт результатов
            await ExportResults();

            // Обход всех проектов и документов
            foreach (var project in solution.Projects)
            {
                Console.WriteLine($"\nПроект: {project.Name} ---");
                Console.WriteLine($"Документов: {project.Documents.Count()}");

                foreach (var document in project.Documents)
                {
                    Console.WriteLine($"\nДокумент: {document.Name}");

                    // Синтаксическое дерево
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();

                    // Создание и запуска обходчика
                    var walker = new SyntaxTreeWalker();
                    walker.Visit(root);

                    Console.WriteLine($"Всего узлов: {walker.NodeCount}");
                    Console.WriteLine($"Всего токенов: {walker.TokenCount}");
                }
            }

            Console.WriteLine("\nАнализ завершен");
        }

        static async Task AnalyzeAllClasses(Solution solution)
        {
            Console.WriteLine("Анализ всех классов в проекте");

            _allClasses.Clear();

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    // Пропускаем автоматически сгенерированные файлы
                    if (document.Name.EndsWith(".g.cs") || document.Name.EndsWith(".designer.cs"))
                        continue;

                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();

                    // Считаем строки кода
                    var lines = (await syntaxTree.GetTextAsync()).Lines;
                    _totalLinesOfCode += lines.Count;

                    // Создаем обходчик для классов
                    var walker = new AllClassesWalker(project.Name, document.Name, document.FilePath);
                    walker.Visit(root);

                    _allClasses.AddRange(walker.Classes);

                    // Собираем статистику по неймспейсам
                    foreach (var classInfo in walker.Classes)
                    {
                        if (!string.IsNullOrEmpty(classInfo.Namespace))
                        {
                            if (!_namespaceStats.ContainsKey(classInfo.Namespace))
                                _namespaceStats[classInfo.Namespace] = 0;
                            _namespaceStats[classInfo.Namespace]++;
                        }
                    }
                }
            }

            Console.WriteLine($"Всего найдено классов: {_allClasses.Count}");
            Console.WriteLine($"Всего строк кода: {_totalLinesOfCode}");
            Console.WriteLine($"Среднее количество строк на класс: {_totalLinesOfCode / (_allClasses.Count > 0 ? _allClasses.Count : 1):F0}");
        }

        static void PrintStatistics()
        {
            Console.WriteLine("Общая статистика проекта");

            var totalMethods = _allClasses.Sum(c => c.Methods.Count);
            var totalProperties = _allClasses.Sum(c => c.Properties.Count);
            var totalFields = _allClasses.Sum(c => c.Fields.Count);
            var totalMembers = _allClasses.Sum(c => c.MemberCount);

            Console.WriteLine($"Статистика по классам:");
            Console.WriteLine($"   Всего классов: {_allClasses.Count}");
            Console.WriteLine($"   Всего методов: {totalMethods}");
            Console.WriteLine($"   Всего свойств: {totalProperties}");
            Console.WriteLine($"   Всего полей: {totalFields}");
            Console.WriteLine($"   Всего членов классов: {totalMembers}");
            Console.WriteLine($"   Среднее количество членов на класс: {(double)totalMembers / _allClasses.Count:F2}");

            Console.WriteLine($"\n📂 Статистика по неймспейсам:");
            foreach (var ns in _namespaceStats.OrderByDescending(x => x.Value))
            {
                Console.WriteLine($"   • {ns.Key}: {ns.Value} классов");
            }

            var largestClass = _allClasses.OrderByDescending(c => c.MemberCount).FirstOrDefault();
            if (largestClass != null)
            {
                Console.WriteLine($"\nСамый большой класс: {largestClass.FullName}");
                Console.WriteLine($"   Членов: {largestClass.MemberCount}");
                Console.WriteLine($"   Методов: {largestClass.Methods.Count}");
                Console.WriteLine($"   Свойств: {largestClass.Properties.Count}");
                Console.WriteLine($"   Поля: {largestClass.Fields.Count}");
            }
        }

        static void PrintClassDetails()
        {
            Console.WriteLine("Детальная информация о классах");

            var groupedClasses = _allClasses
                .GroupBy(c => c.ProjectName)
                .OrderBy(g => g.Key);

            foreach (var group in groupedClasses)
            {
                Console.WriteLine($"\nПроект: {group.Key}");
                Console.WriteLine($"   Классов: {group.Count()}");
                Console.WriteLine("   " + new string('─', 60));

                foreach (var classInfo in group.OrderBy(c => c.Namespace).ThenBy(c => c.Name))
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
                        foreach (var method in classInfo.Methods.Take(3))
                        {
                            var modifiers = string.Join(" ", method.Modifiers);
                            Console.WriteLine($"        - {modifiers} {method.ReturnType} {method.Name}()");
                        }
                        if (classInfo.Methods.Count > 3)
                            Console.WriteLine($"        ... и еще {classInfo.Methods.Count - 3} методов");
                    }

                    // Показываем свойства
                    if (classInfo.Properties.Any())
                    {
                        Console.WriteLine($"      Свойства:");
                        foreach (var prop in classInfo.Properties.Take(5))
                        {
                            var modifiers = string.Join(" ", prop.Modifiers);
                            var accessors = "";
                            if (prop.HasGetter && prop.HasSetter) accessors = "{ get; set; }";
                            else if (prop.HasGetter) accessors = "{ get; }";
                            else if (prop.HasSetter) accessors = "{ set; }";
                            Console.WriteLine($"        - {modifiers} {prop.Type} {prop.Name} {accessors}");
                        }
                        if (classInfo.Properties.Count > 5)
                            Console.WriteLine($"        ... и еще {classInfo.Properties.Count - 5} свойств");
                    }
                }
            }
        }

        static async Task FindIfStatements(Solution solution)
        {
            Console.WriteLine("Поиск if-конструкций в коде");

            var ifStatements = new List<(string Class, string Method, string File, int Line, string Condition)>();

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (document.Name.EndsWith(".g.cs") || document.Name.EndsWith(".designer.cs"))
                        continue;

                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var walker = new IfStatementWalker(document.Name);
                    walker.Visit(root);

                    foreach (var ifInfo in walker.IfStatements)
                    {
                        ifStatements.Add((ifInfo.ClassName, ifInfo.MethodName, document.Name, ifInfo.Line, ifInfo.Condition));
                    }
                }
            }

            Console.WriteLine($"Всего найдено if-конструкций: {ifStatements.Count}\n");

            if (ifStatements.Any())
            {
                var byClass = ifStatements.GroupBy(x => x.Class);
                Console.WriteLine("Распределение по классам:");
                foreach (var group in byClass.OrderByDescending(g => g.Count()).Take(10))
                {
                    Console.WriteLine($"   • {group.Key}: {group.Count()} if-конструкций");
                }

                Console.WriteLine("\nДетализация (первые 10):");
                foreach (var (className, methodName, file, line, condition) in ifStatements.Take(10))
                {
                    var conditionPreview = condition.Length > 40
                        ? condition.Substring(0, 40) + "..."
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

        static async Task FindMethodsWithAttributes(Solution solution)
        {
            Console.WriteLine("Поиск методов с атрибутами");

            var attributedMethods = new List<(string Class, string Method, string File, int Line, string Attribute)>();

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (document.Name.EndsWith(".g.cs") || document.Name.EndsWith(".designer.cs"))
                        continue;

                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var walker = new AttributeWalker(document.Name);
                    walker.Visit(root);

                    foreach (var method in walker.AttributedMethods)
                    {
                        attributedMethods.Add((method.ClassName, method.MethodName, document.Name, method.Line, method.AttributeName));
                    }
                }
            }

            Console.WriteLine($"Всего найдено методов с атрибутами: {attributedMethods.Count}\n");

            if (attributedMethods.Any())
            {
                var byAttribute = attributedMethods.GroupBy(x => x.Attribute);
                Console.WriteLine("Распределение по атрибутам:");
                foreach (var group in byAttribute.OrderByDescending(g => g.Count()))
                {
                    Console.WriteLine($"   • {group.Key}: {group.Count()} методов");
                }

                Console.WriteLine("\nДетализация:");
                foreach (var (className, methodName, file, line, attribute) in attributedMethods)
                {
                    Console.WriteLine($"   • [{attribute}] {className}.{methodName}() → {file}:{line}");
                }
            }
        }

        static async Task AnalyzeMethodComplexity(Solution solution)
        {
            Console.WriteLine("Анализ цикломатической сложности методов ");
            var methodComplexity = new List<(string Class, string Method, int Complexity, int Lines, string File)>();

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (document.Name.EndsWith(".g.cs") || document.Name.EndsWith(".designer.cs"))
                        continue;

                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var walker = new ComplexityWalker(); // Исправлено: без аргументов
                    walker.Visit(root);

                    foreach (var info in walker.MethodComplexity)
                    {
                        methodComplexity.Add((info.ClassName, info.MethodName, info.Complexity, info.LineCount, document.Name));
                    }
                }
            }

            Console.WriteLine($"Проанализировано методов: {methodComplexity.Count}\n");

            if (methodComplexity.Any())
            {
                var avgComplexity = methodComplexity.Average(m => m.Complexity);
                Console.WriteLine($"Средняя цикломатическая сложность: {avgComplexity:F2}");

                var maxComplexity = methodComplexity.OrderByDescending(m => m.Complexity).First();
                Console.WriteLine($"\nСамый сложный метод: {maxComplexity.Class}.{maxComplexity.Method}");
                Console.WriteLine($"   Сложность: {maxComplexity.Complexity}");
                Console.WriteLine($"   Строк: {maxComplexity.Lines}");
                Console.WriteLine($"   Файл: {maxComplexity.File}");

                Console.WriteLine("\nМетоды с высокой сложностью (> 5):");
                foreach (var item in methodComplexity.Where(m => m.Complexity > 5).OrderByDescending(m => m.Complexity))
                {
                    Console.WriteLine($"   • {item.Class}.{item.Method}() → сложность {item.Complexity} (строк {item.Lines})");
                }
            }
        }

        static async Task VisualizeClassHierarchy(Solution solution)
        {
            Console.WriteLine("Визуализация иерархии классов");

            Console.WriteLine("Иерархия наследования:");
            Console.WriteLine(new string('─', 60));

            var classesWithBase = _allClasses.Where(c => c.BaseType != "object").ToList();

            if (classesWithBase.Any())
            {
                foreach (var classInfo in classesWithBase.OrderBy(c => c.BaseType))
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
                foreach (var classInfo in classesWithInterfaces.OrderBy(c => c.Name))
                {
                    Console.WriteLine($"   {classInfo.FullName} → реализует: {string.Join(", ", classInfo.Interfaces)}");
                }
            }
            else
            {
                Console.WriteLine("Нет классов, реализующих интерфейсы");
            }
        }

        static async Task ExportResults()
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

            var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                MaxDepth = 64
            });

            var outputPath = "analysis_results.json";
            await File.WriteAllTextAsync(outputPath, json);
            Console.WriteLine($"Результаты экспортированы в файл: {outputPath}");
            Console.WriteLine($"Размер файла: {new FileInfo(outputPath).Length / 1024.0:F2} KB");
        }
    }
}
