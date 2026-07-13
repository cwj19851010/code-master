<template>
  <div class="app-container">
    <el-tabs v-model="activeTab">
      <el-tab-pane :label="t('lang')" name="lang">
        <el-card shadow="never" class="mb-20">
          <el-form :inline="true" :model="langQueryForm" class="query-form">
            <el-form-item :label="t('code')">
              <el-input v-model="langQueryForm.langCode" :placeholder="$t2('please_input', 'code')" clearable />
            </el-form-item>
            <el-form-item :label="t('name')">
              <el-input v-model="langQueryForm.langName" :placeholder="$t2('please_input', 'name')" clearable />
            </el-form-item>
            <el-form-item :label="t('status')">
              <el-select v-model="langQueryForm.isEnabled" :placeholder="$t2('please_select', 'status')" clearable>
                <el-option :label="t('normal')" :value="0" />
                <el-option :label="t('disabled')" :value="1" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleLangQuery" :icon="Search">{{ t('search') }}</el-button>
              <el-button @click="handleLangReset" :icon="Refresh">{{ t('reset') }}</el-button>
            </el-form-item>
          </el-form>
        </el-card>

        <el-card shadow="never">
          <div class="toolbar">
            <el-button type="primary" @click="handleLangAdd" :icon="Plus" v-permission="'system:lang:create'">
              {{ t('add') }}
            </el-button>
            <el-button type="danger" @click="handleLangBatchDelete" :icon="Delete" v-permission="'system:lang:delete'">
              {{ t('batch_delete') }}
            </el-button>
          </div>

          <el-table

              v-loading="langLoading"
              :data="langTableData"
              border
              stripe
              @selection-change="handleLangSelectionChange"
            >
              <el-table-column type="selection" width="55" />
              <el-table-column prop="langCode" :label="t('code')" width="160" />
              <el-table-column prop="langName" :label="t('name')" width="200" />
                <el-table-column prop="isDefault" :label="t('is_default')" width="110">
                <template #default="{ row }">
                  <el-tag :type="row.isDefault === 1 ? 'success' : 'info'">
                    {{ row.isDefault === 1 ? t('yes') : t('no') }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="isEnabled" :label="t('status')" width="110">
                <template #default="{ row }">
                  <el-tag :type="row.isEnabled === 0 ? 'success' : 'danger'">
                    {{ row.isEnabled === 0 ? t('normal') : t('disabled') }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="sort" :label="t('sort')" width="100" />
              <el-table-column prop="remark" :label="t('remark')" show-overflow-tooltip />
                      <el-table-column prop="createTime" :label="t('create_time')" width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.createTime) }}
          </template>
        </el-table-column>
              <el-table-column :label="t('action')" fixed="right" width="250">
                <template #default="{ row }">
                  <el-button type="primary" link @click="handleLangView(row)" :icon="View" v-permission="'system:lang:view'">
                    {{ t('view') }}
                  </el-button>
                  <el-button type="primary" link @click="handleLangEdit(row)" :icon="Edit" v-permission="'system:lang:update'">
                    {{ t('edit') }}
                  </el-button>
                  <el-button type="danger" link @click="handleLangDelete(row)" :icon="Delete" v-permission="'system:lang:delete'">
                    {{ t('delete') }}
                  </el-button>
                </template>
              </el-table-column>
            </el-table>

          <el-pagination
            v-model:current-page="langQueryForm.pageNum"
            v-model:page-size="langQueryForm.pageSize"
            :total="langTotal"
            :page-sizes="[10, 20, 50, 100]"
            layout="total, sizes, prev, pager, next, jumper"
            @size-change="handleLangQuery"
            @current-change="handleLangQuery"

          />
        </el-card>
      </el-tab-pane>

      <el-tab-pane :label="t('text')" name="text">
        <el-card shadow="never" class="mb-20">
          <el-form :inline="true" :model="textQueryForm" class="query-form">
            <el-form-item :label="t('code')">
              <el-select v-model="textQueryForm.langCode" :placeholder="$t2('please_select', 'lang')" clearable style="width: 180px">
                <el-option
                  v-for="item in langOptions"
                  :key="item.langCode"
                  :label="item.langName"
                  :value="item.langCode"
                />
              </el-select>
            </el-form-item>
            <el-form-item :label="t('key')">
              <el-input v-model="textQueryForm.langKey" :placeholder="$t2('please_input', 'key')" clearable />
            </el-form-item>
            <el-form-item :label="t('category')">
              <el-input v-model="textQueryForm.category" :placeholder="$t2('please_input', 'category')" clearable />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleTextQuery" :icon="Search">{{ t('search') }}</el-button>
              <el-button @click="handleTextReset" :icon="Refresh">{{ t('reset') }}</el-button>
            </el-form-item>
          </el-form>
        </el-card>

        <el-card shadow="never">
          <div class="toolbar">
            <el-button type="primary" @click="handleTextAdd" :icon="Plus" v-permission="'system:lang:text:create'">
              {{ t('add') }}
            </el-button>
            <el-button type="danger" @click="handleTextBatchDelete" :icon="Delete" v-permission="'system:lang:text:delete'">
              {{ t('batch_delete') }}
            </el-button>
          </div>

          <el-table

            v-loading="textLoading"
            :data="textTableData"
            border
            stripe
            @selection-change="handleTextSelectionChange"
          >
            <el-table-column type="selection" width="55" />
            <el-table-column prop="langCode" :label="t('code')" width="150" />
            <el-table-column prop="langKey" :label="t('key')" width="220" show-overflow-tooltip />
            <el-table-column prop="langValue" :label="t('value')" min-width="280" show-overflow-tooltip />
            <el-table-column prop="category" :label="t('category')" width="160" show-overflow-tooltip />
                    <el-table-column prop="createTime" :label="t('create_time')" width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.createTime) }}
          </template>
        </el-table-column>
            <el-table-column :label="t('operation')" fixed="right" width="250">
              <template #default="{ row }">
                <el-button type="primary" link @click="handleTextView(row)" :icon="View" v-permission="'system:lang:text:view'">
                  {{ t('view') }}
                </el-button>
                <el-button type="primary" link @click="handleTextEdit(row)" :icon="Edit" v-permission="'system:lang:text:update'">
                  {{ t('edit') }}
                </el-button>
                <el-button type="danger" link @click="handleTextDelete(row)" :icon="Delete" v-permission="'system:lang:text:delete'">
                  {{ t('delete') }}
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <el-pagination
            v-model:current-page="textQueryForm.pageNum"
            v-model:page-size="textQueryForm.pageSize"
            :total="textTotal"
            :page-sizes="[10, 20, 50, 100]"
            layout="total, sizes, prev, pager, next, jumper"
            @size-change="handleTextQuery"
            @current-change="handleTextQuery"
            
          />
        </el-card>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete, View } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import {
  getPagedList,
  deleteById,
  getTextPagedList,
  deleteTextById
} from '@/api/system/lang'

defineOptions({
  name: 'SystemLang'
})

const { t } = useI18n()
const router = useRouter()
import { formatDateTime } from '@/utils/dateFormat'
const activeTab = ref('lang')

// langs
const langLoading = ref(false)
const langTableData = ref([])
const langTotal = ref(0)
const langSelectedRows = ref([])
const langOptions = ref([])

const langQueryForm = reactive({
  langCode: '',
  langName: '',
  isEnabled: null,
  pageNum: 1,
  pageSize: 10
})

const loadLangOptions = async () => {
  try {
    const res = await getPagedList({ pageNum: 1, pageSize: 1000 })
    langOptions.value = res.items || []
  } catch (error) {
    // ignore
  }
}

const handleLangQuery = async () => {
  langLoading.value = true
  try {
    const res = await getPagedList(langQueryForm)
    langTableData.value = res.items || []
    langTotal.value = res.total || 0
  } catch (error) {
    ElMessage.error(t('query_failed'))
  } finally {
    langLoading.value = false
  }
}

const handleLangReset = () => {
  langQueryForm.langCode = ''
  langQueryForm.langName = ''
  langQueryForm.isEnabled = null
  langQueryForm.pageNum = 1
  handleLangQuery()
}

const handleLangAdd = () => {
  router.push('/system/lang/add')
}

const handleLangView = (row) => {
  router.push({
    path: '/system/lang/detail',
    query: { id: row.id }
  })
}

const handleLangEdit = (row) => {
  router.push({
    path: '/system/lang/edit',
    query: { id: row.id }
  })
}

const handleLangDelete = async (row) => {
  try {
    await ElMessageBox.confirm(t('delete_confirm'), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    await deleteById(row.id)
    ElMessage.success(t('delete_success'))
    await loadLangOptions()
    handleLangQuery()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

const handleLangBatchDelete = async () => {
  if (langSelectedRows.value.length === 0) {
    ElMessage.warning(t('please_select_delete'))
    return
  }
  try {
    await ElMessageBox.confirm(t('batch_delete_confirm', { count: langSelectedRows.value.length }), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    for (const row of langSelectedRows.value) {
      await deleteById(row.id)
    }
    ElMessage.success(t('batch_delete_success'))
    await loadLangOptions()
    handleLangQuery()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

const handleLangSelectionChange = (selection) => {
  langSelectedRows.value = selection
}

// texts
const textLoading = ref(false)
const textTableData = ref([])
const textTotal = ref(0)
const textSelectedRows = ref([])

const textQueryForm = reactive({
  langCode: '',
  langKey: '',
  category: '',
  pageNum: 1,
  pageSize: 10
})

const handleTextQuery = async () => {
  textLoading.value = true
  try {
    const res = await getTextPagedList(textQueryForm)
    textTableData.value = res.items || []
    textTotal.value = res.total || 0
  } catch (error) {
    ElMessage.error(t('query_failed'))
  } finally {
    textLoading.value = false
  }
}

const handleTextReset = () => {
  textQueryForm.langCode = ''
  textQueryForm.langKey = ''
  textQueryForm.category = ''
  textQueryForm.pageNum = 1
  handleTextQuery()
}

const handleTextAdd = () => {
  router.push('/system/lang/text/add')
}

const handleTextView = (row) => {
  router.push({
    path: '/system/lang/text/detail',
    query: { id: row.id }
  })
}

const handleTextEdit = (row) => {
  router.push({
    path: '/system/lang/text/edit',
    query: { id: row.id }
  })
}

const handleTextDelete = async (row) => {
  try {
    await ElMessageBox.confirm(t('delete_confirm'), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    await deleteTextById(row.id)
    ElMessage.success(t('delete_success'))
    handleTextQuery()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

const handleTextBatchDelete = async () => {
  if (textSelectedRows.value.length === 0) {
    ElMessage.warning(t('please_select_delete'))
    return
  }
  try {
    await ElMessageBox.confirm(t('batch_delete_confirm', { count: textSelectedRows.value.length }), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    for (const row of textSelectedRows.value) {
      await deleteTextById(row.id)
    }
    ElMessage.success(t('batch_delete_success'))
    handleTextQuery()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('delete_failed'))
    }
  }
}

const handleTextSelectionChange = (selection) => {
  textSelectedRows.value = selection
}

watch(activeTab, (tab) => {
  if (tab === 'text') {
    handleTextQuery()
  }
})

onMounted(async () => {
  await loadLangOptions()
  handleLangQuery()
})
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';

:deep(.el-tabs) {
  .el-tabs__header {
    margin: 0 0 20px;
    background: #fff;
    padding: 15px 20px 0;
    border-radius: 4px;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  }

  .el-tabs__nav-wrap::after {
    display: none;
  }

  .el-tabs__item {
    font-size: 15px;
    font-weight: 500;
    padding: 0 25px;
    height: 42px;
    line-height: 42px;
  }

  .el-tabs__active-bar {
    height: 3px;
  }
}
</style>
