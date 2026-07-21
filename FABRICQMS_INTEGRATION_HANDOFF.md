# FabricQms 集成改动交接说明

> 更新日期：2026-07-16
> 关联项目：`D:\MyWorks\废料统计\FabricQms`
> CodeMaster 项目 ID：`2077595535296040960`

## 目的

FabricQms 是使用 CodeMaster 生成并继续开发的面料质检 QMS 项目。本次工作不仅完善了 FabricQms，也根据实际生成、初始化、迁移和启动过程中暴露的问题，对 CodeMaster 框架本身进行了修复。

后续 AI 在处理 CodeMaster 的模板、初始化、MCP、迁移或项目启动逻辑时，应先阅读本文，避免误删这些修复或重新引入旧模板迁移问题。

## CodeMaster 框架级修改

### 1. 模板定位与纯净模板导出

涉及文件：

- `CodeMaster.Application/Services/CodeGen/Project/ProjectTemplateLocator.cs`
- `CodeMaster.Application/Services/CodeGen/Project/TemplateExportService.cs`
- `CodeMaster.Application/Services/CodeGen/Project/GeneratedTemplateCleanup.cs`
- `CodeMaster.Application/Services/CodeGen/Project/ProjectService.cs`
- `CodeMaster.CodeGenerator.Tests/Services/ProjectTemplateLocatorTests.cs`
- `CodeMaster.CodeGenerator.Tests/Services/TemplateExportServiceTests.cs`

改动内容：

- 生成或初始化项目时选择最新且有效的模板，而不是固定或误用旧模板。
- 模板导出时排除 `Migrations/*.cs`、MCP Token 页面、MCP 服务引用和客户端桥接残留。
- 清理逻辑兼容 Windows CRLF 换行，避免删除 import 时遗留 `onMounted` 等无效引用。
- 保留旧模板压缩包作为备份是允许的；模板定位器应跳过包含历史迁移或无效结构的模板。

已验证模板：

- `Templates/CodeMaster_Template_20260716102405.zip`
- 压缩包文件数：307
- 历史迁移文件：0
- MCP 文件名：0
- MCP/客户端桥接残留引用：0

### 2. 项目初始化与项目路径解析

涉及文件：

- `CodeMaster.Application/Services/CodeGen/Project/ProjectInitializationService.cs`
- `CodeMaster.Application/Services/CodeGen/Project/ModuleEntityService.cs`

改动内容：

- 初始化后写入 `.codemaster/project-context.json`，供 MCP 自动识别项目 ID、项目名和前后端端口。
- 项目路径既兼容“父目录”形式，也兼容数据库中已经保存的“完整项目根目录”形式。
- 修复生成 `.auto.cs` 等文件时可能多拼接一次项目名称、导致文件写入错误目录的问题。
- 初始化过程中继续执行生成项目专用的 MCP/客户端桥接清理，避免生成代码引用不存在的服务或页面。

### 3. 项目启动与环境变量隔离

涉及文件：

- `CodeMaster.Application/Services/CodeGen/Project/ProjectProcessLauncher.cs`
- `CodeMaster.Application/Services/CodeGen/Project/ProjectInitializationService.cs`

改动内容：

- 后端启动显式使用项目配置的端口和 `Development` 环境。
- 启动生成项目之前清除继承自 CodeMaster/MCP 进程的 `ConnectionStrings__DefaultConnection` 和 `DbProvider` 环境变量。
- 该隔离非常重要：否则生成项目可能错误连接 CodeMaster 元数据库，表现为登录正常但业务接口查询错误数据库。
- Windows 后台启动保持隐藏窗口，启动结果返回真实 PID 和工作目录。

### 4. 数据库迁移与敏感信息处理

涉及文件：

- `CodeMaster.Application/Services/CodeGen/Project/ProjectService.cs`
- `CodeMaster.Migrator/Program.cs`
- `Templates/CodeMaster.Migrator/Program.cs`

改动内容：

- 执行迁移前判断 EF 模型是否真的发生变化，没有变化时不再累计空迁移。
- 迁移和命令输出中的 `Password`/`Pwd` 自动脱敏。
- 纯净模板中的 Migrator 同样包含连接字符串密码脱敏逻辑。

### 5. MCP 稳定性与配置一致性

涉及文件：

- `CodeMaster.McpServer/Program.cs`
- `CodeMaster.McpServer/Tools/ProjectOperationTool.cs`
- `CodeMaster.McpServer/README.md`

改动内容：

- MCP 支持从环境变量读取数据库提供程序和连接配置。
- 本地源码开发模式下，MCP 在自身配置后加载 `CodeMaster.WebApi/appsettings*.json`，使元数据库连接与 WebApi 保持一致，避免旧的 MCP 配置副本使用失效账号。
- 环境变量仍最后加载，部署环境可继续通过环境变量覆盖配置。
- 项目操作工具支持使用 `.codemaster/project-context.json` 解析当前项目。
- 启动、停止、状态、构建和迁移结果尽量返回简洁且可诊断的信息。
- 如果修改 MCP 后出现 DLL 锁定，只停止命令行明确指向 `CodeMaster.McpServer.dll` 的进程，再重新构建；不要结束无关 `dotnet` 进程。

### 6. 真实构建验证

涉及文件：

- `CodeMaster.Application/Services/CodeGen/Project/ProjectService.cs`
- `CodeMaster.McpServer/Tools/ProjectOperationTool.cs`

改动内容：

- CodeMaster 的“构建”操作现在应实际执行生成项目的 `dotnet build` 和 Vue `npm run build`。
- 不应仅根据接口返回成功判断构建成功；输出中应能确认两个构建命令真实执行。

### 7. 生成器回归修复

涉及文件：

- `CodeMaster.Application/Services/CodeGen/TemplateCodeGenerator.cs`
- `CodeMaster.Application/Services/CodeGen/Project/ModuleEntityService.cs`

改动内容：

- 关联实体的图片展示字段在列表和详情页生成 `<el-image>` 及预览，而不是普通下载链接。
- 修复分部实体 `.auto.cs` 文件输出目录解析。

### 8. SignalR 开发代理

涉及文件：

- `CodeMaster.Vue/vite.config.js`

改动内容：

- 增加 `/hubs` WebSocket 代理，生成项目开发环境中的 SignalR 连接不再返回 404。
- 新导出的模板会继承该配置。

## FabricQms 项目完成内容

FabricQms 的实体元数据和生成文件通过 CodeMaster MCP 创建，不应通过直接修改 CodeMaster 元数据表来维护。

### CodeMaster 元数据

- 4 个模块：基础数据、生产管理、质量管理、统计报表。
- 10 个业务实体：Workshop、Vehicle、Fabric、DeviceBinding、WorkshopWorker、FabricVehicleMapping、ImportLog、ProductionTask、DefectType、DefectRecord。
- 当前迁移目录只包含 FabricQms 自身的两次迁移及 Snapshot，不是模板历史迁移。

### 自定义业务实现

- Web API：登录、Excel 导入、生产任务、二维码、PDA 废料登记、当天记录、报表统计及 Excel 导出。
- PC Vue：QMS 工作台、数据导入、质量报表、公开车间看板。
- PDA：`FabricQms.Pda`，使用 Vue 3 + uni-app，可构建 H5 和 Android App 资源。
- 默认开发端口：Web API `9000`，PC Vue `9001`。

主要自定义代码入口：

- `D:\MyWorks\废料统计\FabricQms\FabricQms.Application\Services\Qms\QmsBusinessService.cs`
- `D:\MyWorks\废料统计\FabricQms\FabricQms.WebApi\Controllers\Qms\QmsBusinessControllers.cs`
- `D:\MyWorks\废料统计\FabricQms\FabricQms.Vue\src\views\dashboard\index.vue`
- `D:\MyWorks\废料统计\FabricQms\FabricQms.Pda\README.md`

## 验证结果

- CodeMaster Release 构建：0 错误。
- CodeMaster.CodeGenerator.Tests：25 通过、1 个手工 Seeder 测试跳过、0 失败。
- FabricQms Release 构建：0 错误。
- FabricQms PC Vue 生产构建：成功。
- FabricQms PDA Android 资源构建：成功。
- FabricQms Swagger、登录、当前用户、车间列表和报表接口均完成 200 冒烟验证。

## 尚未完成或需要注意

- CodeMaster 的上述源码修改当前可能仍处于未提交状态，提交前先执行 `git status` 和测试。
- `CodeMaster.WebApi/appsettings*.json`、`CodeMaster.Migrator/appsettings.json` 中存在本机数据库配置，不要把本机密码随框架修改提交。
- `D:\MyWorks\废料统计\FabricQms` 当前不是独立 Git 仓库；如需版本管理，应先决定放入父仓库还是单独初始化。
- FabricQms 尚需录入真实车间和员工、导入真实布料/车型/生产计划，并完成 PDA Android 签名和现场验收。
- 当前依赖仍有安全警告，包括 Scriban、System.Drawing.Common、SQLitePCLRaw 以及 uni-app npm 依赖；升级需要单独做兼容性回归。

## 后续修改原则

- CodeMaster 项目、模块、实体和字段元数据优先通过 MCP 工具维护，不直接改元数据表。
- 不要手工编辑生成的 `.vue`、`.auto.js`、`.script.json`、`.fields.json`、`.tree.json` 作为长期方案。
- 修改模板清理、初始化、迁移或启动逻辑后，至少执行 CodeGenerator 测试、纯净模板内容检查和一个新生成项目的启动验证。
- 不要恢复“模板携带 Migrations”或“生成项目继承 CodeMaster 数据库环境变量”的旧行为。
