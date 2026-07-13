import axios from 'axios'
import { getToken } from '@/utils/auth'
import { isTauriRuntime } from '@/utils/tauriBridge'

const CLIENT_CONFIG_KEY = 'codeMasterClientConfig'
export const DEFAULT_CODEMASTER_SERVER_BASE_URL = import.meta.env.VITE_CODEMASTER_SERVER_BASE_URL || ''

export function normalizeCodeMasterServerBaseUrl(value) {
  let raw = String(value || '').trim()
  if (!raw) {
    return ''
  }

  const markdownUrl = raw.match(/\]\((https?:\/\/[^)]+)\)/i)
  if (markdownUrl?.[1]) {
    raw = markdownUrl[1]
  }

  const candidate = /^https?:\/\//i.test(raw) ? raw : `http://${raw}`
  const url = new URL(candidate)
  if (!['http:', 'https:'].includes(url.protocol)) {
    throw new Error('服务器地址只支持 http 或 https')
  }

  url.search = ''
  url.hash = ''
  url.pathname = url.pathname.replace(/\/api(?:\/.*)?$/i, '').replace(/\/+$/, '')

  return url.toString().replace(/\/$/, '')
}

export function getCodeMasterClientConfig() {
  const raw = localStorage.getItem(CLIENT_CONFIG_KEY)
  if (!raw) {
    return {}
  }

  try {
    return JSON.parse(raw) || {}
  } catch (error) {
    console.error('Failed to parse CodeMaster client config:', error)
    return {}
  }
}

export function saveCodeMasterClientConfig(config) {
  localStorage.setItem(CLIENT_CONFIG_KEY, JSON.stringify(config || {}))
}

function getClientConfig() {
  return {
    ...getCodeMasterClientConfig(),
    ...(window.__CODEMASTER_CLIENT__ || {})
  }
}

function getWebViewBridge() {
  return window.chrome?.webview?.hostObjects?.jsbridge
}

function readServerAccessToken() {
  const userStore = localStorage.getItem('user')
  if (userStore) {
    try {
      const user = JSON.parse(userStore)
      if (user?.token) {
        return user.token
      }
    } catch (error) {
      console.error('Failed to parse user store:', error)
    }
  }

  return getToken()
}

function withLocalOverrides(payload = {}) {
  const config = getClientConfig()
  const projectId = payload.projectId || payload.id
  const projectOverrides = config.projectOverrides?.[projectId] || config.projects?.[projectId] || {}

  const overrides = {}
  const targetPath = projectOverrides.targetPath || projectOverrides.projectPath || config.targetPath || config.projectPath
  const databaseType = projectOverrides.databaseType || config.databaseType
  const connectionString = projectOverrides.connectionString || config.connectionString

  if (targetPath) {
    overrides.targetPath = targetPath
  }
  if (databaseType) {
    overrides.databaseType = databaseType
  }
  if (connectionString) {
    overrides.connectionString = connectionString
  }

  return {
    ...overrides,
    ...payload
  }
}

export function isCodeMasterClient() {
  const config = getClientConfig()
  return Boolean(config.agentUrl || window.CodeMasterBridge?.invoke || isTauriRuntime() || getWebViewBridge()?.ExecuteCodegenAction)
}

export function shouldUseLocalCodegenExecution() {
  const config = getClientConfig()
  return isCodeMasterClient() && config.localExecution !== false
}

function unwrapResponse(data) {
  if (typeof data === 'string') {
    try {
      data = JSON.parse(data)
    } catch {
      return data
    }
  }

  if (data && typeof data === 'object' && !Array.isArray(data) && typeof data.code === 'number') {
    if (data.code === 200) {
      return data.data
    }

    throw new Error(getFailureMessage(data))
  }

  if (data && typeof data === 'object' && !Array.isArray(data) && typeof data.success === 'boolean') {
    if (data.success) {
      return data.dataOnly ? data.data : data
    }

    throw new Error(getFailureMessage(data))
  }

  return data
}

function getFailureMessage(data) {
  if (!data || typeof data !== 'object') {
    return 'Local code generation failed'
  }

  return data.message || data.error || firstOutputLine(data.output) || 'Local code generation failed'
}

function firstOutputLine(output) {
  if (typeof output !== 'string') {
    return ''
  }

  return output
    .split(/\r?\n/)
    .map(line => line.trim())
    .find(Boolean) || ''
}

async function invokeLocalCodegen(action, payload = {}) {
  const config = getClientConfig()
  const serverBaseUrl = normalizeCodeMasterServerBaseUrl(
    config.serverBaseUrl || DEFAULT_CODEMASTER_SERVER_BASE_URL || window.location.origin
  )
  const requestPayload = {
    action,
    payload: withLocalOverrides(payload),
    serverBaseUrl,
    accessToken: readServerAccessToken()
  }

  if (window.CodeMasterBridge?.invoke) {
    const result = await window.CodeMasterBridge.invoke('codegenExecution', requestPayload)
    return unwrapResponse(result)
  }

  if (isTauriRuntime()) {
    const { invoke } = await import('@tauri-apps/api/core')
    const result = await invoke('codegen_execution', { request: requestPayload })
    return unwrapResponse(result)
  }

  const webViewBridge = getWebViewBridge()
  if (webViewBridge?.ExecuteCodegenAction) {
    const result = await webViewBridge.ExecuteCodegenAction(JSON.stringify(requestPayload))
    return unwrapResponse(result)
  }

  if (!config.agentUrl) {
    throw new Error('Local code generation agent is not configured')
  }

  const agentUrl = config.agentUrl.replace(/\/$/, '')
  const response = await axios.post(
    `${agentUrl}/api/codegen/execution/${action}`,
    requestPayload,
    {
      timeout: config.timeout || 300000,
      headers: {
        'X-CodeMaster-Client-Token': config.token || ''
      }
    }
  )

  return unwrapResponse(response.data)
}

export function executeCodegenAction(action, payload, serverExecutor) {
  if (shouldUseLocalCodegenExecution()) {
    return invokeLocalCodegen(action, payload)
  }

  return serverExecutor()
}
