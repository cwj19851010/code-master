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
          <span>字段列表</span>
          <el-button type="primary" size="small" @click="handleAddField">新增字段</el-button>
        </div>
      </template>

      <!-- 字段筛选 -->
      <el-row :gutter="12" style="margin-bottom: 12px">
        <el-col :span="4">
          <el-select v-model="filterDeleted" placeholder="是否已删除" clearable style="width: 100%" size="small">
            <el-option label="全部" :value="null" />
            <el-option label="正常" :value="false" />
            <el-option label="已删除" :value="true" />
          </el-select>
        </el-col>
        <el-col :span="4">
          <el-select v-model="filterSystem" placeholder="是否系统" clearable style="width: 100%" size="small">
            <el-option label="全部" :value="null" />
            <el-option label="系统字段" :value="true" />
            <el-option label="自定义字段" :value="false" />
          </el-select>
        </el-col>
        <el-col :span="4">
          <el-input v-model="filterName" placeholder="字段名" clearable size="small" />
        </el-col>
        <el-col :span="4">
          <el-input v-model="filterDesc" placeholder="描述" clearable size="small" />
        </el-col>
      </el-row>

      <el-table :data="displayFields" border style="width: 100%">
        <el-table-column prop="name" label="字段名" width="150" />
        <el-table-column prop="description" label="描述" width="150" />
        <el-table-column prop="dataType" label="数据类型" width="120" />
        <el-table-column prop="maxLength" label="长度" width="80" />
        <el-table-column label="选项" width="200">
          <template #default="{ row }">
            <el-tag v-if="row.isPrimaryKey" size="small" type="danger">主键</el-tag>
            <el-tag v-if="row.isRequired" size="small" type="warning">必填</el-tag>
            <el-tag v-if="row.isUnique" size="small" type="info">唯一</el-tag>
            <el-tag v-if="row.isIndexed" size="small">索引</el-tag>
            <el-tag v-if="row.isSystemField" size="small" type="info">系统</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag v-if="row._deleted" size="small" type="danger">已删除</el-tag>
            <el-tag v-else size="small" type="success">新增</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="orderNum" label="排序" width="80" />
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row, $index }">
            <el-button link type="primary" size="small" @click="handleEditField(row, $index)" :disabled="row._deleted">编辑</el-button>
            <el-button v-if="!row._deleted" link type="danger" size="small" @click="handleDeleteField(row, $index)">删除</el-button>
            <el-button v-else link type="success" size="small" @click="handleRestoreField(row, $index)">恢复</el-button>
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

        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="选项">
              <el-checkbox v-model="fieldForm.isPrimaryKey">主键</el-checkbox>
              <el-checkbox v-model="fieldForm.isRequired">必填</el-checkbox>
              <el-checkbox v-model="fieldForm.isUnique">唯一</el-checkbox>
              <el-checkbox v-model="fieldForm.isIndexed">索引</el-checkbox>
              <el-checkbox v-model="fieldForm.isSearchable">可搜索</el-checkbox>
              <el-checkbox v-model="fieldForm.isSortable">可排序</el-checkbox>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="最小值">
              <el-input v-model="fieldForm.minValue" placeholder="如 0" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="最大值">
              <el-input v-model="fieldForm.maxValue" placeholder="如 100" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="验证类型">
              <el-checkbox v-model="fieldForm.isEmail" style="margin-right:12px">邮箱</el-checkbox>
              <el-checkbox v-model="fieldForm.isPhone">手机号</el-checkbox>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="16">
            <el-form-item label="正则表达式">
              <el-input v-model="fieldForm.regexPattern" placeholder="如 ^[A-Z]{3}-\d{4}$" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="控件类型">
              <el-select v-model="fieldForm.formControlType" placeholder="默认input" clearable style="width: 100%">
                <el-option label="input" value="input" />
                <el-option label="textarea" value="textarea" />
                <el-option label="number" value="number" />
                <el-option label="select" value="select" />
                <el-option label="switch" value="switch" />
                <el-option label="radio-group" value="radio-group" />
                <el-option label="checkbox" value="checkbox" />
                <el-option label="date" value="date" />
                <el-option label="datetime" value="datetime" />
                <el-option label="图片上传" value="image" />
                <el-option label="富文本" value="editor" />
                <el-option label="附件上传" value="file" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="数据来源">
              <el-select v-model="fieldForm.selectDataSource" placeholder="无" clearable style="width: 100%">
                <el-option label="无" :value="null" />
                <el-option label="字典" value="dict" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20" v-if="fieldForm.selectDataSource === 'dict'">
          <el-col :span="12">
            <el-form-item label="字典类型">
              <el-select v-model="fieldForm.selectOptions" placeholder="选择字典类型" style="width: 100%">
                <el-option v-for="item in dictTypeList" :key="item.dictType" :label="`${item.dictName} (${item.dictType})`" :value="item.dictType" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 字段类别 -->
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="字段类别">
              <el-select v-model="fieldForm.fieldCategory" style="width: 100%">
                <el-option label="普通字段" value="Normal" />
                <el-option label="计算字段" value="Computed" />
                <el-option label="统计字段" value="Aggregate" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <template v-if="fieldForm.fieldCategory === 'Computed'">
          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item label="计算公式">
                <el-input v-model="fieldForm.formula" placeholder="如 [Price]*[Quantity]" style="width: 100%" />
                <div style="color: #999; font-size: 12px; margin-top: 4px">用 [字段名] 引用同表字段</div>
              </el-form-item>
            </el-col>
          </el-row>
        </template>
        <template v-if="fieldForm.fieldCategory === 'Aggregate'">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="统计类型">
                <el-select v-model="fieldForm.aggregateType" style="width: 100%">
                  <el-option label="数值累加" value="Sum" />
                  <el-option label="平均值" value="Avg" />
                  <el-option label="字符串拼接" value="Concat" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="统计子表">
                <el-select v-model="fieldForm.aggregateChildEntityId" style="width: 100%" @change="handleAggChildEntityChange">
                  <el-option v-for="r in displayRelations" :key="r.childEntityId" :label="r.childEntityName" :value="r.childEntityId" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="子表字段">
                <el-select v-model="fieldForm.aggregateChildFieldName" style="width: 100%">
                  <el-option v-for="f in aggChildFieldOptions" :key="f.name" :label="`${f.name}（${f.description}）`" :value="f.name" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20" v-if="fieldForm.aggregateType === 'Concat'">
            <el-col :span="8">
              <el-form-item label="分隔符">
                <el-input v-model="fieldForm.aggregateSeparator" placeholder="如 , " />
              </el-form-item>
            </el-col>
          </el-row>
        </template>
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
import { resetReactiveForm, resetFormValidation } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { t2 } from '@/i18n'
import { create, getById } from '@/api/codegen/moduleEntity'
import { getByProjectId } from '@/api/codegen/projectModule'
import { getList as getProjectList, getDictTypes } from '@/api/codegen/project'

const router = useRouter()
const formRef = ref(null)
const fieldFormRef = ref(null)
const submitLoading = ref(false)
const projectList = ref([])
const moduleList = ref([])
const dictTypeList = ref([])

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

const initialForm = JSON.parse(JSON.stringify(form))

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
    { name: 'ParentId', description: '父级ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: true, showInEditForm: true, showInDetail: false, showInSearch: false }
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

// ===== 字段筛选 =====
const filterDeleted = ref(null)
const filterSystem = ref(null)
const filterName = ref('')
const filterDesc = ref('')

const displayFields = computed(() => {
  let list = [...allFields.value]
  if (filterDeleted.value != null) {
    list = list.filter(f => (filterDeleted.value ? f._deleted : !f._deleted))
  }
  if (filterSystem.value != null) {
    list = list.filter(f => f.isSystemField === filterSystem.value)
  }
  if (filterName.value) {
    const kw = filterName.value.toLowerCase()
    list = list.filter(f => (f.name || '').toLowerCase().includes(kw))
  }
  if (filterDesc.value) {
    const kw = filterDesc.value.toLowerCase()
    list = list.filter(f => (f.description || '').toLowerCase().includes(kw))
  }
  return list.sort((a, b) => (a.orderNum || 0) - (b.orderNum || 0))
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
    relatedEntityName: null, relatedEntityIdField: null, relatedEntityDisplayFields: null,
    listWidth: null, orderNum: 0,
    fieldCategory: 'Normal', formula: null, aggregateType: null,
    aggregateChildEntityId: null, aggregateChildFieldName: null, aggregateSeparator: null
  }
}

// ===== 字段弹框 =====
const fieldDialogVisible = ref(false)
const aggChildFieldOptions = ref([])
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

// 数据源切到字典时加载字典类型
watch(() => fieldForm.selectDataSource, (val) => { if (val === 'dict') loadDictTypes() })

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
  dictTypeList.value = []
}

async function loadDictTypes() {
  if (!form.projectId) return
  try {
    dictTypeList.value = await getDictTypes(form.projectId)
  } catch (e) {
    console.error('加载字典类型失败:', e)
  }
}

// ===== 字段操作 =====
function handleAddField() {
  fieldDialogTitle.value = '新增字段'
  editingFieldIndex.value = -1
  resetFieldForm()
  fieldDialogVisible.value = true
}

function handleEditField(row, index) {
  fieldDialogTitle.value = '编辑字段'
  editingFieldIndex.value = allFields.value.findIndex(f => f === row)
  resetFieldForm()
  Object.assign(fieldForm, row)
  fieldDialogVisible.value = true
}

function handleDeleteField(row, index) {
  // 软删除：标记为已删除
  const idx = allFields.value.findIndex(f => f === row)
  if (idx >= 0) {
    allFields.value[idx]._deleted = true
  }
}

function handleRestoreField(row, index) {
  // 恢复已删除字段
  const idx = allFields.value.findIndex(f => f === row)
  if (idx >= 0) {
    allFields.value[idx]._deleted = false
  }
}

async function handleAggChildEntityChange(entityId) {
  aggChildFieldOptions.value = []
  if (!entityId) return
  try {
    const data = await getById(entityId)
    aggChildFieldOptions.value = (data.fields || []).filter(f => !f.isSystemField)
  } catch (e) { console.error(e) }
}

// 字段弹框提交
async function handleFieldSubmit() {
  try {
    await fieldFormRef.value.validate()

    const data = { ...fieldForm }
    // 确保 _deleted 标记不被覆盖
    if (editingFieldIndex.value >= 0) {
      const existingField = allFields.value[editingFieldIndex.value]
      data._deleted = existingField._deleted
    }

    // 同名字段：新增时 >0 重复，编辑时 >1 重复
    const sameCount = allFields.value.filter(f => f.name === data.name && !f._deleted).length
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
      // 新增模式：添加字段
      data._deleted = false
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

    const activeFields = allFields.value.filter(f => !f._deleted)
    if (activeFields.length === 0) {
      ElMessage.warning('请至少添加一个字段')
      return
    }

    submitLoading.value = true
    const submitData = { ...form, fields: activeFields }
    await create(submitData)
    ElMessage.success('新增成功')
    resetReactiveForm(form, initialForm)
    resetFormValidation(formRef)
    router.back()
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

.form-tip {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}
</style>
