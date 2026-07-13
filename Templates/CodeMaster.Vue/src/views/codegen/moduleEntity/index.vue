<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <!-- 搜索栏 -->
      <el-form :inline="true" :model="queryParams" class="demo-form-inline">
        <el-form-item label="所属模块">
          <el-select v-model="queryParams.moduleId" placeholder="请选择模块" clearable>
            <el-option
              v-for="module in moduleList"
              :key="module.id"
              :label="module.moduleDescription"
              :value="module.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="实体名称">
          <el-input v-model="queryParams.name" placeholder="请输入实体名称" clearable />
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
        <el-table-column label="所属模块" prop="projectModuleName" width="150" />
        <el-table-column label="实体名称" prop="name" width="150" />
        <el-table-column label="实体描述" prop="description" />
        <el-table-column label="表名" prop="tableName" width="150" />
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
        <el-table-column label="操作" width="400" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link @click="handleView(row)">详情</el-button>
            <el-button type="primary" link @click="handleUpdate(row)">编辑</el-button>
            <el-button type="success" link @click="handleGenerateCode(row)">生成代码</el-button>
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
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  getPagedList,
  deleteById,
  generateCode
} from '@/api/codegen/moduleEntity'
import { getPagedList as getModulePagedList } from '@/api/codegen/projectModule'

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
    const res = await getModulePagedList({ pageNum: 1, pageSize: 1000 })
    moduleList.value = res.items || []
  } catch (error) {
    console.error('加载模块列表失败:', error)
  }
}

// 加载实体列表
async function loadEntityList() {
  loading.value = true
  try {
    const res = await getPagedList(queryParams)
    entityList.value = res.items || []
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
</script>

<style scoped>
.mb-20 {
  margin-bottom: 20px;
}

.toolbar {
  margin-bottom: 20px;
}
</style>
