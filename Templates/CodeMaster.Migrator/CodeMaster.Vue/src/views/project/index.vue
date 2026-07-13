<template>
  <div class="app-container">
    <el-card shadow="never" style="margin-bottom: 16px">
      <el-form :inline="true" :model="queryForm" @submit.prevent>
        <el-form-item :label="t('project_name')">
          <el-input v-model="queryForm.projectName" :placeholder="t('please_enter')" clearable />
        </el-form-item>
        <el-form-item :label="t('display_name')">
          <el-input v-model="queryForm.displayName" :placeholder="t('please_enter')" clearable />
        </el-form-item>
        <el-form-item :label="t('database_type')">
          <el-select v-model="queryForm.databaseType" :placeholder="t('please_select')" clearable>
            <el-option :label="t('sqlserver')" :value="1" />
            <el-option :label="t('mysql')" :value="2" />
            <el-option :label="t('postgresql')" :value="3" />
            <el-option :label="t('sqlite')" :value="4" />
            <el-option :label="t('oracle')" :value="5" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('status')">
          <el-select v-model="queryForm.status" :placeholder="t('please_select')" clearable>
            <el-option :label="t('not_initialized')" :value="0" />
            <el-option :label="t('initialized')" :value="1" />
            <el-option :label="t('running')" :value="2" />
            <el-option :label="t('stopped')" :value="3" />
            <el-option :label="t('initialize_failed')" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery">{{ t('search') }}</el-button>
          <el-button @click="handleReset">{{ t('reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <div class="toolbar">
        <el-button type="primary" @click="handleAdd">{{ t('project_add') }}</el-button>
      </div>

      <el-table :data="tableData" border stripe>
        <el-table-column prop="projectName" :label="t('project_name')" min-width="150" />
        <el-table-column prop="displayName" :label="t('display_name')" min-width="150" />
        <el-table-column prop="databaseType" :label="t('database_type')" width="120">
          <template #default="{ row }">
            <el-tag v-if="row.databaseType === 1">SQL Server</el-tag>
            <el-tag v-else-if="row.databaseType === 2" type="success">MySQL</el-tag>
            <el-tag v-else-if="row.databaseType === 3" type="info">PostgreSQL</el-tag>
            <el-tag v-else-if="row.databaseType === 4" type="warning">SQLite</el-tag>
            <el-tag v-else-if="row.databaseType === 5" type="danger">Oracle</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" :label="t('status')" width="120">
          <template #default="{ row }">
            <el-tag v-if="row.status === 0" type="info">{{ t('not_initialized') }}</el-tag>
            <el-tag v-else-if="row.status === 1" type="success">{{ t('initialized') }}</el-tag>
            <el-tag v-else-if="row.status === 2" type="success">{{ t('running') }}</el-tag>
            <el-tag v-else-if="row.status === 3" type="warning">{{ t('stopped') }}</el-tag>
            <el-tag v-else-if="row.status === 4" type="danger">{{ t('initialize_failed') }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="projectPath" :label="t('project_path')" min-width="200" show-overflow-tooltip />
        <el-table-column prop="frontendPort" :label="t('frontend_port')" width="100" />
        <el-table-column prop="backendPort" :label="t('backend_port')" width="100" />
        <el-table-column :label="t('action')" width="300" fixed="right">
          <template #default="{ row }">
            <el-button v-if="row.status === 0" type="primary" size="small" @click="handleInitialize(row)">
              {{ t('initialize') }}
            </el-button>
            <el-button v-if="row.status === 1 || row.status === 3" type="success" size="small" @click="handleStart(row)">
              {{ t('start') }}
            </el-button>
            <el-button v-if="row.status === 2" type="warning" size="small" @click="handleStop(row)">
              {{ t('stop') }}
            </el-button>
            <el-button type="info" size="small" @click="handleDetail(row)">{{ t('detail') }}</el-button>
            <el-button type="primary" size="small" @click="handleEdit(row)">{{ t('edit') }}</el-button>
            <el-button type="danger" size="small" @click="handleDelete(row)">{{ t('delete') }}</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-pagination
        v-model:current-page="pagination.pageIndex"
        v-model:page-size="pagination.pageSize"
        :total="pagination.total"
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
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getProjectPagedList, deleteProject, initializeProject, startProject, stopProject } from '@/api/project'

const { t } = useI18n()
const router = useRouter()

const queryForm = reactive({
  projectName: '',
  displayName: '',
  databaseType: null,
  status: null
})

const pagination = reactive({
  pageIndex: 1,
  pageSize: 10,
  total: 0
})

const tableData = ref([])

const loadData = async () => {
  try {
    const params = {
      ...queryForm,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    }
    const res = await getProjectPagedList(params)
    tableData.value = res.items
    pagination.total = res.total
  } catch (error) {
    ElMessage.error(t('load_failed'))
  }
}

const handleQuery = () => {
  pagination.pageIndex = 1
  loadData()
}

const handleReset = () => {
  queryForm.projectName = ''
  queryForm.displayName = ''
  queryForm.databaseType = null
  queryForm.status = null
  handleQuery()
}

const handleAdd = () => {
  router.push('/project/add')
}

const handleEdit = (row) => {
  router.push(`/project/edit/${row.id}`)
}

const handleDetail = (row) => {
  router.push(`/project/detail/${row.id}`)
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(t('confirm_delete'), t('warning'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    await deleteProject(row.id)
    ElMessage.success(t('delete_success'))
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

const handleInitialize = async (row) => {
  try {
    await ElMessageBox.confirm(t('confirm_initialize'), t('warning'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    await initializeProject(row.id)
    ElMessage.success(t('initialize_success'))
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('initialize_failed'))
    }
  }
}

const handleStart = async (row) => {
  try {
    await startProject(row.id)
    ElMessage.success(t('start_success'))
    loadData()
  } catch (error) {
    ElMessage.error(t('start_failed'))
  }
}

const handleStop = async (row) => {
  try {
    await stopProject(row.id)
    ElMessage.success(t('stop_success'))
    loadData()
  } catch (error) {
    ElMessage.error(t('stop_failed'))
  }
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
@import '@/styles/list-page.scss';
</style>
