## Why

Scriban 模板生成 Vue 前端代码在灵活性和可维护性上已达到瓶颈——追加一个 `@change` 事件、控制换行缩进、条件生成子表逻辑，每一步都在与模板语法较劲。v1 开发中这些问题反复出现，严重拖慢迭代速度。用 C# 类建模 Vue 页面结构（Model → Renderer），可以消除文本模板的局限性，同时为后续解析已有 .vue 文件、更精细的字段布局控制铺路。

## What Changes

- 新增 `VueBuilder` 模块，包含 Model、Builders、Renderer 三层
- **Model**：用 C# 类描述 Vue 页面的完整结构（template 树 + script 声明 + style）
- **Builders**：将 EntityField 元数据转换为 Model（Index/Add/Edit/Detail 四个 Builder）
- **Renderer**：遍历 Model 输出 .vue 文本，处理缩进、属性拼接、事件合并
- 现有 Scriban 模板暂时保留，通过开关切换新旧生成器
- **BREAKING**：无。新旧并行，未启用时行为不变

## Capabilities

### New Capabilities
- `vue-builder-model`: 用 C# 类（VuePage、VueComponent、VueProp、VueSlot、ImportInfo 等）完整描述 Vue SFC 的 template、script setup、style 三部分
- `vue-builder-builders`: IndexPageBuilder、AddPageBuilder、EditPageBuilder、DetailPageBuilder 四个 Builder，从 EntityField 数据构建 VuePage 模型，处理计算字段依赖、统计字段触发、change 事件拼接、子表 rowStatus 等业务逻辑
- `vue-builder-renderer`: VueRenderer 将 VuePage 模型渲染为 .vue 文本，保证缩进正确、属性格式一致

### Modified Capabilities
- 无

## Impact

- 新增 `CodeMaster.CodeGenerator/VueBuilder/` 目录
- `CodeGeneratorService` 新增开关 `UseNewVueBuilder`，默认 false
- 现有 Scriban 模板和 RenderTemplateAsync 逻辑不受影响
