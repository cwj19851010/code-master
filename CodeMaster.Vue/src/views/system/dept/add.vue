<template>
  <div class="app-container">
    <el-card shadow="never">
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
        style="max-width: 600px"
        v-loading="loading"
      >
        <el-form-item :label="t('parent_dept')" prop="parentId">
          <el-tree-select
            v-model="form.parentId"
            :data="deptTreeOptions"
            :props="{ label: 'name', children: 'children', value: 'id' }"
            node-key="id"
            check-strictly
            clearable
            :placeholder="t2('please_select', 'parent_dept')"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item :label="t('name')" prop="name">
          <el-input v-model="form.name" :placeholder="t2('please_input', 'name')" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="submitLoading">{{ t('save') }}</el-button>
          <el-button @click="handleCancel">{{ t('cancel') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { resetReactiveForm, resetFormValidation } from '@/utils/pageLifecycle'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getList, create } from '@/api/system/dept'
import { useI18n } from 'vue-i18n'

import { buildTree } from '@/utils/tree'

const { t, t: t2 } = useI18n()
const router = useRouter()
const route = useRoute()

const loading = ref(false)
const submitLoading = ref(false)
const formRef = ref(null)

const form = reactive({
  parentId: null,
  name: ''
})

const deptTreeOptions = ref([])

const initialForm = JSON.parse(JSON.stringify(form))

const rules = {
  name: [
    { required: true, message: t2('please_input', ''), trigger: 'blur' }
  ]
}

// 获取部门树选项（用于父级选择）
const getDeptTreeOptions = async () => {
  loading.value = true
  try {
    const data = await getList()
    console.log('原始部门数据:', data)
    const tree = buildTree(data || [])
    console.log('构建后的树形数据:', tree)
    // 确保是数组
    deptTreeOptions.value = Array.isArray(tree) ? tree : [tree]
  } catch (error) {
    console.error('获取部门树失败:', error)
    ElMessage.error(t('loadFailed'))
  } finally {
    loading.value = false
  }
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    submitLoading.value = true
    await create({
      parentId: form.parentId,
      name: form.name
    })
    ElMessage.success(t('addSuccess'))
    resetReactiveForm(form, initialForm)
    resetFormValidation(formRef)
    router.back()
  } catch (error) {
    if (error !== false) {
      ElMessage.error(t('saveFailed'))
    }
  } finally {
    submitLoading.value = false
  }
}

const handleCancel = () => {
  router.back()
}

onMounted(() => {
  // 如果有传入 parentId 参数，设置为默认父级
  const parentId = route.query.parentId
  if (parentId && parentId !== 'undefined') {
    form.parentId = parentId
  }
  getDeptTreeOptions()
})
</script>

<style scoped lang="scss">
.card-header {
  font-weight: bold;
}
</style>
