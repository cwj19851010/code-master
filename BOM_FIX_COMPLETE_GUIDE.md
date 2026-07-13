# BOM 编码问题 - 完整修复指南

## 问题现状

你的项目初始化后，前端启动时出现 BOM 编码错误：
```
[SyntaxError] Unexpected token '﻿', "﻿{
  "name"... is not valid JSON
```

## 根本原因

问题有两个来源：

### 1. 模板 ZIP 文件包含 BOM
现有的模板 ZIP 文件（`Templates/CodeMaster_Template_*.zip`）中的文件包含 UTF-8 BOM 字符。

### 2. 项目初始化时的文件处理
虽然我们已经修复了 `ProjectInitializationService.cs` 中的文件写入操作，但如果模板文件本身包含 BOM，解压后仍然会有问题。

## 已完成的修复

### ✅ 修复 1: ProjectInitializationService.cs
所有文件读写操作都已改为使用 `new UTF8Encoding(false)`：
- `ReplaceInFileAsync`
- `UpdateDatabaseConfigAsync`
- `UpdatePortConfigAsync`
- `GenerateSolutionFileAsync`
- `MergeJsonConfigAsync`
- `SaveInitializationStateAsync`
- `ExtractAndReplaceAsync`

### ✅ 修复 2: TemplateExportService.cs
模板导出服务已修复，确保生成的模板文件不包含 BOM：
- `CopyFileWithDirectory` - 对文本文件移除 BOM
- `RemoveAdditionalSeedCallAsync` - 使用 UTF-8 without BOM
- `CopySolutionTemplateAsync` - 使用 UTF-8 without BOM

## 需要执行的步骤

### 步骤 1: 关闭所有占用文件的进程

**关闭 Visual Studio**:
- 如果你在 Visual Studio 中打开了项目，请关闭它
- 或者在 Visual Studio 中停止调试

**停止后端服务**:
```bash
# 在 PowerShell 或 CMD 中执行
taskkill /F /IM dotnet.exe
taskkill /F /IM CodeMaster.WebApi.exe
```

### 步骤 2: 重新构建项目

```bash
cd D:\MyHomeWorks\CodeMaster
dotnet build CodeMaster.WebApi/CodeMaster.WebApi.csproj
```

### 步骤 3: 重新生成模板

**方法 1: 通过前端界面**
1. 启动后端服务
2. 启动前端服务
3. 登录系统
4. 进入"代码生成 > 项目管理"页面
5. 点击"生成模板"按钮
6. 等待生成完成

**方法 2: 通过 API**
```bash
# 启动后端服务
cd CodeMaster.WebApi
dotnet run

# 在另一个终端调用 API
curl -X POST http://localhost:5000/api/codegen/project/export-template \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"outputPath": null}'
```

### 步骤 4: 删除旧的已初始化项目

如果你已经初始化了项目（如 `D:/MyWorks/AttendPro`），需要删除它：

```bash
# 删除整个项目目录
rm -rf D:/MyWorks/AttendPro
```

或者在 Windows 资源管理器中手动删除。

### 步骤 5: 使用新模板重新初始化项目

1. 在前端界面创建新项目
2. 执行初始化
3. 新生成的文件将不包含 BOM 字符

## 验证修复

### 检查模板文件是否包含 BOM

**PowerShell**:
```powershell
# 检查 package.json 的前 3 个字节
$zipPath = "D:\MyHomeWorks\CodeMaster\Templates\CodeMaster_Template_最新.zip"
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
$entry = $zip.Entries | Where-Object { $_.FullName -eq "CodeMaster.Vue/package.json" }
$stream = $entry.Open()
$bytes = New-Object byte[] 3
$stream.Read($bytes, 0, 3)
$bytes -join ","
# 如果输出 "239,187,191"，说明有 BOM ❌
# 如果输出 "123,10,32" 或其他，说明没有 BOM ✅
$stream.Close()
$zip.Dispose()
```

### 检查生成的项目文件

```powershell
# 检查生成的 package.json
$bytes = [System.IO.File]::ReadAllBytes("D:/MyWorks/AttendPro/AttendPro.Vue/package.json")
$bytes[0..2] -join ","
# 应该不是 "239,187,191"
```

### 测试前端启动

```bash
cd D:/MyWorks/AttendPro/AttendPro.Vue
npm run dev
```

应该不再出现 JSON 解析错误。

## 如果问题仍然存在

### 手动移除 BOM

如果新模板仍然包含 BOM，可以使用以下 PowerShell 脚本手动移除：

```powershell
# 移除单个文件的 BOM
function Remove-BOM {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($FilePath, $content, $utf8NoBom)
}

# 移除目录中所有文本文件的 BOM
function Remove-BOMFromDirectory {
    param([string]$Directory)

    $extensions = @("*.json", "*.js", "*.vue", "*.ts", "*.tsx", "*.cs", "*.csproj", "*.sln")

    foreach ($ext in $extensions) {
        Get-ChildItem -Path $Directory -Filter $ext -Recurse | ForEach-Object {
            Write-Host "Processing: $($_.FullName)"
            Remove-BOM -FilePath $_.FullName
        }
    }
}

# 使用示例
Remove-BOMFromDirectory -Directory "D:/MyWorks/AttendPro"
```

## 相关文档

- [MIGRATION_FIX_SUMMARY.md](MIGRATION_FIX_SUMMARY.md) - 数据库迁移修复
- [PROJECT_INIT_FIX_SUMMARY.md](PROJECT_INIT_FIX_SUMMARY.md) - 项目初始化修复总览

## 修复日期

2026-03-05

## 注意事项

1. **必须重新生成模板** - 旧的模板文件仍然包含 BOM，必须使用修复后的代码重新生成
2. **必须删除旧项目** - 已经初始化的项目包含 BOM 文件，需要重新初始化
3. **关闭文件占用** - 构建前确保没有进程占用 DLL 文件
4. **验证修复** - 生成新模板后，验证文件不包含 BOM

## 技术细节

### UTF-8 编码的三种形式

1. **UTF-8 with BOM** (`Encoding.UTF8`)
   - 文件开头包含 `EF BB BF` 字节
   - C# 默认行为
   - JSON 解析器不支持 ❌

2. **UTF-8 without BOM** (`new UTF8Encoding(false)`)
   - 文件开头不包含特殊字节
   - 现代工具推荐 ✅
   - JSON 解析器支持 ✅

3. **ASCII** (仅英文字符)
   - 兼容性最好
   - 不支持中文 ❌

### 为什么 JSON 不支持 BOM？

JSON 规范（RFC 8259）明确规定：
- JSON 文本必须使用 UTF-8、UTF-16 或 UTF-32 编码
- 不应该包含 BOM
- 大多数 JSON 解析器将 BOM 视为语法错误

### 相关标准

- [RFC 8259 - JSON 规范](https://tools.ietf.org/html/rfc8259)
- [Unicode BOM FAQ](https://www.unicode.org/faq/utf_bom.html)
- [UTF-8 Everywhere Manifesto](http://utf8everywhere.org/)
