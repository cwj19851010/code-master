import * as signalR from '@microsoft/signalr'
import { ElMessage } from 'element-plus'
import i18n from '@/i18n'
import {
  DEFAULT_CODEMASTER_SERVER_BASE_URL,
  getCodeMasterClientConfig,
  isCodeMasterClient,
  normalizeCodeMasterServerBaseUrl
} from '@/utils/codegenExecution'

const t = (key, params) => i18n.global.t(key, params)

function getAccessToken() {
  const userStore = localStorage.getItem('user')
  if (userStore) {
    try {
      const user = JSON.parse(userStore)
      if (user.token) {
        return user.token
      }
    } catch (error) {
      console.error('Failed to parse user store:', error)
    }
  }

  return localStorage.getItem('token') || ''
}

function getSignalRBaseUrl() {
  if (!isCodeMasterClient()) {
    return window.location.origin
  }

  const config = {
    ...getCodeMasterClientConfig(),
    ...(window.__CODEMASTER_CLIENT__ || {})
  }

  if (config.serverBaseUrl) {
    return normalizeCodeMasterServerBaseUrl(config.serverBaseUrl)
  }

  return normalizeCodeMasterServerBaseUrl(DEFAULT_CODEMASTER_SERVER_BASE_URL)
}

function getHubUrl(path) {
  return `${getSignalRBaseUrl()}${path}`
}

class SignalRService {
  constructor() {
    this.connection = null
    this.projectInitConnection = null
  }

  async connect() {
    const token = getAccessToken()
    if (!token) {
      console.warn(t('signalr_no_token'))
      return
    }

    if (this.connection && this.connection.state !== signalR.HubConnectionState.Disconnected) {
      return
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(getHubUrl('/hubs/notification'), {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build()

    this.connection.on('ForceOffline', (data) => {
      ElMessage.warning(data?.message || data?.Message || t('force_offline'))
      localStorage.removeItem('token')
      localStorage.removeItem('userInfo')
      localStorage.removeItem('user')
      localStorage.removeItem('permission')
      window.location.href = '/login'
    })

    this.connection.on('UserOnline', (data) => {
      console.log('User online:', data)
    })

    this.connection.on('UserOffline', (data) => {
      console.log('User offline:', data)
    })

    try {
      await this.connection.start()
      console.log('SignalR connected')
    } catch (error) {
      console.error('SignalR connection failed:', error)
    }
  }

  async connectProjectInit(onProgress) {
    const token = getAccessToken()
    if (!token) {
      console.warn(t('signalr_no_token'))
      return
    }

    if (this.projectInitConnection && this.projectInitConnection.state !== signalR.HubConnectionState.Disconnected) {
      return
    }

    this.projectInitConnection = new signalR.HubConnectionBuilder()
      .withUrl(getHubUrl('/hubs/project-initialization'), {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build()

    this.projectInitConnection.on('ReceiveProgress', (projectId, step, message, progress) => {
      if (onProgress) {
        onProgress(projectId, step, message, progress)
      }
    })

    try {
      await this.projectInitConnection.start()
      console.log('Project initialization SignalR connected')
    } catch (error) {
      console.error('Project initialization SignalR connection failed:', error)
    }
  }

  async disconnectProjectInit() {
    if (this.projectInitConnection) {
      await this.projectInitConnection.stop()
      this.projectInitConnection = null
    }
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop()
      this.connection = null
    }
    await this.disconnectProjectInit()
  }
}

export const signalRService = new SignalRService()
