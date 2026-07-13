<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryForm" class="query-form">
        <el-form-item :label="t('username')">
          <el-input v-model="queryForm.userName" :placeholder="t('please_input_username')" clearable />
        </el-form-item>
        <el-form-item :label="t('login_ip')">
          <el-input v-model="queryForm.loginIp" :placeholder="t('please_input_name')" clearable />
        </el-form-item>
        <el-form-item :label="t('login_status')">
          <el-select v-model="queryForm.status" :placeholder="t('please_select_status')" clearable>
            <el-option :label="t('login_status_success')" :value="0" />
            <el-option :label="t('login_status_failed')" :value="1" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery" :icon="Search">{{ t('search') }}</el-button>
          <el-button @click="handleReset" :icon="Refresh">{{ t('reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <div class="toolbar mb-20">
        <el-button type="danger" @click="handleBatchDelete" :icon="Delete" v-permission="'system:loginlog:delete'">
          {{ t('batch_delete') }}
        </el-button>
        <el-button type="danger" plain @click="handleClearAll" :icon="Delete" v-permission="'system:loginlog:delete'">
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
        <el-table-column prop="userName" :label="t('username')" width="120" />
        <el-table-column prop="loginIp" :label="t('login_ip')" width="130" />
        <el-table-column prop="browser" :label="t('browser')" width="120" />
        <el-table-column prop="os" :label="t('os')" width="120" />
        <el-table-column prop="status" :label="t('login_status')" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 0 ? 'success' : 'danger'">
              {{ row.status === 0 ? t('login_status_success') : t('login_status_failed') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="msg" :label="t('msg')" width="150" show-overflow-tooltip />
        <el-table-column prop="loginTime" :label="t('login_time')" width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.loginTime) }}
          </template>
        </el-table-column>
        <el-table-column :label="t('action')" fixed="right" width="150">
          <template #default="{ row }">
            <el-button type="primary" link @click="handleView(row)" :icon="View">
              {{ t('view') }}
            </el-button>
            <el-button type="danger" link @click="handleDelete(row)" :icon="Delete" v-permission="'monitor:loginlog:delete'">
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
        class="mt-20"
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
import { getPagedList, deleteBatch, clear } from '@/api/monitor/loginlog'
import { formatDateTime } from '@/utils/dateFormat'

defineOptions({
  name: 'MonitorLoginlog'
})

const { t } = useI18n()
const router = useRouter()

const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const selectedIds = ref([])

const queryForm = reactive({
  userName: '',
  loginIp: '',
  status: null,
  pageNum: 1,
  pageSize: 10
})

const getList = async () => {
  loading.value = true
  try {
    const res = await getPagedList(queryForm)
    console.log('登录日志查询响应:', res)
    const data = res.items ? res : (res.data || res)
    console.log('处理后的数据:', data)
    // 兼容 PascalCase 和 camelCase
    tableData.value = data.items || data.Items || []
    total.value = data.total || data.Total || 0
  } catch (error) {
    console.error('查询登录日志失败:', error)
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
  queryForm.userName = ''
  queryForm.loginIp = ''
  queryForm.status = null
  handleQuery()
}

const handleSelectionChange = (selection) => {
  selectedIds.value = selection.map(item => item.id)
}

const handleView = (row) => {
  router.push({ path: '/monitor/loginlog/detail', query: { id: row.id } })
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

<style scoped>
.app-container {
  padding: 20px;
}

.mb-20 {
  margin-bottom: 20px;
}

.mt-20 {
  margin-top: 20px;
}

.toolbar {
  display: flex;
  gap: 10px;
}
</style>
