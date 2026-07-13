<template>
  <div class="app-container">
    <el-card shadow="never">
      <div class="mb-20">
        <el-button @click="handleBack" :icon="ArrowLeft">{{ t('back') }}</el-button>
        <el-button type="primary" @click="handleEdit" :icon="Edit" v-permission="'system:menu:update'">
          {{ t('edit') }}
        </el-button>
      </div>

      <el-descriptions :column="2" border v-loading="loading">
        <el-descriptions-item :label="t('menuName')">
          {{ menuDetail.menuName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('titleKey')">
          {{ menuDetail.titleKey || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('menuType')">
          <el-tag v-if="menuDetail.menuType === 'M'" type="primary">{{ t('directory') }}</el-tag>
          <el-tag v-else-if="menuDetail.menuType === 'C'" type="success">{{ t('menu') }}</el-tag>
          <el-tag v-else type="info">{{ t('button') }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('orderNum')">
          {{ menuDetail.orderNum }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('icon')">
          {{ menuDetail.icon || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('path')">
          {{ menuDetail.path || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('component')" :span="2">
          {{ menuDetail.component || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('perms')" :span="2">
          {{ menuDetail.perms || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('visible')">
          <el-tag :type="menuDetail.visible === 0 ? 'success' : 'info'">
            {{ menuDetail.visible === 0 ? t('show') : t('hide') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('status')">
          <el-tag :type="menuDetail.status === 0 ? 'success' : 'danger'">
            {{ menuDetail.status === 0 ? t('normal') : t('disabled') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('isCache')">
          <el-tag :type="menuDetail.isCache === 0 ? 'success' : 'info'">
            {{ menuDetail.isCache === 0 ? t('cache') : t('noCache') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('menuScope')">
          <el-tag v-if="menuDetail.menuScope === 0" type="warning">{{ t('hostOnly') }}</el-tag>
          <el-tag v-else-if="menuDetail.menuScope === 1" type="primary">{{ t('tenantOnly') }}</el-tag>
          <el-tag v-else type="success">{{ t('shared') }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('create_time')">
          {{ formatDateTime(menuDetail.createTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('createBy')">
          {{ menuDetail.createBy || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('updateTime')">
          {{ formatDateTime(menuDetail.updateTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('updateBy')">
          {{ menuDetail.updateBy || '-' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('remark')" :span="2">
          {{ menuDetail.remark || '-' }}
        </el-descriptions-item>
      </el-descriptions>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ArrowLeft, Edit } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { getById } from '@/api/system/menu'
import { formatDateTime } from '@/utils/dateFormat'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()

const loading = ref(false)
const menuDetail = reactive({
  menuName: '',
  titleKey: '',
  menuType: '',
  orderNum: 0,
  icon: '',
  path: '',
  component: '',
  perms: '',
  visible: 0,
  status: 0,
  isCache: 0,
  menuScope: 2,
  createTime: '',
  createBy: '',
  updateTime: '',
  updateBy: '',
  remark: ''
})

// 加载菜单详情
const loadMenuDetail = async () => {
  const id = route.query.id
  if (!id) {
    ElMessage.error(t('invalidId'))
    router.push('/system/menu')
    return
  }

  loading.value = true
  try {
    const data = await getById(id)
    Object.assign(menuDetail, data)
  } catch (error) {
    console.error('加载菜单详情失败:', error)
    ElMessage.error(t('loadFailed'))
  } finally {
    loading.value = false
  }
}

// 编辑
const handleEdit = () => {
  router.push({
    path: '/system/menu/edit',
    query: { id: route.query.id }
  })
}

// 返回
const handleBack = () => {
  router.push('/system/menu')
}

onMounted(() => {
  loadMenuDetail()
})
</script>

<style scoped>
.mb-20 {
  margin-bottom: 20px;
}
</style>
