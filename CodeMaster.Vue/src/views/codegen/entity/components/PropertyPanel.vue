<template>
  <div class="property-panel" v-loading="loading">
    <div v-if="!panelData" class="empty-hint">{{ t('selectFieldHint') }}</div>
    <template v-else>
      <div class="panel-header">
        <span class="field-name">{{ panelData.fieldName }}</span>
        <el-tag size="small">{{ panelData.componentTag }}</el-tag>
      </div>

      <el-tabs v-model="activeTab" class="panel-tabs">
        <!-- ====== 属性 ====== -->
        <el-tab-pane :label="`${t('componentProps')} (${activeCount})`" name="props">
          <div class="tab-toolbar">
            <el-checkbox v-model="showOnlyActive" size="small">{{ t('onlyActive') }}</el-checkbox>
            <el-checkbox v-model="showAdvanced" size="small">{{ t('showAdvanced') }}</el-checkbox>
          </div>
          <div class="prop-list">
            <div v-for="prop in filteredProps" :key="prop.propName" class="prop-row" :class="{ inactive: !prop.isActive }">
              <el-checkbox :model-value="prop.isActive" @change="(v) => toggleProp(prop, v)" size="small" />
              <span class="prop-name" :title="prop.description">{{ prop.propName }}</span>
              <template v-if="prop.isActive">
                <el-select v-if="prop.valueType === 'boolean'" :model-value="'true'" size="small" class="prop-value" disabled>
                  <el-option label="true" value="true" />
                </el-select>
                <el-select v-else-if="prop.enumValues" :model-value="prop.currentValue || ''" size="small" class="prop-value" @change="(v) => updatePropValue(prop, v, 'static')" clearable>
                  <el-option v-for="ev in enumOptions(prop)" :key="ev" :label="ev" :value="ev" />
                </el-select>
                <el-input v-else :model-value="prop.currentValue || ''" size="small" class="prop-value" @change="(v) => updatePropValue(prop, v, 'static')" :placeholder="prop.defaultValue" />
                <el-dropdown trigger="click" size="small" @command="(cmd) => updatePropValueType(prop, cmd)">
                  <el-button size="small" class="mode-btn">
                    {{ modeIcon(prop.valueType) }}
                  </el-button>
                  <template #dropdown>
                    <el-dropdown-menu>
                      <el-dropdown-item command="static">📝 static</el-dropdown-item>
                      <el-dropdown-item command="bind">🔗 :bind</el-dropdown-item>
                      <el-dropdown-item command="boolean">⚡ boolean</el-dropdown-item>
                    </el-dropdown-menu>
                  </template>
                </el-dropdown>
              </template>
            </div>
          </div>
        </el-tab-pane>

        <!-- ====== 指令 ====== -->
        <el-tab-pane :label="`${t('directives')} (${activeDirectiveCount})`" name="directives">
          <div class="tab-toolbar">
            <el-checkbox v-model="showOnlyActiveDir" size="small">{{ t('onlyActive') }}</el-checkbox>
          </div>
          <div class="prop-list">
            <div v-for="d in filteredDirectives" :key="d.directiveName" class="prop-row" :class="{ inactive: !d.isActive }">
              <el-checkbox :model-value="d.isActive" @change="(v) => toggleDirective(d, v)" size="small" />
              <span class="prop-name" :title="d.description">{{ d.directiveName }}</span>
              <template v-if="d.isActive && d.hasValue">
                <el-input :model-value="d.currentValue || ''" size="small" class="prop-value" @change="(v) => updateDirectiveValue(d, v)" placeholder="expression" />
              </template>
            </div>
          </div>
        </el-tab-pane>

        <!-- ====== 事件 ====== -->
        <el-tab-pane :label="`${t('componentEvents')} (${activeEventCount})`" name="events">
          <div class="tab-toolbar">
            <el-checkbox v-model="showOnlyActiveEvt" size="small">{{ t('onlyActive') }}</el-checkbox>
          </div>
          <div class="prop-list">
            <div v-for="evt in filteredEvents" :key="evt.eventName" class="prop-row" :class="{ inactive: !evt.isActive }">
              <el-checkbox :model-value="evt.isActive" @change="(v) => toggleEvent(evt, v)" size="small" />
              <span class="prop-name" :title="evt.description">@{{ evt.eventName }}</span>
              <template v-if="evt.isActive && !evt.isSingleActive">
                <el-input :model-value="evt.currentValue || ''" size="small" class="prop-value" @change="(v) => updateEventValue(evt, v)" placeholder="handler" />
              </template>
              <template v-if="evt.isActive">
                <el-button size="small" class="mode-btn" :type="evt.isSingleActive ? 'primary' : ''" @click="toggleEventSingle(evt)">
                  @
                </el-button>
              </template>
            </div>
          </div>
        </el-tab-pane>

        <!-- ====== 插槽 ====== -->
        <el-tab-pane :label="`${t('componentSlots')} (${activeSlotCount})`" name="slots">
          <div class="tab-toolbar">
            <el-checkbox v-model="showOnlyActiveSlot" size="small">{{ t('onlyActive') }}</el-checkbox>
          </div>
          <div class="prop-list">
            <div v-for="s in filteredSlots" :key="s.slotName" class="prop-row" :class="{ inactive: !s.isActive }">
              <el-checkbox :model-value="s.isActive" @change="(v) => toggleSlot(s, v)" size="small" />
              <span class="prop-name" :title="s.description">#{{ s.slotName }}</span>
            </div>
          </div>
        </el-tab-pane>
      </el-tabs>

      <div class="panel-footer">
        <el-button type="primary" size="small" @click="handleSave" :loading="saving">{{ t('save') }}</el-button>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { getFieldPropertyPanel } from '@/api/system/component'
import { savePageContent } from '@/api/codegen/moduleEntity'

const { t } = useI18n()
const props = defineProps({
  entityId: { type: Number, required: true },
  pageType: { type: String, default: 'index' },
  genId: { type: String, default: '' }
})
const emit = defineEmits(['saved'])

const loading = ref(false)
const saving = ref(false)
const panelData = ref(null)
const activeTab = ref('props')

// filters
const showOnlyActive = ref(true)
const showAdvanced = ref(false)
const showOnlyActiveDir = ref(true)
const showOnlyActiveEvt = ref(true)
const showOnlyActiveSlot = ref(true)

// counts
const activeCount = computed(() => panelData.value?.properties?.filter(p => p.isActive).length || 0)
const activeDirectiveCount = computed(() => panelData.value?.directives?.filter(d => d.isActive).length || 0)
const activeEventCount = computed(() => panelData.value?.events?.filter(e => e.isActive).length || 0)
const activeSlotCount = computed(() => panelData.value?.slots?.filter(s => s.isActive).length || 0)

const filteredProps = computed(() => {
  if (!panelData.value) return []
  let list = panelData.value.properties
  if (showOnlyActive.value) list = list.filter(p => p.isActive)
  if (!showAdvanced.value) list = list.filter(p => !p.isAdvanced)
  return list
})
const filteredDirectives = computed(() => {
  if (!panelData.value) return []
  let list = panelData.value.directives
  if (showOnlyActiveDir.value) list = list.filter(d => d.isActive)
  return list
})
const filteredEvents = computed(() => {
  if (!panelData.value) return []
  let list = panelData.value.events
  if (showOnlyActiveEvt.value) list = list.filter(e => e.isActive)
  return list
})
const filteredSlots = computed(() => {
  if (!panelData.value) return []
  let list = panelData.value.slots
  if (showOnlyActiveSlot.value) list = list.filter(s => s.isActive)
  return list
})

function enumOptions(prop) {
  return (prop.enumValues || '').split(',').map(s => s.trim()).filter(Boolean)
}
function modeIcon(vt) {
  return vt === 'bind' ? '🔗' : vt === 'boolean' ? '⚡' : '📝'
}

// load
watch(() => props.genId, async (newVal) => {
  if (!newVal) { panelData.value = null; return }
  await loadPanel()
}, { immediate: true })

async function loadPanel() {
  loading.value = true
  try {
    panelData.value = await getFieldPropertyPanel(props.entityId, props.pageType, props.genId)
  } catch (e) {
    console.error('加载属性面板失败', e)
    panelData.value = null
  } finally { loading.value = false }
}

// mutations
function toggleProp(prop, val) { prop.isActive = val; if (!val) { prop.currentValue = null } }
function updatePropValue(prop, val, vt) { prop.currentValue = val; prop.valueType = vt || prop.valueType }
function updatePropValueType(prop, cmd) { prop.valueType = cmd; if (cmd === 'boolean') prop.currentValue = null }

function toggleDirective(d, val) { d.isActive = val; if (!val) d.currentValue = null }
function updateDirectiveValue(d, val) { d.currentValue = val }

function toggleEvent(evt, val) { evt.isActive = val; if (!val) { evt.currentValue = null; evt.isSingleActive = false } }
function updateEventValue(evt, val) { evt.currentValue = val }
function toggleEventSingle(evt) { evt.isSingleActive = !evt.isSingleActive; if (evt.isSingleActive) evt.currentValue = null }

function toggleSlot(s, val) { s.isActive = val }

async function handleSave() {
  saving.value = true
  try {
    // Build simplified treeJson from current state
    const treeJson = buildTreeJson()
    await savePageContent(props.entityId, props.pageType, { treeJson })
    ElMessage.success(t('save_success'))
    emit('saved')
  } catch (e) {
    ElMessage.error(t('save_failed'))
  } finally { saving.value = false }
}

function buildTreeJson() {
  // Placeholder — the actual save will be done by the designer host page
  return JSON.stringify([])
}
</script>

<style scoped lang="scss">
.property-panel {
  height: 100%;
  display: flex;
  flex-direction: column;
  font-size: 13px;
}
.empty-hint {
  padding: 20px;
  text-align: center;
  color: #999;
}
.panel-header {
  padding: 8px 12px;
  border-bottom: 1px solid #ebeef5;
  display: flex;
  justify-content: space-between;
  align-items: center;
  .field-name { font-weight: 600; }
}
.panel-tabs {
  flex: 1;
  overflow: hidden;
  :deep(.el-tabs__content) { height: calc(100% - 40px); overflow-y: auto; }
}
.tab-toolbar {
  padding: 4px 8px;
  border-bottom: 1px solid #ebeef5;
}
.prop-list {
  padding: 4px 0;
}
.prop-row {
  display: flex;
  align-items: center;
  padding: 3px 8px;
  gap: 4px;
  &:hover { background: #f5f7fa; }
  &.inactive { opacity: 0.5; }
  .prop-name {
    flex: 0 0 110px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-family: monospace;
    font-size: 12px;
  }
  .prop-value { flex: 1; min-width: 60px; }
  .mode-btn { padding: 2px 4px; min-width: 28px; font-size: 12px; }
}
.panel-footer {
  padding: 8px 12px;
  border-top: 1px solid #ebeef5;
  text-align: right;
}
</style>
