<template>
  <div class="app-container">
    <!-- 页面标题 -->
    <el-page-header @back="handleBack" class="mb8">
      <template #content>
        <span class="page-title">{{ entityName }} - 字段设计</span>
      </template>
    </el-page-header>

    <!-- 操作按钮 -->
    <el-row :gutter="10" class="mb8">
      <el-col :span="1.5">
        <el-button type="primary" icon="Plus" @click="handleAdd">新增字段</el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button type="success" icon="Upload" @click="handleImport">导入字段</el-button>
      </el-col>
    </el-row>

    <!-- 数据表格 -->
    <el-table v-loading="loading" :data="fieldList" border row-key="id">
      <el-table-column label="排序" prop="orderNum" width="80" />
      <el-table-column label="字段名称" prop="name" width="150" />
      <el-table-column label="字段描述" prop="description" width="150" />
      <el-table-column label="数据类型" prop="dataType" width="120" />
      <el-table-column label="系统字段" prop="isSystemField" width="100">
        <template #default="{ row }">
          <el-tag :type="row.isSystemField ? 'warning' : 'info'">
            {{ row.isSystemField ? '是' : '否' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="可空" prop="isNullable" width="80">
        <template #default="{ row }">
          <el-tag :type="row.isNullable ? 'success' : 'danger'">
            {{ row.isNullable ? '是' : '否' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="主键" prop="isPrimaryKey" width="80">
        <template #default="{ row }">
          <el-tag v-if="row.isPrimaryKey" type="danger">是</el-tag>
          <el-tag v-else type="info">否</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="表单控件" prop="formControlType" width="120" />
      <el-table-column label="列表显示" prop="showInList" width="100">
        <template #default="{ row }">
          <el-icon v-if="row.showInList" color="#67C23A"><Check /></el-icon>
          <el-icon v-else color="#C0C4CC"><Close /></el-icon>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="200" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" link @click="handleUpdate(row)">编辑</el-button>
          <el-button type="danger" link @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 新增/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogTitle"
      width="800px"
      @close="handleDialogClose"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="字段名称" prop="name">
              <el-input v-model="form.name" placeholder="如：UserName、Age" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="字段描述" prop="description">
              <el-input v-model="form.description" placeholder="如：用户名、年龄" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="数据类型" prop="dataType">
              <el-select v-model="form.dataType" placeholder="请选择数据类型" style="width: 100%">
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
            <el-form-item label="排序" prop="orderNum">
              <el-input-number v-model="form.orderNum" :min="0" :max="9999" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">字段属性</el-divider>

        <el-row :gutter="20">
          <el-col :span="6">
            <el-form-item label="系统字段">
              <el-switch v-model="form.isSystemField" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="可空">
              <el-switch v-model="form.isNullable" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="主键">
              <el-switch v-model="form.isPrimaryKey" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="忽略">
              <el-switch v-model="form.isIgnore" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">验证规则</el-divider>

        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="必填">
              <el-switch v-model="form.isRequired" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="邮箱验证">
              <el-switch v-model="form.isEmail" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="手机验证">
              <el-switch v-model="form.isPhone" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">显示配置</el-divider>

        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="列表显示">
              <el-switch v-model="form.showInList" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="详情显示">
              <el-switch v-model="form.showInDetail" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="搜索条件">
              <el-switch v-model="form.showInSearch" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="新增表单显示">
              <el-switch v-model="form.showInAddForm" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="编辑表单显示">
              <el-switch v-model="form.showInEditForm" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-divider content-position="left">表单控件</el-divider>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="控件类型" prop="formControlType">
              <el-select v-model="form.formControlType" placeholder="请选择控件类型" style="width: 100%">
                <el-option label="输入框 (input)" value="input" />
                <el-option label="文本域 (textarea)" value="textarea" />
                <el-option label="数字输入 (number)" value="number" />
                <el-option label="下拉选择 (select)" value="select" />
                <el-option label="日期选择 (date)" value="date" />
                <el-option label="日期时间 (datetime)" value="datetime" />
                <el-option label="开关 (switch)" value="switch" />
                <el-option label="单选框 (radio)" value="radio" />
                <el-option label="复选框 (checkbox)" value="checkbox" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row v-if="form.formControlType === 'select' || form.formControlType === 'radio' || form.formControlType === 'checkbox'" :gutter="20">
          <el-col :span="12">
            <el-form-item label="数据源类型">
              <el-radio-group v-model="selectDataSourceType">
                <el-radio label="enum">枚举</el-radio>
                <el-radio label="dict">字典</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row v-if="selectDataSourceType === 'enum' && (form.formControlType === 'select' || form.formControlType === 'radio' || form.formControlType === 'checkbox')" :gutter="20">
          <el-col :span="24">
            <el-form-item label="选项配置">
              <el-input
                v-model="form.selectOptions"
                type="textarea"
                :rows="4"
                placeholder='请输入JSON格式的选项，如：[{"label":"男","value":"1"},{"label":"女","value":"2"}]'
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row v-if="selectDataSourceType === 'dict' && (form.formControlType === 'select' || form.formControlType === 'radio' || form.formControlType === 'checkbox')" :gutter="20">
          <el-col :span="24">
            <el-form-item label="字典编码">
              <el-input v-model="form.selectDataSource" placeholder="请输入字典编码，如：sys_user_sex" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Check, Close } from '@element-plus/icons-vue'
import { t2 } from '@/i18n'
import {
  getPagedList,
  create,
  update,
  deleteById
} from '@/api/codegen/entityField'

defineOptions({
  name: 'CodegenEntityField'
})

const router = useRouter()
const route = useRoute()

// 数据
const loading = ref(false)
const fieldList = ref([])
const moduleEntityId = ref(null)
const entityName = ref('')

// 对话框
const dialogVisible = ref(false)
const dialogTitle = ref('')
const formRef = ref(null)
const selectDataSourceType = ref('enum')
const form = reactive({
  id: null,
  moduleEntityId: null,
  name: '',
  description: '',
  isSystemField: false,
  dataType: 'string',
  isNullable: true,
  isIgnore: false,
  isPrimaryKey: false,
  isRequired: false,
  isEmail: false,
  isPhone: false,
  showInList: true,
  showInDetail: true,
  showInAddForm: true,
  showInEditForm: true,
  showInSearch: false,
  formControlType: 'input',
  selectDataSource: null,
  selectOptions: null,
  orderNum: 0
})

// 表单验证规则
const rules = {
  name: [
    { required: true, message: t2('please_input', 'field_name'), trigger: 'blur' },
    { pattern: /^[A-Z][a-zA-Z0-9]*$/, message: '字段名称必须以大写字母开头，只能包含字母和数字', trigger: 'blur' }
  ],
  description: [{ required: true, message: t2('please_input', 'field_description'), trigger: 'blur' }],
  dataType: [{ required: true, message: t2('please_select', 'data_type'), trigger: 'change' }],
  formControlType: [{ required: true, message: t2('please_select', 'control_type'), trigger: 'change' }],
  orderNum: [{ required: true, message: t2('please_input', 'sort_number'), trigger: 'blur' }]
}

// 生命周期
onMounted(() => {
  moduleEntityId.value = route.query.moduleEntityId
  entityName.value = route.query.entityName || '实体'
  if (moduleEntityId.value) {
    loadFieldList()
  }
})

// 加载字段列表
async function loadFieldList() {
  loading.value = true
  try {
    const res = await getPagedList({
      pageNum: 1,
      pageSize: 1000,
      moduleEntityId: moduleEntityId.value
    })
    fieldList.value = res.data.items || []
  } catch (error) {
    ElMessage.error('加载数据失败')
    console.error(error)
  } finally {
    loading.value = false
  }
}

// 返回
function handleBack() {
  router.back()
}

// 新增
function handleAdd() {
  dialogTitle.value = '新增字段'
  resetForm()
  form.moduleEntityId = moduleEntityId.value
  dialogVisible.value = true
}

// 编辑
function handleUpdate(row) {
  dialogTitle.value = '编辑字段'
  Object.assign(form, row)

  // 判断数据源类型
  if (form.selectDataSource) {
    selectDataSourceType.value = 'dict'
  } else if (form.selectOptions) {
    selectDataSourceType.value = 'enum'
  }

  dialogVisible.value = true
}

// 删除
async function handleDelete(row) {
  try {
    await ElMessageBox.confirm('确定要删除该字段吗？', '提示', {
      type: 'warning'
    })
    await deleteById(row.id)
    ElMessage.success('删除成功')
    loadFieldList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
      console.error(error)
    }
  }
}

// 导入字段
function handleImport() {
  ElMessage.info('导入功能开发中...')
}

// 提交表单
async function handleSubmit() {
  try {
    await formRef.value.validate()

    // 根据数据源类型设置字段
    if (selectDataSourceType.value === 'enum') {
      form.selectDataSource = null
    } else if (selectDataSourceType.value === 'dict') {
      form.selectOptions = null
    }

    loading.value = true

    if (form.id) {
      await update(form)
      ElMessage.success('更新成功')
    } else {
      await create(form)
      ElMessage.success('创建成功')
    }

    dialogVisible.value = false
    loadFieldList()
  } catch (error) {
    if (error !== false) {
      ElMessage.error('操作失败')
      console.error(error)
    }
  } finally {
    loading.value = false
  }
}

// 对话框关闭
function handleDialogClose() {
  resetForm()
}

// 重置表单
function resetForm() {
  form.id = null
  form.moduleEntityId = null
  form.name = ''
  form.description = ''
  form.isSystemField = false
  form.dataType = 'string'
  form.isNullable = true
  form.isIgnore = false
  form.isPrimaryKey = false
  form.isRequired = false
  form.isEmail = false
  form.isPhone = false
  form.showInList = true
  form.showInDetail = true
  form.showInAddForm = true
  form.showInEditForm = true
  form.showInSearch = false
  form.formControlType = 'input'
  form.selectDataSource = null
  form.selectOptions = null
  form.orderNum = 0
  selectDataSourceType.value = 'enum'
  formRef.value?.clearValidate()
}

refreshOnReactivated(loadFieldList)
</script>

<style scoped>
.page-title {
  font-size: 18px;
  font-weight: bold;
}

.mb8 {
  margin-bottom: 8px;
}
</style>
