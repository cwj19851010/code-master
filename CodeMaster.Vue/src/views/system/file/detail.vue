<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
        </div>
      </template>

      <el-descriptions :column="2" border>
        <el-descriptions-item :label="t('file_name')">
          {{ fileInfo.fileName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('file_type')">
          {{ fileInfo.fileType }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('file_size')">
          {{ formatFileSize(fileInfo.fileSize) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('upload_time')">
          {{ formatDateTime(fileInfo.createTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('update_time')">
          {{ formatDateTime(fileInfo.updateTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('file_path')" :span="2">
          {{ fileInfo.filePath }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('remark')" :span="2">
          {{ fileInfo.remark || '-' }}
        </el-descriptions-item>
      </el-descriptions>

      <div class="mt-20">
        <el-button type="primary" @click="handleDownload">
          {{ t('download') }}
        </el-button>
        <el-button @click="handleBack">
          {{ t('back') }}
        </el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { formatDateTime } from '@/utils/dateFormat'
import request from '@/utils/request'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()

const loading = ref(false)
const fileInfo = reactive({
  id: null,
  fileName: '',
  filePath: '',
  fileSize: 0,
  fileType: '',
  createTime: '',
  updateTime: '',
  remark: ''
})

const formatFileSize = (bytes) => {
  if (!bytes) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
}

const loadFileDetail = async () => {
  const fileId = route.query.id
  if (!fileId) {
    ElMessage.error(t('file_id_required'))
    router.back()
    return
  }

  try {
    loading.value = true
    const { data } = await request.get(`/api/system/file/${fileId}`)
    Object.assign(fileInfo, data)
  } catch (error) {
    ElMessage.error(error.message || t('load_failed'))
    router.back()
  } finally {
    loading.value = false
  }
}

const handleDownload = async () => {
  try {
    const response = await request.get(`/api/system/file/downloadfile/${fileInfo.id}`, {
      responseType: 'blob'
    })
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', fileInfo.fileName)
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  } catch (error) {
    ElMessage.error(t('download_failed'))
  }
}

const handleBack = () => {
  router.back()
}

onMounted(() => {
  loadFileDetail()
})
</script>

<style scoped>
.card-header {
  font-size: 16px;
  font-weight: bold;
}

.mt-20 {
  margin-top: 20px;
}
</style>
