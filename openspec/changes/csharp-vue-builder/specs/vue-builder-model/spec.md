## ADDED Requirements

### Requirement: VuePage 描述完整 .vue 文件
系统 SHALL 提供 `VuePage` 类，包含 `Template`（VueComponent 树）、`Script`（Imports/Uses/Refs/Reactive/Functions/Hooks）、`Style`（CSS 文本）三个部分。

#### Scenario: 构建一个最小 Index 页
- **WHEN** Builder 构建 Index 页的 VuePage
- **THEN** VuePage 包含 Template 根节点、Script 包含至少一个 import 和一个 onMounted hook

### Requirement: VueComponent 描述 UI 节点树
系统 SHALL 提供 `VueComponent` 类，包含 `Tag`、`Props`（VueProp 列表）、`Children`（子 VueComponent）、`Slots`（VueSlot 列表）、`SourceLocation?`。

#### Scenario: 构建 el-form 含 el-form-item
- **WHEN** Builder 创建 `<el-form><el-form-item label="名称"><el-input v-model="form.name"/></el-form-item></el-form>`
- **THEN** VueComponent 树包含 el-form → el-form-item → el-input 三层嵌套

### Requirement: VueProp 描述所有属性类型
系统 SHALL 提供 `VueProp` 类，Type 枚举包含：Static、Bind、BindShort、Event、EventShort、Directive、DirectiveArg、Boolean、SlotMark。Name 为属性名，Value 为属性值（Bind 类型不带引号，Static 带引号）。

#### Scenario: 渲染不同类型的属性
- **WHEN** Renderer 渲染含有 `:label="$t('x')"`（Bind）、`clearable`（Boolean）、`@change="fn()"`（Event）的节点
- **THEN** 输出正确的 Vue 语法：`:label="$t('x')" clearable @change="fn()"`

### Requirement: ScriptSection 描述 script setup 内容
系统 SHALL 提供 import/use/ref/reactive/function/hook 的模型类。`FunctionInfo.Body` 直接存 `List<string>` 代码行，Renderer 负责缩进。`ImportInfo` 支持 `Destructured` 标记（`import { a, b }` vs `import a`）。

#### Scenario: 收集并渲染 import 和 ref
- **WHEN** Builder 向 ScriptSection 添加 `import { ref, reactive } from 'vue'` 和 `const x = ref(0)`
- **THEN** Renderer 输出正确的 import 行和 ref 声明行
