<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="140px"
        class="detail-form"
      >
        <el-form-item :label="t('project_name')" prop="projectName">
          <el-input v-model="form.projectName" :placeholder="t2('please_input', 'project_name')" />
          <div class="form-tip">{{ t('project_name_tip') }}</div>
        </el-form-item>

        <el-form-item :label="t('display_name')" prop="displayName">
          <el-input v-model="form.displayName" :placeholder="t2('please_input', 'display_name')" />
        </el-form-item>

        <el-form-item :label="t('display_name_en')" prop="displayNameEn">
          <el-input v-model="form.displayNameEn" :placeholder="t2('please_input', 'display_name')" />
        </el-form-item>

        <el-form-item :label="t('project_description')" prop="description">
          <el-input v-model="form.description" type="textarea" :rows="3" :placeholder="t2('please_input', '')" />
        </el-form-item>

        <el-form-item :label="t('description_en')" prop="descriptionEn">
          <el-input v-model="form.descriptionEn" type="textarea" :rows="3" :placeholder="t2('please_input', '')" />
        </el-form-item>

        <el-form-item :label="t('project_type')" prop="projectType">
          <el-radio-group v-model="form.projectType">
            <el-radio :label="0">{{ t('project_type_server') }}</el-radio>
            <el-radio :label="1">{{ t('project_type_wpf') }}</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item :label="t('database_type')" prop="databaseType">
          <el-select v-model="form.databaseType" :placeholder="t2('please_select', 'database_type')" style="width: 100%">
            <el-option :label="t('db_sqlserver')" :value="1" />
            <el-option :label="t('db_mysql')" :value="2" />
            <el-option :label="t('db_postgresql')" :value="3" />
            <el-option :label="t('db_sqlite')" :value="4" />
            <el-option :label="t('db_oracle')" :value="5" />
          </el-select>
        </el-form-item>

        <el-form-item :label="t('connection_string')" prop="connectionString">
          <el-input v-model="form.connectionString" type="textarea" :rows="2" :placeholder="t2('please_input', 'connection_string')" />
          <div class="form-tip">{{ t('connection_string_tip') }}</div>
        </el-form-item>

        <el-form-item :label="t('project_path')" prop="projectPath">
          <el-input v-model="form.projectPath" :placeholder="t2('please_input', 'project_path')" />
          <div class="form-tip">{{ t('project_path_tip') }}</div>
        </el-form-item>

        <el-form-item :label="t('logo_path')" prop="logoPath">
          <el-input v-model="form.logoPath" :placeholder="t2('please_input', '')" />
        </el-form-item>

        <el-form-item :label="t('frontend_port')" prop="frontendPort">
          <el-input-number v-model="form.frontendPort" :min="1000" :max="65535" />
        </el-form-item>

        <el-form-item :label="t('backend_port')" prop="backendPort">
          <el-input-number v-model="form.backendPort" :min="1000" :max="65535" />
        </el-form-item>

        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="submitLoading">{{ t('save') }}</el-button>
          <el-button @click="handleBack">{{ t('cancel') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { resetReactiveForm, resetFormValidation } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { create } from '@/api/codegen/project'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'

const router = useRouter()
const { t } = useI18n()
const formRef = ref(null)
const submitLoading = ref(false)

const form = reactive({
  projectName: '',
  displayName: '',
  displayNameEn: '',
  description: '',
  descriptionEn: '',
  projectType: 0,
  databaseType: 2,
  connectionString: '',
  projectPath: '',
  logoPath: '',
  frontendPort: 5173,
  backendPort: 5000
})

const initialForm = JSON.parse(JSON.stringify(form))

const rules = {
  projectName: [
    { required: true, message: t2('please_input', 'project_name'), trigger: 'blur' },
    { pattern: /^[A-Z][a-zA-Z0-9]*$/, message: '项目名称必须以大写字母开头，只能包含字母和数字', trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: t2('please_input', 'display_name'), trigger: 'blur' }
  ],
  databaseType: [
    { required: true, message: t2('please_select', 'database_type'), trigger: 'change' }
  ],
  connectionString: [
    { required: true, message: t2('please_input', 'connection_string'), trigger: 'blur' }
  ],
  projectPath: [
    { required: true, message: t2('please_input', 'project_path'), trigger: 'blur' }
  ]
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    submitLoading.value = true
    await create(form)
    ElMessage.success(t('create_success'))
    resetReactiveForm(form, initialForm)
    resetFormValidation(formRef)
    router.back()
  } catch (error) {
    if (error !== false) {
      ElMessage.error(t('create_failed'))
    }
  } finally {
    submitLoading.value = false
  }
}

const handleBack = () => {
  router.back()
}
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';

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
