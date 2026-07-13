<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
        </div>
      </template>

      <el-descriptions :column="2" border v-loading="loading">
        <el-descriptions-item :label="t('username')">{{ detailData.userName }}</el-descriptions-item>
        <el-descriptions-item :label="t('login_ip')">{{ detailData.loginIp }}</el-descriptions-item>
        <el-descriptions-item :label="t('login_location')">{{ detailData.loginLocation }}</el-descriptions-item>
        <el-descriptions-item :label="t('browser')">{{ detailData.browser }}</el-descriptions-item>
        <el-descriptions-item :label="t('os')">{{ detailData.os }}</el-descriptions-item>
        <el-descriptions-item :label="t('login_status')">
          <el-tag :type="detailData.status === 0 ? 'success' : 'danger'">
            {{ detailData.status === 0 ? t('login_success') : t('login_failed') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('msg')" :span="2">{{ detailData.msg }}</el-descriptions-item>
        <el-descriptions-item :label="t('login_time')" :span="2">{{ formatDateTime(detailData.loginTime) }}</el-descriptions-item>
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
import { getById } from '@/api/monitor/loginlog'
import { formatDateTime } from '@/utils/dateFormat'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const loading = ref(false)
const detailData = ref({})

const fetchDetail = async () => {
  const id = route.query.id
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
    console.error('Failed to fetch login log detail:', error)
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
</style>
