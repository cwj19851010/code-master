using System.Reflection;
using CodeMaster.CodeGenerator.Models;
using CodeMaster.Core.Entities;

namespace CodeMaster.CodeGenerator.Scanners;

/// <summary>
/// 实体扫描器
/// </summary>
public class EntityScanner
{
    /// <summary>
    /// 扫描指定程序集中的所有实体
    /// </summary>
    public List<EntityMetadata> ScanEntities(Assembly assembly)
    {
        var entities = new List<EntityMetadata>();

        var entityTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEntity<long>).IsAssignableFrom(t))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var metadata = new EntityMetadata
            {
                EntityName = entityType.Name,
                Namespace = entityType.Namespace ?? string.Empty,
                BusinessName = GetBusinessName(entityType.Name),
                ModuleName = GetModuleName(entityType.Namespace ?? string.Empty),
                Description = GetTypeDescription(entityType),
                HasTenantId = typeof(ITenantEntity).IsAssignableFrom(entityType)
            };

            // 扫描属性
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .ToList();

            foreach (var property in properties)
            {
                var propertyMetadata = new PropertyMetadata
                {
                    Name = property.Name,
                    Type = GetPropertyTypeName(property.PropertyType),
                    IsNullable = IsNullableType(property.PropertyType),
                    Description = GetPropertyDescription(property),
                    IsPrimaryKey = property.Name == "Id"
                };

                metadata.Properties.Add(propertyMetadata);
            }

            entities.Add(metadata);
        }

        return entities;
    }

    /// <summary>
    /// 获取业务名称（去掉 Sys 等前缀）
    /// </summary>
    private string GetBusinessName(string entityName)
    {
        if (entityName.StartsWith("Sys"))
            return entityName.Substring(3);

        return entityName;
    }

    /// <summary>
    /// 获取模块名称
    /// </summary>
    private string GetModuleName(string namespaceName)
    {
        var parts = namespaceName.Split('.');
        if (parts.Length >= 4)
            return parts[3]; // CodeMaster.Domain.Entities.System -> System

        if (parts.Length >= 3)
            return parts[2]; // CodeMaster.Domain.Entities -> Entities

        return "Common";
    }

    /// <summary>
    /// 获取类型描述
    /// </summary>
    private string GetTypeDescription(Type type)
    {
        var summaryAttr = type.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .FirstOrDefault() as System.ComponentModel.DescriptionAttribute;

        return summaryAttr?.Description ?? type.Name;
    }

    /// <summary>
    /// 获取属性描述
    /// </summary>
    private string GetPropertyDescription(PropertyInfo property)
    {
        var summaryAttr = property.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .FirstOrDefault() as System.ComponentModel.DescriptionAttribute;

        return summaryAttr?.Description ?? property.Name;
    }

    /// <summary>
    /// 获取属性类型名称
    /// </summary>
    private string GetPropertyTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return Nullable.GetUnderlyingType(type)?.Name ?? "object";
        }

        return type.Name switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "String" => "string",
            "Boolean" => "bool",
            "DateTime" => "DateTime",
            "Decimal" => "decimal",
            _ => type.Name
        };
    }

    /// <summary>
    /// 判断是否为可空类型
    /// </summary>
    private bool IsNullableType(Type type)
    {
        if (!type.IsValueType)
            return true; // 引用类型默认可空

        return Nullable.GetUnderlyingType(type) != null;
    }
}
