<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryForm" class="query-form">
        <el-form-item :label="t('task_name')">
          <el-input v-model="queryForm.taskName" :placeholder="t('please_input_name')" clearable />
        </el-form-item>
        <el-form-item :label="t('task_id')">
          <el-input v-model="queryForm.taskId" :placeholder="t('please_input_name')" clearable />
        </el-form-item>
        <el-form-item :label="t('status')">
          <el-select v-model="queryForm.status" :placeholder="t('please_select_status')" clearable>
            <el-option :label="t('success')" :value="0" />
            <el-option :label="t('failed')" :value="1" />
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
        <el-button type="danger" @click="handleBatchDelete" :icon="Delete" v-permission="'monitor:tasklog:delete'">
          {{ t('batch_delete') }}
        </el-button>
        <el-button type="danger" plain @click="handleClearAll" :icon="Delete" v-permission="'monitor:tasklog:delete'">
          {{ t('clear_all') }}
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
        <el-table-column prop="taskId" :label="t('task_id')" width="120" />
        <el-table-column prop="taskName" :label="t('task_name')" width="150" show-overflow-tooltip />
        <el-table-column prop="invokeTarget" :label="t('invoke_target')" width="200" show-overflow-tooltip />
        <el-table-column prop="status" :label="t('status')" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 0 ? 'success' : 'danger'">
              {{ row.status === 0 ? t('success') : t('failed') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="elapsed" :label="t('elapsed')" width="120" />
        <el-table-column prop="jobMessage" :label="t('job_message')" width="200" show-overflow-tooltip />
        <el-table-column prop="createTime" :label="t('create_time')" width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.createTime) }}
          </template>
        </el-table-column>
        <el-table-column :label="t('action')" fixed="right" width="150">
          <template #default="{ row }">
            <el-button type="primary" link @click="handleView(row)" :icon="View">
              {{ t('view') }}
            </el-button>
            <el-button type="danger" link @click="handleDelete(row)" :icon="Delete" v-permission="'monitor:tasklog:delete'">
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
        @current-change="getList"

      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Delete, View } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { getPagedList, deleteBatch, clear } from '@/api/monitor/tasklog'
import { formatDateTime } from '@/utils/dateFormat'

defineOptions({
  name: 'MonitorTasklog'
})

const { t } = useI18n()
const router = useRouter()

const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const selectedIds = ref([])

const queryForm = reactive({
  taskName: '',
  taskId: '',
  status: null,
  pageNum: 1,
  pageSize: 10
})

const getList = async () => {
  loading.value = true
  try {
    const res = await getPagedList(queryForm)
    console.log('任务日志查询响应:', res)
    const data = res.items ? res : (res.data || res)
    console.log('处理后的数据:', data)
    tableData.value = data.items || data.Items || []
    total.value = data.total || data.Total || 0
  } catch (error) {
    console.error('查询任务日志失败:', error)
    ElMessage.error(t('query_failed'))
  } finally {
    loading.value = false
  }
}

const handleQuery = () => {
  queryForm.pageNum = 1
  getList()
}

const handleReset = () => {
  queryForm.taskName = ''
  queryForm.taskId = ''
  queryForm.status = null
  handleQuery()
}

const handleSelectionChange = (selection) => {
  selectedIds.value = selection.map(item => item.id)
}

const handleView = (row) => {
  router.push({ path: '/monitor/tasklog/detail', query: { id: row.id } })
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(t('delete_confirm'), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    await deleteBatch([row.id])
    ElMessage.success(t('delete_success'))
    getList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

const handleBatchDelete = async () => {
  if (selectedIds.value.length === 0) {
    ElMessage.warning(t('please_select_delete'))
    return
  }
  try {
    await ElMessageBox.confirm(
      t('batch_delete_confirm').replace('{count}', selectedIds.value.length),
      t('prompt'),
      {
        confirmButtonText: t('confirm'),
        cancelButtonText: t('cancel'),
        type: 'warning'
      }
    )
    await deleteBatch(selectedIds.value)
    ElMessage.success(t('batch_delete_success'))
    getList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

const handleClearAll = async () => {
  try {
    await ElMessageBox.confirm(t('clear_all_confirm'), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    await clear()
    ElMessage.success(t('delete_success'))
    getList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

onMounted(() => {
  getList()
})
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';
</style>
