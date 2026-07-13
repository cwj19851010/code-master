<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <span>{{ t('project_add') }}</span>
      </template>

      <el-form ref="formRef" :model="form" :rules="rules" label-width="150px">
        <el-form-item :label="t('project_name')" prop="projectName">
          <el-input v-model="form.projectName" :placeholder="t('please_enter')" />
          <div class="form-tip">{{ t('project_name_tip') }}</div>
        </el-form-item>

        <el-form-item :label="t('display_name')" prop="displayName">
          <el-input v-model="form.displayName" :placeholder="t('please_enter')" />
          <div class="form-tip">{{ t('display_name_tip') }}</div>
        </el-form-item>

        <el-form-item :label="t('display_name_en')" prop="displayNameEn">
          <el-input v-model="form.displayNameEn" :placeholder="t('please_enter')" />
        </el-form-item>

        <el-form-item :label="t('description')" prop="description">
          <el-input v-model="form.description" type="textarea" :rows="3" :placeholder="t('please_enter')" />
        </el-form-item>

        <el-form-item :label="t('description_en')" prop="descriptionEn">
          <el-input v-model="form.descriptionEn" type="textarea" :rows="3" :placeholder="t('please_enter')" />
        </el-form-item>

        <el-form-item :label="t('database_type')" prop="databaseType">
          <el-select v-model="form.databaseType" :placeholder="t('please_select')">
            <el-option label="SQL Server" :value="1" />
            <el-option label="MySQL" :value="2" />
            <el-option label="PostgreSQL" :value="3" />
            <el-option label="SQLite" :value="4" />
            <el-option label="Oracle" :value="5" />
          </el-select>
        </el-form-item>

        <el-form-item :label="t('connection_string')" prop="connectionString">
          <el-input v-model="form.connectionString" type="textarea" :rows="2" :placeholder="t('please_enter')" />
          <div class="form-tip">{{ t('connection_string_tip') }}</div>
        </el-form-item>

        <el-form-item :label="t('project_path')" prop="projectPath">
          <el-input v-model="form.projectPath" :placeholder="t('please_enter')" />
          <div class="form-tip">{{ t('project_path_tip') }}</div>
        </el-form-item>

        <el-form-item :label="t('logo_path')" prop="logoPath">
          <el-input v-model="form.logoPath" :placeholder="t('please_enter')" />
        </el-form-item>

        <el-form-item :label="t('frontend_port')" prop="frontendPort">
          <el-input-number v-model="form.frontendPort" :min="1000" :max="65535" />
        </el-form-item>

        <el-form-item :label="t('backend_port')" prop="backendPort">
          <el-input-number v-model="form.backendPort" :min="1000" :max="65535" />
        </el-form-item>

        <el-form-item>
          <el-button type="primary" @click="handleSubmit">{{ t('submit') }}</el-button>
          <el-button @click="handleCancel">{{ t('cancel') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { createProject } from '@/api/project'

const { t } = useI18n()
const router = useRouter()
const formRef = ref(null)

const form = reactive({
  projectName: '',
  displayName: '',
  displayNameEn: '',
  description: '',
  descriptionEn: '',
  databaseType: 2,
  connectionString: '',
  projectPath: '',
  logoPath: '',
  frontendPort: 5173,
  backendPort: 5000
})

const rules = {
  projectName: [
    { required: true, message: t('please_enter'), trigger: 'blur' },
    { pattern: /^[a-zA-Z][a-zA-Z0-9]*$/, message: t('project_name_format'), trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: t('please_enter'), trigger: 'blur' }
  ],
  databaseType: [
    { required: true, message: t('please_select'), trigger: 'change' }
  ],
  connectionString: [
    { required: true, message: t('please_enter'), trigger: 'blur' }
  ],
  projectPath: [
    { required: true, message: t('please_enter'), trigger: 'blur' }
  ]
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    await createProject(form)
    ElMessage.success(t('create_success'))
    router.push('/project')
  } catch (error) {
    if (error !== false) {
      ElMessage.error(t('create_failed'))
    }
  }
}

const handleCancel = () => {
  router.back()
}
</script>

<style scoped>
.form-tip {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}
</style>
