<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <!-- 搜索栏 -->
      <el-form :inline="true" :model="queryParams" class="demo-form-inline">
        <el-form-item label="所属项目">
          <el-select v-model="queryParams.projectId" placeholder="请选择项目" clearable>
            <el-option
              v-for="project in projectList"
              :key="project.id"
              :label="project.projectName"
              :value="project.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="模块名称">
          <el-input v-model="queryParams.moduleName" placeholder="请输入模块名称" clearable />
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
      <el-table v-loading="loading" :data="moduleList" border>
        <el-table-column label="所属项目" prop="projectName" width="150" />
        <el-table-column label="模块名称" prop="moduleName" width="150" />
        <el-table-column label="模块描述" prop="moduleDescription" />
        <el-table-column label="图标" prop="icon" width="80">
          <template #default="{ row }">
            <el-icon v-if="row.icon">
              <component :is="row.icon" />
            </el-icon>
          </template>
        </el-table-column>
        <el-table-column label="排序" prop="orderNum" width="80" />
        <el-table-column label="状态" prop="status" width="80">
          <template #default="{ row }">
            <el-tag :type="row.status ? 'success' : 'danger'">
              {{ row.status ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="创建时间" prop="createTime" width="160" />
        <el-table-column label="操作" width="280" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link @click="handleUpdate(row)">编辑</el-button>
            <el-button type="success" link @click="handleSyncToMenu(row)">更新到菜单</el-button>
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
        @current-change="loadModuleList"
      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  getPagedList,
  deleteById,
  updateModuleToMenu
} from '@/api/codegen/projectModule'
import { getList } from '@/api/codegen/project'
import { isInClient, syncModuleToMenu } from '@/utils/jsbridge'

defineOptions({
  name: 'CodegenProjectModule'
})

const router = useRouter()

// 数据
const loading = ref(false)
const moduleList = ref([])
const projectList = ref([])
const total = ref(0)

// 查询参数
const queryParams = reactive({
  pageNum: 1,
  pageSize: 10,
  projectId: null,
  moduleName: ''
})

// 生命周期
onMounted(() => {
  loadProjectList()
  loadModuleList()
})

// 加载项目列表
async function loadProjectList() {
  try {
    const res = await getList({ pageNum: 1, pageSize: 1000 })
    projectList.value = res.items || []
  } catch (error) {
    console.error('加载项目列表失败:', error)
  }
}

// 加载模块列表
async function loadModuleList() {
  loading.value = true
  try {
    const res = await getPagedList(queryParams)
    moduleList.value = res.items || []
    total.value = res.total || 0
  } catch (error) {
    ElMessage.error('加载数据失败')
    console.error(error)
  } finally {
    loading.value = false
  }
}

// 查询
function handleQuery() {
  queryParams.pageNum = 1
  loadModuleList()
}

// 重置
function handleReset() {
  queryParams.projectId = null
  queryParams.moduleName = ''
  handleQuery()
}

// 新增
function handleAdd() {
  router.push('/codegen/projectmodule/add')
}

// 编辑
function handleUpdate(row) {
  router.push({ path: '/codegen/projectmodule/edit', query: { id: row.id } })
}

// 查看
function handleView(row) {
  router.push({ path: '/codegen/projectmodule/detail', query: { id: row.id } })
}

// 删除
async function handleDelete(row) {
  try {
    await ElMessageBox.confirm('确定要删除该模块吗？', '提示', {
      type: 'warning'
    })
    await deleteById(row.id)
    ElMessage.success('删除成功')
    loadModuleList()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
      console.error(error)
    }
  }
}

// 同步到菜单
async function handleSyncToMenu(row) {
  try {
    await ElMessageBox.confirm(
      '确定要将该模块同步到目标项目的菜单吗？',
      '提示',
      { type: 'warning' }
    )

    loading.value = true

    // 获取项目信息
    const project = projectList.value.find(p => p.id === row.projectId)
    if (!project) {
      ElMessage.error('未找到项目信息')
      return
    }

    // 检测运行环境
    if (isInClient()) {
      // 客户端模式：通过 JSBridge 调用
      await syncModuleToMenu({
        moduleId: row.id,
        projectId: project.id,
        projectName: project.projectName,
        projectPath: project.projectPath,
        databaseType: project.databaseType,
        connectionString: project.connectionString,
        moduleName: row.moduleName,
        moduleDescription: row.moduleDescription,
        icon: row.icon,
        orderNum: row.orderNum
      })
    } else {
      // 服务端模式：调用后端 API
      await updateModuleToMenu(row.id)
    }

    ElMessage.success('同步成功')
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || '同步失败')
      console.error(error)
    }
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.mb-20 {
  margin-bottom: 20px;
}

.toolbar {
  margin-bottom: 20px;
}
</style>
