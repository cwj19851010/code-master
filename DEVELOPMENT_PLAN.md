# CodeMaster 完整开发计划

> 最后更新：2026-06-22
> 基于代码库全面审查 + 代码路径整合决策

---

## 使用说明

每完成一项，将 `[ ]` 改为 `[x]`。优先级：**P0 = 阻塞性/架构决策，P1 = 核心功能缺口，P2 = 体验优化，P3 = 战略扩展**。

---

## 阶段〇：代码路径整合（P0 — 最高优先级，为所有后续工作扫清障碍）

### 决策：废弃纯 Scriban 前端生成 + C# VueBuilder，统一为 DB 模板路径

**当前三条路径**：

```
UseTemplateGenerator = true  → TemplateCodeGenerator (DB 模板)   ✅ 主力，默认开启
UseSplitScript = true        → ScriptBuilder (auto.js 分离)      ✅ 配合主力
UseNewVueBuilder = false     → C# VueBuilder                     ❌ 从未启用
Scriban 回退路径              → CodeGeneratorService 前端方法      ❌ 被 return 跳过，死代码
```

**清理后唯一路径**：

```
用户点击「生成代码」
    │
    ├── 后端：CodeGeneratorService (Scriban, 保留)
    │   └── Entity.auto.cs, Entity.cs, DTO, Service, 前端 API .js
    │
    └── 前端：TemplateCodeGenerator (DB 模板)
        └── .vue, .auto.js, .script.json, .fields.json, .tree.json
```

### 0.1 删除 Scriban 前端模板文件（33 个文件）

**目录**：`CodeMaster.CodeGenerator/Templates/Frontend/`

这些文件对应的模板已经存在于数据库种子数据 `TemplateModule.cs` 中，删除不会丢失任何功能。

- [ ] **0.1.1** 删除 4 个页面模板：`IndexTemplate.scriban`, `AddTemplate.scriban`, `EditTemplate.scriban`, `DetailTemplate.scriban`
- [ ] **0.1.2** 删除 1 个 API 模板：`ApiTemplate.scriban`
- [ ] **0.1.3** 删除 28 个控件模板：`controls/*.scriban`（input, number, select, date, datetime, switch, textarea, editor, file, image, cascader, select-table, table-column, detail, detail-*, search-*, select-enum）
- [ ] **0.1.4** 删除空目录 `CodeMaster.CodeGenerator/Templates/Frontend/controls/` 和 `CodeMaster.CodeGenerator/Templates/Frontend/`

### 0.2 删除 C# VueBuilder（25 个文件）

**目录**：`CodeMaster.Application/VueBuilder/`

`UseNewVueBuilder` 默认为 `false`，从未在生产路径中使用。删除整个目录。

- [ ] **0.2.1** 删除 `CodeMaster.Application/VueBuilder/` 整个目录（含 Builders/, Model/, Renderer/, Templates/ 子目录）

### 0.3 删除 CodeGeneratorService 中的死方法（~150 行）

**文件**：`CodeMaster.Application/Services/CodeGen/CodeGeneratorService.cs`

- [ ] **0.3.1** 删除 `GenerateAutoJsAsync()` 方法（行 653-670，内部调用 VueBuilder）
- [ ] **0.3.2** 删除 `GenerateFrontendIndexAsync()` 方法（行 675-696）
- [ ] **0.3.3** 删除 `GenerateFrontendAddAsync()` 方法（行 696-722）
- [ ] **0.3.4** 删除 `GenerateFrontendEditAsync()` 方法（行 722-741）
- [ ] **0.3.5** 删除 `GenerateFrontendDetailAsync()` 方法（行 741-751）
- [ ] **0.3.6** 删除 `PreRenderFields()` 方法（~行 890-910，内部使用 VueBuilder.FieldRenderer）
- [ ] **0.3.7** 删除 `UseNewVueBuilder` 静态属性（行 643）
- [ ] **0.3.8** 删除 `PendingSplitExports` 静态属性及相关引用
- [ ] **0.3.9** 删除顶部的 `using CodeMaster.Application.VueBuilder.*` 等 VueBuilder 相关 using 语句

### 0.4 删除 ModuleEntityService 中的 Scriban 回退路径（~100 行）

**文件**：`CodeMaster.Application/Services/CodeGen/Project/ModuleEntityService.cs`

- [ ] **0.4.1** 删除行 599-697：整个 `if (UseSplitScript) { ... }` Scriban 前端回退代码块（index/add/edit/detail 四个页面的 Scriban 生成 + IncrementalCodeGenerator 调用）
- [ ] **0.4.2** 删除行 1035-1041：`WriteVueFileAsync` 中 `UseNewVueBuilder` 分支
- [ ] **0.4.3** 删除 `VueBuilder.Renderer.IncrementalRenderer` 的 using 语句（如无其他引用）
- [ ] **0.4.4** 删除 `new IncrementalCodeGenerator(generator)` 的实例化（行 462），如 `IncrementalCodeGenerator` 类不再被使用
- [ ] **0.4.5** 确认 `CodeGeneratorService.UseSplitScript` 和 `CodeGeneratorService.UseTemplateGenerator` 两个开关是否还需要保留。`UseTemplateGenerator=true` + `UseSplitScript=true` 已经是唯一路径，可以移除开关或将它们改为 true 常量

### 0.5 删除 IncrementalCodeGenerator（478 行）

**文件**：`CodeMaster.Application/Services/CodeGen/IncrementalCodeGenerator.cs`

该类的所有调用都在 0.4.1 删除的 Scriban 回退路径中。TemplateCodeGenerator 路径使用 `MergeTreeOrder` 实现增量保留拖拽位置，不需要 `IncrementalCodeGenerator`。

- [ ] **0.5.1** 删除 `CodeMaster.Application/Services/CodeGen/IncrementalCodeGenerator.cs`
- [ ] **0.5.2** 如果 `CodeMaster.CodeGenerator.Tests` 中有对该类的测试引用，同步清理

### 0.6 清理 CodeMaster.CodeGenerator 项目

- [ ] **0.6.1** 确认 `CodeMaster.CodeGenerator/` 项目仅保留后端模板：`EntityAutoTemplate.scriban`, `EntityTemplate.scriban`, `DtoTemplate.scriban`, `ServiceInterfaceTemplate.scriban`, `ServiceTemplate.scriban`, `ControllerTemplate.txt`
- [ ] **0.6.2** 如果 `CodeMaster.CodeGenerator/` 项目除了模板文件没有其他代码，考虑将其合并到 `CodeMaster.Application` 的资源文件中（可选，低优先级）
- [ ] **0.6.3** 清理 `CodeMaster.CodeGenerator.Tests/` 中对已删除前端模板的测试引用

### 0.7 验证清理后项目可编译可运行

- [ ] **0.7.1** `dotnet build CodeMaster.sln` 无编译错误
- [ ] **0.7.2** `cd CodeMaster.WebApi && dotnet run` 启动成功
- [ ] **0.7.3** 走通完整生成流程：创建项目 → 创建模块 → 创建实体 → 添加字段 → 生成代码 → 验证输出文件完整
- [ ] **0.7.4** `dotnet test` 所有现有测试通过

---

## 阶段一：Bug 修复（P0 — 必须修，否则产品不可用）

### 1.1 修复 `$t()` 翻译 key 生成问题

**现状**：DB 模板使用 `[field.description]`（中文原文如 "客户名称"）作为 `$t()` 的 key。中文模式下 Vue i18n 回退到 key 本身所以"看起来正常"，但英文模式下没有对应翻译。

**重要**：阶段〇清理完成后，只有 DB 模板路径需要修复。Scriban 控件模板和 C# VueBuilder 已被删除。

**涉及文件**：

| # | 文件 | 行号 | 内容 |
|---|------|------|------|
| 1 | `CodeMaster.Migrator/SeedData/System/TemplateModule.cs` | 138-187 | DB 种子控件模板的 HTML（含 `$t('[field.description]')`） |
| 2 | `CodeMaster.Application/Services/CodeGen/Marker/MarkerReplacer.cs` | 83 | `[field.description]` → `ctx.Description` 的替换逻辑 |
| 3 | `CodeMaster.Application/Services/CodeGen/TemplateCodeGenerator.cs` | - | 生成流程，需新增翻译注册步骤 |

- [ ] **1.1.1** 在 `TemplateCodeGenerator` 或 `ModuleEntityService` 的生成流程中新增：代码生成后，自动将每个字段的 `Description` 作为 key 和 zh-CN value 写入 `sys_lang_texts` 表
- [ ] **1.1.2** DB 种子模板中的 `$t('[field.description]')` 保持不变（Description 作为 i18n key 是合理的设计）
- [ ] **1.1.3** ScriptSection 中的 label 引用同样使用 `field.description`，确认 MarkerReplacer 替换后的一致性
- [ ] **1.1.4** 全量重新生成 OrderManager 测试项目，验证中英文模式下所有 label 显示正常

### 1.2 修复组件面板拖入设计器不生效

**文件**：`CodeMaster.Vue/src/views/codegen/entityDesigner/index.vue`  
**行号**：468-469（`onPaletteDragStart` 设置了 `paletteDragTag`，但没有 `@drop` 消费它）

- [ ] **1.2.1** 在画布区域添加 `@drop` 事件处理：从 `paletteDragTag` 读取标签名 → 创建新节点 → 插入树
- [ ] **1.2.2** 添加 `@dragover` 事件（`e.preventDefault()`）使画布接受 drop
- [ ] **1.2.3** 支持拖放到具体位置（before/after/inside），与现有右键菜单 insert 行为一致
- [ ] **1.2.4** 测试：从面板拖 `el-input` 到空白区域、到 form-item 内部、到两个 form-item 之间

### 1.3 修复设计器保存后 tree.json 不更新

**文件**：`CodeMaster.Application/Services/CodeGen/Project/ModuleEntityService.cs`  
**行号**：1102-1142（`SavePageContentAsync`）  
**问题**：保存时只更新 `.vue` 和 `fields.json`，`tree.json` 不变。

- [ ] **1.3.1** 在 `SavePageContentAsync` 中，反序列化 `TreeJson` 后直接写回 `{entity}.{pageType}.tree.json`
- [ ] **1.3.2** 验证：设计器中删除一个控件 → 保存 → 刷新页面 → 控件确实消失

### 1.4 修复增量生成（TemplateCodeGenerator 路径）

**文件**：`CodeMaster.Application/Services/CodeGen/Project/ModuleEntityService.cs`  
**现状**：`MergeTreeOrder` 已实现（保留树节点拖拽位置），但每次都是全量重新生成。需要验证和增强。

- [ ] **1.4.1** 验证 MergeTreeOrder：创建实体 → 全量生成 → 在设计器中拖拽排序 → 新增一个字段 → 再次生成 → 确认拖拽顺序保留
- [ ] **1.4.2** 如 MergeTreeOrder 有问题，对照旧 tree.json 和新生 tree.json 的 genId 映射修复
- [ ] **1.4.3** 考虑实现字段级增量：比较新旧字段列表，只重新渲染变化字段的控件块（可选优化，非必须）

---

## 阶段二：测试补齐（P0 — 核心功能必须验证）

### 2.1 控件生成测试矩阵

**测试范围**：DB 模板中的控件类型（种子数据在 `TemplateModule.cs`，运行时可在 templateConfig 页面查看）。

| # | 控件类型 | 需测试的 section | 状态 |
|---|---------|-----------------|------|
| 1 | input | add, edit, detail, search | [ ] |
| 2 | textarea | add, edit | [ ] |
| 3 | number | add, edit, search | [ ] |
| 4 | select | add, edit, search | [ ] |
| 5 | select-table | add, edit, search, detail | [ ] |
| 6 | date | add, edit, search, detail | [ ] |
| 7 | datetime | add, edit, search, detail | [ ] |
| 8 | switch | add, edit, search, detail | [ ] |
| 9 | editor | add, edit | [ ] |
| 10 | file | add, edit, detail | [ ] |
| 11 | image | add, edit, detail | [ ] |
| 12 | cascader | add, edit, search, detail | [ ] |
| 13 | select-enum | search, add, edit | [ ] |
| 14 | table-column | list | [ ] |
| 15 | search-input | search | [ ] |
| 16 | search-number | search | [ ] |
| 17 | search-select | search | [ ] |
| 18 | search-date | search | [ ] |
| 19 | search-datetime | search | [ ] |
| 20 | search-switch | search | [ ] |
| 21 | search-cascader | search | [ ] |
| 22 | search-enum | search | [ ] |
| 23 | search-select-table | search | [ ] |

- [ ] **2.1.1** 写参数化测试，遍历以上所有 23 个控件类型 × 对应的 page section
- [ ] **2.1.2** 验证每个生成输出：正确的 Element Plus 标签、正确的 `v-model` 绑定、正确的 `$t()` key、必要的 import
- [ ] **2.1.3** 重点测试 `cascader`（递归控件）：验证 tree 数据加载、`buildTree` 函数、`checkStrictly` 属性
- [ ] **2.1.4** 重点测试 `select-table`（关联表）：验证关联实体数据正确加载、外键绑定正确

### 2.2 统计字段测试

**数据模型**：`EntityField.cs` 行 231-281（FieldCategory/Formula/AggregateType 已有）  
**生成逻辑**：`CodeGeneratorService.cs` 行 445-503  
**注意**：后端完整，前端 UI 缺失（在阶段三补齐）

- [ ] **2.2.1** 测试 Computed 字段：字段 A（Price）× 字段 B（Quantity）= Computed 字段 C（Total），验证生成的 JS watch 逻辑正确
- [ ] **2.2.2** 测试 Aggregate 字段：主子表关系，子表 Amount 字段 AggregateType=Sum，验证聚合计算代码正确
- [ ] **2.2.3** 测试 computed dependency map：修改 Price → Total 自动重算，但修改无关字段不触发
- [ ] **2.2.4** 前端补齐后，走通完整流程：UI 设置 → 生成 → 运行 → 验证计算结果

### 2.3 删除控件后 ScriptSection 清理测试

**文件**：`CodeMaster.Application/Services/CodeGen/Project/ModuleEntityService.cs`，行 1225-1294

- [ ] **2.3.1** 删除单个 form-item（有 genId） → 保存 → 检查 `fields.json` 中对应条目被移除
- [ ] **2.3.2** 主子表场景：删除子表卡片 → 保存 → 检查子表所有字段 genId 从 `fields.json` 清理
- [ ] **2.3.3** 嵌套删除：删除 el-card（内含多个 form-item） → 验证所有嵌套 genId 均被清理
- [ ] **2.3.4** 验证 `CollectNodeInfo` 递归遍历了 `children` 和 `useSlots[].components` 两个路径

### 2.4 设计器端到端测试

- [ ] **2.4.1** 创建实体（5 个字段） → 生成代码 → 打开设计器 → 验证树结构正确加载
- [ ] **2.4.2** 在设计器中拖拽排序 → 保存 → 重新加载 → 顺序保留
- [ ] **2.4.3** 在设计器中新增组件（从面板） → 保存 → 重新加载 → 组件存在
- [ ] **2.4.4** 在设计器中修改 ScriptSection → 保存 → 重新生成代码 → 自定义 script 保留

---

## 阶段三：功能完善（P1 — 影响产品完整度）

### 3.1 联动下拉选择（省市区级联）

**当前状态**：`el-cascader` 已支持（树形级联），但缺少链式下拉（选省 → 市下拉框刷新 → 选区 → 街道下拉框刷新）。

**设计方案**：

```
sys_entity_field 新增字段：
  linked_parent_field_id   long?    指向同一实体的父字段
  linked_filter_field      string?  子选项过滤用的字段名（默认外键）

示例（Region 实体，IsTree=true）：
  Province  (select, 过滤: ParentId IS NULL)
  City      (select, linked_parent_field_id → Province, linked_filter_field = ParentId)
  District  (select, linked_parent_field_id → City, linked_filter_field = ParentId)
```

- [ ] **3.1.1** `EntityField.cs` 新增 `LinkedParentFieldId` 和 `LinkedFilterField` 属性
- [ ] **3.1.2** 创建数据库迁移：ALTER TABLE `sys_entity_field` 添加两列
- [ ] **3.1.3** 更新 DTO（EntityFieldDto, CreateEntityFieldDto, UpdateEntityFieldDto）
- [ ] **3.1.4** 在 DB 种子数据中新增 `linked-select` 控件类型（`sys_field_control_templates` 新记录）
- [ ] **3.1.5** `TemplateCodeGenerator.MapControlType()` 新增 `linked-select` 映射
- [ ] **3.1.6** 在生成逻辑中为 linked-select 生成 watch 链（`form.province` 变 → 重载 `cityOptions` → `form.city` 变 → 重载 `districtOptions`）
- [ ] **3.1.7** 前端 entityField 表单新增联动配置区域
- [ ] **3.1.8** 端到端测试：Region 实体 → Province/City/District 三个联动字段 → 生成 → 运行 → 验证联动正常

### 3.2 前端控件类型选择器补齐

**文件**：`CodeMaster.Vue/src/views/codegen/entityField/index.vue`，行 184-195  
**现状**：下拉框只有 10 种（input, textarea, number, select, date, datetime, switch, radio, checkbox），缺少 5 种。

- [ ] **3.2.1** 新增到下拉框：`cascader`, `select-table`, `editor`, `file`, `image`
- [ ] **3.2.2** `select-table` 选择后显示关联实体选择器
- [ ] **3.2.3** `cascader` 选择后显示树形实体选择器 + 多选开关
- [ ] **3.2.4** `file`/`image` 选择后显示上传配置（数量限制、文件类型等）
- [ ] **3.2.5** 确认 `EntityField.FormControlType` 文档值与实际可用值一致

### 3.3 统计字段前端 UI 补齐

**数据模型已有，只缺前端表单**。

- [ ] **3.3.1** `entityField/add.vue` / `entityField/edit.vue` 新增 `FieldCategory` 选择器（Normal / Computed / Aggregate）
- [ ] **3.3.2** Computed 模式：显示公式输入框（placeholder: `[Price]*[Quantity]`，下方列出可用字段名提示）
- [ ] **3.3.3** Aggregate 模式：显示聚合类型下拉（Sum/Avg/Concat）、子表选择器、子表字段选择器、分隔符（仅 Concat）
- [ ] **3.3.4** `entityField/index.vue` 列表新增 `FieldCategory` 列

### 3.4 设计器优化

- [ ] **3.4.1** 右键菜单增加 `复制到其他页面` 选项（列出同一实体的其他 pageType 作为目标）
- [ ] **3.4.2** 画布中组件 hover 时显示 tooltip：标签名 + genId（非空时）
- [ ] **3.4.3** 撤销/重做覆盖 ScriptSection 编辑操作（目前仅覆盖树结构变更）
- [ ] **3.4.4** 画布中选中组件时，左侧树自动展开并滚动到对应节点

---

## 阶段四：MCP & Skill 修复（P1 — AI 集成质量）

### 4.1 MCP Server 修复

**目录**：`CodeMaster.McpServer/`

- [ ] **4.1.1** 测试 `create_or_update_entity`：创建完整实体 → 验证 DB 数据完整
- [ ] **4.1.2** 测试 `generate_code`：验证所有输出文件生成且无编译错误
- [ ] **4.1.3** 测试 `analyze_requirements`：验证返回 schema 与实际 DTO 一致
- [ ] **4.1.4** 修复 `create_or_update_entity` Update 路径：增量添加字段时保留已有字段
- [ ] **4.1.5** 修复 `Seed/Program.cs`：对照当前 ModuleEntityService API 更新
- [ ] **4.1.6** MCP 生成后自动 `dotnet build` 验证，失败时返回编译错误详情

### 4.2 Skill 审查

- [ ] **4.2.1** 审查 `.cursor/rules/` 4 个规则文件与实际代码一致性
- [ ] **4.2.2** 审查 OpenSpec 工作流是否正确引用当前项目结构
- [ ] **4.2.3** 测试 `code-review` skill 对本项目的 diff 审查准确性

---

## 阶段五：UniApp 手机端（P2 — 战略扩展）

### 5.1 平台抽象层

- [ ] **5.1.1** `sys_page_templates` 和 `sys_field_control_templates` 新增 `platform` 字段（`web` / `uniapp`），默认 `web`
- [ ] **5.1.2** 数据库迁移：ALTER TABLE 添加 platform 列
- [ ] **5.1.3** 更新 `SysPageTemplate.cs` 和 `SysFieldControlTemplate.cs` 实体
- [ ] **5.1.4** `TemplateCodeGenerator.GeneratePageAsync()` 新增 `platform` 参数
- [ ] **5.1.5** `ModuleEntityService.GenerateCodeAsync()` 新增 UniApp 输出路径

### 5.2 UniApp 控件模板（DB 种子数据）

- [ ] **5.2.1** Web→UniApp 控件映射表，写入 `sys_field_control_templates`（platform=`uniapp`）：

| Web 控件 | UniApp 替代 |
|----------|------------|
| el-input | u-input |
| el-input-number | u-input type="number" |
| el-select | u-select (picker) |
| el-date-picker | uni-datetime-picker |
| el-switch | u-switch |
| el-upload | uni-file-picker |
| el-table | u-table (uni-ui) |
| el-form-item | uni-forms-item |
| el-cascader | uni-data-picker |
| el-dialog | uni-popup |
| el-editor | uni-easyinput type="textarea" |
| el-image | uni-image |

- [ ] **5.2.2** 创建 UniApp 页面模板（index/add/edit/detail），platform=`uniapp`
- [ ] **5.2.3** `ScriptBuilder/ScriptRenderer` 适配 UniApp（Vue2 Options API）
- [ ] **5.2.4** `pages.json` 和 `manifest.json` 自动生成

---

## 阶段六：代码质量 & 文档（P2 — 长期维护）

### 6.1 剩余清理

- [ ] **6.1.1** `CodeMaster.Vue/node_modules1/` 目录 → 确认是否需要，不需要则加入 `.gitignore`
- [ ] **6.1.2** 清理临时项目：`_g/`, `_fix2/`, `_fx/`, `.reasonix/` 目录 → 删除
- [ ] **6.1.3** `CodeMaster.CodeGenerator/` 项目瘦身：仅保留后端 Scriban 模板，考虑将其作为资源嵌入 Application
- [ ] **6.1.4** 清理 `openspec/changes/csharp-vue-builder/` 目录（已废弃的提案）

### 6.2 测试基础设施

- [ ] **6.2.1** `CodeMaster.CodeGenerator.Tests` 添加 CI 可运行配置
- [ ] **6.2.2** 添加快照测试：代码生成输出文件对比，防止回归
- [ ] **6.2.3** 新增 `CodeMaster.Application.Tests` 单元测试项目

### 6.3 文档维护

- [ ] **6.3.1** 更新 `README.md`：.NET 版本 → 10.0，补充 CodeGen/SignalR/Quartz/缓存
- [ ] **6.3.2** 更新 `QUICKSTART.md`：修正端口号和 .NET 版本
- [ ] **6.3.3** 更新 `CODEGEN_ARCHITECTURE.md`：反映代码路径整合后的架构
- [ ] **6.3.4** 更新 `CLAUDE.md`：反映代码路径整合后的项目结构
- [ ] **6.3.5** 新增 `TESTING.md`：测试策略和运行指南

---

## 完成进度总览

| 阶段 | 内容 | 总任务数 | 已完成 | 进度 |
|------|------|---------|--------|------|
| 阶段〇 | 代码路径整合 | 22 | 0 | 0% |
| 阶段一 | Bug 修复 | 12 | 0 | 0% |
| 阶段二 | 测试补齐 | 18 | 0 | 0% |
| 阶段三 | 功能完善 | 17 | 0 | 0% |
| 阶段四 | MCP & Skill | 9 | 0 | 0% |
| 阶段五 | UniApp | 9 | 0 | 0% |
| 阶段六 | 代码质量 | 10 | 0 | 0% |
| **合计** | | **97** | **0** | **0%** |

---

## 建议执行顺序

```
Day 1-3:   阶段〇 代码路径整合（删代码、验证编译）
Day 4-5:   1.1 ($t修复) → 1.2 (拖入) → 1.3 (tree.json) → 1.4 (增量验证)
Day 6-8:   2.1 (控件测试) → 2.3 (ScriptSection清理) → 2.4 (设计器E2E)
Day 9-10:  3.2 (控件选择器) → 3.3 (统计字段UI) → 2.2 (统计字段测试)
Day 11-13: 3.1 (联动下拉，含设计+实现+测试)
Day 14-15: 3.4 (设计器优化)
Day 16-17: 4.1 (MCP修复) → 4.2 (Skill审查)
Week 4+:   5.x (UniApp)
Week 5+:   6.x (清理和文档)
```

---

## 清理统计

执行阶段〇后，将从代码库中移除：

| 资源 | 数量 | 路径 |
|------|------|------|
| Scriban 前端模板 | 33 个文件 | `CodeMaster.CodeGenerator/Templates/Frontend/` |
| C# VueBuilder | 25 个文件 | `CodeMaster.Application/VueBuilder/` |
| CodeGeneratorService 死方法 | ~150 行 | `CodeGeneratorService.cs` |
| ModuleEntityService 死代码 | ~100 行 | `ModuleEntityService.cs` |
| IncrementalCodeGenerator | 1 个文件 (478行) | `IncrementalCodeGenerator.cs` |
| **合计删除** | **~60 个文件, ~800 行代码** | |

保留的后端 Scriban 模板（继续用于 Entity/DTO/Service 生成）：

| 文件 | 用途 |
|------|------|
| `EntityAutoTemplate.scriban` | Entity.auto.cs（每次覆盖） |
| `EntityTemplate.scriban` | Entity.cs（仅首次创建） |
| `DtoTemplate.scriban` | DTO 类 |
| `ServiceInterfaceTemplate.scriban` | IService 接口 |
| `ServiceTemplate.scriban` | Service 实现 |
| `ControllerTemplate.txt` | Controller（旧格式，保留参考） |
