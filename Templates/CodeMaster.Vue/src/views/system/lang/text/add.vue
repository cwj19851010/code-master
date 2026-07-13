<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">{{ t('add') }} {{ t('text') }}</div>
      </template>
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="120px"
        style="max-width: 600px"
        v-loading="loading"
      >
        <el-form-item :label="t('code')" prop="langCode">
          <el-select v-model="form.langCode" :placeholder="t2('please_select', 'lang')" style="width: 100%">
            <el-option
              v-for="item in langOptions"
              :key="item.langCode"
              :label="item.langName"
              :value="item.langCode"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('key')" prop="langKey">
          <el-input v-model="form.langKey" :placeholder="t2('please_input', 'key')" />
        </el-form-item>
        <el-form-item :label="t('value')" prop="langValue">
          <el-input v-model="form.langValue" :placeholder="t2('please_input', 'value')" type="textarea" :rows="3" />
        </el-form-item>
        <el-form-item :label="t('category')" prop="category">
          <el-input v-model="form.category" :placeholder="t2('please_input', 'category')" />
        </el-form-item>
        <el-form-item :label="t('remark')" prop="remark">
          <el-input v-model="form.remark" :placeholder="t2('please_input', 'remark')" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="submitLoading">{{ t('submit') }}</el-button>
          <el-button @click="handleCancel">{{ t('cancel') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { createText, getPagedList } from '@/api/system/lang'

const { t } = useI18n()
const router = useRouter()

const loading = ref(false)
const submitLoading = ref(false)
const formRef = ref(null)
const langOptions = ref([])

const form = reactive({
  langCode: 'zh-CN',
  langKey: '',
  langValue: '',
  category: 'common',
  remark: ''
})

const rules = {
  langCode: [{ required: true, message: t2('please_select', 'lang'), trigger: 'change' }],
  langKey: [{ required: true, message: t2('please_input', 'key'), trigger: 'blur' }],
  langValue: [{ required: true, message: t2('please_input', 'value'), trigger: 'blur' }]
}

const loadLangOptions = async () => {
  try {
    const res = await getPagedList({ pageNum: 1, pageSize: 1000 })
    langOptions.value = res.items || []
  } catch (error) {
    // ignore
  }
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    submitLoading.value = true
    await createText(form)
    ElMessage.success(t('saveSuccess'))
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
  loadLangOptions()
})
</script>

<style scoped lang="scss">
.card-header {
  font-weight: bold;
}
</style>
