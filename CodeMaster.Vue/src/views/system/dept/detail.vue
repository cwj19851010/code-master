<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
        </div>
      </template>

      <el-descriptions :column="1" border v-loading="loading">
        <el-descriptions-item :label="t('name')">
          {{ detail.name }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('parentDept')">
          {{ detail.parentName || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('create_time')">
          {{ detail.createTime }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('update_time')">
          {{ detail.updateTime || '-' }}
        </el-descriptions-item>
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
import { getById } from '@/api/system/dept'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()

const loading = ref(false)
const detail = reactive({
  name: '',
  parentName: '',
  createTime: '',
  updateTime: ''
})

const loadDeptDetail = async () => {
  loading.value = true
  try {
    const id = route.query.id
    const res = await getById(id)
    Object.assign(detail, res)
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
  loadDeptDetail()
})
</script>

<style scoped lang="scss">
.card-header {
  font-weight: bold;
}
</style>
