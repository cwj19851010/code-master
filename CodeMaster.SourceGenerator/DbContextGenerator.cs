using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeMaster.SourceGenerator
{
    [Generator]
    public class DbContextGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // 不需要 SyntaxReceiver，直接扫描编译中的所有类型
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;
            var entityClasses = new List<EntityInfo>();
            var tableNames = new HashSet<string>(); // 用于检测重复的表名

            // 扫描所有引用的程序集中的类型
            foreach (var reference in compilation.References)
            {
                var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (assemblySymbol != null)
                {
                    // 只扫描 CodeMaster.Domain 程序集
                    if (assemblySymbol.Name == "CodeMaster.Domain")
                    {
                        var types = GetAllTypes(assemblySymbol.GlobalNamespace);
                        foreach (var type in types)
                        {
                            if (type.TypeKind == TypeKind.Class &&
                                !type.IsAbstract &&
                                !type.IsStatic &&
                                ImplementsIEntity(type))
                            {
                                var entityInfo = AnalyzeEntity(type);

                                // 检查表名是否重复
                                if (!tableNames.Contains(entityInfo.TableName))
                                {
                                    tableNames.Add(entityInfo.TableName);
                                    entityClasses.Add(entityInfo);
                                }
                            }
                        }
                    }
                }
            }

            if (entityClasses.Count == 0)
                return;

            // 生成 DbContext partial class
            var dbContextSource = GenerateDbContextPartial(entityClasses);
            context.AddSource("CodeMasterDbContext.g.cs", SourceText.From(dbContextSource, Encoding.UTF8));

            // 生成 EntityConfiguration 类
            foreach (var entity in entityClasses)
            {
                var configSource = GenerateEntityConfiguration(entity);
                context.AddSource($"{entity.ClassName}Configuration.g.cs", SourceText.From(configSource, Encoding.UTF8));
            }
        }

        private IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                yield return type;
            }

            foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (var type in GetAllTypes(childNamespace))
                {
                    yield return type;
                }
            }
        }

        private bool ImplementsIEntity(INamedTypeSymbol classSymbol)
        {
            // 检查是否实现了 IEntity 接口
            foreach (var iface in classSymbol.AllInterfaces)
            {
                if (iface.Name == "IEntity" && iface.ContainingNamespace.ToDisplayString() == "CodeMaster.Core.Entities")
                {
                    return true;
                }
            }
            return false;
        }

        private bool InheritsFromEntityBase(INamedTypeSymbol classSymbol)
        {
            var baseType = classSymbol.BaseType;
            while (baseType != null)
            {
                var baseTypeName = baseType.Name;
                if (baseTypeName == "EntityBase" || baseTypeName == "TenantEntityBase")
                    return true;
                baseType = baseType.BaseType;
            }
            return false;
        }

        private EntityInfo AnalyzeEntity(INamedTypeSymbol typeSymbol)
        {
            var entityInfo = new EntityInfo
            {
                ClassName = typeSymbol.Name,
                Namespace = typeSymbol.ContainingNamespace.ToDisplayString(),
                InheritsEntityBase = InheritsFromEntityBase(typeSymbol)
            };

            // 1. 检查 SugarTable 特性获取表名
            var sugarTableAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "SugarTableAttribute" || a.AttributeClass?.Name == "SugarTable");

            if (sugarTableAttr != null && sugarTableAttr.ConstructorArguments.Length > 0)
            {
                // 有 SugarTable 特性，使用其 TableName
                entityInfo.TableName = sugarTableAttr.ConstructorArguments[0].Value?.ToString() ?? ToSnakeCase(typeSymbol.Name);
            }
            else
            {
                // 没有 SugarTable 特性，使用默认规则（类名转 snake_case 并复数化）
                var tableName = ToSnakeCase(typeSymbol.Name);
                entityInfo.TableName = tableName.EndsWith("s")
                    ? tableName
                    : tableName + "s";
            }

            // 2. 分析所有属性
            var allMembers = new List<ISymbol>();
            var currentType = typeSymbol;
            while (currentType != null)
            {
                allMembers.AddRange(currentType.GetMembers());
                currentType = currentType.BaseType;
            }

            var properties = allMembers.OfType<IPropertySymbol>();

            foreach (var property in properties)
            {
                bool shouldIgnore = false;
                string columnName = null;
                bool isPrimaryKey = false;

                // 检查属性的所有特性
                foreach (var attribute in property.GetAttributes())
                {
                    var attrName = attribute.AttributeClass?.Name;

                    // 检查 SugarColumn 特性
                    if (attrName == "SugarColumnAttribute" || attrName == "SugarColumn")
                    {
                        foreach (var namedArg in attribute.NamedArguments)
                        {
                            if (namedArg.Key == "IsIgnore" && namedArg.Value.Value is bool isIgnore && isIgnore)
                            {
                                shouldIgnore = true;
                            }
                            else if (namedArg.Key == "ColumnName" && namedArg.Value.Value is string colName)
                            {
                                columnName = colName;
                            }
                            else if (namedArg.Key == "IsPrimaryKey" && namedArg.Value.Value is bool isPk && isPk)
                            {
                                isPrimaryKey = true;
                            }
                        }
                    }

                    // 检查 Navigate 特性（SqlSugar 导航属性）
                    if (attrName == "NavigateAttribute" || attrName == "Navigate")
                    {
                        shouldIgnore = true;
                    }
                }

                if (shouldIgnore)
                {
                    entityInfo.IgnoredProperties.Add(property.Name);
                }
                else
                {
                    // 如果没有指定列名，使用默认规则
                    if (string.IsNullOrEmpty(columnName))
                    {
                        columnName = ToSnakeCase(property.Name);
                    }

                    entityInfo.PropertyColumns[property.Name] = columnName;

                    if (isPrimaryKey)
                    {
                        entityInfo.PrimaryKeys.Add(property.Name);
                    }
                }
            }

            // 3. 如果没有找到主键，判断是否继承 EntityBase
            if (entityInfo.PrimaryKeys.Count == 0)
            {
                if (entityInfo.InheritsEntityBase)
                {
                    // 继承 EntityBase，默认主键是 Id
                    entityInfo.PrimaryKeys.Add("Id");
                }
                // 否则是无主键表
            }

            return entityInfo;
        }

        private string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 处理连续大写字母的情况（如 "HTTPServer" -> "http_server"）
            var result = Regex.Replace(input, "([A-Z]+)([A-Z][a-z])", "$1_$2");
            // 处理普通驼峰命名（如 "userId" -> "user_id"）
            result = Regex.Replace(result, "([a-z0-9])([A-Z])", "$1_$2");
            return result.ToLower();
        }

        private string GenerateDbContextPartial(List<EntityInfo> entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine("using CodeMaster.Migrator.Persistence.EfCore.Configurations;");

            // 添加所有实体的命名空间
            var namespaces = entities.Select(e => e.Namespace).Distinct().OrderBy(n => n);
            foreach (var ns in namespaces)
            {
                sb.AppendLine($"using {ns};");
            }

            sb.AppendLine();
            sb.AppendLine("namespace CodeMaster.Migrator.Persistence.EfCore");
            sb.AppendLine("{");
            sb.AppendLine("    public partial class CodeMasterDbContext");
            sb.AppendLine("    {");

            // 生成 DbSet 属性
            foreach (var entity in entities.OrderBy(e => e.ClassName))
            {
                sb.AppendLine($"        public DbSet<{entity.ClassName}> {entity.ClassName}s {{ get; set; }}");
            }

            sb.AppendLine();
            sb.AppendLine("        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)");
            sb.AppendLine("        {");

            // 应用所有配置
            foreach (var entity in entities.OrderBy(e => e.ClassName))
            {
                sb.AppendLine($"            modelBuilder.ApplyConfiguration(new {entity.ClassName}Configuration());");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateEntityConfiguration(EntityInfo entity)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
            sb.AppendLine($"using {entity.Namespace};");
            sb.AppendLine();
            sb.AppendLine("namespace CodeMaster.Migrator.Persistence.EfCore.Configurations");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {entity.ClassName}Configuration : IEntityTypeConfiguration<{entity.ClassName}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        public void Configure(EntityTypeBuilder<{entity.ClassName}> builder)");
            sb.AppendLine("        {");

            // 配置表名
            sb.AppendLine($"            // 配置表名");
            sb.AppendLine($"            builder.ToTable(\"{entity.TableName}\");");
            sb.AppendLine();

            // 配置主键
            if (entity.PrimaryKeys.Count > 0)
            {
                sb.AppendLine("            // 配置主键");
                if (entity.PrimaryKeys.Count == 1)
                {
                    var pkName = entity.PrimaryKeys[0];
                    sb.AppendLine($"            builder.HasKey(e => e.{pkName});");

                    // IEntity<long> uses application-generated snowflake IDs.
                    if (pkName == "Id")
                    {
                        sb.AppendLine($"            builder.Property(e => e.{pkName}).ValueGeneratedNever();");
                    }
                }
                else
                {
                    // 复合主键
                    var pkProperties = string.Join(", ", entity.PrimaryKeys.Select(pk => $"e.{pk}"));
                    sb.AppendLine($"            builder.HasKey(e => new {{ {pkProperties} }});");
                }
                sb.AppendLine();
            }
            else
            {
                // 无主键表
                sb.AppendLine("            // 无主键表");
                sb.AppendLine("            builder.HasNoKey();");
                sb.AppendLine();
            }

            // 配置列名
            if (entity.PropertyColumns.Count > 0)
            {
                sb.AppendLine("            // 配置列名");
                foreach (var kvp in entity.PropertyColumns.OrderBy(k => k.Key))
                {
                    sb.AppendLine($"            builder.Property(e => e.{kvp.Key}).HasColumnName(\"{kvp.Value}\");");
                }
                sb.AppendLine();
            }

            // 添加 Ignore 配置
            if (entity.IgnoredProperties.Count > 0)
            {
                sb.AppendLine("            // 忽略 SqlSugar 标记为 IsIgnore 的属性和导航属性");
                foreach (var propertyName in entity.IgnoredProperties.OrderBy(p => p))
                {
                    sb.AppendLine($"            builder.Ignore(e => e.{propertyName});");
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private class EntityInfo
        {
            public string ClassName { get; set; }
            public string Namespace { get; set; }
            public string TableName { get; set; }
            public bool InheritsEntityBase { get; set; }
            public List<string> PrimaryKeys { get; set; } = new List<string>();
            public List<string> IgnoredProperties { get; set; } = new List<string>();
            public Dictionary<string, string> PropertyColumns { get; set; } = new Dictionary<string, string>();
        }
    }
}
