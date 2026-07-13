<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
        </div>
      </template>

      <el-descriptions v-if="detail.id" :column="2" border>
        <el-descriptions-item :label="t('project_name')">
          {{ detail.projectName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('display_name')">
          {{ detail.displayName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('display_name_en')">
          {{ detail.displayNameEn }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('project_type')">
          <el-tag v-if="detail.projectType === 0" type="success">{{ t('project_type_server') }}</el-tag>
          <el-tag v-else type="info">{{ t('project_type_wpf') }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('database_type')">
          <span v-if="detail.databaseType === 0">{{ t('db_mysql') }}</span>
          <span v-else-if="detail.databaseType === 1">{{ t('db_sqlserver') }}</span>
          <span v-else-if="detail.databaseType === 2">{{ t('db_postgresql') }}</span>
          <span v-else-if="detail.databaseType === 3">{{ t('db_sqlite') }}</span>
          <span v-else-if="detail.databaseType === 4">{{ t('db_oracle') }}</span>
        </el-descriptions-item>
        <el-descriptions-item :label="t('project_status')">
          <el-tag v-if="detail.status === 0" type="info">{{ t('not_initialized') }}</el-tag>
          <el-tag v-else-if="detail.status === 1" type="success">{{ t('initialized') }}</el-tag>
          <el-tag v-else-if="detail.status === 2" type="success">{{ t('running') }}</el-tag>
          <el-tag v-else-if="detail.status === 3" type="warning">{{ t('stopped') }}</el-tag>
          <el-tag v-else-if="detail.status === 4" type="danger">{{ t('init_failed') }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('project_description')" :span="2">
          {{ detail.description }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('description_en')" :span="2">
          {{ detail.descriptionEn }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('connection_string')" :span="2">
          {{ detail.connectionString }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('project_path')" :span="2">
          {{ detail.projectPath }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('logo_path')" :span="2">
          {{ detail.logoPath }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('frontend_port')">
          {{ detail.frontendPort }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('backend_port')">
          {{ detail.backendPort }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('initialized_at')" :span="2">
          {{ detail.initializedAt }}
        </el-descriptions-item>
        <el-descriptions-item v-if="detail.initError && detail.initError.length > 0" :label="t('init_error')" :span="2">
          <el-text type="danger">{{ detail.initError }}</el-text>
        </el-descriptions-item>
        <el-descriptions-item :label="t('create_time')">
          {{ detail.createTime }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('update_time')">
          {{ detail.updateTime }}
        </el-descriptions-item>
      </el-descriptions>

      <div style="margin-top: 20px">
        <el-button @click="handleBack">{{ t('back') }}</el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getById } from '@/api/codegen/project'
import { useI18n } from 'vue-i18n'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const loading = ref(false)
const detail = ref({
  id: null,
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
  frontendPort: null,
  backendPort: null,
  status: 0,
  initializedAt: '',
  initError: '',
  createTime: '',
  updateTime: ''
})

const loadData = async () => {
  loading.value = true
  try {
    const data = await getById(route.query.id)
    detail.value = data
  } catch (error) {
    ElMessage.error('加载数据失败')
    router.back()
  } finally {
    loading.value = false
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
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
