<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>新增模块实体</span>
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="120px"
        class="detail-form"
      >
        <!-- 实体基本信息 -->
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="所属项目" prop="projectId">
              <el-select v-model="form.projectId" :placeholder="$t2('please_select', 'project')" style="width: 100%" @change="handleProjectChange">
                <el-option
                  v-for="p in projectList"
                  :key="p.id"
                  :label="getProjectOptionLabel(p)"
                  :value="p.id"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="所属模块" prop="moduleId">
              <el-select v-model="form.moduleId" :placeholder="$t2('please_select', 'module')" style="width: 100%">
                <el-option
                  v-for="module in moduleList"
                  :key="module.id"
                  :label="module.moduleDescription"
                  :value="module.id"
                />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="实体名称" prop="name">
              <el-input v-model="form.name" placeholder="如：Customer" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="实体描述" prop="description">
              <el-input v-model="form.description" placeholder="如：客户" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="排序" prop="orderNum">
              <el-input-number v-model="form.orderNum" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="16">
            <el-form-item label="选项" prop="options">
              <el-checkbox v-model="form.hasPrimaryKey">主键</el-checkbox>
              <el-checkbox v-model="form.isTree">树形</el-checkbox>
              <el-checkbox v-model="form.hasTenant">多租户</el-checkbox>
              <el-checkbox v-model="form.hasDataPermission">数据权限</el-checkbox>
              <el-checkbox v-model="form.hasAudit">审计</el-checkbox>
              <el-checkbox v-model="form.hasSoftDelete">软删除</el-checkbox>
              <el-checkbox v-model="form.isChildTable">子表（不生成前端页面）</el-checkbox>
              <el-checkbox v-model="form.isReadOnly">只读</el-checkbox>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
    </el-card>

    <!-- 字段列表 -->
    <el-card shadow="never" style="margin-top: 20px">
      <template #header>
        <div class="card-header">
          <span>字段列表（基础信息）</span>
          <el-button type="primary" size="small" @click="handleAddField">新增字段</el-button>
        </div>
      </template>

      <el-table :data="displayFields" border style="width: 100%">
        <el-table-column prop="name" label="字段名" width="150" />
        <el-table-column prop="description" label="描述" width="150" />
        <el-table-column prop="dataType" label="数据类型" width="120" />
        <el-table-column prop="maxLength" label="最大长度" width="100" />
        <el-table-column prop="defaultValue" label="默认值" min-width="140" show-overflow-tooltip />
        <el-table-column prop="orderNum" label="排序" width="80" />
        <el-table-column label="操作" width="130" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" :disabled="row.isSystemField" @click="handleEditField(row)">编辑</el-button>
            <el-button link type="danger" size="small" :disabled="row.isSystemField" @click="handleDeleteField(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 提交按钮 -->
    <div style="margin-top: 20px; text-align: center">
      <el-button type="primary" @click="handleSubmit" :loading="submitLoading">保存</el-button>
      <el-button @click="handleBack">取消</el-button>
    </div>

    <!-- 字段弹框 -->
    <el-dialog
      v-model="fieldDialogVisible"
      :title="fieldDialogTitle"
      width="800px"
      @close="handleFieldDialogClose"
    >
      <el-form
        ref="fieldFormRef"
        :model="fieldForm"
        :rules="fieldRules"
        label-width="120px"
      >
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="字段名" prop="name">
              <el-input v-model="fieldForm.name" placeholder="如：CustomerName" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="描述" prop="description">
              <el-input v-model="fieldForm.description" placeholder="如：客户名称" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="数据类型" prop="dataType">
              <el-select v-model="fieldForm.dataType" :placeholder="$t2('please_select', '')" style="width: 100%">
                <el-option label="string" value="string" />
                <el-option label="int" value="int" />
                <el-option label="long" value="long" />
                <el-option label="decimal" value="decimal" />
                <el-option label="bool" value="bool" />
                <el-option label="DateTime" value="DateTime" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="最大长度" prop="maxLength">
              <el-input-number v-model="fieldForm.maxLength" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="默认值" prop="defaultValue">
              <el-input v-model="fieldForm.defaultValue" placeholder="可选" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="排序" prop="orderNum">
              <el-input-number v-model="fieldForm.orderNum" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

      </el-form>

      <template #footer>
        <el-button @click="fieldDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleFieldSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { t2 } from '@/i18n'
import { create } from '@/api/codegen/moduleEntity'
import { getByProjectId } from '@/api/codegen/projectModule'
import { getList as getProjectList } from '@/api/codegen/project'
import { useTagsViewStore } from '@/stores/tagsView'

const router = useRouter()
const route = useRoute()
const tagsViewStore = useTagsViewStore()
const formRef = ref(null)
const fieldFormRef = ref(null)
const submitLoading = ref(false)
const projectList = ref([])
const moduleList = ref([])

// 实体表单（不包含 fields，字段单独追踪）
const form = reactive({
  projectId: null,
  moduleId: null,
  name: '',
  description: '',
  tableName: '',
  hasPrimaryKey: true,
  isTree: false,
  isReadOnly: false,
  hasDataPermission: false,
  hasTenant: false,
  hasAudit: true,
  hasSoftDelete: true,
  isChildTable: false,
  orderNum: 0
})

const rules = {
  projectId: [{ required: true, message: t2('please_select', 'project'), trigger: 'change' }],
  moduleId: [{ required: true, message: t2('please_select', 'module'), trigger: 'change' }],
  name: [{ required: true, message: t2('please_input', 'entity_name'), trigger: 'blur' }],
  description: [{ required: true, message: t2('please_input', 'entity_description'), trigger: 'blur' }]
}

// ===== 系统字段定义 =====
const systemFieldDefs = {
  hasPrimaryKey: [
    { name: 'Id', description: '主键', dataType: 'long', isPrimaryKey: true, isRequired: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ],
  isTree: [
    { name: 'ParentId', description: '父级ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: true, showInEditForm: true, showInDetail: false, showInSearch: false },
    { name: 'Ancestors', description: '祖级列表', dataType: 'string?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ],
  hasTenant: [
    { name: 'TenantId', description: '租户ID', dataType: 'long', isRequired: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ],
  hasDataPermission: [
    { name: 'DeptId', description: '部门ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'DeptAncestors', description: '部门祖级', dataType: 'string?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'CreateUserId', description: '创建人ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ],
  hasAudit: [
    { name: 'CreateUserId', description: '创建人ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'CreateBy', description: '创建人', dataType: 'string?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'CreateTime', description: '创建时间', dataType: 'DateTime', isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'UpdateUserId', description: '更新人ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'UpdateBy', description: '更新人', dataType: 'string?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'UpdateTime', description: '更新时间', dataType: 'DateTime?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ],
  hasSoftDelete: [
    { name: 'IsDeleted', description: '是否删除', dataType: 'bool', isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'DeleteTime', description: '删除时间', dataType: 'DateTime?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'DeleteBy', description: '删除人', dataType: 'string?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'DeleteUserId', description: '删除人ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ]
}

// ===== 字段追踪（全部视为新增状态） =====
const allFields = ref([])

const displayFields = computed(() => {
  return [...allFields.value].sort((a, b) => (a.orderNum || 0) - (b.orderNum || 0))
})

// ===== 默认字段表单 =====
function getDefaultFieldForm() {
  return {
    name: '', description: '', dataType: 'string', maxLength: 0, defaultValue: '',
    formControlType: 'input', isPrimaryKey: false, isRequired: false,
    isNullable: false, isIgnore: false, isSystemField: false, isMultiple: false,
    isUnique: false, isIndexed: false, isSearchable: false, isSortable: false,
    showInList: true, showInDetail: true, showInAddForm: true, showInEditForm: true, showInSearch: false,
    selectDataSource: null, selectOptions: null,
    relatedEntityName: null, relatedEntityIdField: null, relatedEntityDisplayFields: null, resultMappings: null,
    listWidth: null, orderNum: 0,
    fieldCategory: 'Normal', formula: null, aggregateType: null,
    aggregateChildEntityId: null, aggregateChildFieldName: null, aggregateSeparator: null
  }
}

// ===== 字段弹框 =====
const fieldDialogVisible = ref(false)
const fieldDialogTitle = ref('新增字段')
const editingFieldIndex = ref(-1)

const fieldForm = reactive(getDefaultFieldForm())

const fieldRules = {
  name: [{ required: true, message: t2('please_input', 'field_name'), trigger: 'blur' }],
  description: [{ required: true, message: t2('please_input', 'description'), trigger: 'blur' }],
  dataType: [{ required: true, message: t2('please_select', 'data_type'), trigger: 'change' }]
}

// ===== 勾选/取消勾选自动字段管理 =====
function addSystemFields(configKey) {
  const defs = systemFieldDefs[configKey] || []
  defs.forEach(def => {
    const exists = allFields.value.some(f => f.name === def.name)
    if (!exists) {
      allFields.value.push({ ...getDefaultFieldForm(), ...def, orderNum: allFields.value.length })
    }
  })
}

// 获取其他已勾选选项所需的字段名集合（防止误删共享字段如 CreateUserId）
function getFieldNamesNeededByOtherConfigs(currentKey) {
  const allKeys = ['hasPrimaryKey', 'isTree', 'hasTenant', 'hasDataPermission', 'hasAudit', 'hasSoftDelete']
  const names = new Set()
  allKeys.forEach(key => {
    if (key !== currentKey && form[key]) {
      (systemFieldDefs[key] || []).forEach(d => names.add(d.name))
    }
  })
  return names
}

function removeSystemFields(configKey) {
  const defs = systemFieldDefs[configKey] || []
  const allNames = defs.map(d => d.name)
  // 过滤掉其他已勾选选项仍需要共享的字段
  const stillNeeded = getFieldNamesNeededByOtherConfigs(configKey)
  const namesToRemove = allNames.filter(n => !stillNeeded.has(n))
  if (namesToRemove.length === 0) return
  allFields.value = allFields.value.filter(f => !namesToRemove.includes(f.name) || !f.isSystemField)
}

watch(() => form.hasPrimaryKey, (val, old) => { if (old === undefined) return; val ? addSystemFields('hasPrimaryKey') : removeSystemFields('hasPrimaryKey') })
watch(() => form.isTree, (val, old) => { if (old === undefined) return; val ? addSystemFields('isTree') : removeSystemFields('isTree') })
watch(() => form.hasTenant, (val, old) => { if (old === undefined) return; val ? addSystemFields('hasTenant') : removeSystemFields('hasTenant') })
watch(() => form.hasDataPermission, (val, old) => { if (old === undefined) return; val ? addSystemFields('hasDataPermission') : removeSystemFields('hasDataPermission') })
watch(() => form.hasAudit, (val, old) => { if (old === undefined) return; val ? addSystemFields('hasAudit') : removeSystemFields('hasAudit') })
watch(() => form.hasSoftDelete, (val, old) => { if (old === undefined) return; val ? addSystemFields('hasSoftDelete') : removeSystemFields('hasSoftDelete') })

// 能力组合约束：无主键只能作为只读查询模型；树形实体必须有主键。
watch(() => form.hasPrimaryKey, (val) => {
  if (!val) {
    form.isTree = false
    form.isReadOnly = true
  }
})
watch(() => form.isTree, (val) => { if (val) form.hasPrimaryKey = true })
watch(() => form.isReadOnly, (val) => { if (!val && !form.hasPrimaryKey) form.hasPrimaryKey = true })

// ===== 生命周期 =====
onMounted(() => {
  loadProjectList()
  // 初始化默认勾选的系统字段
  if (form.hasPrimaryKey) addSystemFields('hasPrimaryKey')
  if (form.hasAudit) addSystemFields('hasAudit')
  if (form.hasSoftDelete) addSystemFields('hasSoftDelete')
})

async function loadProjectList() {
  try {
    const res = await getProjectList()
    projectList.value = Array.isArray(res) ? res : (res?.items || [])
  } catch (error) {
    console.error('加载项目列表失败:', error)
  }
}

function getProjectOptionLabel(project) {
  return project?.description || project?.displayName || '未设置项目描述'
}

async function loadModuleList() {
  if (!form.projectId) {
    moduleList.value = []
    return
  }
  try {
    const res = await getByProjectId(form.projectId)
    moduleList.value = Array.isArray(res) ? res : []
  } catch (error) {
    console.error('加载模块列表失败:', error)
  }
}

function handleProjectChange() {
  form.moduleId = null
  loadModuleList()
}

// ===== 字段操作 =====
function handleAddField() {
  fieldDialogTitle.value = '新增字段'
  editingFieldIndex.value = -1
  resetFieldForm()
  const orders = allFields.value.map(field => Number(field.orderNum || 0))
  fieldForm.orderNum = orders.length > 0 ? Math.max(...orders) + 10 : 10
  fieldDialogVisible.value = true
}

function handleEditField(row) {
  fieldDialogTitle.value = '编辑字段'
  editingFieldIndex.value = allFields.value.findIndex(f => f === row)
  resetFieldForm()
  Object.assign(fieldForm, row)
  fieldDialogVisible.value = true
}

function handleDeleteField(row) {
  const idx = allFields.value.findIndex(f => f === row)
  if (idx >= 0) allFields.value.splice(idx, 1)
}

// 字段弹框提交
async function handleFieldSubmit() {
  try {
    await fieldFormRef.value.validate()

    const data = { ...fieldForm }

    const sameCount = allFields.value.filter(f => f.name === data.name).length
    const maxOk = editingFieldIndex.value >= 0 ? 1 : 0
    if (data.name && sameCount > maxOk) {
      ElMessage.warning(`字段名「${data.name}」已存在，不能重复添加`)
      return
    }

    if (editingFieldIndex.value >= 0) {
      // 编辑模式：更新已有字段
      allFields.value.splice(editingFieldIndex.value, 1, data)
      ElMessage.success('修改成功')
    } else {
      allFields.value.push(data)
      ElMessage.success('添加成功')
    }

    fieldDialogVisible.value = false
  } catch (error) {
    if (error !== false) {
      console.error(error)
    }
  }
}

function handleFieldDialogClose() {
  resetFieldForm()
}

function resetFieldForm() {
  Object.assign(fieldForm, getDefaultFieldForm())
  fieldFormRef.value?.clearValidate()
}

// ===== 提交 =====
async function handleSubmit() {
  try {
    await formRef.value.validate()

    const activeFields = allFields.value
    if (activeFields.length === 0) {
      ElMessage.warning('请至少添加一个字段')
      return
    }

    submitLoading.value = true
    const submitData = { ...form, fields: activeFields }
    const entityId = await create(submitData)
    ElMessage.success('新增成功，请继续配置字段')
    await tagsViewStore.delView(route)
    await router.replace({
      path: '/codegen/moduleentity/edit',
      query: { id: entityId }
    })
  } catch (error) {
    if (error !== false) {
      ElMessage.error('新增失败')
      console.error(error)
    }
  } finally {
    submitLoading.value = false
  }
}

function handleBack() {
  router.back()
}
</script>

<style scoped lang="scss">
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
