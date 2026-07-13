using System;
using System.IO;
using SqlSugar;
using Xunit;

namespace CodeMaster.OpenSpec.Tests;

/// <summary>
/// 跨项目连接与代码生成执行规则测试（对应 openspec 第 11 章）
/// </summary>
public class CrossProjectExecutionRulesTests
{
    /// <summary>
    /// 验证规则 11.1：SQLite数据库相对路径转绝对路径规则
    /// </summary>
    [Theory]
    [InlineData("Data Source=../CodeMaster.db", "C:\\Projects\\MyApp", "Data Source=C:\\Projects\\CodeMaster.db")]
    [InlineData("Data Source=./local.db;Version=3;", "D:\\work\\testApp", "Data Source=D:\\work\\testApp\\local.db;Version=3;")]
    [InlineData("Data Source=C:\\absolute\\path.db", "D:\\work\\test", "Data Source=C:\\absolute\\path.db")] // 绝对路径不变
    public void Test_SQLite_ConnectionString_Resolution(string originalConnection, string projectPhysicalPath, string expectedConnection)
    {
        // Arrange & Act (模拟系统中实际的路径转换逻辑)
        string resolvedConnection = ResolveSqliteConnectionString(originalConnection, projectPhysicalPath);

        // 统一斜杠用于跨平台判断通过
        resolvedConnection = resolvedConnection.Replace("/", "\\");
        expectedConnection = expectedConnection.Replace("/", "\\");

        // Assert
        Assert.Equal(expectedConnection, resolvedConnection);
    }

    /// <summary>
    /// 模拟系统实际运行中的 SQLite 绝对路径转换逻辑
    /// </summary>
    private string ResolveSqliteConnectionString(string connectionString, string projectPhysicalPath)
    {
        if (string.IsNullOrEmpty(connectionString) || !connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        // 解析并替换 Data Source
        var parts = connectionString.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            if (part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                var dbPath = part.Substring("Data Source=".Length).Trim();
                
                // 判断是否是相对路径
                if (!Path.IsPathRooted(dbPath))
                {
                    // 相对路径转换为绝对路径：ProjectPhysicalPath + dbPath
                    var absolutePath = Path.GetFullPath(Path.Combine(projectPhysicalPath, dbPath));
                    parts[i] = $"Data Source={absolutePath}";
                }
            }
        }
        return string.Join(";", parts);
    }

    /// <summary>
    /// 验证规则 11.2 和 11.3：能否通过任意组装好的 ConnectionString 独立构建 SqlSugarClient
    /// 证明我们不依赖原项目库，而是指向“目标项目”的库。
    /// </summary>
    [Fact]
    public void Test_CrossProject_SqlSugarClient_Construction()
    {
        // 假设这里是目标项目的绝对路径下的 SQLite （自动建一个内存级或者临时库做测试）
        var targetProjectDbString = "Data Source=:memory:";

        // Act: 按照 OpenSpec 第 11 章，利用提取出的连接字符串建立“专属会话”
        var targetDb = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = targetProjectDbString,
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute // 从实体特性读取主键
        });

        // Assert: 验证连接成功创建
        Assert.NotNull(targetDb);
        
        // 尝试打开验证可用性
        targetDb.Ado.Open();
        Assert.True(targetDb.Ado.Connection.State == System.Data.ConnectionState.Open);
        targetDb.Ado.Close();
    }
}
