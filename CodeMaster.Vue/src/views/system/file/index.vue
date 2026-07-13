<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryForm" class="query-form">
        <el-form-item :label="t('file_name')">
          <el-input v-model="queryForm.fileName" :placeholder="t2('please_input', 'name')" clearable style="width: 180px;" />
        </el-form-item>
        <el-form-item :label="t('file_type')">
          <el-input v-model="queryForm.fileType" :placeholder="t2('please_input', 'name')" clearable style="width: 180px;" />
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
          <el-upload
            :action="uploadUrl"
            :headers="uploadHeaders"
            :on-success="handleUploadSuccess"
            :on-error="handleUploadError"
            :before-upload="beforeUpload"
            :show-file-list="false"
            v-permission="'system:file:upload'"
            style="display: inline-flex;margin-right: 10px;align-items: center; align-content: center;vertical-align: bottom;"

          >
            <el-button type="primary" :icon="Upload">{{ t('upload') }}</el-button>
          </el-upload>
          <el-button type="success" @click="handleExport" :icon="Download" v-permission="'system:file:export'">
            {{ t('export') }}
          </el-button>
          <el-button type="warning" @click="handleImport" :icon="Upload" v-permission="'system:file:import'">
            {{ t('import') }}
          </el-button>
          <el-button type="danger" @click="handleBatchDelete" :icon="Delete" v-permission="'system:file:delete'">
            {{ t('batch_delete') }}
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
        <el-table-column type="selection" width="55" />
        <el-table-column prop="fileName" :label="t('file_name')" width="200" show-overflow-tooltip />
        <el-table-column prop="filePath" :label="t('file_path')" width="300" show-overflow-tooltip />
        <el-table-column prop="fileSize" :label="t('file_size')" width="120">
          <template #default="{ row }">
            {{ formatFileSize(row.fileSize) }}
          </template>
        </el-table-column>
        <el-table-column prop="fileType" :label="t('file_type')" width="100" />
        <el-table-column :label="t('action')" fixed="right" width="220">
          <template #default="{ row }">
            <el-button type="primary" link @click="handleDownload(row)" :icon="Download">
              {{ t('download') }}
            </el-button>
            <el-button type="info" link @click="handleView(row)" :icon="View" v-permission="'system:file:view'">
              {{ t('view') }}
            </el-button>
            <el-button type="danger" link @click="handleDelete(row)" :icon="Delete" v-permission="'system:file:delete'">
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
        @current-change="handleQuery"
        
      />
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Delete, Upload, Download, View, Document } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { getToken } from '@/utils/auth'
import { formatDateTime } from '@/utils/dateFormat'
import { getFileList, deleteFile, downloadFile } from '@/api/system/file'

defineOptions({
  name: 'SystemFile'
})

const router = useRouter()
const { t } = useI18n()

const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const selectedRows = ref([])

const queryForm = reactive({
  fileName: '',
  fileType: '',
  pageNum: 1,
  pageSize: 10
})

const uploadUrl = computed(() => {
  return '/api/system/file/upload'
})

const uploadHeaders = computed(() => {
  return {
    Authorization: `Bearer ${getToken()}`
  }
})

const formatFileSize = (bytes) => {
  if (!bytes) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
}

const handleQuery = async () => {
  loading.value = true
  try {
    const data = await getFileList(queryForm)
    tableData.value = data.items || []
    total.value = data.total || 0
  } catch (error) {
    ElMessage.error(t('query_failed'))
  } finally {
    loading.value = false
  }
}

const handleReset = () => {
  queryForm.fileName = ''
  queryForm.fileType = ''
  queryForm.pageNum = 1
  handleQuery()
}

const handleSelectionChange = (selection) => {
  selectedRows.value = selection
}

const beforeUpload = (file) => {
  const maxSize = 10 * 1024 * 1024 // 10MB
  if (file.size > maxSize) {
    ElMessage.error(t('file_size_limit').replace('{size}', '10'))
    return false
  }
  return true
}

const handleUploadSuccess = (response) => {
  if (response.code === 200) {
    ElMessage.success(t('upload_success'))
    handleQuery()
  } else {
    ElMessage.error(response.msg || t('upload_failed'))
  }
}

const handleUploadError = () => {
  ElMessage.error(t('upload_failed'))
}

const handleDownload = async (row) => {
  try {
    const response = await downloadFile(row.id)
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', row.fileName)
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  } catch (error) {
    ElMessage.error(t('download_failed'))
  }
}

const handleView = (row) => {
  router.push({
    path: '/system/file/detail',
    query: { id: row.id }
  })
}

const handleExport = () => {
  ElMessage.info(t('export_pending'))
}

const handleImport = () => {
  ElMessage.info(t('import_pending'))
}

const handleOperLog = () => {
  ElMessage.info(t('operlog_pending'))
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(t('delete_confirm'), t('prompt'), {
      type: 'warning'
    })
    await deleteFile(row.id)
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
    await ElMessageBox.confirm(
      t('batch_delete_confirm', { count: selectedRows.value.length }),
      t('prompt'),
      {
        type: 'warning'
      }
    )
    for (const row of selectedRows.value) {
      await deleteFile(row.id)
    }
    ElMessage.success(t('batch_delete_success'))
    handleQuery()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

onMounted(() => {
  handleQuery()
})

refreshOnReactivated(handleQuery)
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';
</style>
