using Microsoft.EntityFrameworkCore;
using CodeMaster.Domain.Entities.System;

namespace CodeMaster.Migrator.Persistence.EfCore;

/// <summary>
/// EF Core DbContext（仅用于数据库迁移）
/// DbSet 和 Configuration 由 Source Generator 自动生成
/// </summary>
public partial class CodeMasterDbContext : DbContext
{
    public CodeMasterDbContext(DbContextOptions<CodeMasterDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 调用 Source Generator 生成的配置方法
        OnModelCreatingPartial(modelBuilder);

        // 全局配置snake_case列名（已由 Source Generator 处理，这里保留作为后备）
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // 表名转snake_case
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }

            // 列名转snake_case
            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (!string.IsNullOrEmpty(columnName))
                {
                    property.SetColumnName(ToSnakeCase(columnName));
                }
            }
        }
    }

    /// <summary>
    /// Source Generator 生成的部分方法
    /// </summary>
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    /// <summary>
    /// 转换为snake_case
    /// </summary>
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLower(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLower(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
