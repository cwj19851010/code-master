<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">{{ t('detail') }} {{ t('text') }}</div>
      </template>
      <el-descriptions :column="1" border v-loading="loading">
        <el-descriptions-item :label="t('code')">{{ data.langCode }}</el-descriptions-item>
        <el-descriptions-item :label="t('key')">{{ data.langKey }}</el-descriptions-item>
        <el-descriptions-item :label="t('value')">{{ data.langValue }}</el-descriptions-item>
        <el-descriptions-item :label="t('category')">{{ data.category }}</el-descriptions-item>
        <el-descriptions-item :label="t('remark')">{{ data.remark }}</el-descriptions-item>
        <el-descriptions-item :label="t('create_time')">{{ formatDateTime(data.createTime) }}</el-descriptions-item>
        <el-descriptions-item :label="t('update_time')">{{ formatDateTime(data.updateTime) }}</el-descriptions-item>
      </el-descriptions>
      <div style="margin-top: 20px">
        <el-button @click="handleBack">{{ t('back') }}</el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { getTextById } from '@/api/system/lang'
import { formatDateTime } from '@/utils/dateFormat'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()

const loading = ref(false)
const data = reactive({
  langCode: '',
  langKey: '',
  langValue: '',
  category: '',
  remark: '',
  createTime: '',
  updateTime: ''
})

const loadData = async () => {
  const id = route.query.id
  if (!id) {
    ElMessage.error(t('loadFailed'))
    router.back()
    return
  }

  loading.value = true
  try {
    const res = await getTextById(id)
    Object.assign(data, res)
  } catch (error) {
    ElMessage.error(t('loadFailed'))
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
.card-header {
  font-weight: bold;
}
</style>
