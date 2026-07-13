<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryForm" class="query-form">
        <el-form-item :label="t('code')">
          <el-input v-model="queryForm.tenantCode" :placeholder="t2('please_input', 'code')" clearable style="width: 180px;" />
        </el-form-item>
        <el-form-item :label="t('name')">
          <el-input v-model="queryForm.tenantName" :placeholder="t2('please_input', 'name')" clearable style="width: 180px;" />
        </el-form-item>
        <el-form-item :label="t('isolation_type')">
          <el-select v-model="queryForm.isolationType" :placeholder="t2('please_select', 'isolation')" clearable style="width: 180px;">
            <el-option :label="t('isolation_physical')" :value="0" />
            <el-option :label="t('isolation_logical')" :value="1" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('status')">
          <el-select v-model="queryForm.status" :placeholder="t2('please_select', 'status')" clearable style="width: 180px;">
            <el-option :label="t('normal')" :value="0" />
            <el-option :label="t('disabled')" :value="1" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery" :icon="Search">{{ t('search') }}</el-button>
          <el-button @click="handleReset" :icon="Refresh">{{ t('reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <div class="toolbar">
        <div>
          <el-button type="primary" @click="handleAdd" :icon="Plus" v-permission="'system:tenant:create'">
            {{ t('add') }}
          </el-button>
          <el-button type="success" @click="handleExport" :icon="Download" v-permission="'system:tenant:export'">
            {{ t('export') }}
          </el-button>
          <el-button type="warning" @click="handleImport" :icon="Upload" v-permission="'system:tenant:import'">
            {{ t('import') }}
          </el-button>
          <el-button type="danger" @click="handleBatchDelete" :icon="Delete" v-permission="'system:tenant:delete'">
            {{ t('batch_delete') }}
          </el-button>
        </div>
        <el-button type="info" @click="handleOperLog" :icon="Document">
          {{ t('operlog') }}
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
        <el-table-column prop="tenantCode" :label="t('code')" width="150" />
        <el-table-column prop="tenantName" :label="t('name')"/>
        <el-table-column prop="isolationType" :label="t('isolation_type')" width="120">
          <template #default="{ row }">
            <el-tag :type="row.isolationType === 0 ? 'success' : 'info'">
              {{ row.isolationType === 0 ? t('isolation_physical') : t('isolation_logical') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="dbType" :label="t('db_type')" width="120">
          <template #default="{ row }">
            <span v-if="row.dbType === 1">SQL Server</span>
            <span v-else-if="row.dbType === 2">MySQL</span>
            <span v-else-if="row.dbType === 3">PostgreSQL</span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" :label="t('status')" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 0 ? 'success' : 'danger'">
              {{ row.status === 0 ? t('normal') : t('disabled') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="expireTime" :label="t('expire_time')" width="180">
          <template #default="{ row }">
            {{ row.expireTime ? formatDateTime(row.expireTime) : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="createTime" :label="t('create_time')" width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.createTime) }}
          </template>
        </el-table-column>
        <el-table-column :label="t('action')" fixed="right" width="220">
          <template #default="{ row }">
            <el-button type="primary" link @click="handleEdit(row)" :icon="Edit" v-permission="'system:tenant:update'">
              {{ t('edit') }}
            </el-button>
            <el-button type="info" link @click="handleView(row)" :icon="View" v-permission="'system:tenant:view'">
              {{ t('view') }}
            </el-button>
            <el-button type="danger" link @click="handleDelete(row)" :icon="Delete" v-permission="'system:tenant:delete'">
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
        @current-change="handleQuery"
        @size-change="handleQuery"
        
      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, View, Download, Upload, Document } from '@element-plus/icons-vue'
import { getPagedList, deleteById } from '@/api/system/tenant'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { formatDateTime } from '@/utils/dateFormat'

defineOptions({
  name: 'SystemTenant'
})

const router = useRouter()
const { t } = useI18n()

const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const selectedRows = ref([])

const queryForm = reactive({
  tenantCode: '',
  tenantName: '',
  isolationType: null,
  status: null,
  pageNum: 1,
  pageSize: 10
})

const handleQuery = async () => {
  try {
    loading.value = true
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
  queryForm.tenantCode = ''
  queryForm.tenantName = ''
  queryForm.isolationType = null
  queryForm.status = null
  queryForm.pageNum = 1
  handleQuery()
}

const handleAdd = () => {
  router.push('/system/tenant/add')
}

const handleEdit = (row) => {
  router.push({
    path: '/system/tenant/edit',
    query: { id: row.id }
  })
}

const handleView = (row) => {
  router.push({
    path: '/system/tenant/detail',
    query: { id: row.id }
  })
}

const handleExport = () => {
  ElMessage.info(t('export_pending'))
}

const handleImport = () => {
  ElMessage.info(t('import_pending'))
}

const handleOperLog = () => {
  ElMessage.info(t('operlog_pending'))
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(t('delete_confirm'), t('prompt'), {
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

refreshOnReactivated(handleQuery)
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';
</style>
