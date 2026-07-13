# 实现任务清单

## 1. 领域模型扩展

### 1.1 ModuleEntity 新增 HasPrimaryKey 字段
- [x] 在 `ModuleEntity.cs` 中添加 `HasPrimaryKey` 布尔属性（默认 `true`）
- [x] 添加 `[SugarColumn]` 属性配置

### 1.2 EntityField 新增关联表/多选字段
- [x] 添加 `IsMultiple` 布尔属性
- [x] 添加 `RelatedEntityName` 字符串属性
- [x] 添加 `RelatedEntityIdField` 字符串属性
- [x] 添加 `RelatedEntityDisplayFields` 字符串属性（JSON 数组格式）
- [x] 更新 FormControlType 注释，覆盖全部 14 种控件类型

### 1.3 新增 OneToManyRelation 实体
- [x] 创建 `OneToManyRelation.cs`
- [x] 包含 `ModuleEntityId`、`MasterField`、`ChildEntityId`、`ChildEntityName`、`ChildForeignKey` 字段

### 1.4 数据库迁移
- [ ] 创建 EF Core 迁移以添加新列和新表（SqlSugar 自动建表，无需手动迁移）

---

## 2. 勾选/取消勾选自动字段管理

### 2.1 前端自动字段逻辑
- [x] 在 `moduleEntity/edit.vue` 中，监听 `HasPrimaryKey` 切换，自动管理 `Id` 系统字段
- [x] 监听 `IsTree` 切换，自动管理 `ParentId` 系统字段
- [x] 监听 `HasTenant` 切换，自动管理 `TenantId` 系统字段
- [x] 监听 `HasDataPermission` 切换，自动管理 `DeptId`/`DeptAncestors`/`CreateUserId` 系统字段
- [x] 实现取消勾选时的软删除/恢复逻辑

### 2.2 字段编辑弹框状态管理
- [x] 实现新增→编辑→保存保持"新增"状态
- [x] 实现已保存→编辑→保存变为"更新"状态
- [x] 实现取消不改变状态
- [x] 实现提交时只提交有状态变更的数据（newFields/updatedFields/deletedFieldIds）

---

## 3. Scriban 模板创建

### 3.1 后端模板
- [x] 创建 `EntityAutoTemplate.scriban` — 自动生成实体（含接口继承、字段过滤、partial、导航属性）
- [x] 创建 `EntityTemplate.scriban` — 用户自定义实体（仅首次生成）
- [x] 创建 `DtoTemplate.scriban` — 查询 DTO（含接口继承）
- [x] 创建 `ServiceInterfaceTemplate.scriban` — 服务接口
- [x] 创建 `ServiceTemplate.scriban` — 服务实现（含基类选择、一对多 Include）

### 3.2 前端模板
- [x] 创建 `ApiTemplate.scriban` — API 文件
- [x] 创建 `IndexTemplate.scriban` — 列表页（支持分页/树形切换、全部控件类型列展示）
- [x] 创建 `AddTemplate.scriban` — 新增页（支持全部 14 种控件、子表行内编辑）
- [x] 创建 `EditTemplate.scriban` — 编辑页（同新增页 + 数据回填）
- [x] 创建 `DetailTemplate.scriban` — 详情页（含子表数据展示）

---

## 4. CodeGeneratorService 重写

### 4.1 模板引擎集成
- [x] 重写 `CodeGeneratorService.cs`，使用 Scriban 模板替代 StringBuilder
- [x] 从数据库读取 `ModuleEntity` + `EntityField` + `OneToManyRelation` 元数据
- [x] 实现接口继承计算逻辑
- [x] 实现接口字段过滤逻辑（排除接口已定义的字段）
- [x] 实现 `.auto.cs` 覆盖 / `.cs` 仅首次创建逻辑

### 4.2 约束校验
- [x] 非 `IsReadOnly` 实体必须 `HasPrimaryKey=true`
- [x] 多选字段不能为数值类型
- [x] cascader 关联表必须 `IsTree=true`
- [x] 被引用子表默认不能独立生成

### 4.3 前端代码生成
- [x] 实现前端 API/列表页/新增页/编辑页/详情页生成
- [x] 支持树形列表（无分页，GetList + 前端构建树）
- [x] 支持 select-table 单选/多选模式
- [x] 支持 cascader 树形数据加载和完整路径展示
- [x] 支持字典数据源批量加载
- [x] 支持多选时 Array ↔ 逗号分隔字符串转换

### 4.4 一对多关系处理
- [x] 主表新增/编辑页包含子表行内编辑
- [x] 详情页展示子表数据列表
- [x] 后端 Create/Update 方法同时处理子表增删改
- [x] GetById 时 Include 子表数据

### 4.5 菜单和权限同步
- [x] 生成后同步到菜单表（目录菜单 + 页面菜单 + 按钮权限）
- [x] 自动生成权限标识（`{namespace}:{classname}:{method}`）

---

## 5. ModuleEntity 编辑页 UI 扩展

### 5.1 一对多关系管理 UI
- [x] 在 `edit.vue` 中添加一对多关系列表区域
- [x] 支持添加/编辑/删除一对多关系记录
- [x] 主表字段下拉（从当前实体字段列表中选择）
- [x] 子表名下拉（从 ModuleEntity 列表中选择）
- [x] 子表外键字段下拉（从选择的子表的字段列表中选择）

### 5.2 新增控件类型
- [x] FormControlType 下拉新增 checkbox、checkbox-group、radio-group、select-table、cascader 选项
- [x] 选择 select-table 时展示关联表配置字段
- [x] 选择 cascader 时展示关联树形表配置字段
- [x] 选择 checkbox-group / select / select-table / cascader 时展示"是否多选"开关

### 5.3 DTO 和 Service 更新
- [x] 更新 `ModuleEntityDto` 添加 `HasPrimaryKey` 字段
- [x] 更新 `EntityFieldDto` 添加新增字段
- [x] 新增 `OneToManyRelationDto`/`CreateOneToManyRelationDto`/`UpdateOneToManyRelationDto`
- [x] 新增 `OneToManyRelationService`（通过 ModuleEntityService 统一管理，无需独立 Service）

---

## 6. 验证

### 6.1 编译验证
- [x] `dotnet build CodeMaster.sln` 编译通过（0 错误）

### 6.2 功能验证 (全自动集成测试)
- [x] 创建测试 ModuleEntity，配置各种选项组合
- [x] 生成代码并验证生成产物的正确性
- [x] 验证 partial 类机制（首次生成 + 二次生成不覆盖用户文件）
- [x] 验证接口继承的正确性
- [x] 验证一对多导航属性的正确性
- [x] 验证前端页面生成的完整性
- [x] 验证勾选/取消勾选自动字段管理

---

## 7. 跨项目集成与连结隔离 (OpenSpec V11)

### 7.1 数据库会话完全解耦
- [x] 规定 `SQLite` 在跨项目代码中的绝对路径转换规则与逻辑。
- [x] `ModuleEntityService.cs` 中实现 `GetTargetDbClient` 即时构建指向脱离 CodeMaster 的“生成的子项目数据库”。
- [x] 基于目标项目真实表路径执行 `DbMaintenance.CreateTable`，实现自动同步实体约束到物理表。

### 7.2 专属菜单下发
- [x] 重构 `SyncMenuAndPermissionsAsync`，利用目标的 `targetDb` 会话，将生成的业务目录（M）、功能菜单（C）以及按钮权限（F）精准降落至新生子项目的 `sys_menu` 表内。
- [x] 隔绝 CodeMaster 环境污染，真正实现作为代码孵化器的边界划分。
- [x] 开辟 `CodeMaster.OpenSpec.Tests` 体系完善连结串的单元 / 集成自测闭环。
