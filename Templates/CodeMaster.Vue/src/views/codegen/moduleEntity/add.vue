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
        <!-- 实体基本信息 - 一行多列布局 -->
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="所属模块" prop="projectModuleId">
              <el-select v-model="form.projectModuleId" :placeholder="$t2('please_select', 'module')" style="width: 100%">
                <el-option
                  v-for="module in moduleList"
                  :key="module.id"
                  :label="module.moduleDescription"
                  :value="module.id"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="实体名称" prop="name">
              <el-input v-model="form.name" placeholder="如：Customer" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="实体描述" prop="description">
              <el-input v-model="form.description" placeholder="如：客户" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="表名" prop="tableName">
              <el-input v-model="form.tableName" placeholder="如：sys_customer" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="排序" prop="orderNum">
              <el-input-number v-model="form.orderNum" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="选项" prop="options">
              <el-checkbox v-model="form.isTree">树形</el-checkbox>
              <el-checkbox v-model="form.isReadOnly">只读</el-checkbox>
              <el-checkbox v-model="form.hasDataPermission">数据权限</el-checkbox>
              <el-checkbox v-model="form.hasTenant">多租户</el-checkbox>
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

      <el-table :data="form.fields" border style="width: 100%">
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
          </template>
        </el-table-column>
        <el-table-column prop="orderNum" label="排序" width="80" />
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row, $index }">
            <el-button link type="primary" size="small" @click="handleEditField(row, $index)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDeleteField($index)">删除</el-button>
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
      </el-form>

      <template #footer>
        <el-button @click="fieldDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleFieldSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { t2 } from '@/i18n'
import { create } from '@/api/codegen/moduleEntity'
import { getPagedList as getModuleList } from '@/api/codegen/projectModule'

const router = useRouter()
const formRef = ref(null)
const fieldFormRef = ref(null)
const submitLoading = ref(false)
const moduleList = ref([])

// 实体表单
const form = reactive({
  projectModuleId: null,
  name: '',
  description: '',
  tableName: '',
  isTree: false,
  isReadOnly: false,
  hasDataPermission: false,
  hasTenant: false,
  orderNum: 0,
  fields: [] // 字段列表
})

const rules = {
  projectModuleId: [{ required: true, message: t2('please_select', 'module'), trigger: 'change' }],
  name: [{ required: true, message: t2('please_input', 'entity_name'), trigger: 'blur' }],
  description: [{ required: true, message: t2('please_input', 'entity_description'), trigger: 'blur' }],
  tableName: [{ required: true, message: t2('please_input', 'table_name'), trigger: 'blur' }]
}

// 字段弹框
const fieldDialogVisible = ref(false)
const fieldDialogTitle = ref('新增字段')
const editingFieldIndex = ref(-1)

const fieldForm = reactive({
  name: '',
  description: '',
  dataType: 'string',
  maxLength: 0,
  defaultValue: '',
  isPrimaryKey: false,
  isRequired: false,
  isUnique: false,
  isIndexed: false,
  isSearchable: false,
  isSortable: false,
  orderNum: 0
})

const fieldRules = {
  name: [{ required: true, message: t2('please_input', 'field_name'), trigger: 'blur' }],
  description: [{ required: true, message: t2('please_input', 'description'), trigger: 'blur' }],
  dataType: [{ required: true, message: t2('please_select', 'data_type'), trigger: 'change' }]
}

onMounted(() => {
  loadModuleList()
})

async function loadModuleList() {
  try {
    const res = await getModuleList({ pageNum: 1, pageSize: 1000 })
    moduleList.value = res.items || []
  } catch (error) {
    console.error('加载模块列表失败:', error)
  }
}

// 新增字段
function handleAddField() {
  fieldDialogTitle.value = '新增字段'
  editingFieldIndex.value = -1
  resetFieldForm()
  fieldDialogVisible.value = true
}

// 编辑字段
function handleEditField(row, index) {
  fieldDialogTitle.value = '编辑字段'
  editingFieldIndex.value = index
  Object.assign(fieldForm, row)
  fieldDialogVisible.value = true
}

// 删除字段
function handleDeleteField(index) {
  form.fields.splice(index, 1)
  ElMessage.success('删除成功')
}

// 字段弹框提交
async function handleFieldSubmit() {
  try {
    await fieldFormRef.value.validate()

    if (editingFieldIndex.value >= 0) {
      // 编辑模式：更新字段
      Object.assign(form.fields[editingFieldIndex.value], fieldForm)
      ElMessage.success('修改成功')
    } else {
      // 新增模式：添加字段
      form.fields.push({ ...fieldForm })
      ElMessage.success('添加成功')
    }

    fieldDialogVisible.value = false
  } catch (error) {
    if (error !== false) {
      console.error(error)
    }
  }
}

// 字段弹框关闭
function handleFieldDialogClose() {
  resetFieldForm()
}

// 重置字段表单
function resetFieldForm() {
  Object.assign(fieldForm, {
    name: '',
    description: '',
    dataType: 'string',
    maxLength: 0,
    defaultValue: '',
    isPrimaryKey: false,
    isRequired: false,
    isUnique: false,
    isIndexed: false,
    isSearchable: false,
    isSortable: false,
    orderNum: 0
  })
  fieldFormRef.value?.clearValidate()
}

// 提交实体和字段
async function handleSubmit() {
  try {
    await formRef.value.validate()

    if (form.fields.length === 0) {
      ElMessage.warning('请至少添加一个字段')
      return
    }

    submitLoading.value = true
    await create(form)
    ElMessage.success('新增成功')
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
