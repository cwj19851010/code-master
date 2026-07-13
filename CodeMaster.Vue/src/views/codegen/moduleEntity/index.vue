<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <!-- 搜索栏 -->
      <el-form :inline="true" :model="queryParams" class="query-form">
        <el-form-item label="所属模块">
          <el-select v-model="queryParams.moduleId" :placeholder="$t2('please_select', 'module')" clearable>
            <el-option
              v-for="module in moduleList"
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
      </div>

      <!-- 数据表格 -->
      <el-table v-loading="loading" :data="entityList" border>
        <el-table-column label="所属模块" width="150">
          <template #default="{ row }">
            {{ getModuleName(row.moduleId) }}
          </template>
        </el-table-column>
        <el-table-column label="实体名称" prop="name" width="150" />
        <el-table-column label="实体描述" prop="description" />
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
                  <el-dropdown-item command="detail">详情页</el-dropdown-item>
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
import { ref, reactive, onMounted } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowDown } from '@element-plus/icons-vue'
import {
  getPagedList as getEntityList,
  deleteById,
  generateCode,
  generateIncrementalCode,
  syncMenu,
  syncLanguage
} from '@/api/codegen/moduleEntity'
import { getList as getModuleList } from '@/api/codegen/projectModule'

defineOptions({
  name: 'CodegenModuleEntity'
})

const router = useRouter()

// 数据
const loading = ref(false)
const entityList = ref([])
const moduleList = ref([])
const total = ref(0)

// 查询参数
const queryParams = reactive({
  pageNum: 1,
  pageSize: 10,
  moduleId: null,
  name: ''
})

// 生命周期
onMounted(() => {
  loadModuleList()
  loadEntityList()
})

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
  await loadModuleList()
  await loadEntityList()
}

// 查询
function handleQuery() {
  queryParams.pageNum = 1
  loadEntityList()
}

// 重置
function handleReset() {
  queryParams.moduleId = null
  queryParams.name = ''
  handleQuery()
}

// 新增
function handleAdd() {
  router.push('/codegen/moduleentity/add')
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
}
</style>
