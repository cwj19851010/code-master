<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>{{ t('edit_project') }}</span>
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="140px"
        class="detail-form"
        v-loading="loading"
      >
        <el-form-item :label="t('project_name')" prop="projectName">
          <el-input v-model="form.projectName" :placeholder="t2('please_input', 'project_name')" disabled />
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
          <el-radio-group v-model="form.projectType" disabled>
            <el-radio :label="0">{{ t('project_type_server') }}</el-radio>
            <el-radio :label="1">{{ t('project_type_wpf') }}</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item :label="t('database_type')" prop="databaseType">
          <el-select v-model="form.databaseType" :placeholder="t2('please_select', 'database_type')" style="width: 100%" disabled>
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
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getById, update } from '@/api/codegen/project'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const formRef = ref(null)
const loading = ref(false)
const submitLoading = ref(false)

const form = reactive({
  id: 0,
  projectName: '',
  displayName: '',
  displayNameEn: '',
  description: '',
  descriptionEn: '',
  projectType: 0,
  databaseType: 0,
  connectionString: '',
  projectPath: '',
  logoPath: '',
  frontendPort: 5173,
  backendPort: 5000
})

const rules = {
  displayName: [
    { required: true, message: t2('please_input', 'display_name'), trigger: 'blur' }
  ],
  connectionString: [
    { required: true, message: t2('please_input', 'connection_string'), trigger: 'blur' }
  ]
}

const loadData = async () => {
  loading.value = true
  try {
    const data = await getById(route.query.id)
    Object.assign(form, data)
  } catch (error) {
    ElMessage.error('加载数据失败')
    router.back()
  } finally {
    loading.value = false
  }
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    submitLoading.value = true
    await update(form.id, form)
    ElMessage.success(t('update_success'))
    router.back()
  } catch (error) {
    if (error !== false) {
      ElMessage.error(t('update_failed'))
    }
  } finally {
    submitLoading.value = false
  }
}

const handleBack = () => {
  router.back()
}

onMounted(() => {
  if (route.query.id) {
    loadData()
  }
})
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
