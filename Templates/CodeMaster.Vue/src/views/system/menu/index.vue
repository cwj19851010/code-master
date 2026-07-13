<template>
  <div class="app-container">
    <el-card shadow="never" class="mb-20">
      <el-form :inline="true" :model="queryParams" class="query-form">
        <el-form-item :label="t('menu_name')">
          <el-input
            v-model="queryParams.menuName"
            :placeholder="t2('please_input', 'menu_name')"
            clearable
            @keyup.enter="handleQuery"
          />
        </el-form-item>
        <el-form-item :label="t('status')">
          <el-select v-model="queryParams.status" :placeholder="t2('please_select', 'status')" clearable style="width:140px" >
            <el-option :label="t('normal')" :value="0" />
            <el-option :label="t('disabled')" :value="1" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleQuery" :icon="Search">{{ t('search') }}</el-button>
          <el-button @click="resetQuery" :icon="Refresh">{{ t('reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <div class="toolbar">
        <div>
          <el-button
            v-permission="'system:menu:create'"
            type="primary"
            @click="handleAdd"
            :icon="Plus"
          >
            {{ t('add') }}
          </el-button>
        </div>
      </div>

      <!-- 树形表格 -->
      <el-table
          
          v-loading="loading"
          :data="menuList"
          row-key="id"
          :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
          default-expand-all
          border
          stripe
        >
        <el-table-column prop="menuName" :label="t('menu_name')" min-width="200">
          <template #default="{ row }">
            <span>{{ row.titleKey ? t(row.titleKey) : row.menuName }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="icon" :label="t('icon')" width="100" />
        <el-table-column prop="orderNum" :label="t('order_num')" width="80" />
        <el-table-column prop="perms" :label="t('perms')" width="180" show-overflow-tooltip />
        <el-table-column prop="path" :label="t('path')" width="150" show-overflow-tooltip />
        <el-table-column prop="component" :label="t('component')" width="200" show-overflow-tooltip />
        <el-table-column prop="menuType" :label="t('menu_type')" width="80">
          <template #default="{ row }">
            <el-tag v-if="row.menuType === 'M'" type="primary">{{ t('directory') }}</el-tag>
            <el-tag v-else-if="row.menuType === 'C'" type="success">{{ t('menu') }}</el-tag>
            <el-tag v-else type="info">{{ t('button') }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" :label="t('status')" width="80">
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
        <el-table-column :label="t('action')" fixed="right" width="280">
          <template #default="{ row }">
            <el-button
              v-permission="'system:menu:view'"
              link
              type="primary"
              @click="handleView(row)"
              :icon="View"
            >
              {{ t('view') }}
            </el-button>
            <el-button
              v-permission="'system:menu:create'"
              link
              type="primary"
              @click="handleAdd(row)"
              :icon="Plus"
            >
              {{ t('add') }}
            </el-button>
            <el-button
              v-permission="'system:menu:update'"
              link
              type="primary"
              @click="handleEdit(row)"
              :icon="Edit"
            >
              {{ t('edit') }}
            </el-button>
            <el-button
              v-permission="'system:menu:delete'"
              link
              type="danger"
              @click="handleDelete(row)"
              :icon="Delete"
            >
              {{ t('delete') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Edit, Delete, Search, Refresh, View } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { getList, deleteById } from '@/api/system/menu'
import { buildTree } from '@/utils/tree'
import { formatDateTime } from '@/utils/dateFormat'

defineOptions({
  name: 'SystemMenu'
})

const { t, t: t2 } = useI18n()
const router = useRouter()

const loading = ref(false)
const menuList = ref([])

const queryParams = reactive({
  menuName: '',
  status: null
})

// 查询
const handleQuery = async () => {
  await getMenuList()
}

// 重置
const resetQuery = () => {
  queryParams.menuName = ''
  queryParams.status = null
  getMenuList()
}

// 获取列表
const getMenuList = async () => {
  loading.value = true
  try {
    const data = await getList(queryParams)
    menuList.value = buildTree(data || [], { idKey: 'id', parentIdKey: 'parentId' })
  } catch (error) {
    console.error('获取菜单列表失败:', error)
  } finally {
    loading.value = false
  }
}

// 查看
const handleView = (row) => {
  router.push({
    path: '/system/menu/detail',
    query: { id: row.id }
  })
}

// 新增
const handleAdd = (row) => {
  router.push({
    path: '/system/menu/add',
    query: row ? { parentId: row.id } : {}
  })
}

// 编辑
const handleEdit = (row) => {
  router.push({
    path: '/system/menu/edit',
    query: { id: row.id }
  })
}

// 删除
const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(
      t('confirmDelete'),
      t('warning'),
      {
        confirmButtonText: t('confirm'),
        cancelButtonText: t('cancel'),
        type: 'warning'
      }
    )

    await deleteById(row.id)
    ElMessage.success(t('deleteSuccess'))
    await getMenuList()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

onMounted(() => {
  getMenuList()
})
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';
</style>
