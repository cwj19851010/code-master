## ADDED Requirements

### Requirement: VueRenderer 将 VuePage 渲染为 .vue 文本
系统 SHALL 提供 `VueRenderer` 类，接受 `VuePage` 模型，输出完整的 `.vue` 文件文本。渲染顺序：`<template>` 区 → `<script setup>` 区 → `<style scoped>` 区。每个区由对应的 render 方法处理。

#### Scenario: 渲染含缩进的组件树
- **WHEN** Renderer 渲染一个 3 层嵌套的 Component 树
- **THEN** 每层子节点比父节点多 2 空格缩进，闭合标签与开标签对齐

### Requirement: Template 渲染正确拼接属性
系统 SHALL 在渲染 VueComponent 时，按 Props 的 Type 拼接属性字符串。Bind 类型加 `:` 前缀，Event 类型加 `@` 前缀，Directive 类型输出 `v-xxx`，Boolean 类型只输出属性名。

#### Scenario: 渲染带混合属性的节点
- **WHEN** 节点有 Props：`:label="$t('name')"`（Bind）、`clearable`（Boolean）、`v-if="show"`（Directive）
- **THEN** 输出 `<el-select :label="$t('name')" clearable v-if="show">`

### Requirement: Script 渲染按分区顺序输出
系统 SHALL 按固定的分区顺序渲染 Script：imports → uses → refs → reactive → functions → hooks。每个分区内按添加顺序输出。

#### Scenario: 渲染含 import/ref/function 的 Script
- **WHEN** ScriptSection 有 3 个 import、2 个 ref、1 个 function
- **THEN** 输出按 imports 块 → ref 声明块 → function 定义块顺序，区块之间空一行
