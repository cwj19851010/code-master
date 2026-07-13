<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
          <el-button :icon="Back" @click="handleBack">{{ t('back') }}</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" :rules="rules" label-width="120px" style="max-width: 600px">
        <el-form-item :label="t('dictName')" prop="dictName">
          <el-input v-model="form.dictName" :placeholder="t2('please_input', 'dict_name')" />
        </el-form-item>
        <el-form-item :label="t('dictType')" prop="dictType">
          <el-input v-model="form.dictType" :placeholder="t2('please_input', 'dict_type')" />
        </el-form-item>
        <el-form-item :label="t('lang_key')" prop="langKey">
          <el-input v-model="form.langKey" :placeholder="t2('please_input', 'lang_key')" clearable />
          <div style="color: #909399; font-size: 12px; margin-top: 4px;">
            {{ t('lang_key_tip') }}
          </div>
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
          <el-button type="primary" @click="handleSubmit" :loading="submitLoading">{{ t('save') }}</el-button>
          <el-button @click="handleBack">{{ t('cancel') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Back } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { createType } from '@/api/system/dict'

const router = useRouter()
const { t } = useI18n()

const formRef = ref(null)
const submitLoading = ref(false)

const form = reactive({
  dictName: '',
  dictType: '',
  langKey: '',
  status: 0,
  remark: ''
})

const rules = {
  dictName: [{ required: true, message: t2('please_input', 'dict_name'), trigger: 'blur' }],
  dictType: [{ required: true, message: t2('please_input', 'dict_type'), trigger: 'blur' }]
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    submitLoading.value = true

    await createType(form)
    ElMessage.success(t('create_success'))
    router.back()
  } catch (error) {
    if (error !== false) {
      ElMessage.error(t('create_failed'))
    }
  } finally {
    submitLoading.value = false
  }
}

const handleBack = () => {
  router.back()
}
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
