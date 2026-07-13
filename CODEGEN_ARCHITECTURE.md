# CodeMaster 代码生成器架构文档

## 一、整体架构

```
数据库模板 (sys_page_templates + sys_field_control_templates)
    ↓
TemplateCodeGenerator.GeneratePageAsync()
    ↓
┌──────────────────┼──────────────────┐
│   HTML 渲染        │   Script 渲染     │   组件树
│   (MarkerReplacer) │   (ScriptRenderer)│   (VueTemplateParser)
│        ↓           │        ↓          │       ↓
│   index.vue        │   index.auto.js   │   index.tree.json
└──────────────────┴──────────────────┴───────────┘
```

## 二、ScriptBuilder 体系

### 2.1 ScriptSection 类型（10 种）

| 类型 | 说明 | 示例输出 |
|------|------|---------|
| ImportInfo | 模块导入 (named/default/namespace/sideEffect) | `import { ref } from 'vue'` |
| ConstInfo | 常量 | `const STATUS_MAP = { 0: '正常' }` |
| LetInfo | 可变变量 | `let editingIndex = -1` |
| RefInfo | ref 引用 | `const loading = ref(false)` |
| ReactiveInfo | reactive 响应式对象 | `const form = reactive({ name: '' })` |
| FunctionInfo | 函数 | `const getList = async () => { ... }` |
| HookInfo | 生命周期钩子 | `onMounted(() => { ... })` |
| ComputedInfo | 计算属性 | `const doubled = computed(() => ...)` |
| WatchInfo | 监听器 | `watch(source, (v) => { ... })` |
| DictRefInfo | 字典选项引用 | `const statusOptions = ref([])` |

### 2.2 核心能力

- **Merge()** — 按 From+Mode (Import) 或 Name (其他) 去重合并
- **FromMarker()** — 从旧 DB JSON 格式转换
- **ReplaceMarkers()** — 对所有名称和 body 应用标记替换
- **GetExportNames()** — 收集 composable 导出名

### 2.3 ScriptRenderer

- **RenderComposable(section, entityName)** — 输出 `export function useOrder() { ... return { ... } }`
- **RenderImport(imp)** — 四种导入模式渲染
- **RenderVueShell(entityName, type, exports)** — 输出 .vue 的 `<script setup>` 壳

## 三、模板系统

### 3.1 页面模板 (sys_page_templates)
存储位置：CodeMaster_Test.db

| page_type | 用途 | 标记语法 |
|-----------|------|---------|
| index | 列表页 | `[gen.searchColumns]` `[gen.listColumns]` |
| add | 新增页 | `[gen.addColumns]` `[gen.relationCards]` |
| edit | 编辑页 | `[gen.editColumns]` `[gen.relationCards]` |
| detail | 详情页 | `[gen.detailColumns]` `[gen.relationCards]` |

### 3.2 控件模板 (sys_field_control_templates)

| control_type | page_section | 示例 |
|-------------|-------------|------|
| input | add/edit/search | `<el-input v-model="form.xxx" />` |
| number | add/edit | `<el-input-number v-model="form.xxx" />` |
| select | add/edit/search | `<el-select v-model="form.xxx">` |
| date/datetime | add/edit/search | `<el-date-picker ...>` |
| table-column | list | `<el-table-column prop="xxx" />` |
| select-table | add/edit | `<el-select>` + 关联表数据 |
| editor | add/edit | wangeditor 富文本 |
| file/image | add/edit | `el-upload` 组件 |
| cascader | add/edit | `el-cascader` 级联选择 |

### 3.3 子表模板 (sys_child_templates)

| child_type | page_type | 用途 |
|-----------|----------|------|
| card | add/edit | 子表卡片+表格+操作按钮 |
| dialog | add/edit | 子表弹窗表单 |
| card | detail | 只读子表展示 |

## 四、标记替换引擎

### 4.1 标记类型

| 前缀 | 用途 | 示例 |
|------|------|------|
| `[gen.xxx]` | 实体上下文 | `[gen.entityName]` `[gen.searchColumns]` |
| `[field.xxx]` | 字段上下文 | `[field.name]` `[field.nameLower]` |
| `[relation.xxx]` | 子表上下文 | `[relation.entityName]` `[relation.tableColumns]` |

### 4.2 FieldContext 完整属性

```
Name, NameLower, Description, DataType, IsNullable, IsPrimaryKey,
IsRequired, IsSortable, FormControlType, SelectDataSource, SelectOptions,
RelatedEntityName, RelatedEntityNameLower, RelatedEntityIdField,
RelatedDisplayFields, DictDataUrl, FormPrefix, EntityTable, EntityField
```

## 五、组件树 (tree.json)

### 5.1 节点结构
```json
{
  "tag": "el-form-item",
  "genId": "gen_field_Status",
  "entityTable": "",
  "entityField": "Status",
  "scriptSection": "{...}",
  "children": [...],
  "useSlots": [{ "name": "default", "parameter": "scope", "components": [...] }],
  "props": [{ "key": "label", "value": "...", "isBind": true }],
  "events": [{ "name": "click", "body": "handleClick" }],
  "instructions": [{ "name": "v-if", "value": "show" }]
}
```

### 5.2 关键特性

- 生成时通过 `VueTemplateParser` 解析 HTML → 得到树
- `EmbedScriptsIntoTree` 将字段 ScriptSection 嵌入对应节点
- `MergeTreeOrder` 保留用户拖拽位置（增量生成时不丢失）
- 设计器直接从 tree.json 加载，不再解析 .vue 文件

## 六、设计器功能

### 6.1 工具栏

| 按钮 | 功能 |
|------|------|
| 预览/编辑 | 切换预览/编辑模式 |
| ↩ 撤销 | 撤销操作 |
| ↪ 重做 | 重做操作 |
| 🧩 控件 | 展开/收起左侧控件库面板 |
| 📜 Script | 页面级 ScriptSection 对话框（主表/子表切换，可编辑增删） |
| 🎯 选中 Script | 当前选中控件的 ScriptSection（可编辑增删） |
| 保存 | 保存设计到 tree.json + .vue 文件 |

### 6.2 属性抽屉

- **智能面板**：有 genId 的字段组件，显示 API 提供的所有属性选项
- **降级面板**：普通组件，原始属性编辑
- **标签切换**：下拉框切换 tag 类型（el-input → el-input-number 等）
- **Script 查找**：选中控件自动匹配 scriptSection

### 6.3 组件库面板

30 个常用 Vue/HTML 标签：
```
div, span, template, el-row, el-col, el-card, el-form, el-form-item,
el-table, el-table-column, el-tag, el-dialog, el-descriptions,
el-descriptions-item, el-pagination, el-divider, el-button, el-icon,
el-alert, el-tooltip, el-input, el-input-number, el-select, el-option,
el-date-picker, el-switch, el-upload, el-image, el-cascader,
el-checkbox, el-radio
```

支持搜索过滤、点击添加、拖拽。

## 七、存储体系

### 7.1 每页面产出文件

```
{entity}.{pageType}.vue           — 模板 + script 壳
{entity}.{pageType}.auto.js       — composable 逻辑
{entity}.{pageType}.script.json   — 合并后 ScriptSection
{entity}.{pageType}.fields.json   — 字段级 ScriptSection (值格式: {script, tableId, fieldId})
{entity}.{pageType}.tree.json     — 组件树 + 嵌入 scriptSection
```

### 7.2 数据流

```
生成代码:
  DB 模板 → FieldContext → ReplaceFieldMarkers → RecordFieldScript → fields.json
                                                      ↓
  Page template → ReplaceGen → VueContent → Parse → Tree → EmbedScriptsIntoTree → tree.json
                                                      ↓
  mainSection.Merge(field scripts) → ReplaceMarkers → ScriptRenderer → auto.js + script.json

设计器加载:
  tree.json → GetPageContent → treeJson → DesignCanvas 展示
  fields.json → GetFieldScripts → fieldScripts → 🎯 选中 Script 对话框

用户修改:
  修改 Script → saveFieldScript → fields.json 更新
  修改 Tree → savePageContent → tree.json + .vue 更新
  删除控件 → CleanupRemovedFieldScripts → fields.json 自动清理
```

## 八、关键 API

| API | 方法 | 用途 |
|-----|------|------|
| GetPageContentAsync | GET | 加载设计器页面内容（优先 tree.json） |
| SavePageContentAsync | POST | 保存设计器修改（tree + vue） |
| GetPageScriptAsync | GET | 获取页面级 ScriptSection |
| SavePageScriptAsync | POST | 保存页面级 ScriptSection |
| GetFieldScriptsAsync | GET | 获取所有字段级 ScriptSection（含子表） |
| SaveFieldScriptsAsync | POST | 保存字段级 ScriptSection |
| GenerateCodeAsync | POST | 全量生成代码 |

## 九、新增功能规划

### 9.1 短期

- [ ] 控件右键菜单（复制/粘贴/删除/上移/下移）
- [ ] Dialog 特殊渲染（设计时强制可见 + slot 参数传递）
- [ ] 全局搜索替换（跨页面）
- [ ] Script 编辑的历史记录支持撤销

### 9.2 中期

- [ ] 小程序平台支持（uni-app 模板 + WeUI 控件库）
- [ ] 多语言翻译一键同步
- [ ] 页面间复制粘贴组件
- [ ] 批量生成（选中多个实体一次生成）
- [ ] 模板版本管理（模板修改历史）

### 9.3 长期

- [ ] AI 辅助设计（自然语言描述 → 生成页面）
- [ ] 页面模板市场（分享/导入社区模板）
- [ ] 多项目协同编辑
- [ ] 自动化测试生成（根据实体定义生成 E2E 测试）
- [ ] 微前端支持（子应用代码生成）

## 十、开发方案

### 10.1 环境要求

| 组件 | 版本 | 说明 |
|------|------|------|
| .NET SDK | 10.0 | 后端编译运行 |
| Node.js | 18+ | 前端编译运行 |
| SQLite | 内置 | 模板数据库和业务数据库 |
| Element Plus | 2.x | UI 组件库 |
| Vue 3 | 3.3+ | 前端框架 |

### 10.2 项目结构速查

```
CodeMaster/
├── CodeMaster.Application/         ← 业务逻辑层
│   ├── ScriptBuilder/              ← ScriptSection 结构化体系
│   │   ├── ScriptSection.cs        ← 10 种类型 + Merge + FromMarker + ReplaceMarkers
│   │   └── ScriptRenderer.cs       ← RenderComposable / RenderImport
│   ├── Services/CodeGen/
│   │   ├── TemplateCodeGenerator.cs ← DB 模板生成引擎
│   │   ├── Marker/                 ← 标记替换 (MarkerReplacer, MarkerContexts)
│   │   └── Project/                ← ModuleEntityService (生成入口 + API)
│   └── VueBuilder/                 ← C# Vue 构建器 (备选路径)
├── CodeMaster.Infrastructure/
│   └── VueParser/                  ← .vue 文件解析器
│       ├── VueTemplateParser.cs    ← HTML → 组件树
│       ├── VueTemplateSerializer.cs ← 组件树 → HTML
│       └── Model/
│           └── Component.cs         ← 组件节点模型
├── CodeMaster.Migrator/
│   └── SeedData/System/
│       └── TemplateModule.cs       ← 种子数据 (页面+控件模板)
├── CodeMaster.Vue/
│   ├── src/views/codegen/
│   │   ├── entityDesigner/         ← 可视化设计器
│   │   │   ├── index.vue           ← 主入口 (工具栏+树+抽屉+面板)
│   │   │   └── DesignCanvas.vue    ← 画布组件 (动态渲染)
│   │   └── templateConfig/        ← 模板配置 (树形结构编辑器)
│   └── src/api/codegen/
│       └── moduleEntity.js         ← 前端 API 调用
└── CODEGEN_ARCHITECTURE.md         ← 本文档
```

### 10.3 快速入门

#### 1) 启动开发环境

```bash
# 后端 (端口 5000)
cd CodeMaster.WebApi
dotnet run

# 前端 (端口 5173)
cd CodeMaster.Vue
npm run dev
```

#### 2) 创建第一个实体

1. 打开 CodeMaster → 项目管理 → 新建项目 → 输入项目路径
2. 创建模块 → 创建实体 → 添加字段
3. 对每个字段设置：控件类型 (input/number/select/editor...)、显示在搜索/列表/表单
4. 如果有子表关系，添加子表实体并配置外键
5. 点击「生成代码」

#### 3) 修改模板

**方式 A — 通过界面修改**：打开 `templateConfig` 页面，选择模板类型，编辑 HTML，保存。

**方式 B — 直接改数据库**：
```bash
python -c "
import sqlite3
db = sqlite3.connect('CodeMaster.Migrator/CodeMaster_Test.db')
# 查看所有模板
rows = db.execute('SELECT id, page_type, html_content FROM sys_page_templates').fetchall()
# 修改某个模板
db.execute('UPDATE sys_page_templates SET html_content=? WHERE page_type=?', (newHtml, 'index'))
db.commit()
"
```

**方式 C — 修改种子数据**（永久生效）：
编辑 `CodeMaster.Migrator/SeedData/System/TemplateModule.cs`，然后重新运行 Migrator。

#### 4) 自定义控件 ScriptSection

每种控件类型的 ScriptSection 定义在数据库 `sys_field_control_templates.script_sections` 字段中。可以在 📜 Script 对话框里编辑，保存后下次生成生效。

### 10.4 开发工作流

```
发现问题 → F12 看报错 → 定位文件 → 修改 → dotnet build → 重启后端 → 验证
```

**关键调试技巧**：

1. **前端 Console** — 选中节点后 `$vm0` 可用，打印 `$vm0.selectedNode?.scriptSection`
2. **查看 tree.json** — `订单管理/order/order.index.tree.json`，直接看生成的结构
3. **查看 fields.json** — 确认 ScriptSection 是否正确写入
4. **后端日志** — 生成时会输出 `已注册 XX 个业务服务` 等日志

### 10.5 注意事项

#### ⚠️ 模板修改后要重新生成
数据库模板改了不会自动生效——必须重新点「生成代码」才会产出新文件。

#### ⚠️ tree.json 是设计器的源
设计器不再解析 `.vue` 文件。如果 `.vue` 和 `.tree.json` 不一致，以 `.tree.json` 为准。

#### ⚠️ fields.json 保存用户修改
用户在 `🎯 选中 Script` 里改的 ScriptSection 保存在 `fields.json`。重新生成时优先读这里，不会丢失。

#### ⚠️ 端口占用
如果 `dotnet run` 报端口占用：
```bash
netstat -ano | findstr :5000
taskkill /PID <pid> /F
```

#### ⚠️ DLL 锁定
修改代码后 `dotnet build` 失败，提示文件被占用：
```bash
taskkill /F /IM CodeMaster.WebApi.exe
taskkill /F /IM dotnet.exe
# 然后重新 build
```

#### ⚠️ 不要手改生成的 .vue 文件
生成的代码会被「重新生成」覆盖。想保留修改要改模板或 ScriptSection。

#### ⚠️ node_modules1 目录
仓库里有个 `CodeMaster.Vue/node_modules1/` 目录（不是 `node_modules`），里面是 Element Plus 源码。修改它**不推荐**——改模板才是正道。

#### ⚠️ 双重 JSON 编码
`tree.json` 中 `scriptSection` 可能是双重编码的 JSON 字符串。前端 `parseMaybeDoubleEncoded` 函数处理了这种情况。

#### ⚠️ Slot 解析
解析器现在支持 `#default="scope"` 和 `v-slot:default="scope"` 两种写法。但 `<template v-if>` 不会错误地转成 slot。

## 十一、文档维护方式

### 11.1 更新频率

| 变更类型 | 更新时机 |
|----------|---------|
| 新增控件类型 | 同步更新「三、模板系统」的控件表格 |
| 新增 ScriptSection 类型 | 同步更新「二、ScriptBuilder」 |
| 新增 API | 同步更新「八、关键 API」 |
| 新增前端功能 | 同步更新「六、设计器功能」 |
| 模块重命名/重构 | 更新「十、项目结构速查」和所有涉及路径 |

### 11.2 维护规则

1. **文档和代码一起提交** — 功能改动时，同一个 commit 里包含文档更新
2. **日期标记** — 文档末尾加 `> 最后更新：YYYY-MM-DD，同步 commit：<hash>`
3. **不改原文** — 大型重构写新章节，标注旧章节「已废弃 > 见 XX 节」，不直接删除
4. **配图可选** — 架构图用 ASCII 或 Mermaid，不放截图（截图容易过期）
5. **回退友好** — Git 回退代码时，文档也一起回退，不会出现「文档描述新功能但代码没有」的情况

### 11.3 快速更新命令

```bash
# 修改文档后提交
vim CODEGEN_ARCHITECTURE.md
git add CODEGEN_ARCHITECTURE.md
git commit -m "docs: 更新 XXX 部分"

# 查看某次提交后的文档版本
git show <commit-hash>:CODEGEN_ARCHITECTURE.md
```

---

> 最后更新：2026-06-09，同步 commit：441f7d7

---

## 十三、项目当前状态

### 13.1 设计器如何加载页面结构？

**不再解析 `.vue` 文件。**

```
生成时：TemplateCodeGenerator → VueTemplateParser 解析生成的HTML → 组件树 JSON → .tree.json
设计器加载时：GetPageContentAsync → 直接读 .tree.json → 返回给前端
```

`VueTemplateParser` 使用正则匹配 HTML 标签/属性来构建组件树，但只在**生成时**调用。设计器加载时用的是**预解析好的 JSON**，不需要任何解析。

如果 `.tree.json` 不存在（老项目），才回退到解析 `.vue` 文件。

### 13.2 已完成功能（20个 commit）

| 功能 | Commit |
|------|--------|
| ScriptSection 结构化体系 | `45c1faa` |
| DB模板 + 迁移 + 种子数据 | `cb23901` |
| 详情页子表 + hook分离 + v-slot解析 | `eae9960` |
| 📜Script 编辑（页面级增删改） | `1793fda` |
| 🧩组件面板 + 🎯选中Script编辑 | `333c9bb` |
| 控件切换（tag下拉框） | `8b1a3a1` |
| Dialog 强制可见 | `cf41406` |
| 增量生成保留拖拽位置（MergeTreeOrder） | `4a8d6f1` |
| 完整架构文档 | `21fd2b5` |
| 右键菜单（复制/粘贴/移动/删除） | `0663e94` |
| Dialog 显示开关 | `db06e24` |

### 13.3 已知未完成

1. **DeserializeScript 加 `PropertyNameCaseInsensitive`** 已改但需重新生成代码验证
2. 小程序平台模板
3. 批量生成
4. 模板版本管理

### 13.4 常见踩坑

| 问题 | 解决 |
|------|------|
| DLL 被占用 | `taskkill /F /IM CodeMaster.WebApi.exe` |
| 端口占用 | `netstat -ano \| findstr :5000` 查 PID 杀进程 |
| 修改模板不生效 | 必须重新生成代码 |
| 设计器显示旧数据 | `.tree.json` 是源，`.vue` 是输出 |
| 前端报错 `xxx is not defined` | 检查 Vue `<script setup>` 的 import |
| git 锁文件 | 删 `.git/index.lock` |
| 数据库模板默认值 | 种子数据在 `TemplateModule.cs`，改它再跑 Migrator |

---

## 十二、Git 提交历史

```
4a8d6f1 - feat: incremental generation preserves drag position via MergeTreeOrder
cf41406 - feat: force el-dialog visible in designer preview
8b1a3a1 - feat: control switching - tag type dropdown in property drawer
333c9bb - feat: component palette panel + selected ScriptSection editing
1793fda - feat: editable ScriptSection in selected Script dialog
eae9960 - feat: detail child table, hook separation, field script editing
cb23901 - add ScriptBuilder, TemplateCodeGenerator, migration seed data
```

## 十一、回退操作

```bash
# 查看当前状态
git -C D:/MyHomeWorks/CodeMaster log --oneline -5

# 回退到某个提交（保留工作区改动）
git -C D:/MyHomeWorks/CodeMaster reset --soft <commit-hash>

# 完全回退（丢弃所有改动）
git -C D:/MyHomeWorks/CodeMaster reset --hard <commit-hash>
```
