<template>
  <template v-for="(node, index) in list" :key="node.id || index">
      <template v-if="!isEditorChild(node)">
      <!-- 寄生子组件（不挂拖拽条） -->
      <el-table-column v-if="node.tag==='el-table-column'" v-bind="buildProps(node)" :ref="(el) => trackRef(node.id, el)">
        <template v-if="hasSlot(node,'default')" #default="scope">
          <DesignCanvas :list="getSlotComps(node,'default')" :slotScope="scope" @select="(id)=>emit('select',id)" />
        </template>
      </el-table-column>
      <el-option v-else-if="node.tag==='el-option'" v-bind="buildProps(node)" />
      <el-tab-pane v-else-if="node.tag==='el-tab-pane'" v-bind="buildProps(node)" :ref="(el) => trackRef(node.id, el)" />
      <el-descriptions-item v-else-if="node.tag==='el-descriptions-item'" v-bind="buildProps(node)" :ref="(el) => trackRef(node.id, el)" />
      <!-- 容器组件（直接渲染） -->
      <el-table v-else-if="node.tag==='el-table'" v-bind="buildProps(node)" :ref="(el) => trackRef(node.id, el)" @click.stop="emit('select', node.id)" @header-click="(col) => onHeaderClick(node, col)">
        <template v-for="(cn,i) in (node.children||[])" :key="cn.id||i">
          <el-table-column v-if="cn.tag==='el-table-column'" v-bind="buildProps(cn)" :ref="(el) => trackRef(cn.id, el)">
            <template v-if="hasSlot(cn,'default')" #default="scope">
              <DesignCanvas :list="getSlotComps(cn,'default')" :slotScope="scope" @select="(id)=>emit('select',id)" />
            </template>
          </el-table-column>
        </template>
      </el-table>
      <el-select v-else-if="node.tag==='el-select'" v-bind="buildProps(node)" :ref="(el) => trackRef(node.id, el)" @click.stop="emit('select', node.id)">
        <template v-for="(cn,i) in (node.children||[])" :key="cn.id||i">
          <el-option v-if="cn.tag==='el-option'" v-bind="buildProps(cn)" />
        </template>
      </el-select>
      <el-tabs v-else-if="node.tag==='el-tabs'" v-bind="buildProps(node)" :ref="(el) => trackRef(node.id, el)" @click.stop="emit('select', node.id)">
        <template v-for="(cn,i) in (node.children||[])" :key="cn.id||i">
          <el-tab-pane v-if="cn.tag==='el-tab-pane'" v-bind="buildProps(cn)" :ref="(el) => trackRef(cn.id, el)" />
        </template>
      </el-tabs>
      <el-descriptions v-else-if="node.tag==='el-descriptions'" v-bind="buildProps(node)" :ref="(el) => trackRef(node.id, el)" @click.stop="emit('select', node.id)">
        <template v-for="(cn,i) in (node.children||[])" :key="cn.id||i">
          <el-descriptions-item v-if="cn.tag==='el-descriptions-item'" v-bind="buildProps(cn)" :ref="(el) => trackRef(cn.id, el)" />
          <component v-else :is="resolveTag(cn.tag)" v-bind="buildProps(cn)" />
        </template>
      </el-descriptions>
      <!-- 通用组件（挂 ref 追踪） -->
      <component v-else :is="resolveTag(node.tag)" v-bind="buildProps(node)" :ref="(el) => trackRef(node.id, el)" @click.stop="emit('select', node.id)">
        <span v-if="node.content && (!node.children||(node.children||[]).length===0)">{{ node.content }}</span>
        <EditorPreview v-if="isEditorNode(node)" :fieldName="getFieldName(node)" />
        <DesignCanvas v-else-if="node.children && (node.children||[]).length>0 && !isEditorChild(node)"
          :list="node.children" :selectedId="selectedId" @select="(id)=>emit('select',id)" />
        <!-- 通用 slot 渲染 -->
        <template v-for="s in (node.useSlots||[])" :key="s.name" #[s.name]="scope">
          <DesignCanvas :list="s.components||[]" :selectedId="selectedId" :slotScope="scope" @select="(id)=>emit('select',id)" />
        </template>
      </component>
      </template>
    </template>
</template>

<script setup>
import { onMounted, nextTick, watch } from 'vue'
import EditorPreview from './EditorPreview.vue'

defineOptions({ name: 'DesignCanvas' })
const props = defineProps({
  list: { type: Array, default: () => [] },
  selectedId: { type: String, default: null },
  slotScope: { type: Object, default: null }
})
const emit = defineEmits(['select', 'sort'])

// ====== 组件引用追踪（全局 Map） ======
const handleMap = window.__DESIGNER_HANDLES__ || new Map()
const noHandleTags = new Set(['el-option','template','text','span'])

function trackRef(id, el) {
  if (!el) { handleMap.delete(id); handleMap.delete('__tag__'+id); window.__DESIGNER_REFRESH__?.(); return }
  const node = findNode(props.list, id)
  if (!node) return
  // 寄生组件：从父 DOM 定位真实元素
  if (parasiticTags.has(node.tag)) {
    trackParasiticFromRef(node, el)
    return
  }
  const dom = el?.$el ?? el
  if (!dom || !(dom instanceof Element)) return
  handleMap.set(id, dom)
  handleMap.set('__tag__'+id, node.tag)
  // 追踪寄生子组件（延迟等 DOM 渲染完）
  nextTick(() => {
    for (const c of (node.children||[])) {
      if (parasiticTags.has(c.tag)) trackParasiticChild(c, dom)
    }
    window.__DESIGNER_REFRESH__?.()
  })
  window.__DESIGNER_REFRESH__?.()
}

function trackParasiticFromRef(node, el) {
  // el 是组件实例，$el 可能是 #text 节点
  const refDom = el?.$el ?? el
  if (!refDom || !refDom.parentNode) return
  // 在父节点的 children 中找到索引
  const siblings = refDom.parentNode.children
  let idx = -1
  for (let i = 0; i < siblings.length; i++) {
    if (siblings[i] === refDom) { idx = i; break }
  }
  if (idx < 0) return
  // 从父 DOM 定位真实元素
  const pDom = getRootAncestor(refDom, node.tag)
  if (!pDom) return
  const real = getParasiticDom(pDom, node.tag, idx)
  if (real) {
    handleMap.set(node.id, real)
    handleMap.set('__tag__'+node.id, node.tag)
    window.__DESIGNER_REFRESH__?.()
  }
}

function getRootAncestor(el, tag) {
  // 往上找到对应的根组件（el-table / el-tabs / el-descriptions）
  let cur = el.parentNode
  while (cur) {
    if (tag === 'el-table-column' && cur.classList?.contains('el-table')) return cur
    if (tag === 'el-tab-pane' && cur.classList?.contains('el-tabs')) return cur
    if (tag === 'el-descriptions-item' && cur.classList?.contains('el-descriptions')) return cur
    cur = cur.parentNode
  }
  return null
}

function trackParasiticChild(node, pDom) {
  if (!parasiticTags.has(node.tag)) return
  const idx = (findParent(props.list, node.id)?.children||[]).indexOf(node)
  if (idx < 0) return
  let el = getParasiticDom(pDom, node.tag, idx)
  if (el) {
    handleMap.set(node.id, el)
    handleMap.set('__tag__'+node.id, node.tag)
    window.__DESIGNER_REFRESH__?.()
  }
}

const parasiticTags = new Set(['el-table-column','el-tab-pane','el-descriptions-item'])
function trackParasitic(node, pDom) {
  if (!parasiticTags.has(node.tag)) return
  // SmartCoder 方式：在父 DOM 中定位子元素
  const idx = (findParent(props.list, node.id)?.children||[]).indexOf(node)
  if (idx < 0) return
  let el = getParasiticDom(pDom, node.tag, idx)
  if (el) {
    handleMap.set(node.id, el)
    handleMap.set('__tag__'+node.id, node.tag)
    window.__DESIGNER_REFRESH__?.()
  }
}

function getParasiticDom(pDom, tag, idx) {
  if (tag === 'el-table-column') {
    const cells = pDom.querySelectorAll('.el-table__header th>.cell')
    return cells[idx] || null
  }
  if (tag === 'el-tab-pane') {
    const tabs = pDom.querySelectorAll('.el-tabs__item')
    return tabs[idx] || null
  }
  if (tag === 'el-descriptions-item') {
    const labels = pDom.querySelectorAll('.el-descriptions__label')
    return labels[idx] || null
  }
  return null
}

function findParent(nodes, childId) {
  for (const n of nodes) {
    if (n.children?.some(c => c.id === childId)) return n
    if (n.children) { const f = findParent(n.children, childId); if (f) return f }
  }
  return null
}

onMounted(() => {
  nextTick(() => window.__DESIGNER_REFRESH__?.())
  const obs = new ResizeObserver(() => window.__DESIGNER_REFRESH__?.())
  obs.observe(document.body)
})
watch(() => props.list, () => nextTick(() => {}), { deep: true })

function onHeaderClick(tableNode, col) {
  // 从列 prop 匹配子节点
  const prop = col?.property
  const child = (tableNode.children||[]).find(c => {
    if (c.tag !== 'el-table-column') return false
    const cp = (c.props||[]).find(p => p.key==='prop')
    return cp?.value === prop
  })
  if (child) emit('select', child.id)
}

// ====== 工具函数 ======
function findNode(nodes, id) {
  for (const n of nodes) {
    if (n.id === id) return n
    if (n.children) { const f = findNode(n.children, id); if (f) return f }
  }
  return null
}
function isEditorNode(node) {
  if (node.tag==='el-form-item') {
    const cls=(node.props||[]).find(p=>p.key==='class')
    if (cls&&(cls.value||'').includes('editor-form-item')) return true
  }
  return false
}
function hasSlot(node, name) {
  return (node.useSlots||[]).some(s => s.name === name && (s.components||[]).length > 0)
}
function getSlotComps(node, name) {
  const s = (node.useSlots||[]).find(s => s.name === name)
  return s?.components || []
}

function getFieldName(node) {
  const prop=(node.props||[]).find(p=>p.key==='prop')
  return prop?.value||'editor'
}
function isEditorChild(node) {
  const cls=(node.props||[]).find(p=>p.key==='class')
  const v=(cls?.value||'')
  return v.includes('editor-wrap')||v.includes('editor-toolbar')||v.includes('editor-content')
}
function resolveTag(tag) {
  if (tag==='text'||tag==='template') return 'span'
  return tag
}
function buildProps(node) {
  const r={}
  if (!node.props) return r
  for (const p of node.props) {
    if (!p.key||p.key.startsWith('@')) continue
    if (p.isBind&&p.value&&p.value!=='true'&&p.value!=='false'&&!/^\d+$/.test(p.value)) {
      // 只保留简单变量绑定（如 model/rules），跳过表达式（如 $t(...)+... ）
      if (/^[a-z]\w*$/.test(p.value)) {
        if (p.key==='model'||p.key==='rules') { r[p.key]={}; continue }
        if (p.key==='data'&&node.tag==='el-table') { /* 下方已特殊处理 el-table data */ }
        else continue
      }
      // 复杂表达式（含空格、运算符、函数调用等）→ 预览模式下跳过
      continue
    }
    let v=p.value
    if (v==='true'||v===true) v=true
    else if (v==='false'||v===false) v=false
    else if (/^\d+$/.test(String(v))) v=Number(v)
    else if (v===null||v===undefined) v=true
    r[p.key]=v
  }
  // 兜底：el-form 必须提供 model，防止子 el-form-item 访问 undefined
  if (node.tag==='el-form'&&!('model' in r)) r.model={}
  if (node.tag==='el-form'&&!('rules' in r)) r.rules={}
  if (node.tag==='el-table-column'&&!r.width&&!r.minWidth) r.minWidth='80'
  if (node.tag==='el-table') {
    const mockRow={}
    for (const c of (node.children||[])) {
      if (c.tag!=='el-table-column') continue
      let prop=(c.props||[]).find(p=>p.key==='prop')
      if (!prop?.value&&c.genId) {
        const m=c.genId.match(/^gen_col_(.+)$/)
        if (m) prop={value:m[1].charAt(0).toLowerCase()+m[1].slice(1)}
      }
      if (prop?.value) mockRow[prop.value]=(c.props||[]).find(p=>p.key==='label')?.value||prop.value
      if (!(c.props||[]).some(p=>p.key==='prop')&&prop?.value)
        c.props=[...(c.props||[]),{key:'prop',value:prop.value,isBind:false}]
    }
    r.data=Object.keys(mockRow).length>0?[mockRow]:[{}]
  }
  if (node.tag==='el-upload') { r.autoUpload=false; r.fileList=[] }
  if (node.tag==='el-image'&&!r.src) r.src=''
  if (node.tag==='el-pagination'&&!r.total) r.total=0
  // el-dialog 强制可见
  if (node.tag==='el-dialog') {
    if (!('modelValue' in r) && !('visible' in r)) r.modelValue = true
  }
  return r
}
</script>

<style scoped>
.drag-overlay { position: fixed; top: 0; left: 0; pointer-events: none; z-index: 9999; }
.drag-handle {
  position: fixed; height: 18px; line-height: 18px; padding: 0 5px;
  background: rgba(64,158,255,.82); font-size: 10px; color: #fff;
  border-radius: 3px; cursor: grab; white-space: nowrap; pointer-events: auto;
  opacity: .6; transition: opacity .12s;
}
.drag-handle:hover { opacity: 1; }
.drag-handle.selected { opacity: 1; background: rgba(245,108,108,.85); }
</style>
