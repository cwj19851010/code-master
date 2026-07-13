## Context

当前 CodeMaster 使用 Scriban 文本模板生成 Vue SFC 文件。v1 开发中遇到的核心痛点：追加事件（`@change`）无法判断是否已有其他 change、条件生成导致缩进错乱、条件渲染依赖跨字段分析需要预计算。改用 C# 类建模 Vue 页面结构（Model → Renderer），Builder 层用程序逻辑替代模板语法。

## Goals / Non-Goals

**Goals:**
- 用 C# 对象完整描述 .vue 文件的 template / script setup / style 三部分
- Builder 层处理所有复杂逻辑（事件拼接、计算字段依赖、子表 rowStatus、统计字段触发）
- Renderer 层负责格式化输出，保证缩进正确
- 新旧生成器通过开关并行，不影响现有功能

**Non-Goals:**
- 不替换后端模板（Entity/DTO/Service 仍用 Scriban）
- 不解析已有 .vue 文件（为 v3 预留 Model 字段）
- 不改变现有 EntityField 数据结构

## Decisions

### 1. 两层分离：Model + Renderer（参考 SmartCoder 架构）

Model 层用 C# 对象描述 Vue 页面概念（Component 树、Imports、Refs、Reactive、Functions、Hooks）。Renderer 只关心"如何输出文本"。

**备选方案**：VNode 树 + 直接 render。被否决——丢失了"概念"信息（哪些是 refs、哪些是 imports），后续解析和修改都需要这些元信息。

### 2. `VueProp.Type` 枚举替代 `IsBind` + `IsSingle`

一个 enum（Static/Bind/BindShort/Event/EventShort/Directive/DirectiveArg/Boolean/SlotMark）统一描述属性类型。Renderer 根据类型生成正确的 Vue 语法。

### 3. `FunctionInfo.Body` 直接存代码行

不尝试解析 JS 表达式，存 `List<string>` 代码行。Renderer 只管缩进拼接。Builder 负责生成正确的代码。

### 4. 解析预留字段：`SourceLocation?`

每个 Model 类可选带 `SourceLocation`（Line/Col/EndLine/EndCol）。v2 生成时填 null，v3 解析已有 .vue 时填充，Renderer 输出保留原始格式。

### 5. 开关控制：`CodeGeneratorService.UseNewVueBuilder`

默认 `false` 走 Scriban。设为 `true` 时 Index/Add/Edit/Detail 四个页面使用新 Builder。验证通过后删除开关和旧模板。

## Risks / Trade-offs

- **输出不一致风险**：Renderer 可能输出与 Scriban 模板不同的格式（空格、换行）。缓解：用 diff 对比两种方式输出，逐个调整
- **Builder 代码膨胀**：4 个 Builder + 基类可能代码量大。缓解：提取公共方法到 `PageBuilderBase`，计算/统计字段处理封装为独立方法
