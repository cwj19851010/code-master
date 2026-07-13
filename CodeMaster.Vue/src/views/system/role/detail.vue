<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
          <div>
            <el-button type="primary" @click="handleEdit" :icon="Edit" v-permission="'system:role:update'">
              {{ t('edit') }}
            </el-button>
            <el-button @click="handleBack" :icon="Back">{{ t('back') }}</el-button>
          </div>
        </div>
      </template>

      <el-descriptions :column="2" border>
        <el-descriptions-item :label="t('name')">{{ detail.roleName }}</el-descriptions-item>
        <el-descriptions-item :label="t('code')">{{ detail.roleKey }}</el-descriptions-item>
        <el-descriptions-item :label="t('sort')">{{ detail.roleSort }}</el-descriptions-item>
        <el-descriptions-item :label="t('status')">
          <el-tag :type="detail.status === 0 ? 'success' : 'danger'">
            {{ detail.status === 0 ? t('normal') : t('disabled') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('remark')" :span="2">{{ detail.remark || '-' }}</el-descriptions-item>
        <el-descriptions-item :label="t('create_time')">{{ formatDateTime(detail.createTime) }}</el-descriptions-item>
        <el-descriptions-item :label="t('update_time')">{{ formatDateTime(detail.updateTime) }}</el-descriptions-item>
      </el-descriptions>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Back, Edit } from '@element-plus/icons-vue'
import { getById } from '@/api/system/role'
import { useI18n } from 'vue-i18n'
import { formatDateTime } from '@/utils/dateFormat'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const loading = ref(false)

const detail = reactive({
  roleName: '',
  roleKey: '',
  roleSort: 0,
  status: 0,
  remark: '',
  createTime: '',
  updateTime: ''
})

const loadData = async () => {
  const id = route.query.id
  if (!id) {
    ElMessage.error('Invalid role id')
    router.back()
    return
  }

  try {
    loading.value = true
    const data = await getById(id)
    Object.assign(detail, data)
  } catch (error) {
    ElMessage.error(t('query_failed'))
    router.back()
  } finally {
    loading.value = false
  }
}

const handleEdit = () => {
  router.push({
    path: '/system/role/edit',
    query: { id: route.query.id }
  })
}

const handleBack = () => {
  router.back()
}

onMounted(() => {
  loadData()
})
</script>

<style scoped lang="scss">
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
