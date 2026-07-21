<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">
          <span>查看模块实体</span>
          <el-button @click="handleBack">返回</el-button>
        </div>
      </template>

      <el-descriptions v-if="detail.id" :column="3" border>
        <el-descriptions-item label="所属模块">
          {{ detail.projectModuleName }}
        </el-descriptions-item>
        <el-descriptions-item label="实体名称">
          {{ detail.name }}
        </el-descriptions-item>
        <el-descriptions-item label="实体描述">
          {{ detail.description }}
        </el-descriptions-item>
        <el-descriptions-item label="表名">
          {{ detail.tableName }}
        </el-descriptions-item>
        <el-descriptions-item label="排序">
          {{ detail.orderNum }}
        </el-descriptions-item>
        <el-descriptions-item label="选项">
          <el-tag v-if="detail.isTree" size="small" type="success" style="margin-right: 5px">树形</el-tag>
          <el-tag v-if="detail.isReadOnly" size="small" type="warning" style="margin-right: 5px">只读</el-tag>
          <el-tag v-if="detail.hasDataPermission" size="small" type="info" style="margin-right: 5px">数据权限</el-tag>
          <el-tag v-if="detail.hasTenant" size="small" type="primary">多租户</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="创建时间">
          {{ detail.createTime }}
        </el-descriptions-item>
        <el-descriptions-item label="更新时间" :span="2">
          {{ detail.updateTime }}
        </el-descriptions-item>
      </el-descriptions>
    </el-card>

    <!-- 字段列表 -->
    <el-card shadow="never" style="margin-top: 20px">
      <template #header>
        <div class="card-header">
          <span>字段列表</span>
        </div>
      </template>

      <el-table :data="detail.fields" border style="width: 100%">
        <el-table-column prop="name" label="字段名" width="150" />
        <el-table-column prop="description" label="描述" width="150" />
        <el-table-column prop="dataType" label="数据类型" width="120" />
        <el-table-column prop="maxLength" label="长度" width="80" />
        <el-table-column prop="defaultValue" label="默认值" width="120" />
        <el-table-column label="选项" width="250">
          <template #default="{ row }">
            <el-tag v-if="row.isPrimaryKey" size="small" type="danger" style="margin-right: 5px">主键</el-tag>
            <el-tag v-if="row.isRequired" size="small" type="warning" style="margin-right: 5px">表单必填</el-tag>
            <el-tag v-if="row.isNullable" size="small" type="success" style="margin-right: 5px">数据库可空</el-tag>
            <el-tag v-if="row.isUnique" size="small" type="info" style="margin-right: 5px">唯一</el-tag>
            <el-tag v-if="row.isIndexed" size="small" style="margin-right: 5px">索引</el-tag>
            <el-tag v-if="row.isSearchable" size="small" type="success" style="margin-right: 5px">可搜索</el-tag>
            <el-tag v-if="row.isSortable" size="small" type="primary">可排序</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="orderNum" label="排序" width="80" />
      </el-table>

      <el-empty v-if="!detail.fields || detail.fields.length === 0" description="暂无字段" />
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getById } from '@/api/codegen/moduleEntity'

const router = useRouter()
const route = useRoute()
const loading = ref(false)
const detail = ref({
  id: null,
  projectModuleId: null,
  projectModuleName: '',
  name: '',
  description: '',
  tableName: '',
  isTree: false,
  isReadOnly: false,
  hasDataPermission: false,
  hasTenant: false,
  orderNum: 0,
  createTime: '',
  updateTime: '',
  fields: []
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
