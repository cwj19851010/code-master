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
        <el-form-item :label="t('name')" prop="postName">
          <el-input v-model="form.postName" :placeholder="t2('please_input', 'name')" />
        </el-form-item>
        <el-form-item :label="t('data_scope')" prop="dataScope">
          <el-radio-group v-model="form.dataScope">
            <el-radio :label="1">{{ t('data_scope_self') }}</el-radio>
            <el-radio :label="2">{{ t('data_scope_dept') }}</el-radio>
            <el-radio :label="3">{{ t('data_scope_all') }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('remark')" prop="remark">
          <el-input v-model="form.remark" type="textarea" :rows="3" :placeholder="t2('please_input', 'remark')" />
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
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getById, update } from '@/api/system/post'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()

const loading = ref(false)
const submitLoading = ref(false)
const formRef = ref(null)

const form = reactive({
  id: null,
  postName: '',
  dataScope: 1,
  remark: ''
})

const rules = {
  postName: [
    { required: true, message: t2('please_input', 'name'), trigger: 'blur' }
  ]
}

const loadPostDetail = async () => {
  loading.value = true
  try {
    const id = route.query.id
    const res = await getById(id)
    Object.assign(form, res)
  } catch (error) {
    ElMessage.error(t('load_failed'))
    router.back()
  } finally {
    loading.value = false
  }
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    submitLoading.value = true
    await update(form.id, form)
    ElMessage.success(t('update_success'))
    router.push('/system/post')
  } catch (error) {
    if (error !== false) {
      ElMessage.error(t('update_failed'))
    }
  } finally {
    submitLoading.value = false
  }
}

const handleCancel = () => {
  router.back()
}

onMounted(() => {
  loadPostDetail()
})
</script>

<style scoped lang="scss">
.card-header {
  font-weight: bold;
}
</style>
