<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryParams" class="query-form">
        <el-form-item :label="t('name')">
          <el-input
            v-model="queryParams.name"
            :placeholder="t2('please_input', 'name')"
            clearable
            @keyup.enter="handleQuery"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery" :icon="Search">{{ t('search') }}</el-button>
          <el-button @click="resetQuery" :icon="Refresh">{{ t('reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <div class="toolbar">
        <div>
          <el-button
            v-permission="'system:dept:create'"
            type="primary"
            @click="handleAdd"
            :icon="Plus"
          >
            {{ t('add') }}
          </el-button>
        </div>
      </div>

      <!-- 树形表格 -->
      <el-table

          v-loading="loading"
          :data="deptList"
          row-key="id"
          :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
          default-expand-all
          border
          stripe
        >
        <el-table-column prop="name" :label="t('name')" min-width="200" />
        <el-table-column prop="createTime" :label="t('create_time')" width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.createTime) }}
          </template>
        </el-table-column>
        <el-table-column :label="t('action')" fixed="right" width="320">
          <template #default="{ row }">
            <el-button
              v-permission="'system:dept:create'"
              link
              type="primary"
              @click="handleAdd(row)"
              :icon="Plus"
            >
              {{ t('add') }}
            </el-button>
            <el-button
              v-permission="'system:dept:query'"
              link
              type="primary"
              @click="handleDetail(row)"
              :icon="View"
            >
              {{ t('view') }}
            </el-button>
            <el-button
              v-permission="'system:dept:update'"
              link
              type="primary"
              @click="handleEdit(row)"
              :icon="Edit"
            >
              {{ t('edit') }}
            </el-button>
            <el-button
              v-permission="'system:dept:delete'"
              link
              type="danger"
              @click="handleDelete(row)"
              :icon="Delete"
            >
              {{ t('delete') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, View } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { getList, deleteById } from '@/api/system/dept'
import { formatDateTime } from '@/utils/dateFormat'
import { buildTree } from '@/utils/tree'

defineOptions({
  name: 'SystemDept'
})

const { t, t: t2 } = useI18n()
const router = useRouter()

// 查询参数
const queryParams = reactive({
  name: ''
})

// 数据列表
const deptList = ref([])
const loading = ref(false)

// 查询部门列表
const getDeptList = async () => {
  loading.value = true
  try {
    const data = await getList()
    // 使用通用方法构建树形结构
    deptList.value = buildTree(data || [])
  } catch (error) {
    console.error('获取部门列表失败:', error)
    ElMessage.error(t('loadFailed'))
  } finally {
    loading.value = false
  }
}

// 搜索
const handleQuery = () => {
  getDeptList()
}

// 重置
const resetQuery = () => {
  queryParams.name = ''
  getDeptList()
}

// 新增
const handleAdd = (row) => {
  if (row) {
    // 如果传入了行数据，说明是添加子部门
    router.push(`/system/dept/add?parentId=${row.id}`)
  } else {
    router.push('/system/dept/add')
  }
}

// 查看详情
const handleDetail = (row) => {
  router.push(`/system/dept/detail?id=${row.id}`)
}

// 编辑
const handleEdit = (row) => {
  router.push(`/system/dept/edit?id=${row.id}`)
}

// 删除
const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(
      t('confirmDelete'),
      t('warning'),
      {
        confirmButtonText: t('confirm'),
        cancelButtonText: t('cancel'),
        type: 'warning'
      }
    )

    await deleteById(row.id)
    ElMessage.success(t('deleteSuccess'))
    await getDeptList()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
      ElMessage.error(t('deleteFailed'))
    }
  }
}

// 初始化
onMounted(() => {
  getDeptList()
})

refreshOnReactivated(getDeptList)
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';

.toolbar > div {
  display: flex;
  gap: 10px;
}
</style>
