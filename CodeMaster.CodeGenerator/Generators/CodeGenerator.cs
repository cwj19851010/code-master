using Scriban;
using CodeMaster.CodeGenerator.Models;

namespace CodeMaster.CodeGenerator.Generators;

/// <summary>
/// 代码生成器
/// </summary>
public class CodeGenerator
{
    private readonly string _templatePath;
    private readonly string _outputPath;

    public CodeGenerator(string templatePath, string outputPath)
    {
        _templatePath = templatePath;
        _outputPath = outputPath;
    }

    /// <summary>
    /// 生成 Controller 代码
    /// </summary>
    public async Task GenerateControllerAsync(EntityMetadata entity)
    {
        var templateFile = Path.Combine(_templatePath, "ControllerTemplate.txt");
        var template = await File.ReadAllTextAsync(templateFile);

        var scribanTemplate = Template.Parse(template);
        var result = await scribanTemplate.RenderAsync(new
        {
            entity_name = entity.EntityName,
            business_name = entity.BusinessName,
            business_name_lower = ToCamelCase(entity.BusinessName),
            module_name = entity.ModuleName,
            description = entity.Description
        });

        var outputDir = Path.Combine(_outputPath, "Controllers", entity.ModuleName);
        Directory.CreateDirectory(outputDir);

        var outputFile = Path.Combine(outputDir, $"{entity.EntityName}Controller.cs");
        await File.WriteAllTextAsync(outputFile, result);

        Console.WriteLine($"✓ 生成 Controller: {outputFile}");
    }

    /// <summary>
    /// 生成 DTO 代码
    /// </summary>
    public async Task GenerateDtoAsync(EntityMetadata entity)
    {
        var templateFile = Path.Combine(_templatePath, "DtoTemplate.txt");
        var template = await File.ReadAllTextAsync(templateFile);

        var scribanTemplate = Template.Parse(template);
        var result = await scribanTemplate.RenderAsync(new
        {
            entity_name = entity.EntityName,
            module_name = entity.ModuleName,
            description = entity.Description,
            properties = entity.Properties.Select(p => new
            {
                name = p.Name,
                type = p.Type,
                is_nullable = p.IsNullable,
                description = p.Description
            }).ToList()
        });

        var outputDir = Path.Combine(_outputPath, "Dtos", entity.ModuleName);
        Directory.CreateDirectory(outputDir);

        var outputFile = Path.Combine(outputDir, $"{entity.EntityName}Dto.cs");
        await File.WriteAllTextAsync(outputFile, result);

        Console.WriteLine($"✓ 生成 DTO: {outputFile}");
    }

    /// <summary>
    /// 生成 Service 接口代码
    /// </summary>
    public async Task GenerateServiceInterfaceAsync(EntityMetadata entity)
    {
        var templateFile = Path.Combine(_templatePath, "ServiceInterfaceTemplate.txt");
        var template = await File.ReadAllTextAsync(templateFile);

        var scribanTemplate = Template.Parse(template);
        var result = await scribanTemplate.RenderAsync(new
        {
            entity_name = entity.EntityName,
            module_name = entity.ModuleName,
            description = entity.Description
        });

        var outputDir = Path.Combine(_outputPath, "Services", entity.ModuleName);
        Directory.CreateDirectory(outputDir);

        var outputFile = Path.Combine(outputDir, $"I{entity.EntityName}Service.cs");
        await File.WriteAllTextAsync(outputFile, result);

        Console.WriteLine($"✓ 生成 Service 接口: {outputFile}");
    }

    /// <summary>
    /// 生成 Service 实现代码
    /// </summary>
    public async Task GenerateServiceAsync(EntityMetadata entity)
    {
        var templateFile = Path.Combine(_templatePath, "ServiceTemplate.txt");
        var template = await File.ReadAllTextAsync(templateFile);

        var scribanTemplate = Template.Parse(template);
        var result = await scribanTemplate.RenderAsync(new
        {
            entity_name = entity.EntityName,
            module_name = entity.ModuleName,
            description = entity.Description,
            properties = entity.Properties.Select(p => new
            {
                name = p.Name,
                type = p.Type,
                is_nullable = p.IsNullable,
                description = p.Description
            }).ToList()
        });

        var outputDir = Path.Combine(_outputPath, "Services", entity.ModuleName);
        Directory.CreateDirectory(outputDir);

        var outputFile = Path.Combine(outputDir, $"{entity.EntityName}Service.cs");
        await File.WriteAllTextAsync(outputFile, result);

        Console.WriteLine($"✓ 生成 Service 实现: {outputFile}");
    }

    /// <summary>
    /// 转换为驼峰命名
    /// </summary>
    private string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
