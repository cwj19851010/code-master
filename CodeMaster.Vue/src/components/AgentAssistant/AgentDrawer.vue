<template>
  <el-drawer
    v-model="visible"
    size="560px"
    direction="rtl"
    append-to-body
    class="codemaster-agent-drawer"
    :show-close="false"
  >
    <template #header>
      <div class="drawer-header">
        <div>
          <div class="drawer-title">CodeMaster Agent</div>
          <div class="drawer-subtitle">项目建模与代码生成助手</div>
        </div>
        <div class="drawer-actions">
          <el-tooltip content="新对话" placement="bottom">
            <el-button text circle aria-label="新对话" :icon="EditPen" @click="startNewConversation" />
          </el-tooltip>
          <el-tooltip content="模型配置" placement="bottom">
            <el-button text circle aria-label="模型配置" :icon="Setting" @click="providerDialogVisible = true" />
          </el-tooltip>
          <el-button text circle aria-label="关闭 AI 助手" :icon="Close" @click="visible = false" />
        </div>
      </div>
    </template>

    <div class="agent-layout">
      <div class="context-panel">
        <div class="context-field project-field">
          <span class="context-label">所属项目</span>
          <el-select
            v-model="selectedProjectId"
            filterable
            placeholder="请选择项目"
            style="width: 100%"
            @change="handleProjectChange"
          >
            <el-option
              v-for="project in projects"
              :key="project.id"
              :label="project.displayName || project.projectName"
              :value="project.id"
            >
              <div class="project-option">
                <span>{{ project.displayName || project.projectName }}</span>
                <small>{{ project.projectName }}</small>
              </div>
            </el-option>
          </el-select>
        </div>

        <div class="context-field provider-field">
          <span class="context-label">模型</span>
          <el-select
            v-model="selectedProviderId"
            placeholder="请选择模型"
            style="width: 100%"
            @change="handleProviderChange"
          >
            <el-option
              v-for="provider in enabledProviders"
              :key="provider.id"
              :label="`${provider.name} · ${provider.modelName}`"
              :value="provider.id"
              :disabled="provider.executionMode === 'Local'"
            >
              <div class="provider-option">
                <span>{{ provider.name }}</span>
                <small>{{ provider.modelName }}</small>
              </div>
            </el-option>
          </el-select>
        </div>

        <div v-if="selectedProjectId" class="conversation-field">
          <el-select
            v-model="activeConversationId"
            class="conversation-select"
            placeholder="新对话"
            clearable
            style="width: 100%"
            @change="handleConversationChange"
          >
            <el-option
              v-for="conversation in conversations"
              :key="conversation.id"
              :label="conversation.title"
              :value="conversation.id"
            />
          </el-select>
          <el-tooltip v-if="activeConversationId" content="删除当前对话" placement="bottom">
            <el-button
              class="conversation-delete"
              text
              circle
              aria-label="删除当前对话"
              :icon="Delete"
              @click="archiveCurrentConversation"
            />
          </el-tooltip>
        </div>
      </div>

      <div ref="messagePanelRef" class="message-panel">
        <div v-if="!selectedProjectId" class="empty-state">
          <el-icon><FolderOpened /></el-icon>
          <span>请选择所属项目</span>
        </div>
        <div v-else-if="enabledProviders.length === 0" class="empty-state">
          <el-icon><Connection /></el-icon>
          <span>请先配置模型</span>
          <el-button type="primary" @click="providerDialogVisible = true">配置模型</el-button>
        </div>
        <template v-else>
          <div v-if="messages.length === 0" class="empty-state compact">
            <el-icon><ChatDotRound /></el-icon>
            <span>可以开始描述需求了</span>
          </div>

          <div
            v-for="message in messages"
            :key="message.id"
            class="message-row"
            :class="message.role"
          >
            <div class="message-role">{{ message.role === 'user' ? '你' : 'Agent' }}</div>
            <div class="message-bubble">{{ message.content }}</div>
            <time>{{ formatTime(message.createTime) }}</time>
          </div>

          <div
            v-for="approval in pendingApprovals"
            :key="approval.id"
            class="approval-card"
          >
            <div class="approval-header">
              <div>
                <strong>{{ toolTitle(approval.toolName) }}</strong>
                <span>{{ approval.status === 'PendingApproval' ? '等待确认' : '等待执行生成任务' }}</span>
              </div>
              <el-tag size="small" type="warning">
                {{ approval.status === 'PendingApproval' ? '待审批' : '待本地执行' }}
              </el-tag>
            </div>
            <div v-if="approval.toolName === 'project_change_set'" class="change-set-overview">
              <strong>{{ changeSetOverview(approval.inputJson).summary || '项目建模变更' }}</strong>
              <div class="change-set-counts">
                <el-tag size="small">模块 {{ changeSetOverview(approval.inputJson).modules }}</el-tag>
                <el-tag size="small">实体 {{ changeSetOverview(approval.inputJson).entities }}</el-tag>
                <el-tag size="small">字段 {{ changeSetOverview(approval.inputJson).fields }}</el-tag>
                <el-tag size="small">关系 {{ changeSetOverview(approval.inputJson).relations }}</el-tag>
                <el-tag v-if="changeSetOverview(approval.inputJson).updates" size="small" type="warning">
                  更新 {{ changeSetOverview(approval.inputJson).updates }}
                </el-tag>
                <el-tag v-if="changeSetOverview(approval.inputJson).deletes" size="small" type="danger">
                  删除 {{ changeSetOverview(approval.inputJson).deletes }}
                </el-tag>
                <el-tag v-if="changeSetOverview(approval.inputJson).generationMode !== 'None'" size="small" type="success">
                  {{ changeSetOverview(approval.inputJson).generationMode === 'Full' ? '全量生成' : '增量生成' }}
                </el-tag>
              </div>
            </div>
            <details class="approval-details">
              <summary>查看完整变更</summary>
              <pre>{{ formatToolInput(approval.inputJson) }}</pre>
            </details>
            <div class="approval-actions">
              <el-button
                v-if="approval.status === 'PendingApproval'"
                :loading="approvalLoadingId === approval.id"
                @click="rejectApproval(approval)"
              >拒绝</el-button>
              <el-button type="primary" :loading="approvalLoadingId === approval.id" @click="approveApproval(approval)">
                {{ approval.status === 'PendingApproval' ? '确认执行' : '继续执行生成任务' }}
              </el-button>
            </div>
          </div>

          <div v-if="sending" class="typing-row">
            <span></span><span></span><span></span>
          </div>
        </template>
      </div>

      <div class="composer">
        <el-input
          v-model="draft"
          type="textarea"
          :rows="3"
          resize="none"
          maxlength="4000"
          placeholder="描述要分析或创建的模块、实体和字段"
          :disabled="!canSend"
          @keydown.ctrl.enter.prevent="handleSend"
        />
        <div class="composer-footer">
          <span>Ctrl + Enter</span>
          <el-button
            type="primary"
            :icon="Promotion"
            :loading="sending"
            :disabled="!canSend || !draft.trim()"
            @click="handleSend"
          >
            发送
          </el-button>
        </div>
      </div>
    </div>

    <provider-settings-dialog
      v-model="providerDialogVisible"
      :providers="providers"
      @refresh="loadProviders"
    />
  </el-drawer>
</template>

<script setup>
import { computed, nextTick, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  ChatDotRound,
  Close,
  Connection,
  Delete,
  EditPen,
  FolderOpened,
  Promotion,
  Setting
} from '@element-plus/icons-vue'
import { buildProject, enhanceUiPage, getList as getProjectList } from '@/api/codegen/project'
import {
  generateCode,
  generateIncrementalCode,
  generateProjectCode,
  generateProjectIncrementalCode
} from '@/api/codegen/moduleEntity'
import {
  approveTool,
  completeToolClientActions,
  createConversation,
  deleteConversation,
  getConversations,
  getMessages,
  getProviders,
  getToolExecutions,
  rejectTool,
  sendMessage
} from '@/api/agent'
import ProviderSettingsDialog from './ProviderSettingsDialog.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue'])

const visible = ref(props.modelValue)
const projects = ref([])
const providers = ref([])
const conversations = ref([])
const messages = ref([])
const toolExecutions = ref([])
const selectedProjectId = ref('')
const selectedProviderId = ref('')
const activeConversationId = ref('')
const draft = ref('')
const sending = ref(false)
const retryRequestId = ref('')
const retryContent = ref('')
const providerDialogVisible = ref(false)
const approvalLoadingId = ref('')
const messagePanelRef = ref()
const enabledProviders = computed(() => providers.value.filter(item => item.isEnabled))
const pendingApprovals = computed(() => toolExecutions.value.filter(item =>
  item.status === 'PendingApproval' || item.status === 'AwaitingClientExecution'
))
const canSend = computed(() => Boolean(selectedProjectId.value && selectedProviderId.value && !sending.value))

watch(() => props.modelValue, value => { visible.value = value })
watch(visible, async value => {
  emit('update:modelValue', value)
  if (value) {
    await loadInitialData()
  }
})

const loadInitialData = async () => {
  const [projectData] = await Promise.all([getProjectList({}), loadProviders()])
  projects.value = Array.isArray(projectData) ? projectData : []
}

const loadProviders = async () => {
  providers.value = await getProviders()
  const selectedExists = providers.value.some(item => item.id === selectedProviderId.value && item.isEnabled)
  if (!selectedExists) {
    const preferred = providers.value.find(item => item.isDefault && item.isEnabled) || enabledProviders.value[0]
    selectedProviderId.value = preferred?.id || ''
  }
}

const handleProjectChange = async () => {
  activeConversationId.value = ''
  messages.value = []
  toolExecutions.value = []
  conversations.value = selectedProjectId.value
    ? await getConversations(selectedProjectId.value)
    : []

  if (conversations.value.length > 0) {
    activeConversationId.value = conversations.value[0].id
    await loadConversation()
  }
}

const handleProviderChange = () => {
  if (!activeConversationId.value) return
  const conversation = conversations.value.find(item => item.id === activeConversationId.value)
  if (conversation && conversation.providerId !== selectedProviderId.value) {
    startNewConversation()
  }
}

const handleConversationChange = async () => {
  if (!activeConversationId.value) {
    messages.value = []
    toolExecutions.value = []
    return
  }

  const conversation = conversations.value.find(item => item.id === activeConversationId.value)
  if (conversation) {
    selectedProviderId.value = conversation.providerId
  }
  await loadConversation()
}

const loadConversation = async () => {
  if (!activeConversationId.value) return
  const [messageData, executionData] = await Promise.all([
    getMessages(activeConversationId.value),
    getToolExecutions(activeConversationId.value)
  ])
  messages.value = messageData || []
  toolExecutions.value = executionData || []
  await scrollToBottom()
}

const ensureConversation = async () => {
  if (activeConversationId.value) return activeConversationId.value
  const conversation = await createConversation({
    projectId: selectedProjectId.value,
    providerId: selectedProviderId.value
  })
  activeConversationId.value = conversation.id
  conversations.value.unshift(conversation)
  return conversation.id
}

const handleSend = async () => {
  const content = draft.value.trim()
  if (!content || !canSend.value) return

  const requestId = retryRequestId.value && retryContent.value === content
    ? retryRequestId.value
    : createRequestId()

  sending.value = true
  draft.value = ''
  try {
    const conversationId = await ensureConversation()
    const existingUserMessage = messages.value.some(message =>
      message.role === 'user' && message.requestId === requestId
    )
    if (!existingUserMessage) {
      messages.value.push({
        id: `local-${Date.now()}`,
        requestId,
        role: 'user',
        content,
        createTime: new Date().toISOString()
      })
    }
    await scrollToBottom()

    const result = await sendMessage({ conversationId, requestId, content })
    const assistantIndex = messages.value.findIndex(message =>
      message.role === 'assistant' && message.requestId === requestId
    )
    if (assistantIndex >= 0) {
      messages.value[assistantIndex] = result.message
    } else {
      messages.value.push(result.message)
    }
    if (Array.isArray(result.pendingApprovals)) {
      const otherExecutions = toolExecutions.value.filter(item => item.status !== 'PendingApproval')
      toolExecutions.value = [...otherExecutions, ...result.pendingApprovals]
    }
    conversations.value = await getConversations(selectedProjectId.value)
    retryRequestId.value = ''
    retryContent.value = ''
    await scrollToBottom()
  } catch (error) {
    try {
      await loadConversation()
      const completed = messages.value.some(message =>
        message.role === 'assistant' && message.requestId === requestId
      )
      if (completed) {
        retryRequestId.value = ''
        retryContent.value = ''
        return
      }
    } catch {
      // Keep the original request id so retrying the same text remains idempotent.
    }
    retryRequestId.value = requestId
    retryContent.value = content
    draft.value = content
  } finally {
    sending.value = false
  }
}

const createRequestId = () => {
  if (globalThis.crypto?.randomUUID) return globalThis.crypto.randomUUID()
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`
}

const approveApproval = async approval => {
  approvalLoadingId.value = approval.id
  try {
    const result = approval.status === 'PendingApproval'
      ? await approveTool(approval.id)
      : approval
    updateExecution(result)
    if (result.status === 'AwaitingClientExecution') {
      await executeClientActions(result)
      ElMessage.success('变更和生成任务已完成')
    } else {
      ElMessage.success('已执行')
    }
  } finally {
    approvalLoadingId.value = ''
  }
}

const executeClientActions = async execution => {
  const output = parseJson(execution.outputJson)
  const actions = Array.isArray(output?.clientActions) ? output.clientActions : []
  const completedActions = []
  let currentAction = null
  try {
    for (const action of actions) {
      currentAction = action
      let actionResult
      if (action.action === 'generateCode') {
        if (!action.entityId) throw new Error('全量生成任务缺少实体 ID')
        actionResult = await generateCode(action.entityId)
      } else if (action.action === 'generateIncrementalCode') {
        if (!action.entityId) throw new Error('增量生成任务缺少实体 ID')
        actionResult = await generateIncrementalCode(action.entityId)
      } else if (action.action === 'generateProjectCode') {
        if (!action.projectId) throw new Error('全量生成任务缺少项目 ID')
        actionResult = await generateProjectCode(action.projectId, action.entityIds || [])
      } else if (action.action === 'generateProjectIncrementalCode') {
        if (!action.projectId) throw new Error('增量生成任务缺少项目 ID')
        actionResult = await generateProjectIncrementalCode(action.projectId, action.entityIds || [])
      } else if (action.action === 'buildProject') {
        actionResult = await buildProject(action.projectId)
      } else if (action.action === 'enhanceUiPage') {
        if (!action.projectId) throw new Error('界面优化任务缺少项目 ID')
        actionResult = await enhanceUiPage(action)
      } else {
        throw new Error(`不支持的客户端任务: ${action.action}`)
      }
      ensureClientActionSucceeded(action, actionResult)
      completedActions.push(action.entityId ? `${action.action}:${action.entityId}` : action.action)
    }

    const completion = await completeToolClientActions(execution.id, {
      success: true,
      output: completedActions.join(', ')
    })
    updateExecution(completion.execution || completion)
    await loadConversation()
  } catch (error) {
    try {
      const completion = await completeToolClientActions(execution.id, {
        success: false,
        output: completedActions.join(', '),
        errorMessage: error?.message || '客户端生成任务执行失败',
        failedAction: error?.failedAction || currentAction?.action || '',
        diagnosticOutput: getClientActionDiagnostic(error)
      })
      updateExecution(completion.execution || completion)
      await loadConversation()
    } catch {
      // Keep the original generation error visible to the user.
    }
    throw error
  }
}

const ensureClientActionSucceeded = (action, result) => {
  if (!result || result.success !== false) return

  const error = new Error(result.message || `${action.action} 执行失败`)
  error.failedAction = action.action
  error.diagnosticOutput = result.output || result.message || ''
  throw error
}

const getClientActionDiagnostic = error => {
  const responsePayload = error?.response?.data?.data || error?.response?.data
  const diagnostic = error?.diagnosticOutput ||
    responsePayload?.output ||
    responsePayload?.message ||
    error?.stack ||
    error?.message ||
    '未返回详细错误信息'
  return summarizeBuildDiagnostic(diagnostic, error?.message)
}

const summarizeBuildDiagnostic = (diagnostic, failureMessage = '') => {
  const text = String(diagnostic || '')
  const lines = text.replace(/\r\n/g, '\n').split('\n')
  const importantPattern = /(:\s*error\b|\berror\s+(?:CS|NU|NETSDK|MSB|TS|VITE)\d+|build failed|生成失败|error during build|failed to compile|exited with code|退出码)/i
  const important = []
  const metadataHints = []
  const appendDistinct = value => {
    const normalized = String(value || '').trimEnd()
    if (normalized && !important.includes(normalized)) important.push(normalized)
  }

  appendDistinct(failureMessage)
  lines.forEach((line, index) => {
    const missingMember = line.match(/(?:'|“)(?<type>[^'”]+)(?:'|”)(?:\s+does not contain a definition for\s+|未包含)(?:'|“)(?<member>[^'”]+)(?:'|”)/i)
    if (missingMember?.groups) {
      const hint = `缺少成员：${missingMember.groups.type}.${missingMember.groups.member}。请核对实体字段元数据和手写代码引用，再增量生成对应实体或项目。`
      if (!metadataHints.includes(hint)) metadataHints.push(hint)
    }
    if (/(?:error\s+CS0535|does not implement interface member|未实现接口成员)/i.test(line)) {
      const hint = '生成实体缺少接口成员，请检查实体能力开关及对应系统字段，再重新生成。'
      if (!metadataHints.includes(hint)) metadataHints.push(hint)
    }
    if (!importantPattern.test(line)) return
    const start = Math.max(0, index - 1)
    const end = Math.min(lines.length - 1, index + 2)
    for (let current = start; current <= end; current += 1) {
      appendDistinct(lines[current])
    }
  })

  const tail = lines.slice(-80).filter(line => line.trim())
  const sections = []
  if (metadataHints.length) sections.push(`=== CodeMaster 元数据检查 ===\n${metadataHints.join('\n')}`)
  if (important.length) sections.push(`=== Build errors ===\n${important.join('\n')}`)
  if (tail.length) sections.push(`=== Build log tail ===\n${tail.join('\n')}`)
  const summary = sections.join('\n') || failureMessage || text || '未返回详细错误信息'

  if (summary.length <= 12000) return summary
  const separator = '\n... diagnostic truncated ...\n'
  const headLength = 8000 - separator.length
  const tailLength = 12000 - headLength - separator.length
  return summary.slice(0, headLength) + separator + summary.slice(-tailLength)
}

const rejectApproval = async approval => {
  approvalLoadingId.value = approval.id
  try {
    const result = await rejectTool(approval.id)
    updateExecution(result)
    ElMessage.success('已拒绝')
  } finally {
    approvalLoadingId.value = ''
  }
}

const updateExecution = value => {
  const index = toolExecutions.value.findIndex(item => item.id === value.id)
  if (index >= 0) {
    toolExecutions.value.splice(index, 1, value)
  } else {
    toolExecutions.value.push(value)
  }
}

const startNewConversation = () => {
  activeConversationId.value = ''
  messages.value = []
  toolExecutions.value = []
}

const archiveCurrentConversation = async () => {
  if (!activeConversationId.value) return

  await ElMessageBox.confirm('确认删除当前对话及其待确认操作吗？', '删除对话', {
    type: 'warning'
  })
  await deleteConversation(activeConversationId.value)
  activeConversationId.value = ''
  messages.value = []
  toolExecutions.value = []
  conversations.value = await getConversations(selectedProjectId.value)
  if (conversations.value.length > 0) {
    activeConversationId.value = conversations.value[0].id
    await loadConversation()
  }
  ElMessage.success('对话已删除')
}

const scrollToBottom = async () => {
  await nextTick()
  if (messagePanelRef.value) {
    messagePanelRef.value.scrollTop = messagePanelRef.value.scrollHeight
  }
}

const toolTitle = name => ({
  project_change_set: '项目变更单',
  create_module: '创建模块',
  create_entity: '创建实体'
}[name] || name)

const formatToolInput = value => {
  if (!value) return ''
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

const changeSetOverview = value => {
  try {
    const data = parseJson(value) || {}
    const entities = Array.isArray(data.entities) ? data.entities : []
    const entityUpdates = Array.isArray(data.entityUpdates) ? data.entityUpdates : []
    return {
      summary: data.summary || '',
      modules: Array.isArray(data.modules) ? data.modules.length : 0,
      entities: entities.length,
      fields: entities.reduce((total, entity) => total + (Array.isArray(entity.fields) ? entity.fields.length : 0), 0)
        + entityUpdates.reduce((total, entity) => total
          + (Array.isArray(entity.newFields) ? entity.newFields.length : 0)
          + (Array.isArray(entity.updatedFields) ? entity.updatedFields.length : 0)
          + (Array.isArray(entity.deletedFieldIds) ? entity.deletedFieldIds.length : 0), 0),
      relations: entities.reduce((total, entity) => total + (Array.isArray(entity.relations) ? entity.relations.length : 0), 0)
        + entityUpdates.reduce((total, entity) => total
          + (Array.isArray(entity.newRelations) ? entity.newRelations.length : 0)
          + (Array.isArray(entity.updatedRelations) ? entity.updatedRelations.length : 0)
          + (Array.isArray(entity.deletedRelationIds) ? entity.deletedRelationIds.length : 0), 0),
      updates: (Array.isArray(data.moduleUpdates) ? data.moduleUpdates.length : 0) + entityUpdates.length,
      deletes: (Array.isArray(data.deleteModuleIds) ? data.deleteModuleIds.length : 0)
        + (Array.isArray(data.deleteEntityIds) ? data.deleteEntityIds.length : 0),
      generationMode: data.generationMode || 'None'
    }
  } catch {
    return {
      summary: '',
      modules: 0,
      entities: 0,
      fields: 0,
      relations: 0,
      updates: 0,
      deletes: 0,
      generationMode: 'None'
    }
  }
}

const parseJson = value => {
  if (!value) return null
  try {
    return typeof value === 'string' ? JSON.parse(value) : value
  } catch {
    return null
  }
}

const formatTime = value => {
  if (!value) return ''
  return new Date(value).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}
</script>

<style scoped lang="scss">
.drawer-header {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.drawer-title {
  color: var(--app-text, #111827);
  font-size: 17px;
  font-weight: 700;
}

.drawer-subtitle {
  margin-top: 2px;
  color: var(--app-text-muted, #64748b);
  font-size: 12px;
}

.drawer-actions {
  display: flex;
  align-items: center;
}

.agent-layout {
  height: 100%;
  display: grid;
  grid-template-rows: auto minmax(0, 1fr) auto;
  background: var(--app-bg, #f4f7fb);
}

.context-panel {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 10px;
  padding: 14px 16px;
  background: var(--app-header-bg, #fff);
  border-bottom: 1px solid var(--app-border, #dbe4ef);
}

.context-field {
  min-width: 0;
}

.context-label {
  display: block;
  margin-bottom: 6px;
  color: var(--app-text-muted, #64748b);
  font-size: 12px;
}

.conversation-field {
  grid-column: 1 / -1;
  display: flex;
  align-items: center;
  gap: 6px;
}

.conversation-select {
  min-width: 0;
  flex: 1;
}

.conversation-delete {
  flex: 0 0 auto;
}

.project-option,
.provider-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.project-option small,
.provider-option small {
  color: var(--app-text-muted, #94a3b8);
}

.message-panel {
  min-height: 0;
  overflow-y: auto;
  padding: 18px 16px;
}

.empty-state {
  height: 100%;
  min-height: 240px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  color: var(--app-text-muted, #64748b);
}

.empty-state.compact {
  min-height: 180px;
}

.empty-state .el-icon {
  font-size: 34px;
  color: var(--app-primary, #2563eb);
}

.message-row {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  margin-bottom: 18px;
}

.message-row.user {
  align-items: flex-end;
}

.message-role {
  margin: 0 4px 5px;
  color: var(--app-text-muted, #64748b);
  font-size: 12px;
}

.message-bubble {
  max-width: 88%;
  padding: 11px 13px;
  border: 1px solid var(--app-border, #dbe4ef);
  border-radius: 8px;
  background: var(--app-header-bg, #fff);
  color: var(--app-text, #1f2937);
  line-height: 1.65;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.message-row.user .message-bubble {
  border-color: var(--app-primary, #2563eb);
  background: var(--app-primary, #2563eb);
  color: #fff;
}

.message-row time {
  margin: 4px 4px 0;
  color: var(--app-text-muted, #94a3b8);
  font-size: 11px;
}

.approval-card {
  margin: 0 0 18px;
  padding: 14px;
  border: 1px solid rgba(245, 158, 11, 0.48);
  border-radius: 8px;
  background: color-mix(in srgb, var(--app-header-bg, #fff) 92%, #f59e0b 8%);
}

.approval-header,
.approval-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.approval-header strong {
  display: block;
  color: var(--app-text, #111827);
}

.approval-header span {
  display: block;
  margin-top: 3px;
  color: var(--app-text-muted, #64748b);
  font-size: 12px;
}

.approval-card pre {
  max-height: 220px;
  margin: 12px 0;
  padding: 10px;
  overflow: auto;
  border: 1px solid var(--app-border, #dbe4ef);
  background: var(--app-bg, #f4f7fb);
  color: var(--app-text, #1f2937);
  font-family: Consolas, monospace;
  font-size: 12px;
  line-height: 1.5;
  white-space: pre-wrap;
}

.change-set-overview {
  margin-top: 12px;
  padding: 10px;
  border: 1px solid var(--app-border, #dbe4ef);
  background: var(--app-bg, #f4f7fb);
}

.change-set-overview > strong {
  display: block;
  color: var(--app-text, #111827);
  line-height: 1.5;
}

.change-set-counts {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 8px;
}

.approval-details {
  margin: 10px 0 12px;
}

.approval-details summary {
  color: var(--app-primary, #2563eb);
  cursor: pointer;
  font-size: 12px;
}

.approval-details pre {
  margin-bottom: 0;
}

.approval-actions {
  justify-content: flex-end;
  gap: 8px;
}

.typing-row {
  display: flex;
  gap: 5px;
  padding: 10px 4px 18px;
}

.typing-row span {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: var(--app-primary, #2563eb);
  animation: typing 1s infinite ease-in-out;
}

.typing-row span:nth-child(2) { animation-delay: 0.15s; }
.typing-row span:nth-child(3) { animation-delay: 0.3s; }

.composer {
  padding: 12px 16px 14px;
  background: var(--app-header-bg, #fff);
  border-top: 1px solid var(--app-border, #dbe4ef);
}

.composer-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 8px;
}

.composer-footer span {
  color: var(--app-text-muted, #94a3b8);
  font-size: 11px;
}

@keyframes typing {
  0%, 60%, 100% { opacity: 0.35; transform: translateY(0); }
  30% { opacity: 1; transform: translateY(-3px); }
}

@media (max-width: 640px) {
  .context-panel {
    grid-template-columns: minmax(0, 1fr);
  }

  .conversation-field {
    grid-column: auto;
  }
}
</style>

<style lang="scss">
.codemaster-agent-drawer .el-drawer__header {
  margin-bottom: 0;
  padding: 14px 16px;
  border-bottom: 1px solid var(--app-border, #dbe4ef);
}

.codemaster-agent-drawer .el-drawer__body {
  padding: 0;
  overflow: hidden;
}

@media (max-width: 640px) {
  .codemaster-agent-drawer.el-drawer {
    width: 100% !important;
  }
}
</style>
