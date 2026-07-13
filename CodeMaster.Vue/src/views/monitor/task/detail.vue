<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">{{ t($route.meta.title) }}</div>
      </template>

      <el-descriptions :column="2" border>
        <el-descriptions-item :label="t('task_name')">
          {{ taskDetail.taskName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('job_group')">
          {{ taskDetail.jobGroup }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('task_type')">
          <el-tag v-if="taskDetail.taskType === 0" type="success">{{ t('task_type_assembly') }}</el-tag>
          <el-tag v-else-if="taskDetail.taskType === 1" type="primary">{{ t('task_type_http') }}</el-tag>
          <el-tag v-else-if="taskDetail.taskType === 2" type="warning">{{ t('task_type_sql') }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('status')">
          <el-tag :type="taskDetail.status === 0 ? 'success' : 'danger'">
            {{ taskDetail.status === 0 ? t('normal') : t('paused') }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item :label="t('invoke_target')" :span="2" v-if="taskDetail.taskType === 0">
          {{ taskDetail.invokeTarget }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('api_url')" :span="2" v-if="taskDetail.taskType === 1">
          {{ taskDetail.apiUrl }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('request_method')" v-if="taskDetail.taskType === 1">
          {{ taskDetail.requestMethod }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('request_parameters')" :span="2" v-if="taskDetail.taskType === 1">
          <pre style="margin: 0;">{{ taskDetail.requestParameters }}</pre>
        </el-descriptions-item>
        <el-descriptions-item :label="t('sql_text')" :span="2" v-if="taskDetail.taskType === 2">
          <pre style="margin: 0;">{{ taskDetail.sqlText }}</pre>
        </el-descriptions-item>
        <el-descriptions-item :label="t('cron_expression')" :span="2">
          {{ taskDetail.cronExpression }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('interval_second')">
          {{ taskDetail.intervalSecond }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('run_times')">
          {{ taskDetail.runTimes }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('begin_time')">
          {{ formatDateTime(taskDetail.beginTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('end_time')">
          {{ formatDateTime(taskDetail.endTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('last_run_time')">
          {{ formatDateTime(taskDetail.lastRunTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('create_time')">
          {{ formatDateTime(taskDetail.createTime) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('remark')" :span="2">
          {{ taskDetail.remark }}
        </el-descriptions-item>
      </el-descriptions>

      <div style="margin-top: 20px;">
        <el-button @click="handleBack">{{ t('back') }}</el-button>
        <el-button type="primary" @click="handleEdit" v-permission="'system:task:update'">{{ t('edit') }}</el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'
import { getById } from '@/api/monitor/task'
import { formatDateTime } from '@/utils/dateFormat'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()

const loading = ref(false)
const taskDetail = reactive({
  taskName: '',
  jobGroup: '',
  taskType: 0,
  invokeTarget: '',
  apiUrl: '',
  requestMethod: '',
  requestParameters: '',
  sqlText: '',
  cronExpression: '',
  intervalSecond: 0,
  runTimes: 0,
  beginTime: null,
  endTime: null,
  lastRunTime: null,
  status: 0,
  remark: '',
  createTime: null
})

const loadTaskDetail = async () => {
  try {
    loading.value = true
    const id = route.query.id
    if (!id) {
      ElMessage.error(t('invalid_id'))
      router.back()
      return
    }

    const data = await getById(id)
    Object.assign(taskDetail, data)
  } catch (error) {
    ElMessage.error(error.message || t('load_failed'))
    router.back()
  } finally {
    loading.value = false
  }
}

const handleBack = () => {
  router.back()
}

const handleEdit = () => {
  router.push({
    path: '/monitor/task/edit',
    query: { id: taskDetail.id }
  })
}

onMounted(() => {
  loadTaskDetail()
})
</script>

<style scoped>
.card-header {
  font-size: 16px;
  font-weight: bold;
}

pre {
  white-space: pre-wrap;
  word-wrap: break-word;
}
</style>
