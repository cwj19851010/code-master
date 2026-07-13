# 技术设计方案

## 架构概览

本变更遵循现有的 CodeMaster 分层架构，修改涉及 Domain（实体模型）、Application（服务层 + 代码生成）、CodeGenerator（Scriban 模板）和前端（Vue 模板 + 管理页面）。

```
CodeMaster.Domain           → 实体模型修改 + 新增
CodeMaster.Application      → CodeGeneratorService 重写 + ModuleEntityService 扩展
CodeMaster.CodeGenerator    → Scriban 模板新增/重写
CodeMaster.Vue              → moduleEntity 编辑页 UI 扩展
CodeMaster.Migrator         → 数据库迁移
```

## 组件一：领域模型扩展

### 1.1 ModuleEntity 新增字段

在 `ModuleEntity.cs` 中添加：

```csharp
[SugarColumn(ColumnName = "has_primary_key", IsNullable = false)]
public bool HasPrimaryKey { get; set; } = true;
```

### 1.2 EntityField 新增字段

在 `EntityField.cs` 中添加以下字段支持 select-table / cascader / 多选：

```csharp
// 是否多选（select / select-table / cascader 适用）
public bool IsMultiple { get; set; }

// 关联表名（select-table / cascader 适用）
public string? RelatedEntityName { get; set; }

// 关联表 Id 字段（select-table / cascader 适用）
public string? RelatedEntityIdField { get; set; }

// 关联表显示字段列表（JSON 数组，select-table 适用）
public string? RelatedEntityDisplayFields { get; set; }
```

### 1.3 新增 OneToManyRelation 实体

```csharp
[SugarTable("sys_one_to_many_relation")]
public class OneToManyRelation : EntityBase
{
    public long ModuleEntityId { get; set; }     // 主表实体 ID
    public string MasterField { get; set; }       // 主表关联字段
    public long ChildEntityId { get; set; }       // 子表实体 ID
    public string ChildEntityName { get; set; }  // 子表实体名称
    public string ChildForeignKey { get; set; }  // 子表外键字段
}
```

## 组件二：勾选/取消勾选自动字段管理

在 `ModuleEntityService`（或相关服务）的更新方法中：

1. **比较新旧值**：检测 `HasPrimaryKey`/`IsTree`/`HasTenant`/`HasDataPermission` 是否变化
2. **勾选时**：自动创建对应 `EntityField` 记录（`IsSystemField=true`）
3. **取消勾选时**：
   - 未保存（前端 `isNew` 状态）→ 直接移除
   - 已保存 → 标记软删除
   - 已软删除再勾选 → 恢复

此逻辑放在**前端 `edit.vue`** 中处理（与字段列表的交互模式一致），后端仅做最终持久化。

## 组件三：CodeGeneratorService 重写

### 3.1 从 StringBuilder 迁移到 Scriban 模板

当前 `CodeGeneratorService.cs` 使用 StringBuilder 拼接代码。迁移到 Scriban 模板引擎：

- 将模板文件放在 `CodeMaster.CodeGenerator/Templates/` 下
- 通过 `CodeGeneratorService` 加载元数据 → 传入 Scriban → 输出文件

### 3.2 实体生成（Partial 类）

```
Templates/Entity.auto.scriban     → 生成 {Name}.auto.cs（每次覆盖）
Templates/Entity.scriban          → 生成 {Name}.cs（仅首次）
```

实体模板需要：
- 根据配置项动态选择继承的接口列表（`IBaseEntity` + `IEntity`/`ITree`/`ITenant`/`IDept`）
- 过滤掉接口已定义的字段（避免重复）
- 一对多关系生成 `[Navigate]` 属性

### 3.3 DTO 生成

```
Templates/Dto.scriban         → {Name}Dto.cs
Templates/CreateDto.scriban   → Create{Name}Dto.cs  
Templates/UpdateDto.scriban   → Update{Name}Dto.cs
```

DTO 也需继承相同接口。

### 3.4 Service 生成

```
Templates/ServiceInterface.scriban → I{Name}Service.cs
Templates/Service.scriban          → {Name}Service.cs
```

根据 `IsReadOnly` 选择基类；一对多关系时在 `GetById` 方法中 Include 子表。

### 3.5 前端生成

```
Templates/Frontend/api.scriban    → src/api/{module}/{name}.js
Templates/Frontend/index.scriban  → src/views/{module}/{name}/index.vue
Templates/Frontend/add.scriban    → src/views/{module}/{name}/add.vue
Templates/Frontend/edit.scriban   → src/views/{module}/{name}/edit.vue
Templates/Frontend/detail.scriban → src/views/{module}/{name}/detail.vue
```

前端模板需要支持：
- 树形列表（无分页，`el-table` + `tree-props`）
- 所有 14 种表单控件类型
- 多选时 `Array ↔ 逗号分隔字符串` 转换
- select-table 单选/多选模式
- cascader 树形数据构建 + 完整路径展示
- 一对多子表行内编辑
- 字典数据源批量加载

## 组件四：ModuleEntity 编辑页 UI 扩展

### 4.1 一对多关系管理

在 `moduleEntity/edit.vue` 中添加"一对多关系"区域：
- 可添加/编辑/删除一对多关系记录
- 每条记录选择：主表字段、子表名（从 ModuleEntity 列表选择）、子表外键字段

### 4.2 新增控件类型

在字段编辑弹框的 `FormControlType` 下拉选项中新增：
- checkbox、checkbox-group、radio-group、select-table、cascader

选择 select-table / cascader 时展示额外配置：
- 关联表名、关联表 Id 字段、显示字段列表、是否多选

## 组件五：约束校验

代码生成前校验：
1. 非 `IsReadOnly` 的实体必须 `HasPrimaryKey=true`
2. 多选字段类型不能为数值类型
3. cascader 关联的表必须 `IsTree=true`
4. 被引用的子表默认不能独立生成

## 数据库迁移

需要新增 EF Core 迁移来添加：
- `sys_module_entity` 表新增 `has_primary_key` 列
- `sys_entity_field` 表新增 `is_multiple`、`related_entity_name`、`related_entity_id_field`、`related_entity_display_fields` 列
- 新建 `sys_one_to_many_relation` 表
