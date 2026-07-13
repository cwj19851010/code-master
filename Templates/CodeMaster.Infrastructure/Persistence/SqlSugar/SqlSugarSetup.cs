using SqlSugar;
using System.Text.RegularExpressions;
using CodeMaster.Core.Entities;
using CodeMaster.Core.MultiTenancy;
using CodeMaster.Core.Data;
using CodeMaster.Core.Entities;
using CodeMaster.Core.Enums;
using Microsoft.Extensions.Logging;

namespace CodeMaster.Infrastructure.Persistence.SqlSugar;

/// <summary>
/// SqlSugar配置
/// </summary>
public static class SqlSugarSetup
{
    /// <summary>
    /// 配置SqlSugar
    /// </summary>
    public static ISqlSugarClient CreateSqlSugarClient(
        string connectionString,
        ITenantContext? tenantContext = null,
        IDataFilter? dataFilter = null,
        IDataPermissionContext? dataPermissionContext = null,
        DbType dbType = DbType.SqlServer,
        ILogger? logger = null)
    {
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = dbType,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            ConfigureExternalServices = new ConfigureExternalServices
            {
                // 全局配置：表名转换为复数 + snake_case
                EntityNameService = (type, entity) =>
                {
                    // 如果实体已经有 SugarTable 特性，则不自动转换
                    var sugarTableAttr = type.GetCustomAttributes(typeof(SugarTable), false).FirstOrDefault() as SugarTable;
                    if (sugarTableAttr != null && !string.IsNullOrEmpty(sugarTableAttr.TableName))
                    {
                        entity.DbTableName = sugarTableAttr.TableName;
                        return;
                    }

                    // 自动转换：SysUser -> sys_users
                    var tableName = type.Name;

                    // 转换为复数（简单规则：末尾加 s）
                    if (!tableName.EndsWith("s"))
                    {
                        tableName += "s";
                    }

                    // 转换为 snake_case
                    entity.DbTableName = ToSnakeCase(tableName);
                },
                EntityService = (property, column) =>
                {
                    // 自动转换：UserName -> user_name
                    column.DbColumnName = ToSnakeCase(property.Name);

                    // 自动映射数据库类型
                    column.DataType = GetDatabaseType(property.PropertyType, dbType);
                }
            }
        });

        // 配置全局租户过滤器（动态获取当前租户ID）
        if (tenantContext != null)
        {
            // 使用动态表达式，每次查询时都会重新计算 tenantContext.CurrentTenantId
            var currentTenantId = tenantContext.CurrentTenantId ?? 0;
            db.QueryFilter.AddTableFilter<CodeMaster.Core.Entities.ITenant>(entity =>
                currentTenantId == 0 || entity.TenantId == currentTenantId);
        }

        // 配置全局软删除过滤器
        db.QueryFilter.AddTableFilter<ISoftDelete>(entity => entity.IsDeleted == false);

        // 配置部门/本人数据权限过滤器
        // 在外部判断好条件后，根据不同权限范围添加对应的过滤器
        if (dataPermissionContext != null && dataPermissionContext.IsEnabled)
        {
            // 管理员或全部数据权限：不添加过滤器
            if (!dataPermissionContext.IsAdmin && dataPermissionContext.DataScope != (int)PostDataScope.All)
            {
                // 本部门及以下数据权限
                if (dataPermissionContext.DataScope == (int)PostDataScope.DeptAndBelow
                    && !string.IsNullOrEmpty(dataPermissionContext.DeptAncestors))
                {
                    var deptAncestorsPattern = dataPermissionContext.DeptAncestors;
                    db.QueryFilter.AddTableFilter<IDept>(entity =>
                        entity.DeptAncestors != null && entity.DeptAncestors.StartsWith(deptAncestorsPattern));
                }
                // 本部门数据权限
                else if (dataPermissionContext.DataScope == (int)PostDataScope.Dept
                    && !string.IsNullOrEmpty(dataPermissionContext.DeptAncestors))
                {
                    var deptAncestors = dataPermissionContext.DeptAncestors;
                    db.QueryFilter.AddTableFilter<IDept>(entity =>
                        entity.DeptAncestors == deptAncestors);
                }
                // 仅本人数据权限
                else if (dataPermissionContext.DataScope == (int)PostDataScope.Self
                    && dataPermissionContext.UserId != null)
                {
                    var userId = dataPermissionContext.UserId.Value;
                    db.QueryFilter.AddTableFilter<IDept>(entity =>
                        entity.CreateUserId == userId);
                }
            }
        }

        // 配置SQL日志
        db.Aop.OnLogExecuting = (sql, pars) =>
        {
            var logMessage = $"[SqlSugar] {sql}";
            if (pars != null && pars.Length > 0)
            {
                var parameters = string.Join(", ", pars.Select(p => $"{p.ParameterName}={p.Value}"));
                logMessage += $" | Parameters: {parameters}";
            }

            if (logger != null)
            {
                logger.LogInformation(logMessage);
            }
            else
            {
                Console.WriteLine(logMessage);
            }
        };

        // 配置SQL执行时间日志（超过1秒的慢查询）
        db.Aop.OnLogExecuted = (sql, pars) =>
        {
            // 这里可以记录SQL执行时间
        };

        // 配置SQL错误日志
        db.Aop.OnError = (exp) =>
        {
            if (logger != null)
            {
                logger.LogError(exp.Sql, $"[SqlSugar Error] SQL: {exp.Sql}");
            }
            else
            {
                Console.WriteLine($"[SqlSugar Error] SQL: {exp.Sql}, Exception: {exp.Parametres}");
            }
        };

        return db;
    }

    /// <summary>
    /// 转换为snake_case
    /// </summary>
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // 处理连续大写字母和大写字母后跟小写字母的情况
        // SysUser -> Sys_User -> sys_user
        // UserName -> User_Name -> user_name
        var result = Regex.Replace(input, "([A-Z]+)([A-Z][a-z])", "$1_$2");
        result = Regex.Replace(result, "([a-z0-9])([A-Z])", "$1_$2");
        return result.ToLower();
    }

    /// <summary>
    /// 获取数据库类型映射
    /// </summary>
    private static string GetDatabaseType(Type type, DbType dbType)
    {
        // 处理可空类型
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // SQL Server 类型映射
        if (dbType == DbType.SqlServer)
        {
            if (underlyingType == typeof(string))
                return "nvarchar(255)";
            if (underlyingType == typeof(long))
                return "bigint";
            if (underlyingType == typeof(int))
                return "int";
            if (underlyingType == typeof(short))
                return "smallint";
            if (underlyingType == typeof(byte))
                return "tinyint";
            if (underlyingType == typeof(bool))
                return "bit";
            if (underlyingType == typeof(DateTime))
                return "datetime";
            if (underlyingType == typeof(decimal))
                return "decimal(18,2)";
            if (underlyingType == typeof(double))
                return "float";
            if (underlyingType == typeof(float))
                return "real";
            if (underlyingType == typeof(Guid))
                return "uniqueidentifier";
            if (underlyingType == typeof(byte[]))
                return "varbinary(max)";
        }
        // MySQL 类型映射
        else if (dbType == DbType.MySql)
        {
            if (underlyingType == typeof(string))
                return "varchar(255)";
            if (underlyingType == typeof(long))
                return "bigint";
            if (underlyingType == typeof(int))
                return "int";
            if (underlyingType == typeof(short))
                return "smallint";
            if (underlyingType == typeof(byte))
                return "tinyint";
            if (underlyingType == typeof(bool))
                return "tinyint(1)";
            if (underlyingType == typeof(DateTime))
                return "datetime";
            if (underlyingType == typeof(decimal))
                return "decimal(18,2)";
            if (underlyingType == typeof(double))
                return "double";
            if (underlyingType == typeof(float))
                return "float";
            if (underlyingType == typeof(Guid))
                return "char(36)";
            if (underlyingType == typeof(byte[]))
                return "longblob";
        }
        // PostgreSQL 类型映射
        else if (dbType == DbType.PostgreSQL)
        {
            if (underlyingType == typeof(string))
                return "varchar(255)";
            if (underlyingType == typeof(long))
                return "bigint";
            if (underlyingType == typeof(int))
                return "integer";
            if (underlyingType == typeof(short))
                return "smallint";
            if (underlyingType == typeof(byte))
                return "smallint";
            if (underlyingType == typeof(bool))
                return "boolean";
            if (underlyingType == typeof(DateTime))
                return "timestamp";
            if (underlyingType == typeof(decimal))
                return "numeric(18,2)";
            if (underlyingType == typeof(double))
                return "double precision";
            if (underlyingType == typeof(float))
                return "real";
            if (underlyingType == typeof(Guid))
                return "uuid";
            if (underlyingType == typeof(byte[]))
                return "bytea";
        }

        // 默认返回 text 类型
        if (dbType == DbType.Sqlite)
        {
            if (underlyingType == typeof(string))
                return "text";
            if (underlyingType == typeof(long) ||
                underlyingType == typeof(int) ||
                underlyingType == typeof(short) ||
                underlyingType == typeof(byte) ||
                underlyingType == typeof(bool))
                return "integer";
            if (underlyingType == typeof(DateTime) || underlyingType == typeof(Guid))
                return "text";
            if (underlyingType == typeof(decimal) ||
                underlyingType == typeof(double) ||
                underlyingType == typeof(float))
                return "real";
            if (underlyingType == typeof(byte[]))
                return "blob";
        }

        return dbType == DbType.SqlServer ? "nvarchar(max)" : "text";
    }
}
