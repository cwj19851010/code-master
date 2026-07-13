<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
        </div>
      </template>

      <el-descriptions :column="2" border v-loading="loading">
        <el-descriptions-item :label="t('log_id')">{{ detailData.id }}</el-descriptions-item>
        <el-descriptions-item :label="t('task_id')">{{ detailData.taskId }}</el-descriptions-item>
        <el-descriptions-item :label="t('task_name')">{{ detailData.taskName }}</el-descriptions-item>
        <el-descriptions-item :label="t('status')">
          <el-tag :type="detailData.status === 0 ? 'success' : 'danger'">
            {{ detailData.status === 0 ? t('success') : t('failed') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('elapsed')">{{ detailData.elapsed }} ms</el-descriptions-item>
        <el-descriptions-item :label="t('create_time')">{{ formatDateTime(detailData.createTime) }}</el-descriptions-item>
        <el-descriptions-item :label="t('invoke_target')" :span="2">{{ detailData.invokeTarget }}</el-descriptions-item>
        <el-descriptions-item :label="t('job_message')" :span="2">
          <pre style="max-height: 300px; overflow-y: auto;">{{ detailData.jobMessage }}</pre>
        </el-descriptions-item>
      </el-descriptions>

      <div class="mt-20" style="text-align: center;">
        <el-button @click="handleBack">{{ t('back') }}</el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { getById } from '@/api/monitor/tasklog'
import { formatDateTime } from '@/utils/dateFormat'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const loading = ref(false)
const detailData = ref({})

const fetchDetail = async () => {
  const id = route.query.id
  console.log('Route query id:', id, 'Type:', typeof id)
  if (!id) {
    ElMessage.error(t('invalid_id'))
    handleBack()
    return
  }

  loading.value = true
  try {
    const data = await getById(id)
    detailData.value = data || {}
  } catch (error) {
    console.error('Failed to fetch task log detail:', error)
    ElMessage.error(t('fetch_failed'))
  } finally {
    loading.value = false
  }
}

const handleBack = () => {
  router.back()
}

onMounted(() => {
  fetchDetail()
})
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

pre {
  white-space: pre-wrap;
  word-wrap: break-word;
  margin: 0;
  padding: 10px;
  background-color: #f5f7fa;
  border-radius: 4px;
}
</style>
