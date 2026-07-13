# 项目初始化 apply_migration 步骤错误修复总结

## 问题描述

在项目初始化过程中，`apply_migration` 步骤总是失败，导致无法完成数据库迁移。

## 根本原因

1. **错误信息不详细**：原来的 `RunProcessWithTimeoutAsync` 方法在进程失败时只返回简单的错误信息，无法诊断具体问题
2. **缺少日志输出**：没有实时输出进程的标准输出和错误输出，难以追踪问题
3. **错误处理不完善**：`RunDatabaseMigrationAsync` 方法缺少对常见错误场景的检查和友好提示

## 修复内容

### 1. 改进 `RunProcessWithTimeoutAsync` 方法

**文件**: `CodeMaster.Application/Services/CodeGen/Project/ProjectInitializationService.cs` (第628-728行)

**改进点**:
- 添加异步输出流读取，实时捕获标准输出和错误输出
- 使用 `StringBuilder` 收集所有输出信息
- 在控制台实时打印进程输出，便于调试
- 进程失败时返回详细的错误信息，包括：
  - 退出码
  - 执行的命令
  - 工作目录
  - 标准输出
  - 错误输出

**关键代码**:
```csharp
// 异步读取输出和错误流
var outputBuilder = new StringBuilder();
var errorBuilder = new StringBuilder();

process.OutputDataReceived += (sender, e) =>
{
    if (!string.IsNullOrEmpty(e.Data))
    {
        outputBuilder.AppendLine(e.Data);
        Console.WriteLine($"[Process Output] {e.Data}");
    }
};

process.ErrorDataReceived += (sender, e) =>
{
    if (!string.IsNullOrEmpty(e.Data))
    {
        errorBuilder.AppendLine(e.Data);
        Console.WriteLine($"[Process Error] {e.Data}");
    }
};

process.BeginOutputReadLine();
process.BeginErrorReadLine();
```

### 2. 改进 `RunDatabaseMigrationAsync` 方法

**文件**: `CodeMaster.Application/Services/CodeGen/Project/ProjectInitializationService.cs` (第553-628行)

**改进点**:
- 添加详细的日志输出，记录迁移路径、配置文件内容等
- 检查 `appsettings.json` 是否存在
- 读取并打印数据库配置，便于验证连接字符串
- 为常见错误场景提供友好的错误提示：
  - EF Core 工具未安装
  - 数据库连接字符串错误
  - 数据库服务器未运行
  - 数据库用户权限不足
  - 迁移文件语法错误

**关键代码**:
```csharp
// 检查 appsettings.json 是否存在
var appsettingsPath = Path.Combine(migratorPath, "appsettings.json");
if (!File.Exists(appsettingsPath))
{
    throw new FileNotFoundException($"appsettings.json not found in Migrator project: {appsettingsPath}");
}

// 读取并验证数据库配置
var appsettingsContent = await File.ReadAllTextAsync(appsettingsPath);
Console.WriteLine($"[Migration] appsettings.json content:\n{appsettingsContent}");

// 提供友好的错误提示
catch (Exception ex)
{
    throw new Exception($"Failed to apply database migration. Please check:\n" +
        $"1. Database connection string is correct\n" +
        $"2. Database server is running and accessible\n" +
        $"3. Database user has sufficient permissions\n" +
        $"4. No syntax errors in migration files\n\n" +
        $"Error: {ex.Message}", ex);
}
```

### 3. 改进 `Step6_ApplyMigrationAsync` 方法

**文件**: `CodeMaster.Application/Services/CodeGen/Project/ProjectService.cs` (第516-548行)

**改进点**:
- 添加详细的控制台日志输出
- 记录项目名称和路径
- 在异常处理中打印堆栈跟踪

**关键代码**:
```csharp
Console.WriteLine($"[Step6] Starting migration for project: {project.ProjectName}");
Console.WriteLine($"[Step6] Project path: {projectPath}");

// ... 执行迁移 ...

Console.WriteLine($"[Step6] Migration completed successfully");
```

## 如何使用

### 1. 查看详细日志

修复后，在项目初始化过程中，控制台会输出详细的日志信息：

```
[Migration] Migrator Path: D:\Projects\MyProject\MyProject.Migrator
[Migration] appsettings.json content:
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyProjectDb;..."
  },
  "DbProvider": "SqlServer"
}
[Migration] Has InitialCreate migration: false
[Migration] Creating InitialCreate migration...
[Process] Starting: dotnet ef migrations add InitialCreate
[Process] Working Directory: D:\Projects\MyProject\MyProject.Migrator
[Process Output] Build started...
[Process Output] Build succeeded.
[Process Output] Done.
[Migration] InitialCreate migration created successfully
[Migration] Applying database migration (dotnet run)...
[Process] Starting: dotnet run
[Process] Working Directory: D:\Projects\MyProject\MyProject.Migrator
[Process Output] ===========================================
[Process Output]   CodeMaster 数据库迁移和种子数据初始化
[Process Output] ===========================================
...
```

### 2. 常见错误诊断

#### 错误 1: 数据库连接失败

**错误信息**:
```
Failed to apply database migration. Please check:
1. Database connection string is correct
2. Database server is running and accessible
3. Database user has sufficient permissions
4. No syntax errors in migration files

Error: A network-related or instance-specific error occurred...
```

**解决方法**:
- 检查 `appsettings.json` 中的连接字符串
- 确认数据库服务器正在运行
- 验证数据库用户名和密码
- 检查防火墙设置

#### 错误 2: EF Core 工具未安装

**错误信息**:
```
Failed to create database migration. Please check:
1. EF Core tools are installed (dotnet tool install --global dotnet-ef)
2. Database connection string is correct
3. Database server is running

Error: 'dotnet-ef' is not recognized as an internal or external command...
```

**解决方法**:
```bash
dotnet tool install --global dotnet-ef
```

#### 错误 3: 数据库权限不足

**错误信息**:
```
Failed to apply database migration. Please check:
1. Database connection string is correct
2. Database server is running and accessible
3. Database user has sufficient permissions
4. No syntax errors in migration files

Error: CREATE DATABASE permission denied...
```

**解决方法**:
- 确保数据库用户具有 CREATE DATABASE 权限
- 或者手动创建数据库，然后重新运行迁移

## 测试建议

1. **测试场景 1**: 正常初始化
   - 确保数据库服务器运行
   - 使用正确的连接字符串
   - 观察控制台输出，确认每个步骤都成功

2. **测试场景 2**: 数据库连接失败
   - 故意使用错误的连接字符串
   - 验证错误信息是否清晰明了

3. **测试场景 3**: 数据库服务器未运行
   - 停止数据库服务器
   - 验证错误信息是否提示检查服务器状态

## 后续改进建议

1. **添加重试机制**: 对于网络临时故障，可以自动重试
2. **添加数据库连接测试**: 在执行迁移前先测试数据库连接
3. **支持跳过失败步骤**: 允许用户在某个步骤失败后继续执行后续步骤
4. **添加回滚机制**: 如果初始化失败，自动清理已创建的文件和数据库

## 相关文件

- `CodeMaster.Application/Services/CodeGen/Project/ProjectInitializationService.cs`
- `CodeMaster.Application/Services/CodeGen/Project/ProjectService.cs`
- `CodeMaster.Vue/src/views/codegen/project/index.vue`

## 修复日期

2026-03-05
