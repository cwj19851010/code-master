<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>新增项目模块</span>
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="120px"
        class="detail-form"
      >
        <el-form-item label="所属项目" prop="projectId">
          <el-select v-model="form.projectId" placeholder="请选择项目" style="width: 100%">
            <el-option
              v-for="project in projectList"
              :key="project.id"
              :label="project.projectName"
              :value="project.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="模块名称" prop="moduleName">
          <el-input v-model="form.moduleName" placeholder="请输入模块名称（英文）" />
          <div class="form-tip">用于权限规则和国际化，如：sales、order</div>
        </el-form-item>

        <el-form-item label="模块描述" prop="moduleDescription">
          <el-input v-model="form.moduleDescription" placeholder="请输入模块描述（中文）" />
          <div class="form-tip">用于菜单显示，如：销售管理、订单管理</div>
        </el-form-item>

        <el-form-item label="图标" prop="icon">
          <el-input v-model="form.icon" placeholder="请输入图标名称" />
        </el-form-item>

        <el-form-item label="排序" prop="orderNum">
          <el-input-number v-model="form.orderNum" :min="0" />
        </el-form-item>

        <el-form-item label="状态" prop="status">
          <el-radio-group v-model="form.status">
            <el-radio :label="1">启用</el-radio>
            <el-radio :label="0">禁用</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="submitLoading">保存</el-button>
          <el-button @click="handleBack">取消</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { t2 } from '@/i18n'
import { create } from '@/api/codegen/projectModule'
import { getList } from '@/api/codegen/project'

const router = useRouter()
const formRef = ref(null)
const submitLoading = ref(false)
const projectList = ref([])

const form = reactive({
  projectId: null,
  moduleName: '',
  moduleDescription: '',
  icon: '',
  orderNum: 0,
  status: 1
})

const rules = {
  projectId: [{ required: true, message: t2('please_select', 'project'), trigger: 'change' }],
  moduleName: [{ required: true, message: t2('please_input', 'module_name'), trigger: 'blur' }],
  moduleDescription: [{ required: true, message: t2('please_input', 'module_description'), trigger: 'blur' }]
}

onMounted(() => {
  loadProjectList()
})

async function loadProjectList() {
  try {
    const res = await getList()
    projectList.value = res || []
  } catch (error) {
    console.error('加载项目列表失败:', error)
  }
}

async function handleSubmit() {
  try {
    await formRef.value.validate()
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
