<template>
  <el-dialog
    v-model="visible"
    title="模型配置"
    width="760px"
    append-to-body
    destroy-on-close
  >
    <div class="provider-toolbar">
      <span class="provider-count">共 {{ providers.length }} 个配置</span>
      <el-button type="primary" :icon="Plus" @click="openCreate">新增</el-button>
    </div>

    <el-table :data="providers" max-height="360" empty-text="暂无模型配置">
      <el-table-column prop="name" label="名称" min-width="130" />
      <el-table-column prop="providerType" label="协议" width="150">
        <template #default="{ row }">
          {{ providerTypeLabel(row.providerType) }}
        </template>
      </el-table-column>
      <el-table-column prop="modelName" label="模型" min-width="150" show-overflow-tooltip />
      <el-table-column label="运行位置" width="100">
        <template #default="{ row }">
          <el-tag size="small" :type="row.executionMode === 'Local' ? 'warning' : 'info'">
            {{ row.executionMode === 'Local' ? '本机' : '服务端' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="状态" width="88">
        <template #default="{ row }">
          <el-tag v-if="row.isDefault" size="small" type="success">默认</el-tag>
          <span v-else>{{ row.isEnabled ? '启用' : '停用' }}</span>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="184" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" :loading="testingId === row.id" @click="handleTest(row)">测试</el-button>
          <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
          <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog
      v-model="editorVisible"
      :title="editingId ? '编辑模型配置' : '新增模型配置'"
      width="580px"
      append-to-body
      destroy-on-close
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="92px">
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="例如：本机 Ollama" />
        </el-form-item>
        <el-form-item label="协议" prop="providerType">
          <el-select v-model="form.providerType" style="width: 100%" @change="handleProviderTypeChange">
            <el-option label="OpenAI Compatible" value="OpenAICompatible" />
            <el-option label="Anthropic Claude" value="Anthropic" />
          </el-select>
        </el-form-item>
        <el-form-item label="运行位置" prop="executionMode">
          <el-radio-group v-model="form.executionMode" @change="handleExecutionModeChange">
            <el-radio-button label="Server">服务端</el-radio-button>
            <el-radio-button label="Local" :disabled="!clientMode">本机客户端</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="API 地址" prop="baseUrl">
          <el-input v-model="form.baseUrl" placeholder="https://api.example.com/v1" />
        </el-form-item>
        <el-form-item label="模型名称" prop="modelName">
          <el-input v-model="form.modelName" placeholder="输入服务商提供的模型 ID" />
        </el-form-item>
        <el-form-item label="API Key">
          <el-input
            v-model="form.apiKey"
            type="password"
            show-password
            :placeholder="editingHasApiKey ? '已保存，留空则保持不变' : '本地 OpenAI 兼容服务可留空'"
          />
        </el-form-item>
        <el-form-item v-if="form.providerType === 'OpenAICompatible'" label="附加请求头">
          <el-input
            v-model="form.extraHeadersJson"
            type="textarea"
            :rows="3"
            placeholder='{"X-Provider-Key":"value"}'
          />
        </el-form-item>
        <el-form-item label="默认配置">
          <el-switch v-model="form.isDefault" />
        </el-form-item>
        <el-form-item label="启用">
          <el-switch v-model="form.isEnabled" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editorVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </el-dialog>
</template>

<script setup>
import { reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import {
  createProvider,
  deleteProvider,
  testProvider,
  updateProvider
} from '@/api/agent'
import { executeCodegenAction, isCodeMasterClient } from '@/utils/codegenExecution'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  providers: { type: Array, default: () => [] }
})

const emit = defineEmits(['update:modelValue', 'refresh'])

const visible = ref(props.modelValue)
const editorVisible = ref(false)
const editingId = ref('')
const editingHasApiKey = ref(false)
const saving = ref(false)
const testingId = ref('')
const clientMode = isCodeMasterClient()
const formRef = ref()

const createDefaultForm = () => ({
  name: '',
  providerType: 'OpenAICompatible',
  executionMode: 'Server',
  baseUrl: 'https://api.openai.com/v1',
  modelName: '',
  apiKey: '',
  clearApiKey: false,
  extraHeadersJson: '',
  isDefault: props.providers.length === 0,
  isEnabled: true
})

const form = reactive(createDefaultForm())
const rules = {
  name: [{ required: true, message: '请输入配置名称', trigger: 'blur' }],
  providerType: [{ required: true, message: '请选择协议', trigger: 'change' }],
  executionMode: [{ required: true, message: '请选择运行位置', trigger: 'change' }],
  baseUrl: [{ required: true, message: '请输入 API 地址', trigger: 'blur' }],
  modelName: [{ required: true, message: '请输入模型名称', trigger: 'blur' }]
}

watch(() => props.modelValue, value => { visible.value = value })
watch(visible, value => emit('update:modelValue', value))

const providerTypeLabel = type => type === 'Anthropic' ? 'Anthropic Claude' : 'OpenAI Compatible'

const resetForm = value => {
  Object.assign(form, createDefaultForm(), value || {})
  form.apiKey = ''
  form.clearApiKey = false
}

const openCreate = () => {
  editingId.value = ''
  editingHasApiKey.value = false
  resetForm()
  editorVisible.value = true
}

const openEdit = row => {
  editingId.value = row.id
  editingHasApiKey.value = row.hasApiKey
  resetForm(row)
  editorVisible.value = true
}

const handleProviderTypeChange = value => {
  form.baseUrl = value === 'Anthropic'
    ? 'https://api.anthropic.com'
    : (form.executionMode === 'Local' ? 'http://127.0.0.1:11434/v1' : 'https://api.openai.com/v1')
}

const handleExecutionModeChange = value => {
  if (form.providerType === 'OpenAICompatible') {
    form.baseUrl = value === 'Local'
      ? 'http://127.0.0.1:11434/v1'
      : 'https://api.openai.com/v1'
  }
}

const handleSave = async () => {
  await formRef.value?.validate()
  saving.value = true
  try {
    const payload = { ...form }
    const isLocal = payload.executionMode === 'Local'
    const serverPayload = isLocal
      ? { ...payload, apiKey: '', clearApiKey: true }
      : payload
    let savedProvider
    if (editingId.value) {
      savedProvider = await updateProvider(editingId.value, serverPayload)
    } else {
      savedProvider = await createProvider(serverPayload)
    }
    if (isLocal) {
      try {
        await executeCodegenAction('saveAiProvider', {
          providerId: savedProvider.id,
          providerType: payload.providerType,
          baseUrl: payload.baseUrl,
          modelName: payload.modelName,
          apiKey: payload.apiKey,
          clearApiKey: payload.clearApiKey,
          extraHeadersJson: payload.extraHeadersJson
        }, () => Promise.reject(new Error('本机模型只能在 CodeMaster 客户端中配置')))
      } catch (error) {
        if (!editingId.value) {
          try { await deleteProvider(savedProvider.id) } catch { /* Keep the local configuration error. */ }
        }
        throw error
      }
    }
    ElMessage.success('保存成功')
    editorVisible.value = false
    emit('refresh')
  } finally {
    saving.value = false
  }
}

const handleTest = async row => {
  testingId.value = row.id
  try {
    const result = row.executionMode === 'Local'
      ? await executeCodegenAction(
        'testAiProvider',
        { providerId: row.id },
        () => Promise.reject(new Error('本机模型只能在 CodeMaster 客户端中测试'))
      )
      : await testProvider(row.id)
    if (result.success) {
      ElMessage.success(result.message)
    } else {
      ElMessage.warning(result.message)
    }
    emit('refresh')
  } finally {
    testingId.value = ''
  }
}

const handleDelete = async row => {
  await ElMessageBox.confirm(`确认删除模型配置“${row.name}”吗？`, '提示', { type: 'warning' })
  await deleteProvider(row.id)
  if (row.executionMode === 'Local' && clientMode) {
    try {
      await executeCodegenAction('deleteAiProvider', { providerId: row.id }, () => Promise.resolve())
    } catch {
      // The server record is already deleted; stale local secrets are harmless and can be overwritten.
    }
  }
  ElMessage.success('删除成功')
  emit('refresh')
}
</script>

<style scoped lang="scss">
.provider-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}

.provider-count {
  color: var(--app-text-muted, #64748b);
  font-size: 13px;
}
</style>
