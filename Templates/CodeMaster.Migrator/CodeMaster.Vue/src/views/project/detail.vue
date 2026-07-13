<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <span>{{ t('project_detail') }}</span>
      </template>

      <el-descriptions :column="2" border>
        <el-descriptions-item :label="t('project_name')">
          {{ detail.projectName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('display_name')">
          {{ detail.displayName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('display_name_en')">
          {{ detail.displayNameEn }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('database_type')">
          <el-tag v-if="detail.databaseType === 1">SQL Server</el-tag>
          <el-tag v-else-if="detail.databaseType === 2" type="success">MySQL</el-tag>
          <el-tag v-else-if="detail.databaseType === 3" type="info">PostgreSQL</el-tag>
          <el-tag v-else-if="detail.databaseType === 4" type="warning">SQLite</el-tag>
          <el-tag v-else-if="detail.databaseType === 5" type="danger">Oracle</el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('status')" :span="2">
          <el-tag v-if="detail.status === 0" type="info">{{ t('not_initialized') }}</el-tag>
          <el-tag v-else-if="detail.status === 1" type="success">{{ t('initialized') }}</el-tag>
          <el-tag v-else-if="detail.status === 2" type="success">{{ t('running') }}</el-tag>
          <el-tag v-else-if="detail.status === 3" type="warning">{{ t('stopped') }}</el-tag>
          <el-tag v-else-if="detail.status === 4" type="danger">{{ t('initialize_failed') }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('description')" :span="2">
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
        <el-descriptions-item v-if="detail.initError" :label="t('init_error')" :span="2">
          <el-text type="danger">{{ detail.initError }}</el-text>
        </el-descriptions-item>
        <el-descriptions-item :label="t('create_time')" :span="2">
          {{ detail.createTime }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('update_time')" :span="2">
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
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { getProjectById } from '@/api/project'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()

const detail = ref({})

const loadData = async () => {
  try {
    const id = route.params.id
    const res = await getProjectById(id)
    detail.value = res
  } catch (error) {
    ElMessage.error(t('load_failed'))
  }
}

const handleBack = () => {
  router.back()
}

onMounted(() => {
  loadData()
})
</script>
