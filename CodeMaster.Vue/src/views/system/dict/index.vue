<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryForm" class="query-form">
        <el-form-item :label="t('dictName')">
          <el-input v-model="queryForm.dictName" :placeholder="t2('please_input', 'dict_name')" clearable />
        </el-form-item>
        <el-form-item :label="t('dictType')">
          <el-input v-model="queryForm.dictType" :placeholder="t2('please_input', 'dict_type')" clearable />
        </el-form-item>
        <el-form-item :label="t('label')">
          <el-input v-model="queryForm.label" :placeholder="t2('please_input', 'label')" clearable />
        </el-form-item>
        <el-form-item :label="t('value')">
          <el-input v-model="queryForm.value" :placeholder="t2('please_input', 'value')" clearable />
        </el-form-item>
        <el-form-item :label="t('lang_key')">
          <el-input v-model="queryForm.langKey" :placeholder="t2('please_input', 'lang_key')" clearable />
        </el-form-item>
        <el-form-item :label="t('status')">
          <el-select v-model="queryForm.status" :placeholder="t2('please_select', 'status')" clearable>
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
          <el-button type="primary" @click="handleAddType" :icon="Plus" v-permission="'system:dict:type:create'">
            {{ t('add_dict_type') }}
          </el-button>
          <el-button type="success" @click="handleExport" :icon="Download" v-permission="'system:dict:export'">
            {{ t('export') }}
          </el-button>
          <el-button type="warning" @click="handleImport" :icon="Upload" v-permission="'system:dict:import'">
            {{ t('import') }}
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
          row-key="id"
          :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
          default-expand-all
        >
        <el-table-column prop="dictName" :label="t('dictName')" width="220" show-overflow-tooltip>
          <template #default="{ row }">
            {{ getDictNameText(row) }}
          </template>
        </el-table-column>
        <el-table-column prop="dictType" :label="t('dictType')" width="180" />
        <el-table-column prop="langKey" :label="t('lang_key')" width="150" show-overflow-tooltip>
          <template #default="{ row }">
            <template v-if="!row.children">
              {{ row.langKey }}
            </template>
            <template v-else>
              {{ row.langKey }}
            </template>
          </template>
        </el-table-column>
        <el-table-column prop="value" :label="t('value')" width="150" />
        <el-table-column prop="isDefault" :label="t('is_default')" width="100">
          <template #default="{ row }">
            <el-tag v-if="row.isDefault === 1" type="success">{{ t('yes') }}</el-tag>
            <el-tag v-else-if="row.isDefault === 0" type="info">{{ t('no') }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sort" :label="t('sort')" width="100" />
        <el-table-column prop="status" :label="t('status')" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 0 ? 'success' : 'danger'">
              {{ row.status === 0 ? t('normal') : t('disabled') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" :label="t('remark')" show-overflow-tooltip />
        <el-table-column prop="createTime" :label="t('create_time')" width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.createTime) }}
          </template>
        </el-table-column>
        <el-table-column :label="t('action')" fixed="right" width="280">
          <template #default="{ row }">
            <template v-if="row.children">
              <!-- 字典类型操作 -->
              <el-button type="success" link @click="handleAddDataForType(row)" :icon="Plus" v-permission="'system:dict:data:create'">
                {{ t('add_dict_data') }}
              </el-button>
              <el-button type="primary" link @click="handleEditType(row)" :icon="Edit" v-permission="'system:dict:type:update'">
                {{ t('edit') }}
              </el-button>
              <el-button type="info" link @click="handleViewType(row)" :icon="View" v-permission="'system:dict:type:view'">
                {{ t('view') }}
              </el-button>
              <el-button type="danger" link @click="handleDeleteType(row)" :icon="Delete" v-permission="'system:dict:type:delete'">
                {{ t('delete') }}
              </el-button>
            </template>
            <template v-else>
              <!-- 字典数据操作 -->
              <el-button type="primary" link @click="handleEditData(row)" :icon="Edit" v-permission="'system:dict:data:update'">
                {{ t('edit') }}
              </el-button>
              <el-button type="info" link @click="handleViewData(row)" :icon="View" v-permission="'system:dict:data:view'">
                {{ t('view') }}
              </el-button>
              <el-button type="danger" link @click="handleDeleteData(row)" :icon="Delete" v-permission="'system:dict:data:delete'">
                {{ t('delete') }}
              </el-button>
            </template>
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
import { ref, reactive, onMounted } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, View, Download, Upload, Document } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { formatDateTime } from '@/utils/dateFormat'
import {
  getTypePagedList,
  deleteTypeById,
  deleteDataById
} from '@/api/system/dict'

defineOptions({
  name: 'SystemDict'
})

const router = useRouter()
const { t } = useI18n()

const loading = ref(false)
const tableData = ref([])
const total = ref(0)

const queryForm = reactive({
  dictName: '',
  dictType: '',
  label: '',
  value: '',
  langKey: '',
  status: null,
  pageNum: 1,
  pageSize: 10
})

const handleQuery = async () => {
  console.log('handleQuery 被调用，当前 pageNum:', queryForm.pageNum, 'pageSize:', queryForm.pageSize)
  loading.value = true
  try {
    const res = await getTypePagedList(queryForm)
    console.log('字典查询响应:', res)
    tableData.value = res.items || res.Items || []
    total.value = res.total || res.Total || 0
    console.log('tableData:', tableData.value.length, 'total:', total.value)
  } catch (error) {
    console.error('查询字典失败:', error)
    ElMessage.error(t('query_failed'))
  } finally {
    loading.value = false
  }
}

const handleReset = () => {
  queryForm.dictName = ''
  queryForm.dictType = ''
  queryForm.label = ''
  queryForm.value = ''
  queryForm.langKey = ''
  queryForm.status = null
  queryForm.pageNum = 1
  handleQuery()
}

const getDictNameText = (row) => {
  if (row.children) {
    return row.dictName
  }

  return row.langKey ? t(row.langKey) : row.label || row.dictName
}

// 字典类型操作
const handleAddType = () => {
  router.push('/system/dict/type/add')
}

const handleEditType = (row) => {
  router.push({ path: '/system/dict/type/edit', query: { id: row.id } })
}

const handleViewType = (row) => {
  router.push({ path: '/system/dict/type/detail', query: { id: row.id } })
}

const handleDeleteType = async (row) => {
  try {
    await ElMessageBox.confirm(t('delete_confirm'), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    await deleteTypeById(row.id)
    ElMessage.success(t('delete_success'))
    handleQuery()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

// 字典数据操作
const handleAddDataForType = (row) => {
  router.push({ path: '/system/dict/data/add', query: { dictType: row.dictType } })
}

const handleEditData = (row) => {
  router.push({ path: '/system/dict/data/edit', query: { id: row.id } })
}

const handleViewData = (row) => {
  router.push({ path: '/system/dict/data/detail', query: { id: row.id } })
}

const handleDeleteData = async (row) => {
  try {
    await ElMessageBox.confirm(t('delete_confirm'), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    await deleteDataById(row.id)
    ElMessage.success(t('delete_success'))
    handleQuery()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
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

onMounted(() => {
  handleQuery()
})

refreshOnReactivated(handleQuery)
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';
</style>
