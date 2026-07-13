## 1. Model 层

- [x] 1.1 创建 VueProp 类（Name/Value/Type 枚举/SourceLocation?）
- [x] 1.2 创建 VueComponent 类（Tag/Props/Children/Slots/SourceLocation?）
- [x] 1.3 创建 VueSlot 类（Name/Parameter/Children）
- [x] 1.4 创建 VuePage 类（Template/Script/Style，含完整 ScriptSection）
- [x] 1.5 创建 ImportInfo/UseInfo/RefInfo/ReactiveInfo/FunctionInfo/HookInfo 类
- [x] 1.6 创建 PageBuildContext/FieldMeta/RelationMeta/ImportMeta

## 2. Renderer

- [x] 2.1 实现 VueRenderer.Render——按 Template/Script/Style 顺序输出
- [x] 2.2 实现 Template 渲染：递归遍历 Component 树，处理缩进、属性拼接、自闭合、文本节点
- [x] 2.3 实现 Script 渲染：imports → uses → refs → reactive → functions → hooks
- [x] 2.4 实现 AutoJsRenderer——将 ScriptSection 导出为 useXxx() composable
- [x] 2.5 实现 RenderWrapper——split 模式生成 wrapper .vue（template + import + 用户区）
- [x] 2.6 实现 IncrementalRenderer——基于 @gen:id/@endgen 标记的增量合并

## 3. Builder 基类

- [x] 3.1 实现 PageBuilderBase——构建表单控件（input/number/select/date/select-table 等）
- [x] 3.2 实现 change 事件拼接逻辑（字段依赖收集 → change 列表合并）
- [x] 3.3 实现计算字段 calc 函数生成（[Price]*[Quantity] → form.price * form.quantity）
- [x] 3.4 实现统计字段 calc 函数生成（Sum/Avg/Concat + 子表操作后触发）
- [x] 3.5 实现子表区域构建（表格 + 弹框 + rowStatus 处理 + 增删改逻辑）
- [x] 3.6 实现字段校验规则生成（IsRequired/Min/Max/Email/Phone/Regex）
- [x] 3.7 实现默认值支持（DefaultValue → form 初始值）
- [x] 3.8 实现 ShowCondition → v-if 渲染
- [x] 3.9 实现 IsSortable → 表格列 sortable 属性

## 4. 页面 Builders

- [x] 4.1 实现 IndexPageBuilder——搜索区 + 操作按钮 + 表格 + 分页 + 批量删除
- [x] 4.2 实现 AddPageBuilder——表单 + 子表 + 计算/统计函数 + handleSubmit
- [x] 4.3 实现 EditPageBuilder——同 Add + 数据加载 + 提交后 rowStatus 清理
- [x] 4.4 实现 DetailPageBuilder——el-descriptions + 只读子表展示

## 5. 增量更新

- [x] 5.1 全局 gen_* id 命名体系（gen_search/col/field/child_）
- [x] 5.2 IncrementalRenderer 基于 @gen:id/@endgen 标记替换
- [x] 5.3 字段加/删/改名时旧标记自动移除
- [x] 5.4 用户手写区（无标记）原封不动保留

## 6. Script 拆分

- [x] 6.1 AutoJsRenderer 导出 useXxx() composable
- [x] 6.2 VueRenderer.RenderWrapper 生成 wrapper .vue
- [x] 6.3 UseSplitScript 开关 + 文件写入逻辑
- [x] 6.4 未启用 split 时行为不变（单文件含完整 script）

## 7. 集成

- [x] 7.1 在 CodeGeneratorService 添加 UseNewVueBuilder/UseSplitScript 开关（默认 false）
- [x] 7.2 Index/Add/Edit/Detail 四个生成方法加分支逻辑
- [x] 7.3 ContextAdapter——ScriptObject → PageBuildContext
- [x] 7.4 ModuleEntityService 文件写入加增量合并逻辑
- [ ] 7.5 端到端测试：生成完整项目 → 编译前端 → Vite 无错误 → 页面功能正常
