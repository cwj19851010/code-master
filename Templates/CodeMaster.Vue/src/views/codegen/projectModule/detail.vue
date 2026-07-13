<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">
          <span>查看项目模块</span>
          <el-button @click="handleBack">返回</el-button>
        </div>
      </template>

      <el-descriptions v-if="detail.id" :column="2" border>
        <el-descriptions-item label="所属项目">
          {{ detail.projectName }}
        </el-descriptions-item>
        <el-descriptions-item label="模块名称">
          {{ detail.moduleName }}
        </el-descriptions-item>
        <el-descriptions-item label="模块描述" :span="2">
          {{ detail.moduleDescription }}
        </el-descriptions-item>
        <el-descriptions-item label="图标">
          <el-icon v-if="detail.icon">
            <component :is="detail.icon" />
          </el-icon>
          <span v-else>-</span>
        </el-descriptions-item>
        <el-descriptions-item label="排序">
          {{ detail.orderNum }}
        </el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="detail.status ? 'success' : 'danger'">
            {{ detail.status ? '启用' : '禁用' }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="创建时间">
          {{ detail.createTime }}
        </el-descriptions-item>
        <el-descriptions-item label="更新时间" :span="2">
          {{ detail.updateTime }}
        </el-descriptions-item>
      </el-descriptions>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getById } from '@/api/codegen/projectModule'

const router = useRouter()
const route = useRoute()
const loading = ref(false)
const detail = ref({
  id: null,
  projectId: null,
  projectName: '',
  moduleName: '',
  moduleDescription: '',
  icon: '',
  orderNum: 0,
  status: 1,
  createTime: '',
  updateTime: ''
})

onMounted(() => {
  loadData()
})

async function loadData() {
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

function handleBack() {
  router.back()
}
</script>

<style scoped lang="scss">
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
