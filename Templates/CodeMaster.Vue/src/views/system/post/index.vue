<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryForm" class="query-form">
        <el-form-item :label="t('name')">
          <el-input v-model="queryForm.postName" :placeholder="t('please_input_name')" clearable />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery" :icon="Search">{{ t('search') }}</el-button>
          <el-button @click="handleReset" :icon="Refresh">{{ t('reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <div class="toolbar">
        <el-button type="primary" @click="handleAdd" :icon="Plus" v-permission="'system:post:create'">
          {{ t('add') }}
        </el-button>
        <el-button type="danger" @click="handleBatchDelete" :icon="Delete" v-permission="'system:post:delete'">
          {{ t('batch_delete') }}
        </el-button>
      </div>

      <el-table
          
          v-loading="loading"
          :data="tableData"
          border
          stripe
        @selection-change="handleSelectionChange"
        >
        <el-table-column type="selection" width="55" />
        <el-table-column prop="postName" :label="t('name')" width="200" />
        <el-table-column prop="dataScope" :label="t('data_scope')" width="150">
          <template #default="{ row }">
            <el-tag v-if="row.dataScope === 1" type="info">{{ t('data_scope_self') }}</el-tag>
            <el-tag v-else-if="row.dataScope === 2" type="warning">{{ t('data_scope_dept') }}</el-tag>
            <el-tag v-else type="success">{{ t('data_scope_all') }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" :label="t('remark')" show-overflow-tooltip />
        <el-table-column prop="createTime" :label="t('create_time')" width="180" >
          <template #default="{ row }">
            {{ formatDateTime(row.createTime) }}
          </template>
        </el-table-column>
        <el-table-column :label="t('action')" fixed="right" width="250">
          <template #default="{ row }">
            <el-button type="primary" link @click="handleView(row)" :icon="View" v-permission="'system:post:view'">
              {{ t('view') }}
            </el-button>
            <el-button type="primary" link @click="handleEdit(row)" :icon="Edit" v-permission="'system:post:update'">
              {{ t('edit') }}
            </el-button>
            <el-button type="danger" link @click="handleDelete(row)" :icon="Delete" v-permission="'system:post:delete'">
              {{ t('delete') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        v-model:current-page="queryForm.pageNum"
        v-model:page-size="queryForm.pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleQuery"
        @current-change="handleQuery"
        
      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, View } from '@element-plus/icons-vue'
import { getPagedList, deleteById } from '@/api/system/post'
import { useI18n } from 'vue-i18n'
import { formatDateTime } from '@/utils/dateFormat'

defineOptions({
  name: 'SystemPost'
})

const { t } = useI18n()
const router = useRouter()

const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const selectedRows = ref([])

const queryForm = reactive({
  postName: '',
  pageNum: 1,
  pageSize: 10
})

const handleQuery = async () => {
  loading.value = true
  try {
    const res = await getPagedList(queryForm)
    tableData.value = res.items || []
    total.value = res.total || 0
  } catch (error) {
    ElMessage.error(t('query_failed'))
  } finally {
    loading.value = false
  }
}

const handleReset = () => {
  queryForm.postName = ''
  queryForm.pageNum = 1
  handleQuery()
}

const handleAdd = () => {
  router.push('/system/post/add')
}

const handleView = (row) => {
  router.push({
    path: '/system/post/detail',
    query: { id: row.id }
  })
}

const handleEdit = (row) => {
  router.push({
    path: '/system/post/edit',
    query: { id: row.id }
  })
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(t('delete_confirm'), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    await deleteById(row.id)
    ElMessage.success(t('delete_success'))
    handleQuery()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) {
    ElMessage.warning(t('please_select_delete'))
    return
  }
  try {
    await ElMessageBox.confirm(t('batch_delete_confirm', { count: selectedRows.value.length }), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    for (const row of selectedRows.value) {
      await deleteById(row.id)
    }
    ElMessage.success(t('batch_delete_success'))
    handleQuery()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

const handleSelectionChange = (selection) => {
  selectedRows.value = selection
}

onMounted(() => {
  handleQuery()
})
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';
</style>
