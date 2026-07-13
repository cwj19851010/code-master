<template>
  <div class="template-config-page">
    <div class="page-head">
      <div>
        <h2>模板配置</h2>
        <p>维护数据库中的页面模板、字段控件模板和子表模板。修改后需要重新生成代码才会生效。</p>
      </div>
      <div class="head-actions">
        <el-button :icon="Refresh" :loading="loading" @click="loadTemplates">刷新</el-button>
        <el-button type="primary" :icon="Check" :disabled="dirtyCount === 0" @click="saveAllDirty">
          保存全部变更
        </el-button>
      </div>
    </div>

    <div class="summary-row">
      <button
        v-for="group in groups"
        :key="group.key"
        class="summary-item"
        :class="{ active: activeGroup === group.key }"
        type="button"
        @click="setActiveGroup(group.key)"
      >
        <component :is="group.icon" class="summary-icon" />
        <span class="summary-label">{{ group.label }}</span>
        <strong>{{ group.count }}</strong>
      </button>
    </div>

    <div class="workspace">
      <aside class="template-list">
        <div class="list-tools">
          <el-input
            v-model="keyword"
            clearable
            :prefix-icon="Search"
            placeholder="搜索类型、名称、HTML 或 Script"
          />
        </div>

        <el-scrollbar class="list-scroll">
          <button
            v-for="item in filteredTemplates"
            :key="item._key"
            class="template-item"
            :class="{ active: selectedTemplate?._key === item._key }"
            type="button"
            @click="selectTemplate(item)"
          >
            <span class="item-main">
              <span class="item-title">{{ getTemplateTitle(item) }}</span>
              <span class="item-subtitle">{{ getTemplateSubtitle(item) }}</span>
            </span>
            <el-tag v-if="item._dirty" type="warning" size="small">未保存</el-tag>
          </button>

          <el-empty v-if="!loading && filteredTemplates.length === 0" description="没有匹配的模板" />
        </el-scrollbar>
      </aside>

      <section v-if="selectedTemplate" class="editor-panel">
        <div class="editor-head">
          <div>
            <div class="editor-title">{{ getTemplateTitle(selectedTemplate) }}</div>
            <div class="editor-subtitle">{{ activeGroupMeta.label }} / {{ getTemplateSubtitle(selectedTemplate) }}</div>
          </div>
          <div class="editor-actions">
            <el-button :icon="Document" @click="formatSelectedScript">格式化 Script</el-button>
            <el-button :icon="Edit" @click="openScriptDialog(selectedTemplate)">编辑 ScriptSection</el-button>
            <el-button type="primary" :icon="Check" :loading="saving" @click="saveTemplate(selectedTemplate)">
              保存
            </el-button>
          </div>
        </div>

        <el-form label-position="top" class="meta-form">
          <div class="meta-grid">
            <el-form-item v-for="field in activeGroupMeta.metaFields" :key="field.key" :label="field.label">
              <el-input
                v-model="selectedTemplate[field.key]"
                :disabled="field.readonly"
                @input="markDirty(selectedTemplate)"
              />
            </el-form-item>
          </div>
        </el-form>

        <el-tabs v-model="editorTab" class="editor-tabs">
          <el-tab-pane label="HTML 模板" name="html">
            <el-input
              v-model="selectedTemplate.htmlContent"
              type="textarea"
              :autosize="{ minRows: 22, maxRows: 34 }"
              resize="vertical"
              class="code-input"
              spellcheck="false"
              @input="markDirty(selectedTemplate)"
            />
          </el-tab-pane>

          <el-tab-pane label="ScriptSection" name="script">
            <div class="script-toolbar">
              <div class="script-summary">
                <el-tag v-for="item in scriptSummary" :key="item.key" :type="item.count ? 'primary' : 'info'" size="small">
                  {{ item.label }} {{ item.count }}
                </el-tag>
              </div>
              <el-button size="small" :icon="Edit" @click="openScriptDialog(selectedTemplate)">结构化编辑</el-button>
            </div>
            <el-alert
              v-if="scriptError"
              type="error"
              :closable="false"
              show-icon
              class="script-alert"
              :title="scriptError"
            />
            <el-input
              v-model="selectedTemplate.scriptSections"
              type="textarea"
              :autosize="{ minRows: 22, maxRows: 34 }"
              resize="vertical"
              class="code-input"
              spellcheck="false"
              @input="onRawScriptInput"
            />
          </el-tab-pane>
        </el-tabs>
      </section>

      <section v-else class="empty-panel">
        <el-empty description="请选择一个模板" />
      </section>
    </div>

    <el-dialog
      v-model="scriptDialogVisible"
      title="编辑 ScriptSection"
      width="980px"
      top="5vh"
      destroy-on-close
      class="script-dialog"
    >
      <template v-if="editingScript">
        <el-tabs v-model="scriptEditTab">
          <el-tab-pane label="Imports" name="imports">
            <div class="section-list">
              <div v-for="(item, index) in editingScript.imports" :key="`import-${index}`" class="section-row">
                <el-input v-model="item.path" placeholder="@/api/xxx" @input="scriptDirty = true" />
                <el-input v-model="item.destructured" placeholder="named: ref, reactive" @input="scriptDirty = true" />
                <el-input v-model="item.default" placeholder="default: api" @input="scriptDirty = true" />
                <el-button type="danger" link :icon="Delete" @click="removeScriptItem('imports', index)" />
              </div>
              <el-button :icon="Plus" @click="addScriptItem('imports')">添加 Import</el-button>
            </div>
          </el-tab-pane>

          <el-tab-pane label="Uses" name="uses">
            <div class="section-list">
              <div v-for="(item, index) in editingScript.uses" :key="`use-${index}`" class="section-row">
                <el-input v-model="item.path" placeholder="use hook path" @input="scriptDirty = true" />
                <el-input v-model="item.name" placeholder="name" @input="scriptDirty = true" />
                <el-input v-model="item.as" placeholder="as" @input="scriptDirty = true" />
                <el-button type="danger" link :icon="Delete" @click="removeScriptItem('uses', index)" />
              </div>
              <el-button :icon="Plus" @click="addScriptItem('uses')">添加 Use</el-button>
            </div>
          </el-tab-pane>

          <el-tab-pane label="Refs" name="refs">
            <div class="section-list">
              <div v-for="(item, index) in editingScript.refs" :key="`ref-${index}`" class="section-row">
                <el-input v-model="item.name" placeholder="name" @input="scriptDirty = true" />
                <el-input v-model="item.type" placeholder="type" @input="scriptDirty = true" />
                <el-input v-model="item.initialValue" placeholder="initialValue" @input="scriptDirty = true" />
                <el-button type="danger" link :icon="Delete" @click="removeScriptItem('refs', index)" />
              </div>
              <el-button :icon="Plus" @click="addScriptItem('refs')">添加 Ref</el-button>
            </div>
          </el-tab-pane>

          <el-tab-pane label="Reactives" name="reactives">
            <div class="section-list">
              <div v-for="(item, index) in editingScript.reactives" :key="`reactive-${index}`" class="section-block">
                <div class="block-head">
                  <el-input v-model="item.name" placeholder="reactive 名称" @input="scriptDirty = true" />
                  <el-button type="danger" link :icon="Delete" @click="removeScriptItem('reactives', index)" />
                </div>
                <div v-for="field in getReactiveFields(item)" :key="field" class="field-row">
                  <el-input :model-value="field" disabled />
                  <el-input v-model="item.fields[field]" placeholder="value" @input="scriptDirty = true" />
                  <el-button type="danger" link :icon="Delete" @click="removeReactiveField(item, field)" />
                </div>
                <el-button size="small" :icon="Plus" @click="addReactiveField(item)">添加字段</el-button>
              </div>
              <el-button :icon="Plus" @click="addScriptItem('reactives')">添加 Reactive</el-button>
            </div>
          </el-tab-pane>

          <el-tab-pane label="Functions" name="functions">
            <div class="section-list">
              <div v-for="(item, index) in editingScript.functions" :key="`function-${index}`" class="section-block">
                <div class="block-head">
                  <el-checkbox v-model="item.async" @change="scriptDirty = true">async</el-checkbox>
                  <el-input v-model="item.name" placeholder="函数名" @input="scriptDirty = true" />
                  <el-input v-model="item.params" placeholder="参数" @input="scriptDirty = true" />
                  <el-button type="danger" link :icon="Delete" @click="removeScriptItem('functions', index)" />
                </div>
                <el-input
                  v-model="item.bodyText"
                  type="textarea"
                  :rows="8"
                  resize="vertical"
                  class="code-input"
                  @input="scriptDirty = true"
                />
              </div>
              <el-button :icon="Plus" @click="addScriptItem('functions')">添加 Function</el-button>
            </div>
          </el-tab-pane>

          <el-tab-pane label="Hooks" name="hooks">
            <div class="section-list">
              <div v-for="(item, index) in editingScript.hooks" :key="`hook-${index}`" class="section-block">
                <div class="block-head">
                  <el-select v-model="item.name" placeholder="Hook" @change="scriptDirty = true">
                    <el-option label="onMounted" value="onMounted" />
                    <el-option label="onUnmounted" value="onUnmounted" />
                    <el-option label="onBeforeMount" value="onBeforeMount" />
                    <el-option label="onUpdated" value="onUpdated" />
                  </el-select>
                  <el-button type="danger" link :icon="Delete" @click="removeScriptItem('hooks', index)" />
                </div>
                <el-input
                  v-model="item.bodyText"
                  type="textarea"
                  :rows="6"
                  resize="vertical"
                  class="code-input"
                  @input="scriptDirty = true"
                />
              </div>
              <el-button :icon="Plus" @click="addScriptItem('hooks')">添加 Hook</el-button>
            </div>
          </el-tab-pane>

          <el-tab-pane label="Computed" name="computed">
            <div class="section-list">
              <div v-for="(item, index) in editingScript.computed" :key="`computed-${index}`" class="section-block">
                <div class="block-head">
                  <el-input v-model="item.name" placeholder="computed 名称" @input="scriptDirty = true" />
                  <el-button type="danger" link :icon="Delete" @click="removeScriptItem('computed', index)" />
                </div>
                <el-input
                  v-model="item.bodyText"
                  type="textarea"
                  :rows="6"
                  resize="vertical"
                  class="code-input"
                  @input="scriptDirty = true"
                />
              </div>
              <el-button :icon="Plus" @click="addScriptItem('computed')">添加 Computed</el-button>
            </div>
          </el-tab-pane>

          <el-tab-pane label="Watches" name="watches">
            <div class="section-list">
              <div v-for="(item, index) in editingScript.watches" :key="`watch-${index}`" class="section-block">
                <div class="block-head">
                  <el-input v-model="item.source" placeholder="watch source" @input="scriptDirty = true" />
                  <el-checkbox v-model="item.deep" @change="scriptDirty = true">deep</el-checkbox>
                  <el-checkbox v-model="item.immediate" @change="scriptDirty = true">immediate</el-checkbox>
                  <el-button type="danger" link :icon="Delete" @click="removeScriptItem('watches', index)" />
                </div>
                <el-input
                  v-model="item.bodyText"
                  type="textarea"
                  :rows="6"
                  resize="vertical"
                  class="code-input"
                  @input="scriptDirty = true"
                />
              </div>
              <el-button :icon="Plus" @click="addScriptItem('watches')">添加 Watch</el-button>
            </div>
          </el-tab-pane>

          <el-tab-pane label="Raw JSON" name="raw">
            <el-alert
              v-if="rawEditError"
              type="error"
              :closable="false"
              show-icon
              class="script-alert"
              :title="rawEditError"
            />
            <el-input
              v-model="rawEditJson"
              type="textarea"
              :autosize="{ minRows: 24, maxRows: 34 }"
              resize="vertical"
              class="code-input"
              spellcheck="false"
              @input="onRawEditInput"
            />
          </el-tab-pane>
        </el-tabs>
      </template>

      <template #footer>
        <el-button @click="scriptDialogVisible = false">取消</el-button>
        <el-button type="primary" :disabled="!!rawEditError" @click="applyScriptDialog">应用到当前模板</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { ElMessage } from 'element-plus'
import {
  Check,
  Delete,
  Document,
  Edit,
  FolderOpened,
  Plus,
  Refresh,
  Search,
  Tickets
} from '@element-plus/icons-vue'
import {
  getChildTemplates,
  getFieldControlTemplates,
  getPageTemplates,
  saveChildTemplate,
  saveFieldControlTemplate,
  savePageTemplate
} from '@/api/codegen/moduleEntity'

const loading = ref(false)
const saving = ref(false)
const keyword = ref('')
const activeGroup = ref('pages')
const editorTab = ref('html')

const pageTemplates = ref([])
const controlTemplates = ref([])
const childTemplates = ref([])
const selectedTemplate = ref(null)

const scriptDialogVisible = ref(false)
const scriptEditTab = ref('imports')
const editingScript = ref(null)
const editingRow = ref(null)
const rawEditJson = ref('')
const rawEditError = ref('')
const scriptDirty = ref(false)

const groupConfig = {
  pages: {
    label: '页面模板',
    icon: Document,
    items: pageTemplates,
    save: savePageTemplate,
    metaFields: [
      { key: 'pageType', label: '页面类型' },
      { key: 'name', label: '名称' },
      { key: 'sort', label: '排序' }
    ],
    title: row => row.name || row.pageType,
    subtitle: row => `pageType: ${row.pageType}`
  },
  controls: {
    label: '控件模板',
    icon: Tickets,
    items: controlTemplates,
    save: saveFieldControlTemplate,
    metaFields: [
      { key: 'controlType', label: '控件类型' },
      { key: 'pageSection', label: '页面区域' },
      { key: 'sort', label: '排序' }
    ],
    title: row => row.controlType,
    subtitle: row => `section: ${row.pageSection}`
  },
  children: {
    label: '子表模板',
    icon: FolderOpened,
    items: childTemplates,
    save: saveChildTemplate,
    metaFields: [
      { key: 'pageType', label: '页面类型' },
      { key: 'childType', label: '子表区域' },
      { key: 'sort', label: '排序' }
    ],
    title: row => `${row.pageType} / ${row.childType}`,
    subtitle: row => row.childType === 'dialog' ? '子表弹窗模板' : '子表列表/详情模板'
  }
}

const groups = computed(() => Object.entries(groupConfig).map(([key, group]) => ({
  key,
  label: group.label,
  icon: group.icon,
  count: group.items.value.length
})))

const activeGroupMeta = computed(() => groupConfig[activeGroup.value])

const currentTemplates = computed(() => activeGroupMeta.value.items.value)

const filteredTemplates = computed(() => {
  const text = keyword.value.trim().toLowerCase()
  if (!text) return currentTemplates.value

  return currentTemplates.value.filter(item => {
    const haystack = [
      getTemplateTitle(item),
      getTemplateSubtitle(item),
      item.htmlContent,
      item.scriptSections
    ].join('\n').toLowerCase()
    return haystack.includes(text)
  })
})

const dirtyCount = computed(() => {
  return [...pageTemplates.value, ...controlTemplates.value, ...childTemplates.value]
    .filter(item => item._dirty)
    .length
})

const scriptError = computed(() => {
  if (!selectedTemplate.value) return ''
  const result = tryParseScript(selectedTemplate.value.scriptSections)
  return result.error
})

const scriptSummary = computed(() => {
  const parsed = tryParseScript(selectedTemplate.value?.scriptSections)
  const script = parsed.value || emptyScript()
  return [
    { key: 'imports', label: 'imports', count: script.imports.length },
    { key: 'uses', label: 'uses', count: script.uses.length },
    { key: 'refs', label: 'refs', count: script.refs.length },
    { key: 'reactives', label: 'reactives', count: script.reactives.length },
    { key: 'functions', label: 'functions', count: script.functions.length },
    { key: 'hooks', label: 'hooks', count: script.hooks.length },
    { key: 'computed', label: 'computed', count: script.computed.length },
    { key: 'watches', label: 'watches', count: script.watches.length }
  ]
})

watch(activeGroup, () => {
  selectedTemplate.value = filteredTemplates.value[0] || null
  editorTab.value = 'html'
})

async function loadTemplates() {
  loading.value = true
  try {
    const [pages, controls, children] = await Promise.all([
      getPageTemplates(),
      getFieldControlTemplates(),
      getChildTemplates()
    ])

    pageTemplates.value = normalizeRows(pages, 'pages')
    controlTemplates.value = normalizeRows(controls, 'controls')
    childTemplates.value = normalizeRows(children, 'children')
    selectedTemplate.value = currentTemplates.value[0] || null
    ElMessage.success('模板已刷新')
  } catch (error) {
    ElMessage.error(`模板加载失败: ${error.message}`)
  } finally {
    loading.value = false
  }
}

function normalizeRows(rows, group) {
  return (rows || []).map(row => ({
    ...row,
    scriptSections: row.scriptSections || JSON.stringify(emptyScript()),
    _group: group,
    _key: `${group}:${row.id}`,
    _dirty: false
  }))
}

function setActiveGroup(group) {
  activeGroup.value = group
}

function selectTemplate(row) {
  selectedTemplate.value = row
  editorTab.value = 'html'
}

function getTemplateTitle(row) {
  return groupConfig[row?._group || activeGroup.value].title(row)
}

function getTemplateSubtitle(row) {
  return groupConfig[row?._group || activeGroup.value].subtitle(row)
}

function markDirty(row) {
  if (row) row._dirty = true
}

function toPayload(row) {
  const payload = { ...row }
  delete payload._group
  delete payload._key
  delete payload._dirty
  return payload
}

async function saveTemplate(row) {
  if (!row) return
  const parsed = tryParseScript(row.scriptSections)
  if (parsed.error) {
    ElMessage.error(parsed.error)
    editorTab.value = 'script'
    return
  }

  saving.value = true
  try {
    await groupConfig[row._group].save(toPayload(row))
    row._dirty = false
    ElMessage.success('模板已保存')
  } catch (error) {
    ElMessage.error(`保存失败: ${error.message}`)
  } finally {
    saving.value = false
  }
}

async function saveAllDirty() {
  const dirtyRows = [...pageTemplates.value, ...controlTemplates.value, ...childTemplates.value]
    .filter(item => item._dirty)

  for (const row of dirtyRows) {
    const parsed = tryParseScript(row.scriptSections)
    if (parsed.error) {
      selectedTemplate.value = row
      editorTab.value = 'script'
      ElMessage.error(`${getTemplateTitle(row)}: ${parsed.error}`)
      return
    }
  }

  saving.value = true
  try {
    for (const row of dirtyRows) {
      await groupConfig[row._group].save(toPayload(row))
      row._dirty = false
    }
    ElMessage.success('所有变更已保存')
  } catch (error) {
    ElMessage.error(`保存失败: ${error.message}`)
  } finally {
    saving.value = false
  }
}

function onRawScriptInput() {
  markDirty(selectedTemplate.value)
}

function formatSelectedScript() {
  const row = selectedTemplate.value
  if (!row) return
  const parsed = tryParseScript(row.scriptSections)
  if (parsed.error) {
    ElMessage.error(parsed.error)
    editorTab.value = 'script'
    return
  }
  row.scriptSections = stringifyScript(parsed.value)
  markDirty(row)
}

function openScriptDialog(row) {
  editingRow.value = row
  const parsed = tryParseScript(row.scriptSections)
  if (parsed.error) {
    rawEditError.value = parsed.error
    editingScript.value = emptyScript()
    rawEditJson.value = row.scriptSections || ''
    scriptEditTab.value = 'raw'
  } else {
    rawEditError.value = ''
    editingScript.value = normalizeScript(parsed.value)
    rawEditJson.value = stringifyScript(editingScript.value)
    scriptEditTab.value = 'imports'
  }
  scriptDirty.value = false
  scriptDialogVisible.value = true
}

function applyScriptDialog() {
  if (!editingRow.value || rawEditError.value) return

  const script = scriptEditTab.value === 'raw'
    ? tryParseScript(rawEditJson.value).value
    : buildScript(editingScript.value)

  editingRow.value.scriptSections = stringifyScript(script)
  markDirty(editingRow.value)
  scriptDialogVisible.value = false
  ElMessage.success('ScriptSection 已应用，请保存模板')
}

function onRawEditInput() {
  const parsed = tryParseScript(rawEditJson.value)
  rawEditError.value = parsed.error
  if (!parsed.error) {
    editingScript.value = normalizeScript(parsed.value)
    scriptDirty.value = true
  }
}

watch(scriptEditTab, tab => {
  if (tab === 'raw' && editingScript.value && !rawEditError.value) {
    rawEditJson.value = stringifyScript(editingScript.value)
  }
})

function tryParseScript(value) {
  if (!value || !String(value).trim()) return { value: emptyScript(), error: '' }
  try {
    return { value: normalizeScript(JSON.parse(value)), error: '' }
  } catch (error) {
    return { value: null, error: `ScriptSection JSON 格式错误: ${error.message}` }
  }
}

function emptyScript() {
  return {
    imports: [],
    uses: [],
    refs: [],
    reactives: [],
    functions: [],
    hooks: [],
    computed: [],
    watches: []
  }
}

function normalizeScript(script) {
  const data = { ...emptyScript(), ...(script || {}) }
  return {
    imports: (data.imports || []).map(item => ({
      path: item.path || '',
      destructured: item.destructured || '',
      default: item.default || ''
    })),
    uses: (data.uses || []).map(item => ({
      path: item.path || '',
      name: item.name || '',
      as: item.as || ''
    })),
    refs: (data.refs || []).map(item => ({
      name: item.name || '',
      type: item.type || '',
      initialValue: item.initialValue ?? 'null'
    })),
    reactives: (data.reactives || []).map(item => ({
      name: item.name || '',
      fields: { ...(item.fields || {}) }
    })),
    functions: (data.functions || []).map(item => ({
      name: item.name || '',
      params: item.params || item.parameters || '',
      async: !!item.async,
      bodyText: Array.isArray(item.body) ? item.body.join('\n') : (item.body || '')
    })),
    hooks: (data.hooks || []).map(item => ({
      name: item.name || '',
      bodyText: Array.isArray(item.body) ? item.body.join('\n') : (item.body || '')
    })),
    computed: (data.computed || []).map(item => ({
      name: item.name || '',
      bodyText: Array.isArray(item.body) ? item.body.join('\n') : (item.body || '')
    })),
    watches: (data.watches || []).map(item => ({
      source: item.source || '',
      deep: !!item.deep,
      immediate: !!item.immediate,
      bodyText: Array.isArray(item.body) ? item.body.join('\n') : (item.body || '')
    }))
  }
}

function buildScript(script) {
  const data = script || emptyScript()
  return {
    imports: data.imports
      .filter(item => item.path)
      .map(item => compactObject({
        path: item.path,
        destructured: item.destructured,
        default: item.default
      })),
    uses: data.uses
      .filter(item => item.path || item.name)
      .map(item => compactObject({
        path: item.path,
        name: item.name,
        as: item.as
      })),
    refs: data.refs
      .filter(item => item.name)
      .map(item => compactObject({
        name: item.name,
        type: item.type,
        initialValue: item.initialValue
      })),
    reactives: data.reactives
      .filter(item => item.name)
      .map(item => ({
        name: item.name,
        fields: item.fields || {}
      })),
    functions: data.functions
      .filter(item => item.name)
      .map(item => compactObject({
        name: item.name,
        params: item.params,
        async: item.async || undefined,
        body: splitBody(item.bodyText)
      })),
    hooks: data.hooks
      .filter(item => item.name)
      .map(item => ({
        name: item.name,
        body: splitBody(item.bodyText)
      })),
    computed: data.computed
      .filter(item => item.name)
      .map(item => ({
        name: item.name,
        body: splitBody(item.bodyText)
      })),
    watches: data.watches
      .filter(item => item.source)
      .map(item => compactObject({
        source: item.source,
        body: splitBody(item.bodyText),
        deep: item.deep || undefined,
        immediate: item.immediate || undefined
      }))
  }
}

function compactObject(obj) {
  return Object.fromEntries(Object.entries(obj).filter(([, value]) => value !== undefined && value !== ''))
}

function splitBody(bodyText) {
  if (!bodyText) return []
  return String(bodyText).split('\n')
}

function stringifyScript(script) {
  return JSON.stringify(buildScript(normalizeScript(script)), null, 2)
}

function addScriptItem(type) {
  if (!editingScript.value) return
  const factories = {
    imports: () => ({ path: '', destructured: '', default: '' }),
    uses: () => ({ path: '', name: '', as: '' }),
    refs: () => ({ name: '', type: '', initialValue: 'null' }),
    reactives: () => ({ name: 'state', fields: {} }),
    functions: () => ({ name: 'newFunction', params: '', async: false, bodyText: '' }),
    hooks: () => ({ name: 'onMounted', bodyText: '' }),
    computed: () => ({ name: 'newComputed', bodyText: '' }),
    watches: () => ({ source: '', deep: false, immediate: false, bodyText: '' })
  }
  editingScript.value[type].push(factories[type]())
  scriptDirty.value = true
  rawEditJson.value = stringifyScript(editingScript.value)
}

function removeScriptItem(type, index) {
  editingScript.value[type].splice(index, 1)
  scriptDirty.value = true
  rawEditJson.value = stringifyScript(editingScript.value)
}

function getReactiveFields(item) {
  return Object.keys(item.fields || {})
}

function addReactiveField(item) {
  item.fields = item.fields || {}
  let index = Object.keys(item.fields).length + 1
  let key = `field${index}`
  while (Object.prototype.hasOwnProperty.call(item.fields, key)) {
    index += 1
    key = `field${index}`
  }
  item.fields[key] = ''
  scriptDirty.value = true
  rawEditJson.value = stringifyScript(editingScript.value)
}

function removeReactiveField(item, field) {
  delete item.fields[field]
  scriptDirty.value = true
  rawEditJson.value = stringifyScript(editingScript.value)
}

onMounted(loadTemplates)

refreshOnReactivated(loadTemplates)
</script>

<style scoped>
.template-config-page {
  min-height: calc(100vh - 96px);
  padding: 16px;
  background: var(--app-bg, #f5f7fb);
  color: var(--app-text, #1f2937);
}

.page-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 12px;
}

.page-head h2 {
  margin: 0;
  font-size: 20px;
  font-weight: 650;
  color: var(--app-text, #1f2d3d);
}

.page-head p {
  margin: 6px 0 0;
  color: var(--app-text-muted, #667085);
  font-size: 13px;
}

.head-actions,
.editor-actions,
.script-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.summary-row {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;
  margin-bottom: 12px;
}

.summary-item {
  display: grid;
  grid-template-columns: 28px minmax(0, 1fr) auto;
  align-items: center;
  gap: 10px;
  height: 56px;
  border: 1px solid var(--app-border, #d8dee9);
  border-radius: 6px;
  padding: 0 14px;
  background: var(--app-surface, #fff);
  color: var(--app-text, #344054);
  text-align: left;
  cursor: pointer;
}

.summary-item.active {
  border-color: var(--el-color-primary, #409eff);
  background: color-mix(in srgb, var(--el-color-primary, #409eff) 14%, var(--app-surface, #fff));
}

.summary-icon {
  width: 20px;
  height: 20px;
  color: #409eff;
}

.summary-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.workspace {
  display: grid;
  grid-template-columns: 320px minmax(0, 1fr);
  gap: 12px;
  align-items: start;
  min-height: 0;
}

.template-list,
.editor-panel,
.empty-panel {
  min-width: 0;
  border: 1px solid var(--app-border, #d8dee9);
  border-radius: 6px;
  background: var(--app-surface, #fff);
}

.template-list {
  display: flex;
  flex-direction: column;
  height: clamp(420px, calc(100vh - 230px), 760px);
  min-height: 0;
  max-height: none;
  overflow: hidden;
}

.list-tools {
  padding: 12px;
  border-bottom: 1px solid var(--app-border, #edf0f5);
}

.list-scroll {
  flex: 1;
  min-height: 0;
}

.list-scroll :deep(.el-scrollbar__wrap) {
  max-height: 100%;
}

.template-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
  min-height: 58px;
  border: 0;
  border-bottom: 1px solid var(--app-border, #f0f2f5);
  padding: 10px 12px;
  background: var(--app-surface, #fff);
  color: var(--app-text, #303133);
  text-align: left;
  cursor: pointer;
}

.template-item:hover {
  background: var(--app-surface-soft, #f8fafc);
}

.template-item.active {
  background: color-mix(in srgb, var(--el-color-primary, #409eff) 14%, var(--app-surface, #fff));
}

.item-main {
  min-width: 0;
  display: grid;
  gap: 4px;
}

.item-title,
.item-subtitle {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item-title {
  font-weight: 600;
}

.item-subtitle,
.editor-subtitle {
  color: var(--app-text-muted, #667085);
  font-size: 12px;
}

.editor-panel {
  padding: 14px;
  overflow: hidden;
}

.empty-panel {
  display: flex;
  align-items: center;
  justify-content: center;
}

.editor-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 12px;
}

.editor-title {
  color: var(--app-text, #1f2d3d);
  font-size: 18px;
  font-weight: 650;
}

.meta-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.editor-tabs {
  margin-top: 4px;
}

.code-input :deep(textarea) {
  font-family: Consolas, "Courier New", monospace;
  font-size: 12px;
  line-height: 1.55;
}

.script-toolbar {
  justify-content: space-between;
  margin-bottom: 8px;
}

.script-summary {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.script-alert {
  margin-bottom: 8px;
}

.section-list {
  display: grid;
  gap: 10px;
}

.section-row {
  display: grid;
  grid-template-columns: minmax(180px, 1fr) minmax(180px, 1fr) minmax(160px, 1fr) 40px;
  gap: 8px;
  align-items: center;
}

.section-block {
  border: 1px solid var(--app-border, #e4e7ed);
  border-radius: 6px;
  padding: 10px;
  background: var(--app-surface-soft, #fbfcfe);
}

.template-config-page :deep(.el-tabs__item) {
  color: var(--app-text-muted, #64748b);
}

.template-config-page :deep(.el-tabs__item.is-active) {
  color: var(--el-color-primary, #409eff);
}

.block-head {
  display: grid;
  grid-template-columns: auto minmax(160px, 1fr) minmax(140px, 1fr) 40px;
  gap: 8px;
  align-items: center;
  margin-bottom: 8px;
}

.field-row {
  display: grid;
  grid-template-columns: 160px minmax(0, 1fr) 40px;
  gap: 8px;
  align-items: center;
  margin-bottom: 6px;
}

@media (max-width: 1100px) {
  .summary-row,
  .workspace,
  .meta-grid {
    grid-template-columns: 1fr;
  }

  .template-list {
    height: 360px;
    min-height: 320px;
    max-height: 360px;
  }
}
</style>
