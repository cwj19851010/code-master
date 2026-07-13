<template>
  <div class="app-container">
    <!-- 搜索表单 -->
    <el-card shadow="never" class="search-card">
      <el-form :model="queryParams" inline>
        <el-form-item :label="$t('project_name')">
          <el-input
            v-model="queryParams.projectName"
            :placeholder="$t('please_input_project_name')"
            clearable
            style="width: 200px"
            @keyup.enter="handleQuery"
          />
        </el-form-item>
        <el-form-item :label="$t('display_name')">
          <el-input
            v-model="queryParams.displayName"
            :placeholder="$t('please_input_display_name')"
            clearable
            style="width: 200px"
            @keyup.enter="handleQuery"
          />
        </el-form-item>
        <el-form-item :label="$t('database_type')">
          <el-select
            v-model="queryParams.databaseType"
            :placeholder="$t('please_select_database_type')"
            clearable
            style="width: 150px"
          >
            <el-option :label="$t('db_mysql')" :value="0" />
            <el-option :label="$t('db_sqlserver')" :value="1" />
            <el-option :label="$t('db_postgresql')" :value="2" />
            <el-option :label="$t('db_sqlite')" :value="3" />
            <el-option :label="$t('db_oracle')" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item :label="$t('project_status')">
          <el-select
            v-model="queryParams.status"
            :placeholder="$t('please_select')"
            clearable
            style="width: 150px"
          >
            <el-option :label="$t('status_not_initialized')" :value="0" />
            <el-option :label="$t('status_initialized')" :value="1" />
            <el-option :label="$t('status_running')" :value="2" />
            <el-option :label="$t('status_stopped')" :value="3" />
            <el-option :label="$t('status_init_failed')" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery">{{ $t('search') }}</el-button>
          <el-button @click="resetQuery">{{ $t('reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 操作按钮和数据表格 -->
    <el-card shadow="never" class="mt-2">
      <el-row :gutter="10" class="mb-2">
        <el-col :span="1.5">
          <el-button
            type="primary"
            plain
            @click="handleAdd"
            v-permission="['system:project:create']"
          >
            {{ $t('add_project') }}
          </el-button>
        </el-col>
      </el-row>

      <!-- 数据表格 -->
      <div class="table-container">
        <el-table
          v-loading="loading"
          :data="projectList"
          height="100%"
        >
          <el-table-column :label="$t('project_name')" prop="projectName" width="150" />
          <el-table-column :label="$t('display_name')" prop="displayName" width="150" />
          <el-table-column :label="$t('project_type')" width="120">
            <template #default="{ row }">
              <el-tag v-if="row.projectType === 0" type="success">{{ $t('project_type_server') }}</el-tag>
              <el-tag v-else type="info">{{ $t('project_type_wpf') }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column :label="$t('database_type')" width="120">
            <template #default="{ row }">
              <span v-if="row.databaseType === 0">{{ $t('db_mysql') }}</span>
              <span v-else-if="row.databaseType === 1">{{ $t('db_sqlserver') }}</span>
              <span v-else-if="row.databaseType === 2">{{ $t('db_postgresql') }}</span>
              <span v-else-if="row.databaseType === 3">{{ $t('db_sqlite') }}</span>
              <span v-else-if="row.databaseType === 4">{{ $t('db_oracle') }}</span>
            </template>
          </el-table-column>
          <el-table-column :label="$t('project_status')" width="120">
            <template #default="{ row }">
              <el-tag v-if="row.status === 0" type="info">{{ $t('status_not_initialized') }}</el-tag>
              <el-tag v-else-if="row.status === 1" type="success">{{ $t('status_initialized') }}</el-tag>
              <el-tag v-else-if="row.status === 2" type="success">{{ $t('status_running') }}</el-tag>
              <el-tag v-else-if="row.status === 3" type="warning">{{ $t('status_stopped') }}</el-tag>
              <el-tag v-else-if="row.status === 4" type="danger">{{ $t('status_init_failed') }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column :label="$t('project_path')" prop="projectPath" min-width="200" show-overflow-tooltip />
          <el-table-column :label="$t('frontend_port')" prop="frontendPort" width="100" />
          <el-table-column :label="$t('backend_port')" prop="backendPort" width="100" />
          <el-table-column :label="$t('initialized_at')" prop="initializedAt" width="160" />
          <el-table-column :label="$t('operation')" width="280" fixed="right">
            <template #default="{ row }">
              <el-button
                link
                type="primary"
                @click="handleView(row)"
                v-permission="['system:project:view']"
              >
                {{ $t('view') }}
              </el-button>
              <el-button
                link
                type="primary"
                @click="handleEdit(row)"
                v-permission="['system:project:update']"
              >
                {{ $t('edit') }}
              </el-button>
              <el-button
                v-if="row.status === 0"
                link
                type="success"
                @click="handleInitialize(row)"
                v-permission="['system:project:initialize']"
              >
                {{ $t('initialize_project') }}
              </el-button>
              <el-button
                v-if="row.status === 1 || row.status === 3"
                link
                type="success"
                @click="handleStart(row)"
                v-permission="['system:project:start']"
              >
                {{ $t('start_project') }}
              </el-button>
              <el-button
                v-if="row.status === 2"
                link
                type="warning"
                @click="handleStop(row)"
                v-permission="['system:project:stop']"
              >
                {{ $t('stop_project') }}
              </el-button>
              <el-button
                link
                type="danger"
                @click="handleDelete(row)"
                v-permission="['system:project:delete']"
              >
                {{ $t('delete') }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <!-- 分页 -->
      <el-pagination
        v-model:current-page="queryParams.pageNum"
        v-model:page-size="queryParams.pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="getList"
        @current-change="getList"
      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { getProjectPagedList, deleteProject, initializeProject, startProject, stopProject } from '@/api/system/project'

const { t } = useI18n()
const router = useRouter()

const loading = ref(false)
const projectList = ref([])
const total = ref(0)

const queryParams = ref({
  pageNum: 1,
  pageSize: 10,
  projectName: '',
  displayName: '',
  databaseType: null,
  status: null
})

const getList = async () => {
  loading.value = true
  try {
    const { data } = await getProjectList(queryParams.value)
    projectList.value = data.items
    total.value = data.total
  } catch (error) {
    console.error('获取项目列表失败:', error)
  } finally {
    loading.value = false
  }
}

const handleQuery = () => {
  queryParams.value.pageNum = 1
  getList()
}

const resetQuery = () => {
  queryParams.value = {
    pageNum: 1,
    pageSize: 10,
    projectName: '',
    displayName: '',
    databaseType: null,
    status: null
  }
  getList()
}

const handleAdd = () => {
  router.push('/system/project/add')
}

const handleView = (row) => {
  router.push(`/system/project/detail?id=${row.id}`)
}

const handleEdit = (row) => {
  router.push(`/system/project/edit?id=${row.id}`)
}

const handleInitialize = async (row) => {
  try {
    await ElMessageBox.confirm(
      '确认要初始化该项目吗？',
      t('warning'),
      {
        confirmButtonText: t('confirm'),
        cancelButtonText: t('cancel'),
        type: 'warning'
      }
    )
    await initializeProject(row.id)
    ElMessage.success('初始化成功')
    getList()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('初始化项目失败:', error)
    }
  }
}

const handleStart = async (row) => {
  try {
    await startProject(row.id)
    ElMessage.success('启动成功')
    getList()
  } catch (error) {
    console.error('启动项目失败:', error)
  }
}

const handleStop = async (row) => {
  try {
    await stopProject(row.id)
    ElMessage.success('停止成功')
    getList()
  } catch (error) {
    console.error('停止项目失败:', error)
  }
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(
      t('confirm_delete'),
      t('warning'),
      {
        confirmButtonText: t('confirm'),
        cancelButtonText: t('cancel'),
        type: 'warning'
      }
    )
    await deleteProject(row.id)
    ElMessage.success(t('delete_success'))
    getList()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除项目失败:', error)
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
