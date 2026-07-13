<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
        </div>
      </template>

      <el-descriptions :column="2" border>
        <el-descriptions-item :label="t('username')">
          {{ userInfo.userName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('nickname')">
          {{ userInfo.nickName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('email')">
          {{ userInfo.email }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('phone')">
          {{ userInfo.phoneNumber }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('dept')">
          {{ userInfo.deptName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('post')">
          {{ userInfo.postName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('role')">
          <el-tag v-for="role in userInfo.roles" :key="role.id" class="mr-5">
            {{ role.roleName }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('gender')">
          <span v-if="userInfo.sex === 0">{{ t('gender_male') }}</span>
          <span v-else-if="userInfo.sex === 1">{{ t('gender_female') }}</span>
          <span v-else>{{ t('gender_unknown') }}</span>
        </el-descriptions-item>
        <el-descriptions-item :label="t('status')">
          <el-tag :type="userInfo.status === 0 ? 'success' : 'danger'">
            {{ userInfo.status === 0 ? t('normal') : t('disabled') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('create_time')">
          {{ formatDateTime(userInfo.createTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('update_time')">
          {{ formatDateTime(userInfo.updateTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('remark')" :span="2">
          {{ userInfo.remark || '-' }}
        </el-descriptions-item>
      </el-descriptions>

      <div class="mt-20">
        <el-button type="primary" @click="handleEdit">
          {{ t('edit') }}
        </el-button>
        <el-button @click="handleBack">
          {{ t('back') }}
        </el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { formatDateTime } from '@/utils/dateFormat'
import { getById } from '@/api/system/user'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()

const loading = ref(false)
const userInfo = reactive({
  id: null,
  userName: '',
  nickName: '',
  email: '',
  phoneNumber: '',
  deptName: '',
  postName: '',
  roles: [],
  sex: 0,
  status: 0,
  createTime: '',
  updateTime: '',
  remark: ''
})

// 加载用户详情
const loadUserDetail = async () => {
  const userId = route.query.id
  if (!userId) {
    ElMessage.error('用户ID不能为空')
    router.back()
    return
  }

  try {
    loading.value = true
    const data = await getById(userId)
    Object.assign(userInfo, data)
  } catch (error) {
    ElMessage.error(error.message || '加载用户信息失败')
    router.back()
  } finally {
    loading.value = false
  }
}

// 编辑
const handleEdit = () => {
  router.push({
    path: '/system/user/edit',
    query: { id: userInfo.id }
  })
}

// 返回
const handleBack = () => {
  router.back()
}

onMounted(() => {
  loadUserDetail()
})
</script>

<style scoped>
.card-header {
  font-size: 16px;
  font-weight: bold;
}

.mr-5 {
  margin-right: 5px;
}

.mt-20 {
  margin-top: 20px;
}
</style>
