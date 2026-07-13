# BOM 编码问题修复总结

## 问题描述

在项目初始化过程中，前端启动时出现以下错误：

```
The CJS build of Vite's Node API is deprecated.
[Failed to load PostCSS config: Failed to load PostCSS config (searchPath: D:/MyWorks/AttendPro/AttendPro.Vue):
[SyntaxError] Unexpected token '﻿', "﻿{
  "name"... is not valid JSON
SyntaxError: Unexpected token '﻿', "﻿{
  "name"... is not valid JSON
```

## 根本原因

生成的 JSON 文件（如 `package.json`、`vite.config.js` 等）包含了 **UTF-8 BOM (Byte Order Mark)** 字符（`﻿`），导致：
- JSON 解析器无法正确解析文件
- Vite 无法加载配置文件
- 前端服务启动失败

### 什么是 BOM？

BOM (Byte Order Mark) 是 UTF-8 编码文件开头的特殊字符序列（`EF BB BF`），用于标识文件编码。但是：
- JSON 规范不允许 BOM 字符
- JavaScript/Node.js 的 JSON 解析器会将 BOM 视为语法错误
- 大多数现代工具（Vite、ESLint、Prettier）都不支持带 BOM 的文件

### 为什么会产生 BOM？

在 C# 中，`Encoding.UTF8` 默认会在文件开头添加 BOM 字符。要避免 BOM，需要使用 `new UTF8Encoding(false)`。

## 修复内容

### 修改的文件

`CodeMaster.Application/Services/CodeGen/Project/ProjectInitializationService.cs`

### 修复的方法

1. **ReplaceInFileAsync** (第329-346行)
   - 读取和写入文件时使用 `new UTF8Encoding(false)`

2. **UpdateDatabaseConfigAsync** (第349-411行)
   - 所有 JSON 配置文件的读写都使用 UTF-8 without BOM

3. **UpdatePortConfigAsync** (第413-490行)
   - 更新 `launchSettings.json`、`vite.config.js`、`edit.vue` 时使用 UTF-8 without BOM

4. **GenerateSolutionFileAsync** (第529-547行)
   - 生成 `.sln` 文件时使用 UTF-8 without BOM

5. **MergeJsonConfigAsync** (第836-866行)
   - 合并 JSON 配置文件时使用 UTF-8 without BOM

6. **SaveInitializationStateAsync** (第730-738行)
   - 保存初始化状态文件时使用 UTF-8 without BOM

7. **ExtractAndReplaceAsync** (第789-801行)
   - 从模板解压文件时使用 UTF-8 without BOM

## 修复前后对比

### 修复前

```csharp
// 会产生 BOM
await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
await File.WriteAllTextAsync(filePath, content); // 默认也会产生 BOM
```

### 修复后

```csharp
// 不产生 BOM
var utf8WithoutBom = new UTF8Encoding(false);
await File.WriteAllTextAsync(filePath, content, utf8WithoutBom);
```

## 验证修复

### 1. 检查文件是否包含 BOM

**PowerShell 命令**:
```powershell
# 检查文件的前 3 个字节
$bytes = [System.IO.File]::ReadAllBytes("package.json")
$bytes[0..2] -join ","
# 如果输出 "239,187,191"，说明有 BOM
# 如果输出其他值（如 "123,10,32"），说明没有 BOM
```

**Linux/Mac 命令**:
```bash
# 检查文件是否包含 BOM
file package.json
# 输出包含 "UTF-8 Unicode (with BOM)" 说明有 BOM
# 输出包含 "UTF-8 Unicode text" 说明没有 BOM
```

### 2. 测试项目初始化

1. 创建一个新项目
2. 执行初始化流程
3. 检查生成的文件：
   - `package.json`
   - `vite.config.js`
   - `appsettings.json`
   - `launchSettings.json`
4. 确认这些文件都不包含 BOM
5. 启动前端服务，确认没有 JSON 解析错误

## 受影响的文件类型

以下文件类型在生成时都已修复为 UTF-8 without BOM：

- `.json` - JSON 配置文件
- `.js` - JavaScript 文件
- `.vue` - Vue 组件文件
- `.cs` - C# 源代码文件
- `.csproj` - 项目文件
- `.sln` - 解决方案文件
- `.ts` - TypeScript 文件
- `.tsx` - TypeScript JSX 文件
- `.html` - HTML 文件

## 最佳实践

### 在 C# 中处理文本文件

```csharp
// ✅ 推荐：使用 UTF-8 without BOM
var utf8WithoutBom = new UTF8Encoding(false);
await File.WriteAllTextAsync(filePath, content, utf8WithoutBom);

// ❌ 避免：会产生 BOM
await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
await File.WriteAllTextAsync(filePath, content); // 默认编码
```

### 在 Visual Studio 中检查文件编码

1. 打开文件
2. 点击 **文件** > **高级保存选项**
3. 查看编码设置：
   - **UTF-8 with signature** = UTF-8 with BOM ❌
   - **UTF-8 without signature** = UTF-8 without BOM ✅

### 在 VS Code 中检查文件编码

1. 打开文件
2. 查看右下角状态栏的编码信息
3. 点击编码名称，选择 **Save with Encoding**
4. 选择 **UTF-8** (不是 UTF-8 with BOM)

## 相关资源

- [UTF-8 BOM 问题详解](https://en.wikipedia.org/wiki/Byte_order_mark)
- [JSON 规范](https://www.json.org/)
- [Vite 配置文件格式](https://vitejs.dev/config/)
- [.NET UTF8Encoding 文档](https://docs.microsoft.com/en-us/dotnet/api/system.text.utf8encoding)

## 修复日期

2026-03-05

## 相关问题

- [MIGRATION_FIX_SUMMARY.md](MIGRATION_FIX_SUMMARY.md) - 数据库迁移错误修复
