<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>{{ t('online_user') }}</span>
          <div>
            <el-button type="primary" @click="loadOnlineUsers">
              <el-icon><Refresh /></el-icon>
              {{ t('refresh') }}
            </el-button>
          </div>
        </div>
      </template>

        <el-table :data="onlineUsers" border stripe >
        <el-table-column prop="userId" :label="t('user_id')" width="150" />
        <el-table-column prop="userName" :label="t('username')" width="150" />
        <el-table-column prop="connectionId" :label="t('connection_id')" width="300" />
        <el-table-column prop="connectTime" :label="t('connect_time')" width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.connectTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="lastActiveTime" :label="t('last_active_time')" width="180">
          <template #default="{ row }">
            {{ formatDateTime(row.lastActiveTime) }}
          </template>
        </el-table-column>
        <el-table-column :label="t('action')" width="150" fixed="right">
          <template #default="{ row }">
            <el-button type="danger" size="small" @click="handleForceOffline(row)">
              {{ t('force_offline') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { refreshOnReactivated } from '@/utils/pageLifecycle'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Refresh } from '@element-plus/icons-vue'
import { getOnlineUsers, forceOffline } from '@/api/monitor/online'
import { useI18n } from 'vue-i18n'
import { formatDateTime } from '@/utils/dateFormat'

defineOptions({
  name: 'MonitorOnline'
})

const onlineUsers = ref([])
const { t } = useI18n()

const loadOnlineUsers = async () => {
  try {
    const res = await getOnlineUsers()
    // API 直接返回数组，响应拦截器已解包
    onlineUsers.value = Array.isArray(res) ? res : (res.data || [])
    console.log('在线用户数据:', onlineUsers.value)
  } catch (error) {
    console.error('加载在线用户失败:', error)
    ElMessage.error(t('load_online_failed'))
  }
}

const handleForceOffline = async (row) => {
  try {
    await ElMessageBox.confirm(t('force_offline_confirm', { name: row.userName }), t('prompt'), {
      confirmButtonText: t('confirm'),
      cancelButtonText: t('cancel'),
      type: 'warning'
    })
    
    await forceOffline(row.userId)
    ElMessage.success(t('action_success'))
    await loadOnlineUsers()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(t('action_failed'))
    }
  }
}

onMounted(() => {
  loadOnlineUsers()
})

refreshOnReactivated(loadOnlineUsers)
</script>

<style scoped>
@import '@/styles/list-page.scss';
</style>

<style scoped>
.online-container {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
