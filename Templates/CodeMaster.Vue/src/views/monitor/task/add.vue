<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">{{ t('add_task') }}</div>
      </template>

      <el-form ref="formRef" :model="form" :rules="rules" label-width="140px" style="max-width: 800px;">
        <el-form-item :label="t('task_name')" prop="taskName">
          <el-input v-model="form.taskName" :placeholder="t2('please_input', 'name')" />
        </el-form-item>

        <el-form-item :label="t('job_group')" prop="jobGroup">
          <el-input v-model="form.jobGroup" :placeholder="t2('please_input', 'name')" />
        </el-form-item>

        <el-form-item :label="t('task_type')" prop="taskType">
          <el-select v-model="form.taskType" :placeholder="t2('please_select', '')" @change="handleTaskTypeChange" style="width: 100%;">
            <el-option :label="t('task_type_assembly')" :value="0" />
            <el-option :label="t('task_type_http')" :value="1" />
            <el-option :label="t('task_type_sql')" :value="2" />
          </el-select>
        </el-form-item>

        <el-form-item :label="t('invoke_target')" prop="invokeTarget" v-if="form.taskType === 0">
          <el-input v-model="form.invokeTarget" :placeholder="t('invoke_target_placeholder')" />
          <div style="color: #909399; font-size: 12px; margin-top: 4px;">
            {{ t('invoke_target_tip') }}
          </div>
        </el-form-item>

        <el-form-item :label="t('api_url')" prop="apiUrl" v-if="form.taskType === 1">
          <el-input v-model="form.apiUrl" :placeholder="t('api_url_placeholder')" />
        </el-form-item>

        <el-form-item :label="t('request_method')" prop="requestMethod" v-if="form.taskType === 1">
          <el-select v-model="form.requestMethod" :placeholder="t2('please_select', '')" style="width: 100%;">
            <el-option label="GET" value="GET" />
            <el-option label="POST" value="POST" />
            <el-option label="PUT" value="PUT" />
            <el-option label="DELETE" value="DELETE" />
          </el-select>
        </el-form-item>

        <el-form-item :label="t('request_parameters')" prop="requestParameters" v-if="form.taskType === 1">
          <el-input v-model="form.requestParameters" type="textarea" :rows="3" :placeholder="t('request_parameters_placeholder')" />
        </el-form-item>

        <el-form-item :label="t('sql_text')" prop="sqlText" v-if="form.taskType === 2">
          <el-input v-model="form.sqlText" type="textarea" :rows="5" :placeholder="t('sql_text_placeholder')" />
        </el-form-item>

        <el-form-item :label="t('cron_expression')" prop="cronExpression">
          <el-input v-model="form.cronExpression" :placeholder="t('cron_expression_placeholder')" />
          <div style="color: #909399; font-size: 12px; margin-top: 4px;">
            {{ t('cron_expression_tip') }}
          </div>
        </el-form-item>

        <el-form-item :label="t('interval_second')" prop="intervalSecond">
          <el-input-number v-model="form.intervalSecond" :min="1" />
        </el-form-item>

        <el-form-item :label="t('begin_time')" prop="beginTime">
          <el-date-picker
            v-model="form.beginTime"
            type="datetime"
            :placeholder="t2('please_select', '')"
            style="width: 100%;"
          />
        </el-form-item>

        <el-form-item :label="t('end_time')" prop="endTime">
          <el-date-picker
            v-model="form.endTime"
            type="datetime"
            :placeholder="t2('please_select', '')"
            style="width: 100%;"
          />
        </el-form-item>

        <el-form-item :label="t('status')" prop="status">
          <el-radio-group v-model="form.status">
            <el-radio :label="0">{{ t('normal') }}</el-radio>
            <el-radio :label="1">{{ t('paused') }}</el-radio>
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
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { create } from '@/api/monitor/task'

const router = useRouter()
const { t } = useI18n()

const formRef = ref(null)
const submitLoading = ref(false)

const form = reactive({
  taskName: '',
  jobGroup: '',
  taskType: 0,
  invokeTarget: '',
  apiUrl: '',
  requestMethod: 'GET',
  requestParameters: '',
  sqlText: '',
  cronExpression: '',
  intervalSecond: 60,
  beginTime: null,
  endTime: null,
  status: 0,
  remark: ''
})

const rules = {
  taskName: [
    { required: true, message: t2('please_input', 'name'), trigger: 'blur' }
  ],
  jobGroup: [
    { required: true, message: t2('please_input', 'name'), trigger: 'blur' }
  ],
  taskType: [
    { required: true, message: t2('please_select', ''), trigger: 'change' }
  ],
  cronExpression: [
    { required: true, message: t2('please_input', 'cron'), trigger: 'blur' }
  ]
}

const handleTaskTypeChange = () => {
  // 清空相关字段
  form.invokeTarget = ''
  form.apiUrl = ''
  form.requestMethod = 'GET'
  form.requestParameters = ''
  form.sqlText = ''
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    submitLoading.value = true

    await create(form)
    ElMessage.success(t('add_success'))
    router.back()
  } catch (error) {
    if (error !== false) {
      ElMessage.error(error.message || t('add_failed'))
    }
  } finally {
    submitLoading.value = false
  }
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
