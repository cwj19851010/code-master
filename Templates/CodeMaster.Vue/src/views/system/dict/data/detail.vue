<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
          <el-button :icon="Back" @click="handleBack">{{ t('back') }}</el-button>
        </div>
      </template>

      <el-descriptions :column="2" border>
        <el-descriptions-item :label="t('dictType')">{{ data.dictType }}</el-descriptions-item>
        <el-descriptions-item :label="t('label')">{{ data.label }}</el-descriptions-item>
        <el-descriptions-item :label="t('value')">{{ data.value }}</el-descriptions-item>
        <el-descriptions-item :label="t('lang_key')">{{ data.langKey || '-' }}</el-descriptions-item>
        <el-descriptions-item :label="t('is_default')">
          <el-tag :type="data.isDefault === 1 ? 'success' : 'info'">
            {{ data.isDefault === 1 ? t('yes') : t('no') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('sort')">{{ data.sort }}</el-descriptions-item>
        <el-descriptions-item :label="t('status')">
          <el-tag :type="data.status === 0 ? 'success' : 'danger'">
            {{ data.status === 0 ? t('normal') : t('disabled') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('remark')" :span="2">{{ data.remark || '-' }}</el-descriptions-item>
        <el-descriptions-item :label="t('create_time')">{{ data.createTime }}</el-descriptions-item>
        <el-descriptions-item :label="t('update_time')">{{ data.updateTime || '-' }}</el-descriptions-item>
      </el-descriptions>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Back } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { getDataById } from '@/api/system/dict'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()

const loading = ref(false)
const data = ref({})

const loadData = async () => {
  const id = route.query.id
  if (!id) {
    ElMessage.error('Invalid dict data id')
    router.back()
    return
  }

  try {
    loading.value = true
    data.value = await getDataById(id)
  } catch (error) {
    ElMessage.error(t('query_failed'))
    router.back()
  } finally {
    loading.value = false
  }
}

const handleBack = () => {
  router.back()
}

onMounted(() => {
  loadData()
})
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
