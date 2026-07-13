import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'
import i18n from '@/i18n'

const t = (key, params) => i18n.global.t(key, params)

function normalizeDateValues(value) {
  if (value instanceof Date) {
    return value.toISOString()
  }

  if (Array.isArray(value)) {
    return value.map(item => normalizeDateValues(item))
  }

  if (value && typeof value === 'object') {
    Object.keys(value).forEach(key => {
      value[key] = normalizeDateValues(value[key])
    })
  }

  return value
}

const service = axios.create({
  baseURL: '/api',
  timeout: 10000
})

// 请求拦截器
service.interceptors.request.use(
  config => {
    // 从 Pinia 持久化存储中读取 token
    normalizeDateValues(config.params)
    normalizeDateValues(config.data)

    const userStore = localStorage.getItem('user')
    if (userStore) {
      try {
        const user = JSON.parse(userStore)
        if (user.token) {
          config.headers['Authorization'] = `Bearer ${user.token}`
        }
      } catch (e) {
        console.error('解析 user store 失败:', e)
      }
    }

    // 从 settings store 中读取语言设置
    const settingsStore = localStorage.getItem('settings')
    if (settingsStore) {
      try {
        const settings = JSON.parse(settingsStore)
        if (settings.language) {
          config.headers['Accept-Language'] = settings.language
        }
      } catch (e) {
        console.error('解析 settings store 失败:', e)
      }
    }
    return config
  },
  error => {
    console.error('请求错误:', error)
    return Promise.reject(error)
  }
)

// 响应拦截器
service.interceptors.response.use(
  response => {
    const res = response.data

    // 如果返回的是标准 ApiResponse 格式 { code, message, data }
    // 注意：code 必须是数字类型，避免与业务数据中的 code 字段混淆
    if (res && typeof res === 'object' && !Array.isArray(res) && typeof res.code === 'number') {
      // 检查业务状态码
      if (res.code === 200) {
        // 返回 data 字段（自动解包）
        return res.data
      } else {
        // 业务错误
        ElMessage.error(res.message || t('request_failed'))
        return Promise.reject(new Error(res.message || t('request_failed')))
      }
    }

    // 如果不是标准格式，直接返回（数组、字典、分页对象等）
    return res
  },
  error => {
    console.error('响应错误:', error)

    // 处理 401 未授权
    if (error.response && error.response.status === 401) {
      ElMessage.error(t('login_expired'))
      // 清除 Pinia 持久化的数据
      localStorage.removeItem('user')
      localStorage.removeItem('permission')
      router.push('/login')
    } else {
      // 只在有错误消息时才显示，避免显示 undefined
      const errorMsg = error.response?.data?.message || error.message
      if (errorMsg) {
        ElMessage.error(errorMsg)
      }
    }

    return Promise.reject(error)
  }
)

export default service
