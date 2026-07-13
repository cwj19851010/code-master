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
        style="max-width: 800px"
      >
        <el-form-item :label="t('code')" prop="tenantCode">
          <el-input v-model="form.tenantCode" :placeholder="t2('please_input', 'code')" />
        </el-form-item>

        <el-form-item :label="t('name')" prop="tenantName">
          <el-input v-model="form.tenantName" :placeholder="t2('please_input', 'name')" />
        </el-form-item>

        <el-form-item :label="t('isolation_type')" prop="isolationType">
          <el-radio-group v-model="form.isolationType">
            <el-radio :label="0">{{ t('isolation_physical') }}</el-radio>
            <el-radio :label="1">{{ t('isolation_logical') }}</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item :label="t('db_type')" prop="dbType" v-if="form.isolationType === 0">
          <el-select v-model="form.dbType" :placeholder="t2('please_select', 'db_type')" style="width: 100%">
            <el-option label="SQL Server" :value="1" />
            <el-option label="MySQL" :value="2" />
            <el-option label="PostgreSQL" :value="3" />
          </el-select>
        </el-form-item>

        <el-form-item :label="t('db_connection')" prop="connectionString" v-if="form.isolationType === 0">
          <el-input
            v-model="form.connectionString"
            type="textarea"
            :rows="3"
            :placeholder="t2('please_input', 'db_connection')"
          />
          <el-button
            type="primary"
            size="small"
            style="margin-top: 10px"
            @click="handleTestConnection"
            :loading="testingConnection"
          >
            {{ t('test_connection') }}
          </el-button>
        </el-form-item>

        <el-form-item :label="t('status')" prop="status">
          <el-radio-group v-model="form.status">
            <el-radio :label="0">{{ t('normal') }}</el-radio>
            <el-radio :label="1">{{ t('disabled') }}</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item :label="t('expire_time')">
          <el-date-picker
            v-model="form.expireTime"
            type="datetime"
            :placeholder="t2('please_select', 'expire_time')"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item :label="t('remark')">
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
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { create, testConnection } from '@/api/system/tenant'

const router = useRouter()
const { t } = useI18n()

const formRef = ref(null)
const submitLoading = ref(false)
const testingConnection = ref(false)

const form = reactive({
  tenantCode: '',
  tenantName: '',
  isolationType: 1,
  dbType: 1,
  connectionString: '',
  status: 0,
  expireTime: null,
  remark: ''
})

const rules = {
  tenantCode: [
    { required: true, message: t2('please_input', 'code'), trigger: 'blur' }
  ],
  tenantName: [
    { required: true, message: t2('please_input', 'name'), trigger: 'blur' }
  ],
  isolationType: [
    { required: true, message: t2('please_select', 'isolation'), trigger: 'change' }
  ],
  dbType: [
    { required: true, message: t2('please_select', 'db_type'), trigger: 'change' }
  ],
  connectionString: [
    { required: true, message: t2('please_input', 'db_connection'), trigger: 'blur' }
  ]
}

const handleTestConnection = async () => {
  if (!form.connectionString || !form.dbType) {
    ElMessage.warning(t2('please_input', 'db_connection'))
    return
  }
  testingConnection.value = true
  try {
    await testConnection({
      connectionString: form.connectionString,
      dbType: form.dbType
    })
    ElMessage.success(t('connection_success'))
  } catch (error) {
    ElMessage.error(t('connection_failed'))
  } finally {
    testingConnection.value = false
  }
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    try {
      submitLoading.value = true
      await create(form)
      ElMessage.success(t('saveSuccess'))
      router.back()
    } catch (error) {
      ElMessage.error(error.message || t('saveFailed'))
    } finally {
      submitLoading.value = false
    }
  })
}

const handleCancel = () => {
  router.back()
}
</script>

<style scoped>
.card-header {
  font-size: 16px;
  font-weight: bold;
}
</style>
