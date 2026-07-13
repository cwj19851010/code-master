# 实体接口继承规范

## ADDED: HasPrimaryKey 配置字段

`ModuleEntity` 新增 `HasPrimaryKey` 布尔字段。非 `IsReadOnly` 的实体必须 `HasPrimaryKey=true`。

## MODIFIED: 接口继承矩阵

所有生成的**实体**和**DTO**都必须继承 `IBaseEntity`，然后根据配置项额外继承：

| 配置项 | 继承接口 | 接口字段 |
|--------|---------|---------|
| `HasPrimaryKey=true` | `IEntity` | `long Id` |
| `IsTree=true` | `ITree` | `long? ParentId` |
| `HasTenant=true` | `ITenant` | `long TenantId` |
| `HasDataPermission=true` | `IDept` | `long? DeptId`, `string? DeptAncestors`, `long? CreateUserId` |

## ADDED: Partial 类分离机制

实体使用 `partial` 关键字分为两个文件：
- `.cs` — 用户自定义部分，仅首次生成
- `.auto.cs` — 代码生成部分，每次覆盖

---

# 服务层生成规范

## MODIFIED: Service 基类选择

| 配置 | 基类 |
|------|------|
| `IsReadOnly=true` | `ReadOnlyApplicationService<TEntity, TDto>` |
| `IsReadOnly=false` | `CrudServiceBase<TEntity, TDto, TCreateDto, TUpdateDto>` |

---

# 自动字段管理规范

## ADDED: 勾选时自动新增字段

勾选 `HasPrimaryKey` → 自动新增 `Id` 字段（IsSystemField=true）  
勾选 `IsTree` → 自动新增 `ParentId` 字段  
勾选 `HasTenant` → 自动新增 `TenantId` 字段  
勾选 `HasDataPermission` → 自动新增 `DeptId`、`DeptAncestors`、`CreateUserId` 字段

## ADDED: 取消勾选时删除字段

- 未保存字段 → 直接移除
- 已保存字段 → 软删除
- 软删除字段重新勾选 → 恢复

---

# 一对多关系规范

## ADDED: OneToManyRelation 配置实体

| 配置项 | 说明 |
|--------|------|
| 主表字段 | 主表关联字段（不限于主键） |
| 子表名 | 关联的 ModuleEntity 名称 |
| 子表外键字段 | 子表中的外键字段 |

## ADDED: SqlSugar 导航属性生成

主表实体生成 `[Navigate]` 属性和 `List<子表>` 导航属性。

## ADDED: API Include 策略

- `GetPagedList` / `GetList` → 不 Include 子表
- `GetById` → Include 子表

## ADDED: 外键引用标记

被其他实体引用的子表标记为"被引用"状态，默认不能独立生成。

---

# 表单控件类型规范

## ADDED: 新增控件类型

| 控件类型 | 组件 | 说明 |
|---------|------|------|
| checkbox | el-checkbox | 单个复选框 |
| checkbox-group | el-checkbox-group | 复选框组（多选） |
| radio-group | el-radio-group | 单选按钮组 |
| select-table | el-select | 关联表选择器 |
| cascader | el-cascader | 级联选择器 |

## ADDED: 多选处理规则

多选时：绑定变量为 `{字段名}List`，存储为逗号分隔字符串，展示用 el-tag。

---

# select-table 关联表选择器规范

## ADDED: 配置项

关联表名、关联表Id字段、显示字段列表、是否多选。

## ADDED: 前端生成规则

单选：附加 JSON 对象字段。  
多选：附加 JSON 数组字段 + el-tag 展示。

---

# cascader 级联选择器规范

## ADDED: 配置项

关联树形表、关联Id字段、显示字段、是否多选。

## ADDED: 前端生成规则

加载扁平数据 → 构建树形 → el-cascader 组件。列表/详情显示完整路径。

---

# 前端代码生成规范

## ADDED: 完整前端页面生成

| 文件 | 路径 |
|------|------|
| API | `src/api/{模块名}/{实体名小写}.js` |
| 列表页 | `src/views/{模块名}/{实体名小写}/index.vue` |
| 新增页 | `src/views/{模块名}/{实体名小写}/add.vue` |
| 编辑页 | `src/views/{模块名}/{实体名小写}/edit.vue` |
| 详情页 | `src/views/{模块名}/{实体名小写}/detail.vue` |

---

# Scriban 模板规范

## MODIFIED: 从 StringBuilder 迁移到 Scriban 模板

所有代码生成从 `CodeGeneratorService.cs` 的 StringBuilder 拼接迁移为 Scriban 模板文件。
