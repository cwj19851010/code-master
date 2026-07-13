# 实现 code-generation-rules.md 完整代码生成规则

## 概述

本变更旨在将 `openspec/code-generation-rules.md` 中定义的所有规则完整实现到 CodeMaster 项目中。当前的代码生成器（`CodeGeneratorService.cs`）仅支持基础的实体/DTO/Service/API 生成（StringBuilder 方式），缺少大量规则文档中定义的功能。

## 变更动机

现有代码生成器的不足：

1. **实体模型缺失**：`ModuleEntity` 缺少 `HasPrimaryKey` 字段；缺少一对多关系（`OneToManyRelation`）实体
2. **字段模型缺失**：`EntityField` 缺少 `IsMultiple`（多选）、`RelatedEntityName`（关联表名）、`RelatedEntityIdField`（关联表Id字段）、`RelatedEntityDisplayFields`（显示字段列表）等 select-table/cascader 配置项
3. **接口继承未实现**：实体/DTO 未根据 `HasPrimaryKey`/`IsTree`/`HasTenant`/`HasDataPermission` 动态继承 `IEntity`/`ITree`/`ITenant`/`IDept`
4. **Partial 类机制未实现**：生成的实体未分离为 `.cs`（用户文件）和 `.auto.cs`（自动覆盖文件）
5. **前端页面未生成**：只生成了 API 文件，未生成列表页/新增页/编辑页/详情页
6. **勾选/取消勾选自动字段规则未实现**：缺少对系统字段的自动管理
7. **一对多关系完全未实现**：缺少子表配置、导航属性生成、子表 Include 策略
8. **新增表单控件类型未实现**：checkbox、checkbox-group、radio-group、select-table、cascader
9. **模板引擎未统一**：当前使用 StringBuilder 拼接，需迁移到 Scriban 模板

## 变更范围

### 后端修改
- `ModuleEntity.cs` — 添加 `HasPrimaryKey` 字段
- `EntityField.cs` — 添加多选、关联表、级联等配置字段
- 新增 `OneToManyRelation.cs` — 一对多关系配置实体
- `CodeGeneratorService.cs` — 完整重写，实现全部规则
- Scriban 模板 — 新增/重写实体、DTO、Service、前端模板
- `ModuleEntityService` — 实现勾选/取消勾选自动字段逻辑

### 前端修改
- `moduleEntity/edit.vue` — 添加一对多关系配置 UI、新增控件类型选项
- Scriban 前端模板 — 新增列表页/新增页/编辑页/详情页模板

## 成功标准

- 根据 `ModuleEntity` 的配置项，能正确生成所有后端代码（含 partial 类、接口继承、一对多导航属性）
- 能正确生成所有前端代码（API、列表页、新增页、编辑页、详情页）
- 所有表单控件类型正确支持
- 勾选/取消勾选配置项时自动管理系统字段
