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
      >
        <el-form-item :label="t('code')" prop="langCode">
          <el-input v-model="form.langCode" :placeholder="t2('please_input', 'code')" />
        </el-form-item>
        <el-form-item :label="t('name')" prop="langName">
          <el-input v-model="form.langName" :placeholder="t2('please_input', 'name')" />
        </el-form-item>
        <el-form-item :label="t('is_default')" prop="isDefault">
          <el-switch v-model="form.isDefault" :active-value="1" :inactive-value="0" />
        </el-form-item>
        <el-form-item :label="t('status')" prop="isEnabled">
          <el-radio-group v-model="form.isEnabled">
            <el-radio :label="0">{{ t('normal') }}</el-radio>
            <el-radio :label="1">{{ t('disabled') }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('sort')" prop="sort">
          <el-input-number v-model="form.sort" :min="0" :max="999" />
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
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { create } from '@/api/system/lang'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'

const { t } = useI18n()
const router = useRouter()

const submitLoading = ref(false)
const formRef = ref(null)

const form = reactive({
  langCode: '',
  langName: '',
  isDefault: 0,
  isEnabled: 0,
  sort: 0,
  remark: ''
})

const rules = {
  langCode: [
    { required: true, message: t2('please_input', 'code'), trigger: 'blur' }
  ],
  langName: [
    { required: true, message: t2('please_input', 'name'), trigger: 'blur' }
  ]
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    submitLoading.value = true
    await create(form)
    ElMessage.success(t('create_success'))
    router.push('/system/lang')
  } catch (error) {
    if (error !== false) {
      ElMessage.error(t('create_failed'))
    }
  } finally {
    submitLoading.value = false
  }
}

const handleCancel = () => {
  router.back()
}
</script>

<style scoped lang="scss">
.card-header {
  font-weight: bold;
}
</style>
