<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryForm" class="query-form">
        <el-form-item :label="t('task_name')">
          <el-input v-model="queryForm.taskName" :placeholder="t2('please_input', 'name')" clearable style="width: 180px;" />
        </el-form-item>
        <el-form-item :label="t('job_group')">
          <el-input v-model="queryForm.jobGroup" :placeholder="t2('please_input', 'name')" clearable style="width: 180px;" />
        </el-form-item>
        <el-form-item :label="t('task_type')">
          <el-select v-model="queryForm.taskType" :placeholder="t2('please_select', '')" clearable style="width: 180px;">
            <el-option :label="t('task_type_assembly')" :value="0" />
            <el-option :label="t('task_type_http')" :value="1" />
            <el-option :label="t('task_type_sql')" :value="2" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('status')">
          <el-select v-model="queryForm.status" :placeholder="t2('please_select', 'status')" clearable style="width: 180px;">
            <el-option :label="t('normal')" :value="0" />
            <el-option :label="t('paused')" :value="1" />
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
          <el-button type="primary" @click="handleAdd" :icon="Plus" v-permission="'system:task:create'">
            {{ t('add') }}
          </el-button>
          <el-button type="danger" @click="handleBatchDelete" :icon="Delete" :disabled="selectedRows.length === 0" v-permission="'system:task:delete'">
            {{ t('batch_delete') }}
          </el-button>
        </div>
      </div>

      <el-table
          
          v-loading="loading"
          :data="tableData"
          border
          stripe
        @selection-change="handleSelectionChange"
        >
        <el-table-column type="selection" width="55" />
        <el-table-column prop="id" :label="t('task_id')" width="80" show-overflow-tooltip />
        <el-table-column prop="taskName" :label="t('task_name')" width="140" show-overflow-tooltip />
        <el-table-column prop="jobGroup" :label="t('job_group')" width="120" />
        <el-table-column prop="invokeTarget" :label="t('invoke_target')" show-overflow-tooltip />
        <el-table-column prop="taskType" :label="t('task_type')" width="100">
          <template #default="{ row }">
            <el-tag v-if="row.taskType === 0" type="success">{{ t('task_type_assembly') }}</el-tag>
            <el-tag v-else-if="row.taskType === 1" type="primary">{{ t('task_type_http') }}</el-tag>
            <el-tag v-else-if="row.taskType === 2" type="warning">{{ t('task_type_sql') }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="cronExpression" :label="t('cron_expression')" width="150" show-overflow-tooltip />
        <el-table-column prop="runTimes" :label="t('run_times')" width="100" />
        <el-table-column prop="status" :label="t('status')" width="100">
          <template #default="{ row }">
            <el-switch
              v-model="row.status"
              :active-value="0"
              :inactive-value="1"
              @change="handleStatusChange(row)"
            />
          </template>
        </el-table-column>
        <el-table-column :label="t('action')" fixed="right" width="280">
          <template #default="{ row }">
            <el-button type="success" link @click="handleRun(row)" :icon="VideoPlay" v-permission="'system:task:run'">
              {{ t('run') }}
            </el-button>
            <el-button type="primary" link @click="handleEdit(row)" :icon="Edit" v-permission="'system:task:update'">
              {{ t('edit') }}
            </el-button>
            <el-button type="info" link @click="handleView(row)" :icon="View">
              {{ t('view') }}
            </el-button>
            <el-button type="danger" link @click="handleDelete(row)" :icon="Delete" v-permission="'system:task:delete'">
              {{ t('delete') }}
            </el-button>
            <el-button type="info" link @click="handleLog(row)" :icon="Document">
              {{ t('log') }}
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
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, View, Document, VideoPlay } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { getPagedList, deleteById, runTask, changeTaskStatus } from '@/api/monitor/task'

defineOptions({
  name: 'MonitorTask'
})

const router = useRouter()
const { t } = useI18n()

const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const selectedRows = ref([])

const queryForm = reactive({
  taskName: '',
  jobGroup: '',
  taskType: null,
  status: null,
  pageNum: 1,
  pageSize: 10
})

const handleQuery = async () => {
  loading.value = true
  try {
    const res = await getPagedList(queryForm)
    console.log('任务查询响应:', res)
    // 兼容响应拦截器解包后的数据
    const data = res.items ? res : (res.data || res)
    tableData.value = data.items || data.Items || []
    total.value = data.total || data.Total || 0
  } catch (error) {
    console.error('查询任务失败:', error)
    ElMessage.error(t('query_failed'))
  } finally {
    loading.value = false
  }
}

const handleReset = () => {
  queryForm.taskName = ''
  queryForm.jobGroup = ''
  queryForm.taskType = null
  queryForm.status = null
  queryForm.pageNum = 1
  handleQuery()
}

const handleAdd = () => {
  router.push('/monitor/task/add')
}

const handleEdit = (row) => {
  router.push({
    path: '/monitor/task/edit',
    query: { id: row.id }
  })
}

const handleView = (row) => {
  router.push({
    path: '/monitor/task/detail',
    query: { id: row.id }
  })
}

const handleRun = async (row) => {
  try {
    await ElMessageBox.confirm(t('run_task_confirm'), t('prompt'), {
      type: 'warning'
    })
    await runTask(row.id)
    ElMessage.success(t('run_success'))
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('run_failed'))
    }
  }
}

const handleStatusChange = async (row) => {
  try {
    await changeTaskStatus(row.id, row.status)
    ElMessage.success(t('update_success'))
    handleQuery()
  } catch (error) {
    ElMessage.error(t('update_failed'))
    // 恢复原状态
    row.status = row.status === 0 ? 1 : 0
  }
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
    // 批量删除逻辑
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

const handleLog = (row) => {
  router.push({
    path: '/monitor/tasklog',
    query: { taskId: row.id }
  })
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
