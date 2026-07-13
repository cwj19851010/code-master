<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryParams" class="query-form">
        <el-form-item :label="$t('project_name')">
          <el-input v-model="queryParams.projectName" :placeholder="$t2('please_input', 'project_name')" clearable />
        </el-form-item>
        <el-form-item :label="$t('display_name')">
          <el-input v-model="queryParams.displayName" :placeholder="$t2('please_input', 'display_name')" clearable />
        </el-form-item>
        <el-form-item :label="$t('database_type')">
          <el-select v-model="queryParams.databaseType" :placeholder="$t2('please_select', 'database_type')" clearable>
            <el-option :label="$t('mysql')" :value="0" />
            <el-option :label="$t('sqlserver')" :value="1" />
            <el-option :label="$t('postgresql')" :value="2" />
            <el-option :label="$t('sqlite')" :value="3" />
            <el-option :label="$t('oracle')" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item :label="$t('project_type')">
          <el-select v-model="queryParams.projectType" :placeholder="$t2('please_select', 'project_type')" clearable>
            <el-option :label="$t('server_version')" :value="0" />
            <el-option :label="$t('client_version')" :value="1" />
          </el-select>
        </el-form-item>
        <el-form-item :label="$t('status')">
          <el-select v-model="queryParams.status" :placeholder="$t2('please_select', 'status')" clearable>
            <el-option :label="$t('not_initialized')" :value="0" />
            <el-option :label="$t('initialized')" :value="1" />
            <el-option :label="$t('running')" :value="2" />
            <el-option :label="$t('stopped')" :value="3" />
            <el-option :label="$t('initialize_failed')" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery">{{ $t('search') }}</el-button>
          <el-button @click="resetQuery">{{ $t('reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <div class="toolbar">
        <div>
          <el-button type="primary" @click="handleAdd" v-permission="'system:project:create'">
            {{ $t('add') }}
          </el-button>
          <el-button type="success" @click="handleGenerateTemplate" v-permission="'system:project:export'">
            {{ $t('generate_template') }}
          </el-button>
        </div>
      </div>

      <el-table :data="projectList" border>
          <el-table-column prop="projectName" :label="$t('project_name')" min-width="150" />
          <el-table-column prop="displayName" :label="$t('display_name')" min-width="150" />
          <el-table-column prop="databaseType" :label="$t('database_type')" width="120">
            <template #default="{ row }">
              <span v-if="row.databaseType === 0">{{ $t('mysql') }}</span>
              <span v-else-if="row.databaseType === 1">{{ $t('sqlserver') }}</span>
              <span v-else-if="row.databaseType === 2">{{ $t('postgresql') }}</span>
              <span v-else-if="row.databaseType === 3">{{ $t('sqlite') }}</span>
              <span v-else-if="row.databaseType === 4">{{ $t('oracle') }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="projectType" :label="$t('project_type')" width="120">
            <template #default="{ row }">
              <span v-if="row.projectType === 0">{{ $t('server_version') }}</span>
              <span v-else-if="row.projectType === 1">{{ $t('client_version') }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="status" :label="$t('status')" width="200">
            <template #default="{ row }">
              <div v-if="initializingProjects[row.id]">
                <el-progress :percentage="initializingProjects[row.id].progress" :status="initializingProjects[row.id].progress === 100 ? 'success' : undefined" />
                <div style="font-size: 12px; color: #909399; margin-top: 4px;">{{ initializingProjects[row.id].message }}</div>
              </div>
              <div v-else>
                <el-tag v-if="row.status === 0" type="info">{{ $t('not_initialized') }}</el-tag>
                <el-tag v-else-if="row.status === 1" type="success">{{ $t('initialized') }}</el-tag>
                <el-tag v-else-if="row.status === 2" type="success">{{ $t('running') }}</el-tag>
                <el-tag v-else-if="row.status === 3" type="warning">{{ $t('stopped') }}</el-tag>
                <el-tag v-else-if="row.status === 4" type="danger">{{ $t('initialize_failed') }}</el-tag>
              </div>
            </template>
          </el-table-column>
          <el-table-column prop="createTime" :label="$t('create_time')" width="180" >
              <template #default="{ row }">
                {{ formatDateTime(row.createTime) }}
              </template>
          </el-table-column>
          <el-table-column :label="$t('actions')" width="300" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" @click="handleView(row)" v-permission="'system:project:view'">
                {{ $t('view') }}
              </el-button>
              <el-button link type="primary" @click="handleEdit(row)" v-permission="'system:project:update'">
                {{ $t('edit') }}
              </el-button>
              <el-button
                link
                type="primary"
                @click="handleInitialize(row)"
                :loading="initializingProjects[row.id]?.loading"
                v-permission="'system:project:initialize'">
                {{ $t('initialize') }}
              </el-button>
              <el-button
                link
                type="success"
                @click="handleStart(row)"
                v-if="row.status === 1 || row.status === 3"
                v-permission="'system:project:start'">
                {{ $t('start') }}
              </el-button>
              <el-button
                link
                type="warning"
                @click="handleStop(row)"
                v-if="row.status === 2"
                v-permission="'system:project:stop'">
                {{ $t('stop') }}
              </el-button>
              <el-button link type="danger" @click="handleDelete(row)" v-permission="'system:project:delete'">
                {{ $t('delete') }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>

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
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { formatDateTime } from '@/utils/dateFormat'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  getPagedList,
  deleteById,
  initializeProject,
  startProject,
  stopProject,
  getById,
  exportTemplate,
  initializeStep1,
  initializeStep2,
  initializeStep3,
  initializeStep4,
  initializeStep5,
  initializeStep6,
  initializeStep7,
  initializeStep8,
  initializeStep9,
  initializeStep10,
  initializeStep11,
  getInitializationState
} from '@/api/codegen/project'
import { isInClient, clientInitializeProject } from '@/utils/jsbridge'
import { getClientInitializeData } from '@/api/codegen/project'

defineOptions({
  name: 'CodegenProject'
})

const { t } = useI18n()
const router = useRouter()

const projectList = ref([])
const total = ref(0)
const initializingProjects = ref({})
const queryParams = ref({
  pageNum: 1,
  pageSize: 10,
  projectName: '',
  displayName: '',
  databaseType: null,
  projectType: null,
  status: null
})

const getList = async () => {
  try {
    const data = await getPagedList(queryParams.value)
    projectList.value = data.items
    total.value = data.total
  } catch (error) {
    ElMessage.error(t('query_failed'))
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
    projectType: null,
    status: null
  }
  getList()
}

const handleAdd = () => {
  router.push('/codegen/project/add')
}

const handleView = (row) => {
  router.push({
    path: '/codegen/project/detail',
    query: { id: row.id }
  })
}

const handleEdit = (row) => {
  router.push({
    path: '/codegen/project/edit',
    query: { id: row.id }
  })
}

const handleInitialize = async (row) => {
  try {
    // 检测运行环境
    const inClient = isInClient()

    if (inClient) {
      // 客户端模式：通过 JSBridge 初始化
      await ElMessageBox.confirm(
        t('confirm_initialize') + '\n' + t('client_mode_tip'),
        t('warning'),
        {
          confirmButtonText: t('confirm'),
          cancelButtonText: t('cancel'),
          type: 'warning'
        }
      )

      // 显示加载提示
      const loading = ElMessage({
        message: t('initializing'),
        type: 'info',
        duration: 0
      })

      try {
        // 获取客户端初始化数据（包含模板 Base64）
        const data = await getClientInitializeData(row.id)

        // 调用 JSBridge 在客户端初始化
        const result = await clientInitializeProject(data)

        loading.close()
        ElMessage.success(t('initialize_success'))
        getList()
      } catch (error) {
        loading.close()
        ElMessage.error(t('initialize_failed') + ': ' + error.message)
      }
    } else {
      // 服务端模式：分步调用后端 API 初始化
      await ElMessageBox.confirm(
        t('confirm_initialize') + '\n' + t('server_mode_tip'),
        t('warning'),
        {
          confirmButtonText: t('confirm'),
          cancelButtonText: t('cancel'),
          type: 'warning'
        }
      )

      // 设置初始化状态
      initializingProjects.value[row.id] = {
        loading: true,
        progress: 0,
        message: t('initializing')
      }

      // 分步初始化流程
      const steps = [
        { fn: initializeStep1, name: 'extract_template', progress: 20 },
        { fn: initializeStep2, name: 'generate_solution', progress: 30 },
        { fn: initializeStep3, name: 'update_database_config', progress: 40 },
        { fn: initializeStep4, name: 'update_port_config', progress: 50 },
        { fn: initializeStep5, name: 'create_migration', progress: 60 },
        { fn: initializeStep6, name: 'apply_migration', progress: 70 },
        { fn: initializeStep7, name: 'dotnet_restore', progress: 75 },
        { fn: initializeStep8, name: 'write_translations', progress: 80 },
        { fn: initializeStep9, name: 'npm_install', progress: 90 },
        { fn: initializeStep10, name: 'start_backend', progress: 95 },
        { fn: initializeStep11, name: 'start_frontend', progress: 100 }
      ]

      try {
        for (const step of steps) {
          // 更新进度
          initializingProjects.value[row.id] = {
            loading: true,
            progress: step.progress - 5,
            message: t(step.name)
          }

          // 调用步骤 API
          const result = await step.fn({ projectId: row.id })

          if (!result.success) {
            throw new Error(result.error || result.message)
          }

          // 更新完成进度
          initializingProjects.value[row.id] = {
            loading: step.progress < 100,
            progress: step.progress,
            message: result.message
          }
        }

        // 初始化完成
        ElMessage.success(t('initialize_success'))
        setTimeout(() => {
          delete initializingProjects.value[row.id]
          getList()
        }, 3000)
      } catch (error) {
        console.error('Initialize error:', error)
        initializingProjects.value[row.id] = {
          loading: false,
          progress: 0,
          message: t('initialize_failed') + ': ' + error.message
        }
        ElMessage.error(t('initialize_failed') + ': ' + error.message)
        setTimeout(() => {
          delete initializingProjects.value[row.id]
          getList()
        }, 5000)
      }
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('Initialize error:', error)
    }
  }
}

const handleStart = async (row) => {
  try {
    await startProject(row.id)
    ElMessage.success(t('start_success'))
    getList()
  } catch (error) {
    ElMessage.error(t('start_failed'))
  }
}

const handleStop = async (row) => {
  try {
    await stopProject(row.id)
    ElMessage.success(t('stop_success'))
    getList()
  } catch (error) {
    ElMessage.error(t('stop_failed'))
  }
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(t('confirm_delete'), t('warning'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    await deleteById(row.id)
    ElMessage.success(t('delete_success'))
    getList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

const handleGenerateTemplate = async () => {
  try {
    await ElMessageBox.confirm(t('confirm_generate_template'), t('warning'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })

    const loading = ElMessage({
      message: t('generating_template'),
      type: 'info',
      duration: 0
    })

    try {
      await exportTemplate()
      loading.close()
      ElMessage.success(t('generate_template_success'))
    } catch (error) {
      loading.close()
      ElMessage.error(t('generate_template_failed'))
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('Generate template error:', error)
    }
  }
}

onMounted(async () => {
  getList()
})
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';
</style>
