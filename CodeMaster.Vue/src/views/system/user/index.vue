<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryForm" class="query-form">
        <el-form-item :label="t('username')">
          <el-input v-model="queryForm.userName" :placeholder="$t2('please_input', 'username')" clearable />
        </el-form-item>
        <el-form-item :label="t('phone')">
          <el-input v-model="queryForm.phoneNumber" :placeholder="$t2('please_input', 'phone')" clearable />
        </el-form-item>
        <el-form-item :label="t('status')">
          <el-select v-model="queryForm.status" :placeholder="$t2('please_select', 'status')" clearable style="width: 120px;">
            <el-option :label="t('normal')" :value="0" />
            <el-option :label="t('disabled')" :value="1" />
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
          <el-button type="primary" @click="handleAdd" :icon="Plus" v-permission="'system:user:create'">
            {{ t('add') }}
          </el-button>
          <el-button type="success" @click="handleExport" :icon="Download" v-permission="'system:user:export'">
            {{ t('export') }}
          </el-button>
          <el-button type="warning" @click="handleImport" :icon="Upload" v-permission="'system:user:import'">
            {{ t('import') }}
          </el-button>
          <el-button type="danger" @click="handleBatchDelete" :icon="Delete" v-permission="'system:user:delete'">
            {{ t('batchDelete') }}
          </el-button>
        </div>
        <el-button type="info" @click="handleOperLog" :icon="Document">
          {{ t('operlog') }}
        </el-button>
      </div>

      <el-table
        v-loading="loading"
        :data="tableData"
        border
        stripe

        @selection-change="handleSelectionChange"
      >
        <el-table-column type="selection" width="55" :selectable="(row) => !row.userName.endsWith('admin')" />
        <el-table-column prop="userName" :label="t('username')" width="160" />
        <el-table-column prop="nickName" :label="t('nickname')" width="160" />
        <el-table-column prop="phoneNumber" :label="t('phone')" width="160" />
        <el-table-column prop="email" :label="t('email')" />
        <el-table-column prop="status" :label="t('status')" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 0 ? 'success' : 'danger'">
              {{ row.status === 0 ? t('normal') : t('disabled') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createTime" :label="t('create_time')" width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.createTime) }}
          </template>
        </el-table-column>
        <el-table-column :label="t('action')" fixed="right" width="220">
          <template #default="{ row }">
            <el-button
              v-if="!row.userName.endsWith('admin')"
              type="primary"
              link
              @click="handleEdit(row)"
              :icon="Edit"
              v-permission="'system:user:update'"
            >
              {{ t('edit') }}
            </el-button>
            <el-button type="info" link @click="handleView(row)" :icon="View" v-permission="'system:user:view'">
              {{ t('view') }}
            </el-button>
            <el-button
              v-if="!row.userName.endsWith('admin')"
              type="danger"
              link
              @click="handleDelete(row)"
              :icon="Delete"
              v-permission="'system:user:delete'"
            >
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
        @current-change="handleQuery"
        @size-change="handleQuery"
      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, View, Download, Upload, Document } from '@element-plus/icons-vue'
import { getPagedList, deleteById } from '@/api/system/user'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { formatDateTime } from '@/utils/dateFormat'

defineOptions({
  name: 'SystemUser'
})

const router = useRouter()
const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const selectedRows = ref([])
const { t } = useI18n()

const queryForm = reactive({
  userName: '',
  phoneNumber: '',
  status: null,
  pageNum: 1,
  pageSize: 10
})

const handleQuery = async () => {
  try {
    loading.value = true
    const res = await getPagedList(queryForm)
    tableData.value = res.items || []
    total.value = res.total || 0
  } catch (error) {
    ElMessage.error(t('query_failed'))
  } finally {
    loading.value = false
  }
}

const handleReset = () => {
  queryForm.userName = ''
  queryForm.phoneNumber = ''
  queryForm.status = null
  queryForm.pageNum = 1
  handleQuery()
}

const handleAdd = () => {
  router.push('/system/user/add')
}

const handleEdit = (row) => {
  router.push({
    path: '/system/user/edit',
    query: { id: row.id }
  })
}

const handleView = (row) => {
  router.push({
    path: '/system/user/detail',
    query: { id: row.id }
  })
}

const handleExport = () => {
  ElMessage.info('导出功能待实现')
}

const handleImport = () => {
  ElMessage.info('导入功能待实现')
}

const handleOperLog = () => {
  ElMessage.info('操作日志功能待实现')
}

const handleDelete = async (row) => {
  // 防止删除后缀为 admin 的账号
  if (row.userName.endsWith('admin')) {
    ElMessage.warning(t('cannot_delete_admin'))
    return
  }

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
    ElMessage.success(t('batch_delete_success'))
    handleQuery()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
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
