<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="120px"
        style="max-width: 800px"
      >
        <el-form-item :label="t('username')" prop="userName">
          <el-input v-model="form.userName" :placeholder="t2('please_input', 'username')" disabled />
        </el-form-item>

        <el-form-item :label="t('nickname')" prop="nickName">
          <el-input v-model="form.nickName" :placeholder="t2('please_input', 'nickname')" />
        </el-form-item>

        <el-form-item :label="t('email')" prop="email">
          <el-input v-model="form.email" :placeholder="t2('please_input', 'email')" />
        </el-form-item>

        <el-form-item :label="t('phone')" prop="phoneNumber">
          <el-input v-model="form.phoneNumber" :placeholder="t2('please_input', 'phone')" />
        </el-form-item>

        <el-form-item :label="t('dept')" prop="deptId">
          <el-tree-select
            v-model="form.deptId"
            :data="deptTree"
            :props="{ label: 'name', value: 'id' }"
            :placeholder="t2('please_select', 'dept')"
            check-strictly
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item :label="t('post')" prop="postId">
          <el-select v-model="form.postId" :placeholder="t2('please_select', 'post')" clearable style="width: 100%">
            <el-option
              v-for="post in postList"
              :key="post.id"
              :label="post.postName"
              :value="post.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item :label="t('role')" prop="roleIds">
          <el-select v-model="form.roleIds" :placeholder="t2('please_select', 'role')" multiple style="width: 100%">
            <el-option
              v-for="role in roleList"
              :key="role.id"
              :label="role.roleName"
              :value="role.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item :label="t('gender')" prop="sex">
          <el-radio-group v-model="form.sex">
            <el-radio :label="0">{{ t('gender_male') }}</el-radio>
            <el-radio :label="1">{{ t('gender_female') }}</el-radio>
            <el-radio :label="2">{{ t('gender_unknown') }}</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item :label="t('status')" prop="status">
          <el-radio-group v-model="form.status">
            <el-radio :label="0">{{ t('normal') }}</el-radio>
            <el-radio :label="1">{{ t('disabled') }}</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item :label="t('remark')" prop="remark">
          <el-input v-model="form.remark" type="textarea" :rows="3" :placeholder="t2('please_input', 'remark')" />
        </el-form-item>

        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="submitLoading">
            {{ t('save') }}
          </el-button>
          <el-button @click="handleCancel">
            {{ t('cancel') }}
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { getById, update } from '@/api/system/user'
import request from '@/utils/request'
import { buildTree } from '@/utils/tree'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()

const formRef = ref(null)
const loading = ref(false)
const submitLoading = ref(false)
const deptTree = ref([])
const postList = ref([])
const roleList = ref([])

const form = reactive({
  id: null,
  userName: '',
  nickName: '',
  email: '',
  phoneNumber: '',
  deptId: null,
  postId: null,
  roleIds: [],
  sex: 0,
  status: 0,
  remark: ''
})

const rules = {
  nickName: [
    { required: true, message: t2('please_input', 'nickname'), trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: t2('format_error', 'email'), trigger: 'blur' }
  ],
  phoneNumber: [
    { pattern: /^1[3-9]\d{9}$/, message: t2('format_error', 'phone'), trigger: 'blur' }
  ]
}

// 加载部门树
const loadDeptTree = async () => {
  try {
    const data = await request.get('/system/dept/getlist')
    deptTree.value = buildTree(data || [], 'id', 'parentId', 'name')
  } catch (error) {
    console.error('加载部门树失败:', error)
  }
}

// 加载岗位列表
const loadPostList = async () => {
  try {
    const data = await request.get('/system/post/getalllist')
    postList.value = data || []
  } catch (error) {
    console.error('加载岗位列表失败:', error)
  }
}

// 加载角色列表
const loadRoleList = async () => {
  try {
    const data = await request.get('/system/role/getalllist')
    roleList.value = data || []
  } catch (error) {
    console.error('加载角色列表失败:', error)
  }
}

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

    Object.assign(form, {
      id: data.id,
      userName: data.userName,
      nickName: data.nickName,
      email: data.email,
      phoneNumber: data.phoneNumber,
      deptId: data.deptId,
      postId: data.postId,
      roleIds: data.roleIds || [],
      sex: data.sex,
      status: data.status,
      remark: data.remark
    })
  } catch (error) {
    ElMessage.error(error.message || '加载用户信息失败')
    router.back()
  } finally {
    loading.value = false
  }
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    try {
      submitLoading.value = true
      await update(form.id, form)
      ElMessage.success(t('saveSuccess'))
      router.back()
    } catch (error) {
      ElMessage.error(error.message || t('saveFailed'))
    } finally {
      submitLoading.value = false
    }
  })
}

// 取消
const handleCancel = () => {
  router.back()
}

onMounted(() => {
  loadDeptTree()
  loadPostList()
  loadRoleList()
  loadUserDetail()
})
</script>

<style scoped>
.card-header {
  font-size: 16px;
  font-weight: bold;
}
</style>
