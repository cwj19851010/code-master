using System.Reflection;
using CodeMaster.CodeGenerator.Scanners;
using CodeMaster.CodeGenerator.Generators;

Console.WriteLine("===========================================");
Console.WriteLine("  CodeMaster 代码生成器");
Console.WriteLine("===========================================");

// 获取项目根目录
var currentDir = Directory.GetCurrentDirectory();
var solutionDir = Path.GetFullPath(Path.Combine(currentDir, ".."));

Console.WriteLine($"\n当前目录: {currentDir}");
Console.WriteLine($"解决方案目录: {solutionDir}");

// 加载 Domain 程序集
var domainAssemblyPath = Path.Combine(solutionDir, "CodeMaster.Domain", "bin", "Debug", "net8.0", "CodeMaster.Domain.dll");
if (!File.Exists(domainAssemblyPath))
{
    Console.WriteLine($"\n✗ 错误: 找不到 Domain 程序集，请先编译项目");
    Console.WriteLine($"  路径: {domainAssemblyPath}");
    return;
}

var domainAssembly = Assembly.LoadFrom(domainAssemblyPath);
Console.WriteLine($"✓ 加载 Domain 程序集成功");

// 扫描实体
Console.WriteLine("\n[1/4] 扫描实体...");
var scanner = new EntityScanner();
var entities = scanner.ScanEntities(domainAssembly);
Console.WriteLine($"✓ 找到 {entities.Count} 个实体");

foreach (var entity in entities)
{
    Console.WriteLine($"  - {entity.EntityName} ({entity.ModuleName})");
}

// 设置路径
var templatePath = Path.Combine(solutionDir, "CodeMaster.CodeGenerator", "Templates");
var outputBasePath = Path.Combine(solutionDir, "Generated");

Console.WriteLine($"\n模板路径: {templatePath}");
Console.WriteLine($"输出路径: {outputBasePath}");

// 创建生成器
var generator = new CodeMaster.CodeGenerator.Generators.CodeGenerator(templatePath, outputBasePath);

// 生成代码
Console.WriteLine("\n[2/4] 生成 DTO...");
foreach (var entity in entities)
{
    await generator.GenerateDtoAsync(entity);
}

Console.WriteLine("\n[3/4] 生成 Service...");
foreach (var entity in entities)
{
    await generator.GenerateServiceInterfaceAsync(entity);
    await generator.GenerateServiceAsync(entity);
}

Console.WriteLine("\n[4/4] 生成 Controller...");
foreach (var entity in entities)
{
    await generator.GenerateControllerAsync(entity);
}

Console.WriteLine("\n===========================================");
Console.WriteLine("  代码生成完成！");
Console.WriteLine("===========================================");
Console.WriteLine($"\n生成的代码位于: {outputBasePath}");
Console.WriteLine("\n提示:");
Console.WriteLine("  1. 请检查生成的代码");
Console.WriteLine("  2. 根据需要调整代码");
Console.WriteLine("  3. 将代码复制到对应的项目中");
Console.WriteLine("===========================================");
