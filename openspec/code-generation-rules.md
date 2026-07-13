# CodeMaster 代码生成规则文档

## 概述

CodeMaster 采用 Scriban 模板引擎进行代码生成，基于 `ModuleEntity` 和 `EntityField` 元数据自动生成前后端的增删改查（CRUD）代码。

## 一、实体和DTO接口继承规则

### 1.1 统一基础接口

所有生成的**实体**和**DTO**都必须继承 `IBaseEntity` 作为标记接口，然后根据 `ModuleEntity` 的配置项额外继承对应接口。

### 1.2 接口继承矩阵

| 配置项 | 继承接口 | 接口包含字段 | 说明 |
|--------|---------|-------------|------|
| `HasPrimaryKey=true` | `IEntity` | `long Id` | 主键字段，非ReadOnly时必须为true |
| `IsTree=true` | `ITree` | `long? ParentId` | 树形结构 |
| `HasTenant=true` | `ITenant` | `long TenantId` | 多租户过滤 |
| `HasDataPermission=true` | `IDept` | `long? DeptId`, `string? DeptAncestors`, `long? CreateUserId` | 数据权限过滤 |

**约束规则：**
- 非 `IsReadOnly` 的实体，`HasPrimaryKey` **必须**为 `true`
- 接口同时应用于实体（`.cs`）和 DTO（`Dto.cs`、`CreateDto.cs`、`UpdateDto.cs`）

### 1.3 Partial 类分离机制

实体必须采用 `partial` 关键字，分为两个文件：

- **`.cs` 文件**：用户自定义部分，只生成一次，不会被覆盖
- **`.auto.cs` 文件**：代码生成部分，每次生成时覆盖

**生成路径：**
```
{项目路径}/{项目名}.Domain/{模块名}/{实体名}.cs
{项目路径}/{项目名}.Domain/{模块名}/{实体名}.auto.cs
```

### 1.4 主键规则

- 所有实体主键统一采用**雪花 ID**（long 类型）
- 主键字段名统一为 `Id`
- 数据库列名为 `id`

### 1.5 表名生成规则

表名由 SqlSugar 框架自动生成：
- 实体名转换为复数 + snake_case
- 示例：`SysUser` → `sys_users`

### 1.6 ModuleEntity 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| Name | string | 实体名称（PascalCase），如 SysUser |
| Description | string | 实体描述（中文），如"系统用户" |
| **HasPrimaryKey** | **bool** | **是否有主键（新增），非ReadOnly时必须为true** |
| IsTree | bool | 是否树形结构 |
| IsReadOnly | bool | 是否只读（只生成查询接口） |
| HasTenant | bool | 是否启用多租户 |
| HasDataPermission | bool | 是否启用数据权限 |
| GenerateFrontend | bool | 是否生成前端页面 |
| FrontendRoute | string | 前端路由路径 |
| MenuIcon | string | 菜单图标 |

## 二、后端服务层生成规则

### 2.1 Service 基类选择

| 配置 | 继承基类 | 提供方法 |
|------|---------|---------|
| `IsReadOnly=true` | `ReadOnlyApplicationService<TEntity, TDto>` | `GetPagedList`, `GetById`, `GetList` |
| `IsReadOnly=false` | `CrudServiceBase<TEntity, TDto, TCreateDto, TUpdateDto>` | `GetPagedList`, `GetById`, `GetList`, `Create`, `Update`, `Delete` |

**约束：** `IsReadOnly=false` 时，`HasPrimaryKey` 必须为 `true`。

### 2.2 生成文件清单

| 文件类型 | 生成路径 | 说明 |
|---------|---------|------|
| 实体类 | `{项目名}.Domain/{模块名}/{实体名}.auto.cs` | 自动生成部分 |
| 实体类（用户） | `{项目名}.Domain/{模块名}/{实体名}.cs` | 仅首次生成 |
| DTO | `{项目名}.Application/Dtos/{模块名}/{实体名}Dto.cs` | 数据传输对象 |
| CreateDto | `{项目名}.Application/Dtos/{模块名}/Create{实体名}Dto.cs` | 创建 DTO |
| UpdateDto | `{项目名}.Application/Dtos/{模块名}/Update{实体名}Dto.cs` | 更新 DTO |
| Service 接口 | `{项目名}.Application/Services/{模块名}/I{实体名}Service.cs` | 服务接口 |
| Service 实现 | `{项目名}.Application/Services/{模块名}/{实体名}Service.cs` | 服务实现 |

### 2.3 动态 API 规则

无需编写 Controller，框架自动生成 API：

**路由规则：** `api/{命名空间小写}/{类名小写}/{方法名去掉Async后小写}`

**权限规则：** `{命名空间小写}:{类名小写}:{方法标志}`

**方法标志映射：** `list`=列表, `create`=新增, `update`=编辑, `detail`=详情, `delete`=删除

### 2.4 Update 方法参数规则

```csharp
Task<TDto> UpdateAsync(long id, TUpdateDto input);
```

## 三、勾选/取消勾选自动生成字段规则

### 3.1 勾选时自动新增字段

当在 ModuleEntity 编辑页勾选某个配置项时，如果对应的字段尚未存在于 EntityField 列表中，则**自动新增** EntityField 记录，并设置相应属性。

| 勾选项 | 自动新增的字段 | 字段属性 |
|--------|--------------|---------|
| `HasPrimaryKey` | `Id` | `IsPrimaryKey=true`, `DataType=long`, `IsRequired=true`, `IsSystemField=true` |
| `IsTree` | `ParentId` | `DataType=long?`, `IsNullable=true`, `IsSystemField=true` |
| `HasTenant` | `TenantId` | `DataType=long`, `IsRequired=true`, `IsSystemField=true` |
| `HasDataPermission` | `DeptId` | `DataType=long?`, `IsNullable=true`, `IsSystemField=true` |
| `HasDataPermission` | `DeptAncestors` | `DataType=string?`, `IsNullable=true`, `IsSystemField=true` |
| `HasDataPermission` | `CreateUserId` | `DataType=long?`, `IsNullable=true`, `IsSystemField=true` |

### 3.2 取消勾选时删除字段

| 场景 | 处理方式 |
|------|---------|
| 字段**未保存**过数据库（前端新增状态） | 直接从列表中移除 |
| 字段**已保存**过数据库 | 标记为已删除状态（软删除） |
| 已删除状态的字段**重新勾选** | 取消删除状态，恢复字段 |

## 四、字段编辑弹框保存/取消规则

此规则对应 `moduleEntity/edit.vue` 中字段列表的编辑弹框。

### 4.1 状态管理规则

| 操作场景 | 结果状态 |
|---------|---------|
| 新增状态 → 编辑后 → 点击**保存** | 保持**新增**状态 |
| 已保存数据库 → 编辑后 → 点击**保存** | 变成**更新**状态 |
| 任何状态 → 编辑后 → 点击**取消** | **不改变**原来状态 |

### 4.2 提交规则

前端提交字段数据时，**只提交有状态变更的数据**：

```javascript
const submitData = {
  ...form,
  newFields: newFields.value,         // 状态=新增
  updatedFields: updatedFields.value, // 状态=更新
  deletedFieldIds: deletedFieldIds.value // 状态=删除
}
```

后台根据这三种状态分别执行 Insert、Update、Delete 操作。**未变更的字段不提交，减少网络开销。**

## 五、树形结构特殊规则

### 5.1 后端规则

- 树形结构（`IsTree=true`）使用 `GetList` 返回**扁平数据**（不分页）
- 不使用 `GetPagedList`

### 5.2 前端规则

- 前端接收扁平数据后，使用工具函数转换为树形结构显示
- 列表页**不显示分页组件**
- 树形结构的表格使用 `el-table` 的 `row-key` 和 `tree-props` 属性

---

## 六、一对多关系

### 6.1 ModuleEntity 一对多配置

`ModuleEntity` 支持配置**多个**一对多关系。一个实体（主表）可以关联多个子表。

**配置项说明：**

| 配置项 | 说明 |
|--------|------|
| 主表字段 | 主表中用于关联的字段（不限于主键，任意字段均可） |
| 子表名 | 关联的 ModuleEntity 表名 |
| 子表外键字段 | 子表中对应主表字段的外键字段 |

### 6.2 前端界面

ModuleEntity 编辑页需要提供一对多关系的**添加/编辑/删除**功能，界面风格参考 `moduleEntity/edit.vue` 中字段列表的交互模式。

### 6.3 SqlSugar 导航属性

生成的实体代码必须声明 SqlSugar 导航属性：

```csharp
// 主表实体（如 Order）
[Navigate(NavigateType.OneToMany, nameof(Id), nameof(OrderDetail.OrderId))]
public List<OrderDetail>? OrderDetails { get; set; }

// 子表实体（如 OrderDetail）
[SugarColumn(ColumnName = "order_id")]
public long OrderId { get; set; }
```

**导航属性字段映射规则：**
- 不限于主键字段，主表的**任意字段**都可以作为关联字段
- 需要在配置中明确指定主表哪个字段对应子表哪个外键字段

### 6.4 API Include 策略

| API 类型 | Include 子表 | 说明 |
|---------|-------------|------|
| `GetPagedList`（分页列表） | ❌ 不 Include | 列表性能优先 |
| `GetList`（不分页列表） | ❌ 不 Include | 列表性能优先 |
| `GetById`（详情） | ✅ Include | 详情页需要展示子表数据 |

### 6.5 外键引用标记

如果某个 ModuleEntity 被**任何其他实体**当作子表（外键引用），则该实体需要**额外标记一个状态**：

- 标记为"被引用"状态
- 被引用的实体**默认不能独立生成代码**（因为它作为子表，应随主表一起生成）
- 可手动解除此限制

### 6.6 生成代码的子表处理

- 主表的新增页面需要包含子表的行内编辑（添加/编辑/删除子记录）
- 主表的编辑页面同理
- 主表的详情页面需要展示子表数据列表
- 后端 Create/Update 方法需要同时处理子表数据的增删改

## 七、表单控件类型

### 7.1 完整控件类型列表

| FormControlType | Element Plus 组件 | 适用数据类型 | 说明 |
|----------------|------------------|-------------|------|
| input | el-input | string | 单行文本 |
| textarea | el-input type="textarea" | string | 多行文本 |
| number | el-input-number | int, long, decimal | 数字 |
| select | el-select | 枚举、字典 | 下拉选择（支持多选） |
| switch | el-switch | bool | 开关 |
| **checkbox** | **el-checkbox** | **bool** | **单个复选框（新增）** |
| **checkbox-group** | **el-checkbox-group** | **string** | **复选框组，多选（新增）** |
| **radio-group** | **el-radio-group** | **string/int** | **单选按钮组（新增）** |
| date | el-date-picker type="date" | DateTime | 日期 |
| datetime | el-date-picker type="datetime" | DateTime | 日期时间 |
| upload | el-upload | 文件 | 文件上传 |
| image | el-upload + 图片预览 | 图片 | 图片上传 |
| editor | 富文本编辑器 | string（HTML） | 富文本 |
| **select-table** | **el-select** | **long/string** | **关联表选择器（新增，见第八章）** |
| **cascader** | **el-cascader** | **long/string** | **级联选择器（新增，支持单选/多选，见第八章附）** |

### 7.2 checkbox-group / radio-group / select 数据源配置

这三种控件需要配置数据源（`SelectDataSource`），支持两种方式：

**方式一：从数据字典获取（`dict`）**
- `SelectDataSource = "dict"`
- `SelectOptions` 存储字典类型编码（如 `"user_status"`）
- 前端生成**全局代码**，调用字典接口获取选项列表
- **批量加载优化**：尽量一次性请求多种字典类型，减少网络请求次数

**方式二：用户手动输入（`enum`）**
- `SelectDataSource = "enum"`
- `SelectOptions` 存储 JSON 格式的 key-value 数组
- 示例：`[{"value":1,"label":"男"},{"value":2,"label":"女"}]`

### 7.3 单选/多选控制

各控件的单选/多选支持情况：

| 控件类型 | 单选/多选 | 说明 |
|---------|----------|------|
| checkbox-group | **仅多选** | 复选框组天然为多选 |
| radio-group | **仅单选** | 单选按钮组天然为单选 |
| select | **用户可选** | 用户配置单选或多选 |
| select-table | **用户可选** | 用户配置单选或多选 |
| cascader | **用户可选** | 用户配置单选或多选 |

### 7.4 多选处理规则

`checkbox-group`、`select`（多选）、`select-table`（多选）、`cascader`（多选）适用以下规则：

| 规则 | 说明 |
|------|------|
| 绑定字段名 | 变为 `{字段名}List`（全局变量），如 `statusList` |
| 存储格式 | 以**逗号分隔的字符串**保存到数据库，如 `"1,2,3"` |
| 字段类型限制 | 多选时字段类型**不能为数值类型**，必须是 string |
| 列表/详情展示 | 以逗号分割取值后，以 **el-tag** 组件逐个展示 |
| 数据转换 | 保存时 `Array → 逗号分隔字符串`；读取时 `逗号分隔字符串 → Array` |

## 八、select-table（关联表选择器）

### 8.1 概述

`select-table` 是一种特殊的表单控件，实际生成 `el-select` 组件，用于从关联表中选择数据。

**典型场景：** 班级表中有 `TeacherId` 字段，需要从教师表（Teacher）中选择教师，并显示教师名、年龄、性别等信息。

### 8.2 配置项

| 配置项 | 说明 | 示例 |
|--------|------|------|
| 关联表名 | 从 ModuleEntity 中选择一个表 | `Teacher` |
| 关联表Id字段 | 目标表用于绑定值的字段 | `Id` |
| 显示字段列表 | 需要展示的字段Id（支持多选） | `Name`, `Age`, `Gender` |
| 是否多选 | select 是否支持多选 | `true` / `false` |

### 8.3 前端生成规则

#### 8.3.1 API 引入

- 引入关联表的**不分页** `getList` API
- import 重命名为 `get{关联表名}List` 避免命名冲突

```javascript
// 示例：关联 Teacher 表
import { getList as getTeacherList } from '@/api/system/teacher'

// 示例：关联 Department 表
import { getList as getDepartmentList } from '@/api/system/department'
```

#### 8.3.2 单选模式

- 列表页中每条记录增加 `{关联表名小写}` JSON 对象（如 `teacher`）
- 后端通过关联Id获取到关联数据后，附加到记录中
- 在关联Id字段后面增加显示列（如教师名列、年龄列、性别列）
- **如果关联Id在列表中不显示**，则只显示关联的名称等列（不显示Id列）

```javascript
// 单选时 - 列表数据结构示例
{
  id: 1,
  className: "一年级一班",
  teacherId: 100,
  teacher: { id: 100, name: "张三", age: 35, gender: "男" }
}
```

#### 8.3.3 多选模式

- 列表页中每条记录增加 `{关联表名小写}List` JSON 数组（如 `teacherList`）
- 以 **el-tag** 组件展示多个关联数据的名称、年龄、性别等
- 多选值以逗号分隔字符串存储

```javascript
// 多选时 - 列表数据结构示例
{
  id: 1,
  className: "一年级一班",
  teacherIds: "100,101,102",
  teacherList: [
    { id: 100, name: "张三", age: 35, gender: "男" },
    { id: 101, name: "李四", age: 28, gender: "女" },
    { id: 102, name: "王五", age: 40, gender: "男" }
  ]
}
```

### 8.4 命名规则总结

| 场景 | 字段/变量名 | 示例（关联表=Teacher） |
|------|-----------|----------------------|
| 单选绑定字段 | `{关联表名}Id` | `teacherId` |
| 单选关联对象 | `{关联表名小写}` | `teacher` |
| 多选绑定字段 | `{关联表名}Ids` | `teacherIds` |
| 多选关联数组 | `{关联表名小写}List` | `teacherList` |
| API import 别名 | `get{关联表名}List` | `getTeacherList` |

## 八附、cascader（级联选择器）

### 概述

`cascader` 用于从一个**树形结构的关联表**中选择数据。选择的关联表必须是 `IsTree=true` 的 ModuleEntity。前端使用 `el-cascader` 组件，支持**单选**或**多选**（由用户配置）。

### 配置项

| 配置项 | 说明 | 示例 |
|--------|------|------|
| 关联表名 | 从 ModuleEntity 中选择一个 `IsTree=true` 的表 | `Department` |
| 关联表Id字段 | 目标表用于绑定值的字段 | `Id` |
| 显示字段 | 级联节点显示的文本字段 | `Name` |
| 是否多选 | 是否允许选择多个节点 | `true` / `false` |

### 前端生成规则

**数据加载：**
- 引入关联表的不分页 `getList` API，import 重命名为 `get{关联表名}List`
- 获取所有扁平数据后，前端构建树形结构供 `el-cascader` 使用

```javascript
import { getList as getDepartmentList } from '@/api/system/department'
const departmentTree = buildTree(flatData) // 扁平数据转换为树形
```

**表单页（新增/编辑）：**
- 使用 `el-cascader` 组件，数据源为构建好的树形结构
- 单选时绑定字段为 `{关联表名}Id`（如 `departmentId`）
- 多选时绑定字段为 `{关联表名}Ids`，逗号分隔存储

**列表页/详情页：**
- 根据树形数据和关联 Id，计算并显示**完整路径**
- 完整路径格式：`总公司 / 技术部 / 前端组`（以 ` / ` 分隔，从根到当前节点）
- 多选时以 **el-tag** 显示多个完整路径

```javascript
// 单选 - 列表数据结构
{ id: 1, name: "张三", departmentId: 305, departmentFullPath: "总公司 / 技术部 / 前端组" }

// 多选 - 列表数据结构
{ id: 1, name: "张三", departmentIds: "305,201",
  departmentFullPathList: ["总公司 / 技术部 / 前端组", "总公司 / 市场部"] }
```

### 命名规则

| 场景 | 字段/变量名 | 示例（关联表=Department） |
|------|-----------|------------------------|
| 单选绑定字段 | `{关联表名}Id` | `departmentId` |
| 单选完整路径 | `{关联表名小写}FullPath` | `departmentFullPath` |
| 多选绑定字段 | `{关联表名}Ids` | `departmentIds` |
| 多选路径数组 | `{关联表名小写}FullPathList` | `departmentFullPathList` |
| 树形数据变量 | `{关联表名小写}Tree` | `departmentTree` |
| API import 别名 | `get{关联表名}List` | `getDepartmentList` |

## 九、前端代码生成

### 9.1 生成文件清单

| 文件类型 | 生成路径 | 说明 |
|---------|---------|------|
| API 文件 | `src/api/{模块名}/{实体名小写}.js` | API 请求封装 |
| 列表页 | `src/views/{模块名}/{实体名小写}/index.vue` | 列表页面 |
| 新增页 | `src/views/{模块名}/{实体名小写}/add.vue` | 新增页面 |
| 编辑页 | `src/views/{模块名}/{实体名小写}/edit.vue` | 编辑页面 |
| 详情页 | `src/views/{模块名}/{实体名小写}/detail.vue` | 详情页面（必需） |

### 9.2 页面规则

- 列表页的新增、修改、详情默认**跳转到独立页面**，不使用弹框
- **详情页为必需页面**，即使是只读实体也需生成
- 树形结构列表**无分页**，后端返回扁平化数据，前端转换为树形结构

## 十、Scriban 模板变量与代码生成流程

### 10.1 实体模板变量

```scriban
{{ entity.name }}                  # 实体名称（PascalCase）
{{ entity.description }}           # 实体描述
{{ entity.module_name }}           # 模块名称
{{ entity.has_primary_key }}       # 是否有主键
{{ entity.is_tree }}               # 是否树形结构
{{ entity.is_read_only }}          # 是否只读
{{ entity.has_tenant }}            # 是否多租户
{{ entity.has_data_permission }}   # 是否数据权限
{{ entity.one_to_many_relations }} # 一对多关系列表
```

### 10.2 字段模板变量

```scriban
{{ for field in entity.fields }}
  {{ field.name }}               # 字段名称（PascalCase）
  {{ field.description }}        # 字段描述
  {{ field.data_type }}          # C# 数据类型
  {{ field.is_nullable }}        # 是否可空
  {{ field.is_primary_key }}     # 是否主键
  {{ field.form_control_type }}  # 表单控件类型
  {{ field.select_data_source }} # 数据源类型
  {{ field.select_options }}     # 选项数据
  {{ field.show_in_list }}       # 是否列表显示
  {{ field.show_in_add_form }}   # 是否新增表单显示
  {{ field.show_in_edit_form }}  # 是否编辑表单显示
  {{ field.show_in_detail }}     # 是否详情显示
  {{ field.show_in_search }}     # 是否搜索条件
{{ end }}
```

### 10.3 代码生成流程

1. **读取元数据**：从数据库读取 `ModuleEntity` 和 `EntityField`
2. **检查约束**：验证 `HasPrimaryKey`（非ReadOnly时必须为true）等约束
3. **计算接口继承**：根据配置计算需要继承的接口列表
4. **过滤字段**：排除接口已有的字段
5. **加载模板**：读取 Scriban 模板文件
6. **渲染模板**：将元数据传入模板引擎渲染
7. **写入文件**：`.auto.cs` 直接覆盖，`.cs` 仅首次创建
8. **执行迁移**：调用 EF Core 生成数据库迁移
9. **更新菜单**：同步到菜单表

### 10.4 注意事项

1. **不要覆盖用户代码**：`.cs` 文件只生成一次
2. **保持命名一致**：严格遵循 PascalCase 和 snake_case 规则
3. **验证元数据**：生成前校验完整性
4. **事务处理**：代码生成失败时回滚文件变更
5. **外键引用检查**：被引用的实体默认不能独立生成
6. **权限同步**：生成后自动同步权限到菜单表

---

**文档版本：** 2.0
**最后更新：** 2026-03-23

---

## 十一、跨项目逻辑与代码生成执行规则 (Cross-Project Execution Rules)

在 CodeMaster 架构中，本系统不单单为其自身表生成逻辑，其核心功能是**作为孵化器为“目标项目”生成代码、创建菜单和更新数据库**。为了保证连接的稳定性和部署不出错，需要遵循以下跨项目边界规则：

### 11.1 项目初始化与连接字符串转换

1. **构建目标**：CodeMaster 在创建项目时，通过拷贝/解压模板，将项目部署到指定的物理目录中。
2. **连接注入**：CodeMaster 根据配置页面的项目连接字符串，向新项目写入连接信息（更新目标项目的 `appsettings.json`）。系统**必须保证**目标项目的 `{ProjectName}.Migrator` 与 `{ProjectName}.WebApi` （以及一切执行子模块）中的 DB 连接字符串完全一致。这样后续 CodeMaster 需要跨目录连接生成的项目时，读取和写入口径才能吻合。
3. **SQLite 绝对路径转换规则**：如果是 SQLite 数据库，连接字符串中的 `Data Source=` 可能为相对路径（如 `../db.sqlite`）。因为相对执行路径不同（Migrator vs WebApi），因此必须将其判断并转换为**基于目标项目根目录的绝对路径**。

### 11.2 创建模块菜单与数据库连接

1. **连接目标数据库**：当基于模块更新菜单表据时，必须根据模块关联的 `Project` 获取对应的项目连接字符串。
2. **构建专属会话**：利用 SqlSugar 的多租户或新实例能力，建立**连结向目标项目的数据库**实例。
3. **写入/更新表结构**：生成的模块与菜单结构，不可插入 CodeMaster 自己的 `sys_menu` 表，而必须插入/更新**最终生成的衍生项目**中，维护清晰的应用隔离域。

### 11.3 实体代码生成流及关联落库执行

为 `ModuleEntity` 生成真实代码和表时也是同样的链路：
1. 从 `ModuleEntity` 源头上行查找到归属的 `Project` 以及其连接字符串和物理安装路径。
2. **生成目标数据库表**：用 SqlSugar 连接此项目的 DB，执行目标表建立 / 更新操作（如通过 SqlSugar 配置同步表或运行原声 SQL），保证目标工程的表结构（如 `business_...`）已被建立。
3. **写入目标物理路径**：在运行模板渲染引擎产生最新的 `.cs` 和 `.vue` 产物后，根据 `Project` 的物理放置路径，准确定位如 `{ProjectPath}/{ProjectName}.Domain/{ModuleName}/` 的位置完成代码物理落盘覆写，真正注入目标项目。
