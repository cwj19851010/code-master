using System.Reflection;
using CodeMaster.Core.Attributes;
using CodeMaster.Core.Services;
using OfficeOpenXml;
using SqlSugar;

namespace CodeMaster.Infrastructure.Services;

/// <summary>
/// Excel 导入导出服务实现
/// </summary>
public class ExcelService : IExcelService
{
    private readonly ISqlSugarClient _db;

    public ExcelService(ISqlSugarClient db)
    {
        _db = db;
        // EPPlus 4.5.3.3 不需要设置 LicenseContext
    }

    /// <summary>
    /// 导出数据到 Excel
    /// </summary>
    public async Task<byte[]> ExportAsync<T>(List<T> data, string sheetName = "Sheet1") where T : class
    {
        return await Task.Run(() =>
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // 获取导出列信息
            var columns = GetExportColumns<T>();

            // 写入表头
            for (int i = 0; i < columns.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = columns[i].ColumnName;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // 写入数据
            for (int row = 0; row < data.Count; row++)
            {
                var item = data[row];
                for (int col = 0; col < columns.Count; col++)
                {
                    var columnInfo = columns[col];
                    var value = columnInfo.Property.GetValue(item);
                    worksheet.Cells[row + 2, col + 1].Value = value?.ToString() ?? string.Empty;
                }
            }

            // 自动调整列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        });
    }

    /// <summary>
    /// 从 Excel 导入数据
    /// </summary>
    public async Task<List<T>> ImportAsync<T>(byte[] fileBytes, string sheetName = "Sheet1") where T : class, new()
    {
        using var package = new ExcelPackage(new MemoryStream(fileBytes));
        var worksheet = package.Workbook.Worksheets[sheetName];

        if (worksheet == null)
        {
            throw new Exception($"工作表 '{sheetName}' 不存在");
        }

        var columns = GetExportColumns<T>();
        var result = new List<T>();

        // 构建列名到列信息的映射
        var columnMap = new Dictionary<string, ExportColumnInfo>();
        for (int i = 0; i < columns.Count; i++)
        {
            var headerValue = worksheet.Cells[1, i + 1].Value?.ToString();
            if (!string.IsNullOrEmpty(headerValue))
            {
                columnMap[headerValue] = columns[i];
            }
        }

        // 读取数据（从第2行开始，第1行是表头）
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var item = new T();

            for (int col = 0; col < columns.Count; col++)
            {
                var columnInfo = columns[col];
                var cellValue = worksheet.Cells[row, col + 1].Value;

                if (cellValue == null) continue;

                // 如果是外键列且允许通过显示字段导入
                if (columnInfo.IsForeignKey && columnInfo.ForeignKeyConfig?.AllowImportByDisplay == true)
                {
                    // 通过显示字段值反查外键ID
                    var foreignId = await QueryForeignKeyAsync(
                        columnInfo.ForeignKeyConfig.EntityType,
                        columnInfo.ForeignKeyConfig.DisplayField,
                        cellValue.ToString()!
                    );

                    if (foreignId != null)
                    {
                        var propertyType = columnInfo.Property.PropertyType;
                        var convertedValue = Convert.ChangeType(foreignId, Nullable.GetUnderlyingType(propertyType) ?? propertyType);
                        columnInfo.Property.SetValue(item, convertedValue);
                    }
                }
                else
                {
                    // 普通字段直接赋值
                    try
                    {
                        var propertyType = columnInfo.Property.PropertyType;
                        var convertedValue = Convert.ChangeType(cellValue, Nullable.GetUnderlyingType(propertyType) ?? propertyType);
                        columnInfo.Property.SetValue(item, convertedValue);
                    }
                    catch
                    {
                        // 类型转换失败，跳过该字段
                    }
                }
            }

            result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// 通过显示字段值查询外键ID
    /// </summary>
    private async Task<object?> QueryForeignKeyAsync(Type entityType, string displayField, string displayValue)
    {
        try
        {
            // 使用反射调用 SqlSugar 的 Queryable 方法
            var queryableMethod = typeof(ISqlSugarClient).GetMethod("Queryable", Type.EmptyTypes);
            var genericMethod = queryableMethod!.MakeGenericMethod(entityType);
            var queryable = genericMethod.Invoke(_db, null);

            // 构建查询条件：Where(x => x.DisplayField == displayValue)
            var whereMethod = queryable!.GetType().GetMethod("Where", new[] { typeof(string), typeof(object) });
            var filteredQueryable = whereMethod!.Invoke(queryable, new object[] { $"{displayField} = @0", displayValue });

            // 获取 Id 字段
            var selectMethod = filteredQueryable!.GetType().GetMethod("Select", new[] { typeof(string) });
            var selectedQueryable = selectMethod!.Invoke(filteredQueryable, new object[] { "Id" });

            // 执行查询
            var firstMethod = selectedQueryable!.GetType().GetMethod("FirstAsync", Type.EmptyTypes);
            var task = (Task)firstMethod!.Invoke(selectedQueryable, null)!;
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取导出列信息
    /// </summary>
    private List<ExportColumnInfo> GetExportColumns<T>()
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = new List<ExportColumnInfo>();

        foreach (var property in properties)
        {
            var isIgnored = property.GetCustomAttribute<ExportIgnoreAttribute>() != null;
            var foreignKeyAttr = property.GetCustomAttribute<ExportForeignKeyAttribute>();
            var columnAttr = property.GetCustomAttribute<ExportColumnAttribute>();

            // 如果有外键特性，添加关联显示列
            if (foreignKeyAttr != null)
            {
                var foreignColumnInfo = new ExportColumnInfo
                {
                    Property = property,
                    IsForeignKey = true,
                    ForeignKeyConfig = foreignKeyAttr,
                    Order = foreignKeyAttr.Order
                };

                // 确定关联列的列名
                if (!string.IsNullOrEmpty(foreignKeyAttr.ColumnName))
                {
                    foreignColumnInfo.ColumnName = foreignKeyAttr.ColumnName;
                }
                else if (!string.IsNullOrEmpty(foreignKeyAttr.TitleKey))
                {
                    foreignColumnInfo.ColumnName = foreignKeyAttr.TitleKey;
                    foreignColumnInfo.TitleKey = foreignKeyAttr.TitleKey;
                }
                else
                {
                    // 使用显示字段的属性名作为列名
                    foreignColumnInfo.ColumnName = foreignKeyAttr.DisplayField;
                }

                columns.Add(foreignColumnInfo);
            }

            // 如果字段本身被忽略，则不添加该字段本身的列
            if (isIgnored)
            {
                continue;
            }

            // 添加字段本身的列
            var columnInfo = new ExportColumnInfo
            {
                Property = property,
                IsForeignKey = false,
                Order = columnAttr?.Order ?? 0
            };

            // 确定列名
            if (columnAttr != null && !string.IsNullOrEmpty(columnAttr.ColumnName))
            {
                columnInfo.ColumnName = columnAttr.ColumnName;
            }
            else if (columnAttr != null && !string.IsNullOrEmpty(columnAttr.TitleKey))
            {
                columnInfo.ColumnName = columnAttr.TitleKey;
                columnInfo.TitleKey = columnAttr.TitleKey;
            }
            else
            {
                // 使用属性名（转换为小写开头）
                columnInfo.ColumnName = char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
            }

            columns.Add(columnInfo);
        }

        // 按 Order 排序
        return columns.OrderBy(c => c.Order).ThenBy(c => c.ColumnName).ToList();
    }
}
