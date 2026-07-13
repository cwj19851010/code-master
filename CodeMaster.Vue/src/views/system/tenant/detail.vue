<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
        </div>
      </template>

      <el-descriptions :column="2" border>
        <el-descriptions-item :label="t('code')">
          {{ tenantInfo.tenantCode }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('name')">
          {{ tenantInfo.tenantName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('isolation_type')">
          <el-tag :type="tenantInfo.isolationType === 0 ? 'success' : 'info'">
            {{ tenantInfo.isolationType === 0 ? t('isolation_physical') : t('isolation_logical') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('db_type')" v-if="tenantInfo.isolationType === 0">
          <span v-if="tenantInfo.dbType === 1">SQL Server</span>
          <span v-else-if="tenantInfo.dbType === 2">MySQL</span>
          <span v-else-if="tenantInfo.dbType === 3">PostgreSQL</span>
          <span v-else>-</span>
        </el-descriptions-item>
        <el-descriptions-item :label="t('db_connection')" :span="2" v-if="tenantInfo.isolationType === 0">
          {{ tenantInfo.connectionString || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('status')">
          <el-tag :type="tenantInfo.status === 0 ? 'success' : 'danger'">
            {{ tenantInfo.status === 0 ? t('normal') : t('disabled') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('expire_time')">
          {{ tenantInfo.expireTime ? formatDateTime(tenantInfo.expireTime) : '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('create_time')">
          {{ formatDateTime(tenantInfo.createTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('update_time')">
          {{ formatDateTime(tenantInfo.updateTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('remark')" :span="2">
          {{ tenantInfo.remark || '-' }}
        </el-descriptions-item>
      </el-descriptions>

      <div class="mt-20">
        <el-button type="primary" @click="handleEdit">
          {{ t('edit') }}
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
import { getById } from '@/api/system/tenant'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()

const loading = ref(false)
const tenantInfo = reactive({
  id: null,
  tenantCode: '',
  tenantName: '',
  isolationType: 1,
  dbType: 1,
  connectionString: '',
  status: 0,
  expireTime: null,
  createTime: '',
  updateTime: '',
  remark: ''
})

const loadTenantDetail = async () => {
  const tenantId = route.query.id
  if (!tenantId) {
    ElMessage.error(t('tenant_id_required'))
    router.back()
    return
  }

  try {
    loading.value = true
    const data = await getById(tenantId)
    Object.assign(tenantInfo, data)
  } catch (error) {
    ElMessage.error(error.message || t('load_failed'))
    router.back()
  } finally {
    loading.value = false
  }
}

const handleEdit = () => {
  router.push({
    path: '/system/tenant/edit',
    query: { id: tenantInfo.id }
  })
}

const handleBack = () => {
  router.back()
}

onMounted(() => {
  loadTenantDetail()
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
