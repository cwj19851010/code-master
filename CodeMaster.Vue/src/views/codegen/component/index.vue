<template>
  <div class="app-container">
    <el-row :gutter="16">
      <!-- 左侧：分组树 -->
      <el-col :span="5">
        <el-card shadow="never" class="mb-20">
          <template #header>
            <div class="card-header">
              <span>{{ t('componentGroup') }}</span>
              <el-button type="primary" link :icon="Plus" @click="handleAddGroup" v-permission="'codegen:component:create'">
                {{ t('add') }}
              </el-button>
            </div>
          </template>
          <el-menu :default-active="activeGroupId?.toString()" @select="handleGroupSelect">
            <el-menu-item v-for="group in groupList" :key="group.id" :index="group.id.toString()">
              <el-icon><component :is="group.icon || 'Grid'" /></el-icon>
              <span>{{ group.groupName }}</span>
              <el-tag size="small" class="ml-5">{{ group.componentCount }}</el-tag>
            </el-menu-item>
          </el-menu>
        </el-card>
      </el-col>

      <!-- 右侧：组件列表 -->
      <el-col :span="19">
        <el-card shadow="never">
          <template #header>
            <div class="card-header">
              <span>{{ activeGroup?.groupName || t('component') }}</span>
              <el-button type="primary" :icon="Plus" @click="handleAddComponent" v-permission="'codegen:component:create'" :disabled="!activeGroupId">
                {{ t('addComponent') }}
              </el-button>
            </div>
          </template>

          <el-table v-loading="loading" :data="componentList" border stripe row-key="id">
            <el-table-column prop="name" :label="t('componentName')" width="160" />
            <el-table-column prop="tag" :label="t('componentTag')" width="140">
              <template #default="{ row }"><el-tag>{{ row.tag }}</el-tag></template>
            </el-table-column>
            <el-table-column :label="t('componentProps')" width="90" align="center">
              <template #default="{ row }">{{ row.propertyCount }}</template>
            </el-table-column>
            <el-table-column :label="t('componentSlots')" width="90" align="center">
              <template #default="{ row }">{{ row.slotCount }}</template>
            </el-table-column>
            <el-table-column :label="t('componentEvents')" width="90" align="center">
              <template #default="{ row }">{{ row.eventCount }}</template>
            </el-table-column>
            <el-table-column :label="t('componentExposes')" width="90" align="center">
              <template #default="{ row }">{{ row.exposeCount }}</template>
            </el-table-column>
            <el-table-column :label="t('action')" fixed="right" width="200">
              <template #default="{ row }">
                <el-button type="primary" link :icon="View" @click="handleViewDetail(row)" v-permission="'codegen:component:view'">{{ t('view') }}</el-button>
                <el-button type="success" link :icon="Edit" @click="handleEditComponent(row)" v-permission="'codegen:component:update'">{{ t('edit') }}</el-button>
                <el-button type="danger" link :icon="Delete" @click="handleDeleteComponent(row)" v-permission="'codegen:component:delete'">{{ t('delete') }}</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>

        <!-- 组件详情对话框 -->
        <el-dialog v-model="detailVisible" :title="detailComponent?.name + ' - ' + t('componentDetail')" width="90%" top="3vh" destroy-on-close>
          <el-tabs v-model="activeTab" v-if="detailData">
            <el-tab-pane :label="t('componentProps') + ` (${detailData.properties?.length || 0})`" name="props">
              <el-table :data="detailData.properties" border stripe max-height="500">
                <el-table-column prop="propName" :label="t('propName')" width="160" fixed />
                <el-table-column prop="propType" :label="t('propType')" width="100" />
                <el-table-column prop="typeDescription" :label="t('propType') + ' ' + t('detail')" min-width="220" show-overflow-tooltip />
                <el-table-column prop="defaultValue" :label="t('propDefault')" width="120" />
                <el-table-column prop="enumValues" :label="t('enumValues')" width="120" show-overflow-tooltip />
                <el-table-column prop="description" :label="t('propDescription')" min-width="150" show-overflow-tooltip />
                <el-table-column :label="t('isCommon')" width="80" align="center">
                  <template #default="{ row: propRow }">
                    <el-switch :model-value="propRow.isCommon" @change="(val) => handleTogglePropCommon(propRow, val)" size="small" />
                  </template>
                </el-table-column>
                <el-table-column :label="t('isAdvanced')" width="80" align="center">
                  <template #default="{ row: propRow }">
                    <el-switch :model-value="propRow.isAdvanced" @change="(val) => handleTogglePropAdvanced(propRow, val)" size="small" />
                  </template>
                </el-table-column>
                <el-table-column :label="t('action')" width="120">
                  <template #default="{ row: propRow }">
                    <el-button type="primary" link :icon="Edit" @click="handleEditProperty(propRow)">{{ t('edit') }}</el-button>
                    <el-button type="danger" link :icon="Delete" @click="handleDeleteProperty(propRow)">{{ t('delete') }}</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </el-tab-pane>

            <el-tab-pane :label="t('componentEvents') + ` (${detailData.events?.length || 0})`" name="events">
              <el-table :data="detailData.events" border stripe max-height="500">
                <el-table-column prop="eventName" :label="t('eventName')" width="160" fixed />
                <el-table-column prop="description" :label="t('description')" min-width="200" show-overflow-tooltip />
                <el-table-column prop="eventType" :label="t('propType')" width="100" />
                <el-table-column prop="typeDescription" :label="t('propType') + ' ' + t('detail')" min-width="300" show-overflow-tooltip />
                <el-table-column :label="t('isCommon')" width="80" align="center">
                  <template #default="{ row: evtRow }">
                    <el-switch :model-value="evtRow.isCommon" @change="(val) => handleToggleEventCommon(evtRow, val)" size="small" />
                  </template>
                </el-table-column>
                <el-table-column :label="t('isSingle')" width="80" align="center">
                  <template #default="{ row: evtRow }">
                    <el-switch :model-value="evtRow.isSingle" @change="(val) => handleToggleEventSingle(evtRow, val)" size="small" />
                  </template>
                </el-table-column>
              </el-table>
            </el-tab-pane>

            <el-tab-pane :label="t('componentSlots') + ` (${detailData.slots?.length || 0})`" name="slots">
              <el-table :data="detailData.slots" border stripe max-height="500">
                <el-table-column prop="slotName" :label="t('slotName')" width="160" fixed />
                <el-table-column prop="description" :label="t('description')" min-width="200" show-overflow-tooltip />
                <el-table-column prop="slotType" :label="t('propType')" width="100" />
                <el-table-column prop="typeDescription" :label="t('propType') + ' ' + t('detail')" min-width="250" show-overflow-tooltip />
                <el-table-column :label="t('isCommon')" width="80" align="center">
                  <template #default="{ row: slotRow }">
                    <el-switch :model-value="slotRow.isCommon" @change="(val) => handleToggleSlotCommon(slotRow, val)" size="small" />
                  </template>
                </el-table-column>
              </el-table>
            </el-tab-pane>

            <el-tab-pane :label="t('componentExposes') + ` (${detailData.exposes?.length || 0})`" name="exposes">
              <el-table :data="detailData.exposes" border stripe max-height="500">
                <el-table-column prop="exposeName" :label="t('exposeName')" width="160" fixed />
                <el-table-column prop="description" :label="t('description')" min-width="200" show-overflow-tooltip />
                <el-table-column prop="exposeType" :label="t('propType')" width="100" />
                <el-table-column prop="typeDescription" :label="t('propType') + ' ' + t('detail')" min-width="250" show-overflow-tooltip />
                <el-table-column :label="t('isCommon')" width="80" align="center">
                  <template #default="{ row: expRow }">
                    <el-switch :model-value="expRow.isCommon" @change="(val) => handleToggleExposeCommon(expRow, val)" size="small" />
                  </template>
                </el-table-column>
              </el-table>
            </el-tab-pane>
          </el-tabs>
        </el-dialog>

        <!-- 分组编辑 -->
        <el-dialog v-model="groupDialogVisible" :title="editingGroup?.id ? t('edit') : t('add')" width="500px" destroy-on-close>
          <el-form ref="groupFormRef" :model="groupForm" label-width="100px">
            <el-form-item :label="t('name')"><el-input v-model="groupForm.groupName" /></el-form-item>
            <el-form-item :label="t('code')"><el-input v-model="groupForm.groupCode" /></el-form-item>
            <el-form-item :label="t('icon')"><el-input v-model="groupForm.icon" /></el-form-item>
            <el-form-item :label="t('sort')"><el-input-number v-model="groupForm.sort" :min="0" :max="999" /></el-form-item>
          </el-form>
          <template #footer>
            <el-button @click="groupDialogVisible = false">{{ t('cancel') }}</el-button>
            <el-button type="primary" @click="handleSaveGroup">{{ t('confirm') }}</el-button>
          </template>
        </el-dialog>

        <!-- 组件新增/编辑 -->
        <el-dialog v-model="compDialogVisible" :title="editingComponent?.id ? t('editComponent') : t('addComponent')" width="550px" destroy-on-close>
          <el-form ref="compFormRef" :model="compForm" label-width="100px">
            <el-form-item :label="t('componentName')" prop="name"><el-input v-model="compForm.name" placeholder="kebab-case, e.g. date-picker" /></el-form-item>
            <el-form-item :label="t('componentTag')" prop="tag"><el-input v-model="compForm.tag" placeholder="e.g. el-date-picker" /></el-form-item>
            <el-form-item :label="t('link')" prop="link"><el-input v-model="compForm.link" /></el-form-item>
            <el-form-item :label="t('sort')"><el-input-number v-model="compForm.sort" :min="0" :max="999" /></el-form-item>
          </el-form>
          <template #footer>
            <el-button @click="compDialogVisible = false">{{ t('cancel') }}</el-button>
            <el-button type="primary" @click="handleSaveComponent">{{ t('confirm') }}</el-button>
          </template>
        </el-dialog>

        <!-- 属性编辑 -->
        <el-dialog v-model="propDialogVisible" :title="t('edit') + ' ' + t('propName')" width="600px" destroy-on-close>
          <el-form :model="propForm" label-width="100px">
            <el-form-item :label="t('propName')"><el-input v-model="propForm.propName" /></el-form-item>
            <el-form-item :label="t('propType')"><el-input v-model="propForm.propType" /></el-form-item>
            <el-form-item :label="t('propDefault')"><el-input v-model="propForm.defaultValue" /></el-form-item>
            <el-form-item :label="t('enumValues')"><el-input v-model="propForm.enumValues" placeholder="逗号分隔，如 large,default,small" /></el-form-item>
            <el-form-item :label="t('isCommon')"><el-switch v-model="propForm.isCommon" /></el-form-item>
            <el-form-item :label="t('isAdvanced')"><el-switch v-model="propForm.isAdvanced" /></el-form-item>
            <el-form-item :label="t('description')"><el-input v-model="propForm.description" type="textarea" :rows="2" /></el-form-item>
            <el-form-item :label="t('propType') + ' ' + t('detail')"><el-input v-model="propForm.typeDescription" type="textarea" :rows="3" /></el-form-item>
          </el-form>
          <template #footer>
            <el-button @click="propDialogVisible = false">{{ t('cancel') }}</el-button>
            <el-button type="primary" @click="handleSaveProperty">{{ t('confirm') }}</el-button>
          </template>
        </el-dialog>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, View } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import {
  getGroupList, createGroup, updateGroup, deleteGroup,
  getComponentListByGroup, getComponentDetail, createComponent, updateComponent, deleteComponent,
  updateProperty, deleteProperty, setPropertyCommon,
  updateEvent, setEventCommon,
  updateSlot, updateExpose
} from '@/api/system/component'

defineOptions({ name: 'SystemComponent' })
const { t } = useI18n()

// ==================== 分组 ====================
const groupList = ref([])
const activeGroupId = ref(null)
const activeGroup = computed(() => groupList.value.find(g => g.id === activeGroupId.value))
const groupDialogVisible = ref(false)
const editingGroup = ref(null)
const groupForm = reactive({ groupName: '', groupCode: '', icon: 'Grid', sort: 0 })

const loadGroups = async () => {
  try {
    const res = await getGroupList()
    groupList.value = res || []
    if (groupList.value.length > 0 && !activeGroupId.value) {
      activeGroupId.value = groupList.value[0].id
      loadComponents()
    }
  } catch (e) { console.error('加载分组失败', e) }
}

const handleGroupSelect = (id) => { activeGroupId.value = Number(id); loadComponents() }
const handleAddGroup = () => {
  editingGroup.value = null
  Object.assign(groupForm, { groupName: '', groupCode: '', icon: 'Grid', sort: 0 })
  groupDialogVisible.value = true
}
const handleSaveGroup = async () => {
  try {
    if (editingGroup.value?.id) await updateGroup(editingGroup.value.id, groupForm)
    else await createGroup(groupForm)
    ElMessage.success(t('save_success'))
    groupDialogVisible.value = false
    loadGroups()
  } catch { ElMessage.error(t('save_failed')) }
}

// ==================== 组件 ====================
const loading = ref(false)
const componentList = ref([])
const compDialogVisible = ref(false)
const editingComponent = ref(null)
const compForm = reactive({ name: '', tag: '', link: '', sort: 0 })

const loadComponents = async () => {
  if (!activeGroupId.value) return
  loading.value = true
  try { componentList.value = await getComponentListByGroup(activeGroupId.value) || [] }
  catch (e) { console.error('加载组件失败', e) }
  finally { loading.value = false }
}

const handleAddComponent = () => {
  editingComponent.value = null
  Object.assign(compForm, { name: '', tag: '', link: '', sort: 0 })
  compDialogVisible.value = true
}

const handleEditComponent = (row) => {
  editingComponent.value = row
  Object.assign(compForm, { name: row.name, tag: row.tag, link: row.link, sort: row.sort })
  compDialogVisible.value = true
}

const handleSaveComponent = async () => {
  try {
    if (editingComponent.value?.id) {
      await updateComponent(editingComponent.value.id, compForm)
    } else {
      await createComponent({ ...compForm, groupId: activeGroupId.value, status: 0 })
    }
    ElMessage.success(t('save_success'))
    compDialogVisible.value = false
    loadComponents()
  } catch { ElMessage.error(t('save_failed')) }
}

const handleDeleteComponent = async (row) => {
  try {
    await ElMessageBox.confirm(t('delete_confirm'), t('prompt'), { type: 'warning' })
    await deleteComponent(row.id)
    ElMessage.success(t('delete_success'))
    loadComponents()
  } catch (e) { if (e !== 'cancel') ElMessage.error(t('delete_failed')) }
}

// ==================== 详情 ====================
const detailVisible = ref(false)
const detailData = ref(null)
const detailComponent = ref(null)
const activeTab = ref('props')

const handleViewDetail = async (row) => {
  try {
    detailData.value = await getComponentDetail(row.id)
    detailComponent.value = row
    activeTab.value = 'props'
    detailVisible.value = true
  } catch { ElMessage.error(t('load_failed')) }
}

// ==================== 属性 ====================
const propDialogVisible = ref(false)
const editingProp = ref(null)
const propForm = reactive({ propName: '', propType: '', typeDescription: '', defaultValue: '', description: '', isCommon: false, enumValues: '', isAdvanced: false })

const handleTogglePropCommon = async (propRow, val) => {
  try {
    await setPropertyCommon({ id: propRow.id, isCommon: val })
    propRow.isCommon = val
    ElMessage.success(val ? t('setCommon') : t('cancelCommon'))
  } catch { ElMessage.error(t('save_failed')) }
}

const handleEditProperty = (propRow) => {
  editingProp.value = propRow
  Object.assign(propForm, {
    propName: propRow.propName, propType: propRow.propType || '',
    typeDescription: propRow.typeDescription || '', defaultValue: propRow.defaultValue || '',
    description: propRow.description || '', isCommon: propRow.isCommon,
    enumValues: propRow.enumValues || '', isAdvanced: propRow.isAdvanced
  })
  propDialogVisible.value = true
}

const handleSaveProperty = async () => {
  try {
    await updateProperty(editingProp.value.id, propForm)
    ElMessage.success(t('save_success'))
    propDialogVisible.value = false
    handleViewDetail(detailComponent.value)
  } catch { ElMessage.error(t('save_failed')) }
}

const handleDeleteProperty = async (propRow) => {
  try {
    await ElMessageBox.confirm(t('delete_confirm'), t('prompt'), { type: 'warning' })
    await deleteProperty(propRow.id)
    ElMessage.success(t('delete_success'))
    handleViewDetail(detailComponent.value)
  } catch (e) { if (e !== 'cancel') ElMessage.error(t('delete_failed')) }
}

const handleTogglePropAdvanced = async (propRow, val) => {
  try {
    await updateProperty(propRow.id, { isAdvanced: val })
    propRow.isAdvanced = val
    ElMessage.success(val ? t('setAdvanced') : t('cancelAdvanced'))
  } catch { ElMessage.error(t('save_failed')) }
}

const handleToggleEventCommon = async (evtRow, val) => {
  try {
    await setEventCommon({ id: evtRow.id, isCommon: val })
    evtRow.isCommon = val
    ElMessage.success(val ? t('setCommon') : t('cancelCommon'))
  } catch { ElMessage.error(t('save_failed')) }
}

const handleToggleEventSingle = async (evtRow, val) => {
  try {
    await updateEvent(evtRow.id, { isSingle: val })
    evtRow.isSingle = val
    ElMessage.success(t('save_success'))
  } catch { ElMessage.error(t('save_failed')) }
}

const handleToggleSlotCommon = async (slotRow, val) => {
  try {
    await updateSlot(slotRow.id, { isCommon: val })
    slotRow.isCommon = val
    ElMessage.success(val ? t('setCommon') : t('cancelCommon'))
  } catch { ElMessage.error(t('save_failed')) }
}

const handleToggleExposeCommon = async (expRow, val) => {
  try {
    await updateExpose(expRow.id, { isCommon: val })
    expRow.isCommon = val
    ElMessage.success(val ? t('setCommon') : t('cancelCommon'))
  } catch { ElMessage.error(t('save_failed')) }
}

onMounted(() => { loadGroups() })

refreshOnReactivated(loadGroups)
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';
.card-header { display: flex; justify-content: space-between; align-items: center; }
.ml-5 { margin-left: 5px; }
</style>
