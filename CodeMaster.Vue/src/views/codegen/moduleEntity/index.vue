<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <!-- 搜索栏 -->
      <el-form :inline="true" :model="queryParams" class="query-form">
        <el-form-item label="所属项目">
          <el-select
            v-model="queryParams.projectId"
            :placeholder="$t2('please_select', 'project')"
            clearable
            @change="handleProjectChange"
          >
            <el-option
              v-for="project in projectList"
              :key="project.id"
              :label="project.projectName"
              :value="project.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="所属模块">
          <el-select v-model="queryParams.moduleId" :placeholder="$t2('please_select', 'module')" clearable>
            <el-option
              v-for="module in filteredModuleList"
              :key="module.id"
              :label="module.moduleDescription || module.moduleName || ('模块' + module.id)"
              :value="module.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="实体名称">
          <el-input v-model="queryParams.name" :placeholder="$t2('please_input', 'entity_name')" clearable />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery">查询</el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <!-- 操作按钮 -->
      <div class="toolbar mb-20">
        <el-button type="primary" icon="Plus" @click="handleAdd">新增</el-button>
        <el-button
          type="danger"
          plain
          :icon="RefreshRight"
          :disabled="!queryParams.projectId"
          @click="handleRegenerateAll"
        >
          全部重新生成
        </el-button>
        <el-button
          type="success"
          plain
          :icon="MagicStick"
          :disabled="!queryParams.projectId"
          @click="handleGenerateAllChanges"
        >
          生成全部变更
        </el-button>
        <el-button
          type="warning"
          plain
          :icon="Menu"
          :disabled="!queryParams.projectId"
          @click="handleSyncAllMenus"
        >
          全部更新菜单
        </el-button>
        <el-button
          type="warning"
          plain
          :icon="Connection"
          :disabled="!queryParams.projectId"
          @click="handleSyncAllLanguages"
        >
          全部更新语言
        </el-button>
      </div>

      <!-- 数据表格 -->
      <el-table v-loading="loading" :data="entityList" border>
        <el-table-column label="所属项目" width="160">
          <template #default="{ row }">
            {{ row.projectName || projectNameMap[row.projectId] || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="所属模块" width="150">
          <template #default="{ row }">
            {{ getModuleName(row.moduleId) }}
          </template>
        </el-table-column>
        <el-table-column label="实体名称" prop="name" width="150" />
        <el-table-column label="实体描述" prop="description" />
        <el-table-column label="主键" prop="hasPrimaryKey" width="80">
          <template #default="{ row }">
            <el-tag :type="row.hasPrimaryKey ? 'success' : 'info'">
              {{ row.hasPrimaryKey ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="树形结构" prop="isTree" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isTree ? 'success' : 'info'">
              {{ row.isTree ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="只读" prop="isReadOnly" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isReadOnly ? 'warning' : 'info'">
              {{ row.isReadOnly ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="数据权限" prop="hasDataPermission" width="100">
          <template #default="{ row }">
            <el-tag :type="row.hasDataPermission ? 'success' : 'info'">
              {{ row.hasDataPermission ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="多租户" prop="hasTenant" width="100">
          <template #default="{ row }">
            <el-tag :type="row.hasTenant ? 'success' : 'info'">
              {{ row.hasTenant ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="审计" prop="hasAudit" width="80">
          <template #default="{ row }">
            <el-tag :type="row.hasAudit ? 'success' : 'info'">
              {{ row.hasAudit ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="软删除" prop="hasSoftDelete" width="90">
          <template #default="{ row }">
            <el-tag :type="row.hasSoftDelete ? 'success' : 'info'">
              {{ row.hasSoftDelete ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="排序" prop="orderNum" width="80" />
        <el-table-column label="操作" width="660" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link @click="handleView(row)">详情</el-button>
            <el-button type="primary" link @click="handleUpdate(row)">编辑</el-button>
            <el-dropdown @command="(cmd) => handleDesign(row, cmd)" style="margin-right:8px;margin-top:3px;margin-left:8px">
              <el-button type="info" link>
                设计<el-icon class="el-icon--right"><ArrowDown /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="index">列表页</el-dropdown-item>
                  <el-dropdown-item command="add" :disabled="row.isReadOnly">新增页</el-dropdown-item>
                  <el-dropdown-item command="edit" :disabled="row.isReadOnly">编辑页</el-dropdown-item>
                  <el-dropdown-item command="detail" :disabled="!row.hasPrimaryKey">详情页</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
            <el-button type="primary" link @click="handleGenerateIncrementalCode(row)">增量生成</el-button>
            <el-button type="success" link @click="handleGenerateCode(row)">生成代码</el-button>
            <el-button type="warning" link @click="handleSyncMenu(row)">更新到菜单</el-button>
            <el-button type="warning" link @click="handleSyncLanguage(row)">更新到多语言</el-button>
            <el-button type="danger" link @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <el-pagination
        v-model:current-page="queryParams.pageNum"
        v-model:page-size="queryParams.pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleQuery"
        @current-change="loadEntityList"
      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowDown, Connection, MagicStick, Menu, RefreshRight } from '@element-plus/icons-vue'
import {
  getPagedList as getEntityList,
  deleteById,
  generateCode,
  generateIncrementalCode,
  generateProjectCode,
  generateProjectIncrementalCode,
  syncMenu,
  syncLanguage,
  syncProjectMenus,
  syncProjectLanguages
} from '@/api/codegen/moduleEntity'
import { getList as getModuleList } from '@/api/codegen/projectModule'
import { buildProject, getList as getProjectList } from '@/api/codegen/project'

defineOptions({
  name: 'CodegenModuleEntity'
})

const router = useRouter()

// 数据
const loading = ref(false)
const entityList = ref([])
const moduleList = ref([])
const projectList = ref([])
const total = ref(0)

const projectNameMap = computed(() => {
  const map = {}
  for (const project of projectList.value) {
    map[project.id] = project.projectName
  }
  return map
})

const filteredModuleList = computed(() => {
  if (!queryParams.projectId) return moduleList.value
  return moduleList.value.filter(module => String(module.projectId) === String(queryParams.projectId))
})

const selectedProjectName = computed(() => {
  const project = projectList.value.find(item => String(item.id) === String(queryParams.projectId))
  return project?.projectName || '当前项目'
})

// 查询参数
const queryParams = reactive({
  pageNum: 1,
  pageSize: 10,
  projectId: null,
  moduleId: null,
  name: ''
})

// 生命周期
onMounted(() => {
  loadProjectList()
  loadModuleList()
  loadEntityList()
})

// 加载项目列表
async function loadProjectList() {
  try {
    const res = await getProjectList()
    projectList.value = Array.isArray(res) ? res : (res?.items || [])
  } catch (error) {
    console.error('加载项目列表失败:', error)
  }
}

// 加载模块列表
async function loadModuleList() {
  try {
    const res = await getModuleList()
    moduleList.value = Array.isArray(res) ? res : (res?.items || [])
  } catch (error) {
    console.error('加载模块列表失败:', error)
  }
}

// 获取模块显示名称
function getModuleName(moduleId) {
  const module = moduleList.value.find(m => String(m.id) === String(moduleId))
  return module?.moduleDescription || module?.moduleName || `模块${moduleId}`
}

// 加载实体列表
async function loadEntityList() {
  loading.value = true
  try {
    const res = await getEntityList(queryParams)
    entityList.value = res.items || []
    total.value = res.total || 0
  } catch (error) {
    ElMessage.error('加载数据失败')
    console.error(error)
  } finally {
    loading.value = false
  }
}

async function refreshEntityPage() {
  await loadProjectList()
  await loadModuleList()
  await loadEntityList()
}

// 查询
function handleQuery() {
  queryParams.pageNum = 1
  loadEntityList()
}

function handleProjectChange() {
  queryParams.moduleId = null
}

// 重置
function handleReset() {
  queryParams.projectId = null
  queryParams.moduleId = null
  queryParams.name = ''
  handleQuery()
}

// 新增
function handleAdd() {
  router.push('/codegen/moduleentity/add')
}

async function runProjectBatchAction({
  title,
  message,
  loadingMessage,
  successMessage,
  action,
  validateBuild = false
}) {
  if (!queryParams.projectId) {
    ElMessage.warning('请先选择所属项目')
    return
  }

  let loadingMessageInstance
  try {
    await ElMessageBox.confirm(message, title, {
      type: 'warning',
      confirmButtonText: '确定执行',
      cancelButtonText: '取消'
    })
    loadingMessageInstance = ElMessage({
      message: loadingMessage,
      type: 'info',
      duration: 0
    })
    await action(queryParams.projectId)

    if (validateBuild) {
      loadingMessageInstance?.close()
      loadingMessageInstance = ElMessage({
        message: '代码生成完成，正在检查项目编译结果...',
        type: 'info',
        duration: 0
      })

      const buildResult = await buildProject(queryParams.projectId)
      if (!buildResult.success) {
        await loadEntityList()
        await ElMessageBox.alert(
          buildResult.output || buildResult.message || '项目编译失败，请检查生成结果。',
          '代码已生成，但编译检查失败',
          {
            type: 'error',
            confirmButtonText: '知道了'
          }
        ).catch(() => {})
        return
      }
    }

    ElMessage.success(successMessage)
    await loadEntityList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(`${title}失败：${error.message || '未知错误'}`)
      console.error(error)
    }
  } finally {
    loadingMessageInstance?.close()
  }
}

function handleRegenerateAll() {
  return runProjectBatchAction({
    title: '全部重新生成',
    message: `确定重新生成项目“${selectedProjectName.value}”的全部实体吗？所有生成文件都会按当前元数据和模板重新生成。`,
    loadingMessage: '正在重新生成项目全部实体，请稍候...',
    successMessage: '项目全部实体重新生成完成',
    action: projectId => generateProjectCode(projectId),
    validateBuild: true
  })
}

function handleGenerateAllChanges() {
  return runProjectBatchAction({
    title: '生成全部变更',
    message: `确定检查项目“${selectedProjectName.value}”的全部实体吗？仅增量生成实体、字段、关系或模块配置发生变化的部分。`,
    loadingMessage: '正在检查并增量生成全部变更，请稍候...',
    successMessage: '项目变更检查和增量生成完成',
    action: projectId => generateProjectIncrementalCode(projectId),
    validateBuild: true
  })
}

function handleSyncAllMenus() {
  return runProjectBatchAction({
    title: '全部更新菜单',
    message: `确定将项目“${selectedProjectName.value}”的全部实体菜单和权限同步到目标数据库吗？`,
    loadingMessage: '正在同步项目菜单和权限，请稍候...',
    successMessage: '项目菜单和权限同步完成',
    action: projectId => syncProjectMenus(projectId)
  })
}

function handleSyncAllLanguages() {
  return runProjectBatchAction({
    title: '全部更新语言',
    message: `确定将项目“${selectedProjectName.value}”的模块、实体和字段语言同步到目标数据库吗？`,
    loadingMessage: '正在同步项目多语言，请稍候...',
    successMessage: '项目多语言同步完成',
    action: projectId => syncProjectLanguages(projectId)
  })
}

// 详情
function handleView(row) {
  router.push({
    path: '/codegen/moduleentity/detail',
    query: { id: row.id }
  })
}

// 编辑
function handleUpdate(row) {
  router.push({
    path: '/codegen/moduleentity/edit',
    query: { id: row.id }
  })
}

// 删除
async function handleDelete(row) {
  try {
    await ElMessageBox.confirm('确定要删除该实体吗？', '提示', {
      type: 'warning'
    })
    await deleteById(row.id)
    ElMessage.success('删除成功')
    loadEntityList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
      console.error(error)
    }
  }
}

// 生成代码
async function handleGenerateCode(row) {
  try {
    await ElMessageBox.confirm(
      `确定要为实体 "${row.name}" 生成代码吗？这将生成后端实体、DTO、Service 和前端 API 文件。`,
      '生成代码',
      {
        type: 'warning',
        confirmButtonText: '确定生成',
        cancelButtonText: '取消'
      }
    )

    const loading = ElMessage({
      message: '正在生成代码，请稍候...',
      type: 'info',
      duration: 0
    })

    await generateCode(row.id)
    loading.close()
    ElMessage.success('代码生成成功！')
    loadEntityList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('代码生成失败：' + (error.message || '未知错误'))
      console.error(error)
    }
  }
}

// 增量生成代码
async function handleGenerateIncrementalCode(row) {
  try {
    await ElMessageBox.confirm(
      `确定要为实体 "${row.name}" 增量生成代码吗？这会按字段、排序和子表关系变更更新已生成页面。`,
      '增量生成代码',
      {
        type: 'warning',
        confirmButtonText: '确定增量生成',
        cancelButtonText: '取消'
      }
    )

    const loading = ElMessage({
      message: '正在增量生成代码，请稍候...',
      type: 'info',
      duration: 0
    })

    await generateIncrementalCode(row.id)
    loading.close()
    ElMessage.success('增量生成成功！')
    loadEntityList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('增量生成失败：' + (error.message || '未知错误'))
      console.error(error)
    }
  }
}

// 同步菜单到目标项目
async function handleSyncMenu(row) {
  try {
    await ElMessageBox.confirm(
      `确定要将实体 "${row.name}" 的菜单同步到目标项目吗？`,
      '同步菜单',
      {
        type: 'warning',
        confirmButtonText: '确定',
        cancelButtonText: '取消'
      }
    )

    const loading = ElMessage({
      message: '正在同步菜单，请稍候...',
      type: 'info',
      duration: 0
    })

    await syncMenu(row.id)
    loading.close()
    ElMessage.success('菜单同步成功！')
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('菜单同步失败：' + (error.message || '未知错误'))
      console.error(error)
    }
  }
}

// 同步字段多语言到目标项目
async function handleSyncLanguage(row) {
  try {
    await ElMessageBox.confirm(
      `确定要将实体 "${row.name}" 的所有字段多语言同步到目标项目吗？`,
      '同步多语言',
      {
        type: 'warning',
        confirmButtonText: '确定',
        cancelButtonText: '取消'
      }
    )

    const loading = ElMessage({
      message: '正在同步多语言，请稍候...',
      type: 'info',
      duration: 0
    })

    await syncLanguage(row.id)
    loading.close()
    ElMessage.success('多语言同步成功！')
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('多语言同步失败：' + (error.message || '未知错误'))
      console.error(error)
    }
  }
}

// 设计页面
function handleDesign(row, command) {
  router.push({
    path: `/codegen/entityDesigner`,
    query: { entityId: row.id, pageType: command, entityName: row.name }
  })
}

refreshOnReactivated(refreshEntityPage)
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';

.mb-20 {
  margin-bottom: 20px;
}

.toolbar {
  margin-bottom: 20px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.toolbar :deep(.el-button + .el-button) {
  margin-left: 0;
}
</style>
