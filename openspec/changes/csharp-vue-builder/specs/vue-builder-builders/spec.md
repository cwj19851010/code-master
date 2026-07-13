## ADDED Requirements

### Requirement: IndexPageBuilder 构建列表页
系统 SHALL 提供 `IndexPageBuilder`，从 EntityField 数据构建列表页 VuePage。包含搜索区、操作按钮区、数据表格（含 select-table/dict/date 等列类型）、分页组件。搜索区 select 控件按 `select_data_source` 区分 dict 和普通选项。

#### Scenario: 构建带 select-table 关联列的列表页
- **WHEN** 实体有一个 select-table 字段 `productId` 关联 `Product` 表
- **THEN** 表格列的 Options 使用 `getSelectLabel(s.row.productId, productOptions, 'Name')`，对应的 import 和 onMounted 数据获取自动生成

### Requirement: AddPageBuilder 构建新增页
系统 SHALL 提供 `AddPageBuilder`，从 EntityField 数据构建新增页 VuePage。包含表单控件（按 form_control_type 生成对应 el-* 组件）、计算字段 change 事件注入、子表区域（含弹框和 rowStatus 管理）、统计字段计算函数。

#### Scenario: 构建含计算字段和子表的新增页
- **WHEN** 实体有计算字段 `TotalPrice`（公式 `[Price]*[Quantity]`）和子表 `OrderItems`
- **THEN** Price 和 Quantity 的 el-input 自动追加 `@change="calcTotalPrice()"`，子表新增时 `rowStatus=1`，表单提交时 `filter(i => i.rowStatus)` 过滤未修改行

### Requirement: EditPageBuilder 构建编辑页
系统 SHALL 提供 `EditPageBuilder`，与 AddPageBuilder 共享表单构建逻辑，额外处理：加载数据后初始化子表 rowStatus、编辑已有行为 Modified、提交成功后清除 rowStatus 并移除已删除行。

#### Scenario: 构建编辑页并编辑现有子表行
- **WHEN** 编辑页加载数据后，用户点击编辑子表行并保存
- **THEN** handleEditOrderItem 调用 resetForm + Object.assign + 设置 rowStatus=2，提交后后端收到 status=Modified

### Requirement: DetailPageBuilder 构建详情页
系统 SHALL 提供 `DetailPageBuilder`，用 `el-descriptions` 展示只读字段。select-table 字段使用 `getSelectLabel` 转换，dict 字段使用 `dictOptions` 查找标签，子表用 el-table 展示。

#### Scenario: 构建含 select-table 和 dict 的详情页
- **WHEN** 实体有 `status`（dict）和 `productId`（select-table）字段
- **THEN** 详情页正确渲染 `getDictLabel(detail.status, dictOptions['xxx'])` 和 `getSelectLabel(detail.productId, productOptions, 'Name')`
