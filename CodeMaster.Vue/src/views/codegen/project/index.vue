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
            <el-option :label="$t('mysql')" :value="2" />
            <el-option :label="$t('sqlserver')" :value="1" />
            <el-option :label="$t('postgresql')" :value="3" />
            <el-option :label="$t('sqlite')" :value="4" />
            <el-option :label="$t('oracle')" :value="5" />
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
          <el-button type="primary" @click="handleAdd" v-permission="'codegen:project:create'">
            {{ $t('add') }}
          </el-button>
          <el-button v-if="canManageProjectTemplate" type="success" @click="handleGenerateTemplate">
            {{ $t('generate_template') }}
          </el-button>
          <el-upload
            v-if="canManageProjectTemplate"
            ref="templateUploadRef"
            class="template-upload"
            accept=".zip"
            :auto-upload="false"
            :show-file-list="false"
            :on-change="handleTemplateUploadChange"
          >
            <el-button type="warning" :loading="uploadingTemplate">
              上传模板
            </el-button>
          </el-upload>
          <el-button type="warning" :loading="downloadingTemplate" @click="handleDownloadTemplate">
            下载模板
          </el-button>
          <el-button type="info" @click="openClientConfig">
            本地配置
          </el-button>
        </div>
      </div>

      <el-table :data="projectList" border>
          <el-table-column prop="projectName" :label="$t('project_name')" min-width="150" />
          <el-table-column prop="displayName" :label="$t('display_name')" min-width="150" />
          <el-table-column prop="databaseType" :label="$t('database_type')" width="120">
            <template #default="{ row }">
              <span v-if="row.databaseType === 2">{{ $t('mysql') }}</span>
              <span v-else-if="row.databaseType === 1">{{ $t('sqlserver') }}</span>
              <span v-else-if="row.databaseType === 3">{{ $t('postgresql') }}</span>
              <span v-else-if="row.databaseType === 4">{{ $t('sqlite') }}</span>
              <span v-else-if="row.databaseType === 5">{{ $t('oracle') }}</span>
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
          <el-table-column :label="$t('actions')" width="480" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" @click="handleView(row)" v-permission="'codegen:project:view'">
                {{ $t('view') }}
              </el-button>
              <el-button link type="primary" @click="handleEdit(row)" v-permission="'codegen:project:update'">
                {{ $t('edit') }}
              </el-button>
              <el-button
                link
                type="primary"
                @click="handleInitialize(row)"
                :loading="initializingProjects[row.id]?.loading"
                v-permission="'codegen:project:initialize'">
                {{ $t('initialize') }}
              </el-button>

              <!-- 启动下拉按钮 -->
              <el-dropdown
                trigger="click"
                v-permission="'codegen:project:start'"
                style="margin: 0 4px; padding-top:4px"
                @command="(cmd) => handleStartDropdown(cmd, row)">
                <el-button link type="success" :loading="startingMap[row.id]">
                  {{ $t('start') }}<el-icon class="el-icon--right"><arrow-down /></el-icon>
                </el-button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item command="frontend">🖥️ 启动前端</el-dropdown-item>
                    <el-dropdown-item command="backend">⚙️ 启动后端</el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>

              <!-- 停止下拉按钮 -->
              <el-dropdown
                trigger="click"
                v-permission="'codegen:project:stop'"
                style="margin: 0 4px; padding-top:4px"
                @command="(cmd) => handleStopDropdown(cmd, row)">
                <el-button link type="warning" :loading="stoppingMap[row.id]">
                  {{ $t('stop') }}<el-icon class="el-icon--right"><arrow-down /></el-icon>
                </el-button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item command="frontend">🖥️ 停止前端</el-dropdown-item>
                    <el-dropdown-item command="backend">⚙️ 停止后端</el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>

              <!-- 迁移数据库 -->
              <el-button
                link
                type="primary"
                :loading="migratingMap[row.id]"
                @click="handleMigrate(row)"
                v-permission="'codegen:project:migrate'">
                迁移数据库
              </el-button>

              <!-- 编译 -->
              <el-button
                link
                type="info"
                :loading="buildingMap[row.id]"
                @click="handleBuild(row)"
                v-permission="'codegen:project:build'">
                编译
              </el-button>

              <el-button link type="danger" @click="handleDelete(row)" v-permission="'codegen:project:delete'">
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

    <el-dialog v-model="clientConfigVisible" title="本地执行配置" width="640px">
      <el-form :model="clientConfigForm" label-width="130px">
        <el-form-item label="启用本地执行">
          <el-switch v-model="clientConfigForm.localExecution" />
        </el-form-item>
        <el-form-item label="Agent URL">
          <el-input v-model="clientConfigForm.agentUrl" placeholder="http://127.0.0.1:39721" clearable />
        </el-form-item>
        <el-form-item label="Agent Token">
          <el-input v-model="clientConfigForm.token" show-password clearable />
        </el-form-item>
        <el-form-item label="服务器地址">
          <el-input v-model="clientConfigForm.serverBaseUrl" placeholder="https://codemaster.example.com" clearable />
        </el-form-item>
        <el-form-item label="本地项目目录">
          <el-input v-model="clientConfigForm.targetPath" clearable />
        </el-form-item>
        <el-form-item label="数据库类型">
          <el-select v-model="clientConfigForm.databaseType" clearable>
            <el-option label="MySQL" value="MySQL" />
            <el-option label="SqlServer" value="SqlServer" />
            <el-option label="PostgreSQL" value="PostgreSQL" />
            <el-option label="SQLite" value="SQLite" />
            <el-option label="Oracle" value="Oracle" />
          </el-select>
        </el-form-item>
        <el-form-item label="连接字符串">
          <el-input v-model="clientConfigForm.connectionString" type="textarea" :rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="clientConfigVisible = false">取消</el-button>
        <el-button type="primary" @click="saveClientConfig">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ArrowDown } from '@element-plus/icons-vue'
import { t2 } from '@/i18n'
import { formatDateTime } from '@/utils/dateFormat'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'
import {
  getPagedList,
  deleteById,
  initializeProject,
  startProject,
  stopProject,
  startFrontend,
  startBackend,
  stopFrontend,
  stopBackend,
  migrateDatabase,
  buildProject,
  getById,
  exportTemplate,
  downloadTemplate,
  downloadTemplateToLocal,
  uploadTemplate,
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
import {
  DEFAULT_CODEMASTER_SERVER_BASE_URL,
  getCodeMasterClientConfig,
  saveCodeMasterClientConfig,
  shouldUseLocalCodegenExecution
} from '@/utils/codegenExecution'

defineOptions({
  name: 'CodegenProject'
})

const { t } = useI18n()
const router = useRouter()
const userStore = useUserStore()

const projectList = ref([])
const total = ref(0)
const initializingProjects = ref({})
const startingMap = ref({})    // key=项目ID，value=true表示正在启动
const stoppingMap = ref({})    // key=项目ID，value=true表示正在停止
const migratingMap = ref({})   // key=项目ID，value=true表示正在迁移
const buildingMap = ref({})    // key=项目ID，value=true表示正在编译
const downloadingTemplate = ref(false)
const uploadingTemplate = ref(false)
const templateUploadRef = ref(null)
const canManageProjectTemplate = computed(() => userStore.isHostAdmin)
const clientConfigVisible = ref(false)
const clientConfigForm = ref({
  localExecution: true,
  agentUrl: 'http://127.0.0.1:39721',
  token: '',
  serverBaseUrl: DEFAULT_CODEMASTER_SERVER_BASE_URL,
  targetPath: '',
  databaseType: '',
  connectionString: ''
})
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

const openClientConfig = () => {
  clientConfigForm.value = {
    localExecution: true,
    agentUrl: 'http://127.0.0.1:39721',
    token: '',
    serverBaseUrl: DEFAULT_CODEMASTER_SERVER_BASE_URL,
    targetPath: '',
    databaseType: '',
    connectionString: '',
    ...getCodeMasterClientConfig()
  }
  clientConfigVisible.value = true
}

const saveClientConfig = () => {
  saveCodeMasterClientConfig(clientConfigForm.value)
  clientConfigVisible.value = false
  ElMessage.success('保存成功')
}

const handleInitialize = async (row) => {
  try {
    // 检测运行环境
    const useLegacyClientBridge = isInClient() && !shouldUseLocalCodegenExecution()

    if (useLegacyClientBridge) {
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

// ===================== 新增：下拉启动/停止/迁移/编译 =====================

const handleStartDropdown = async (command, row) => {
  startingMap.value[row.id] = true
  try {
    let result
    if (command === 'frontend') {
      result = await startFrontend(row.id)
    } else {
      result = await startBackend(row.id)
    }
    if (result.success) {
      ElMessage.success(result.message || (command === 'frontend' ? '前端启动成功' : '后端启动成功'))
    } else {
      ElMessage.error(result.message || '启动失败')
    }
    getList()
  } catch (error) {
    ElMessage.error(`启动失败：${error.message || ''}`)
  } finally {
    startingMap.value[row.id] = false
  }
}

const handleStopDropdown = async (command, row) => {
  stoppingMap.value[row.id] = true
  try {
    let result
    if (command === 'frontend') {
      result = await stopFrontend(row.id)
    } else {
      result = await stopBackend(row.id)
    }
    if (result.success) {
      ElMessage.success(result.message || (command === 'frontend' ? '前端已停止' : '后端已停止'))
    } else {
      ElMessage.warning(result.message || '停止操作完成')
    }
    getList()
  } catch (error) {
    ElMessage.error(`停止失败：${error.message || ''}`)
  } finally {
    stoppingMap.value[row.id] = false
  }
}

const handleMigrate = async (row) => {
  try {
    await ElMessageBox.confirm(
      `确认对项目「${row.projectName}」执行数据库迁移？\n系统将自动生成迁移脚本并更新数据库。`,
      '迁移数据库',
      {
        confirmButtonText: '确认迁移',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
  } catch {
    return
  }

  migratingMap.value[row.id] = true
  const loadingMsg = ElMessage({
    message: `正在迁移「${row.projectName}」数据库，请稍候...`,
    type: 'info',
    duration: 0
  })

  try {
    const result = await migrateDatabase(row.id)
    loadingMsg.close()
    if (result.success) {
      ElMessage.success(result.message || '数据库迁移成功')
    } else {
      await ElMessageBox.alert(
        `<strong>${result.message}</strong><br/><pre style="white-space:pre-wrap;font-size:12px;max-height:300px;overflow-y:auto">${result.output || ''}</pre>`,
        '迁移失败',
        { dangerouslyUseHTMLString: true, type: 'error' }
      ).catch(() => {})
    }
  } catch (error) {
    loadingMsg.close()
    ElMessage.error(`迁移请求失败：${error.message || ''}`)
  } finally {
    migratingMap.value[row.id] = false
  }
}

const handleBuild = async (row) => {
  buildingMap.value[row.id] = true
  const loadingMsg = ElMessage({
    message: `正在编译「${row.projectName}」，请稍候...`,
    type: 'info',
    duration: 0
  })

  try {
    const result = await buildProject(row.id)
    loadingMsg.close()
    if (result.success) {
      ElMessage.success(result.message || '编译成功')
    } else {
      await ElMessageBox.alert(
        `<strong>${result.message}</strong><br/><pre style="white-space:pre-wrap;font-size:12px;max-height:300px;overflow-y:auto">${result.output || ''}</pre>`,
        '编译失败',
        { dangerouslyUseHTMLString: true, type: 'error' }
      ).catch(() => {})
    }
  } catch (error) {
    loadingMsg.close()
    ElMessage.error(`编译请求失败：${error.message || ''}`)
  } finally {
    buildingMap.value[row.id] = false
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

const createTemplateFileName = () => {
  const now = new Date()
  const pad = value => String(value).padStart(2, '0')
  const timestamp = [
    now.getFullYear(),
    pad(now.getMonth() + 1),
    pad(now.getDate()),
    pad(now.getHours()),
    pad(now.getMinutes()),
    pad(now.getSeconds())
  ].join('')
  return `CodeMaster_Template_${timestamp}.zip`
}

const readDownloadError = async (error) => {
  const data = error?.response?.data
  if (data instanceof Blob) {
    const text = await data.text()
    if (text) {
      try {
        return JSON.parse(text).message || text
      } catch {
        return text
      }
    }
  }
  return error?.message || '下载失败'
}

const handleTemplateUploadChange = async (uploadFile) => {
  const file = uploadFile?.raw
  if (!file) {
    return
  }

  const maxTemplateUploadSize = 500 * 1024 * 1024
  if (!file.name.toLowerCase().endsWith('.zip')) {
    ElMessage.warning('请选择 .zip 模板文件')
    templateUploadRef.value?.clearFiles()
    return
  }

  if (file.size > maxTemplateUploadSize) {
    ElMessage.warning('模板文件不能超过 500 MB')
    templateUploadRef.value?.clearFiles()
    return
  }

  try {
    await ElMessageBox.confirm(
      `确认上传模板「${file.name}」？上传后会作为服务端最新项目模板。`,
      '上传模板',
      {
        confirmButtonText: '确认上传',
        cancelButtonText: t('cancel'),
        type: 'warning'
      }
    )

    uploadingTemplate.value = true
    const result = await uploadTemplate(file)
    ElMessage.success(`模板上传成功：${result?.fileName || file.name}`)
    await handleDownloadTemplate()
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') {
      ElMessage.error(error?.message || '模板上传失败')
    }
  } finally {
    uploadingTemplate.value = false
    templateUploadRef.value?.clearFiles()
  }
}

const revokeDownloadUrlLater = (url) => {
  window.setTimeout(() => {
    window.URL.revokeObjectURL(url)
  }, 60000)
}

const downloadTemplateInBrowser = async () => {
  const response = await downloadTemplate()
  const blob = response instanceof Blob
    ? response
    : new Blob([response], { type: 'application/zip' })

  if (!blob.size) {
    throw new Error('模板文件为空')
  }

  const url = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = createTemplateFileName()
  link.style.display = 'none'
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  revokeDownloadUrlLater(url)
}

const handleDownloadTemplate = async () => {
  downloadingTemplate.value = true
  try {
    if (shouldUseLocalCodegenExecution()) {
      const result = await downloadTemplateToLocal()
      const data = result?.data || result || {}
      const message = data.filePath ? `模板已保存到本地：${data.filePath}` : (result?.message || '模板已保存到本地')
      ElMessage.success(message)
      return
    }

    await downloadTemplateInBrowser()
    ElMessage.success('模板下载已开始')
  } catch (error) {
    ElMessage.error(await readDownloadError(error))
  } finally {
    downloadingTemplate.value = false
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

refreshOnReactivated(getList)
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';

.template-upload {
  display: inline-flex;
  margin-left: 12px;
  margin-right: 12px;
  vertical-align: middle;
}
</style>
