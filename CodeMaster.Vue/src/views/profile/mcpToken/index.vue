<template>
  <div class="app-container mcp-token-page">
    <div class="page-head">
      <div>
        <h2>MCP Token</h2>
        <p>用于 Codex / MCP 调用 CodeMaster API。Token 只显示一次，请妥善保存。</p>
      </div>
      <el-button type="primary" :icon="Plus" @click="openCreateDialog">生成 Token</el-button>
    </div>

    <el-alert
      class="token-alert"
      type="info"
      show-icon
      :closable="false"
      title="生成项目目录只保存 projectId，不保存 token。MCP 首次使用时粘贴这里生成的 token，会保存到本机用户目录。"
    />

    <el-card shadow="never">
      <el-table v-loading="loading" :data="tokens" style="width: 100%">
        <el-table-column prop="name" label="名称" min-width="180" />
        <el-table-column prop="tokenPrefix" label="前缀" width="150" />
        <el-table-column prop="scopes" label="权限范围" min-width="220" />
        <el-table-column prop="createTime" label="创建时间" width="180">
          <template #default="{ row }">{{ formatTime(row.createTime) }}</template>
        </el-table-column>
        <el-table-column prop="lastUsedAt" label="最后使用" width="180">
          <template #default="{ row }">{{ formatTime(row.lastUsedAt) || '-' }}</template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'">
              {{ row.isActive ? '可用' : '已失效' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="110" fixed="right">
          <template #default="{ row }">
            <el-button link type="danger" :icon="Delete" :disabled="!row.isActive" @click="handleRevoke(row)">
              撤销
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="createDialogVisible" title="生成 MCP Token" width="520px">
      <el-form :model="form" label-width="90px">
        <el-form-item label="名称">
          <el-input v-model="form.name" maxlength="100" placeholder="例如：我的 Codex" />
        </el-form-item>
        <el-form-item label="有效期">
          <el-date-picker
            v-model="form.expiresAt"
            type="datetime"
            value-format="YYYY-MM-DDTHH:mm:ss"
            placeholder="不选择表示长期有效"
            style="width: 100%"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="creating" @click="handleCreate">生成</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="tokenDialogVisible" title="请立即复制 Token" width="680px" :close-on-click-modal="false">
      <el-alert
        type="warning"
        show-icon
        :closable="false"
        title="这个 token 只显示一次。关闭窗口后无法再次查看，只能重新生成。"
      />
      <el-input
        class="token-value"
        :model-value="createdToken"
        type="textarea"
        :rows="4"
        readonly
      />
      <template #footer>
        <el-button :icon="CopyDocument" @click="copyToken">复制</el-button>
        <el-button type="primary" @click="tokenDialogVisible = false">完成</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CopyDocument, Delete, Plus } from '@element-plus/icons-vue'
import { createMcpToken, listMcpTokens, revokeMcpToken } from '@/api/mcpToken'

const loading = ref(false)
const creating = ref(false)
const tokens = ref([])
const createDialogVisible = ref(false)
const tokenDialogVisible = ref(false)
const createdToken = ref('')

const form = reactive({
  name: 'CodeMaster MCP',
  expiresAt: ''
})

function formatTime(value) {
  if (!value) return ''
  return new Date(value).toLocaleString()
}

async function loadTokens() {
  loading.value = true
  try {
    tokens.value = await listMcpTokens()
  } finally {
    loading.value = false
  }
}

function openCreateDialog() {
  form.name = 'CodeMaster MCP'
  form.expiresAt = ''
  createDialogVisible.value = true
}

async function handleCreate() {
  creating.value = true
  try {
    const result = await createMcpToken({
      name: form.name,
      expiresAt: form.expiresAt ? new Date(form.expiresAt).toISOString() : null
    })
    createdToken.value = result.token
    createDialogVisible.value = false
    tokenDialogVisible.value = true
    await loadTokens()
  } finally {
    creating.value = false
  }
}

async function handleRevoke(row) {
  await ElMessageBox.confirm(`确认撤销 ${row.name}？撤销后使用该 token 的 MCP 将无法继续调用 CodeMaster。`, '提示', {
    type: 'warning',
    confirmButtonText: '撤销',
    cancelButtonText: '取消'
  })
  await revokeMcpToken(row.id)
  ElMessage.success('已撤销')
  await loadTokens()
}

async function copyToken() {
  await navigator.clipboard.writeText(createdToken.value)
  ElMessage.success('已复制')
}

onMounted(loadTokens)
</script>

<style scoped lang="scss">
.mcp-token-page {
  .page-head {
    display: flex;
    justify-content: space-between;
    gap: 16px;
    align-items: flex-start;
    margin-bottom: 14px;

    h2 {
      margin: 0 0 6px;
      font-size: 22px;
      color: var(--app-text, #111827);
    }

    p {
      margin: 0;
      color: var(--app-text-muted, #64748b);
      line-height: 1.6;
    }
  }

  .token-alert {
    margin-bottom: 14px;
  }

  .token-value {
    margin-top: 16px;
  }
}
</style>
