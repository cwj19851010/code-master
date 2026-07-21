<template>
  <div class="login-container">
    <theme-picker class="login-theme" />
    <div class="ambient-grid" />
    <div class="login-shell">
      <section class="brand-panel">
        <div class="brand-topline">
          <span>CODEMASTER CORE</span>
          <strong>{{ isClientLogin ? 'LOCAL AGENT LINKED' : 'SERVER CONSOLE' }}</strong>
        </div>

        <div class="brand-main">
          <div class="brand-mark">
            <el-icon :size="38"><Platform /></el-icon>
          </div>
          <div>
            <h1>{{ t('app_title') }}</h1>
            <p>代码生成、模板设计与本地/远程交付工作台</p>
          </div>
        </div>

        <div class="command-stack">
          <div>
            <span>01</span>
            <strong>Schema Intelligence</strong>
            <em>实体建模与字段编排</em>
          </div>
          <div>
            <span>02</span>
            <strong>Visual Template Engine</strong>
            <em>页面树、脚本段、模板联动</em>
          </div>
          <div>
            <span>03</span>
            <strong>Sidecar Execution</strong>
            <em>客户端安全调用本地能力</em>
          </div>
        </div>

        <div class="brand-grid">
          <div>
            <span>Templates</span>
            <strong>DB Driven</strong>
          </div>
          <div>
            <span>Designer</span>
            <strong>Vue Tree</strong>
          </div>
          <div>
            <span>Client</span>
            <strong>{{ isClientLogin ? 'Enabled' : 'Server' }}</strong>
          </div>
        </div>
      </section>

    <!-- 登录表单卡片 -->
    <section class="login-card">
      <div class="access-line">
        <span>SECURE ACCESS</span>
        <i />
      </div>
      <div class="card-header">
        <div>
          <h2>{{ t('login_title') }}</h2>
          <p>{{ isClientLogin ? '客户端授权登录' : '服务器控制台登录' }}</p>
        </div>
        <span class="mode-badge">{{ isClientLogin ? 'Client' : 'Web' }}</span>
      </div>

      <el-form ref="loginFormRef" :model="loginForm" :rules="loginRules" class="login-form">
        <el-form-item prop="username">
          <el-input
            ref="username"
            v-model="loginForm.username"
            :placeholder="$t2('please_input', 'username')"
            size="large"
            :prefix-icon="User"
            clearable
          />
        </el-form-item>

        <el-form-item prop="password">
          <el-input
            ref="password"
            v-model="loginForm.password"
            :type="passwordType"
            :placeholder="$t2('please_input', 'password')"
            size="large"
            :prefix-icon="Lock"
            @keyup.enter="handleLogin"
          >
            <template #suffix>
              <el-icon class="password-icon" @click="showPwd">
                <View v-if="passwordType === 'password'" />
                <Hide v-else />
              </el-icon>
            </template>
          </el-input>
        </el-form-item>

        <el-button
          :loading="loading"
          type="primary"
          size="large"
          class="login-button"
          @click.prevent="handleLogin"
        >
          <span v-if="!loading">{{ t('login') }}</span>
          <span v-else>{{ t('login_loading') }}</span>
        </el-button>
      </el-form>

      <div class="auth-links">
        <el-button text @click="openRegister">注册账号</el-button>
        <el-button text @click="openGithubLogin">GitHub 登录</el-button>
      </div>

      <div class="card-footer">
        <p>{{ t('copyright') }}</p>
      </div>
    </section>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { User, Lock, View, Hide, Platform } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import { getToken } from '@/utils/auth'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import ThemePicker from '@/layout/components/ThemePicker.vue'
import {
  DEFAULT_CODEMASTER_SERVER_BASE_URL,
  getCodeMasterClientConfig,
  isCodeMasterClient,
  normalizeCodeMasterServerBaseUrl,
  saveCodeMasterClientConfig
} from '@/utils/codegenExecution'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const loginFormRef = ref(null)
const { t } = useI18n()
const clientConfig = getCodeMasterClientConfig()
const isClientLogin = isCodeMasterClient()
const defaultServerBaseUrl = normalizeCodeMasterServerBaseUrl(
  isClientLogin
    ? clientConfig.serverBaseUrl ||
      window.__CODEMASTER_CLIENT__?.serverBaseUrl ||
      DEFAULT_CODEMASTER_SERVER_BASE_URL ||
      (/^https?:$/i.test(window.location.protocol) ? window.location.origin : '')
    : window.location.origin
)

const loginForm = reactive({
  username: 'admin',
  password: 'admin123'
})

const loginRules = {
  username: [{ required: true, trigger: 'blur', message: t2('please_input', 'username') }],
  password: [{ required: true, trigger: 'blur', message: t2('please_input', 'password') }]
}

const loading = ref(false)
const passwordType = ref('password')

const showPwd = () => {
  passwordType.value = passwordType.value === 'password' ? 'text' : 'password'
}

function getPublicUrl(path) {
  return new URL(path, defaultServerBaseUrl || window.location.origin).toString()
}

function openRegister() {
  window.location.href = getPublicUrl('/register')
}

function openGithubLogin() {
  window.location.href = getPublicUrl('/api/account/github/login')
}

async function completeExternalLogin() {
  if (!route.query.github && !route.query.registered) {
    return
  }

  const token = getToken() || localStorage.getItem('access_token') || localStorage.getItem('token')
  if (!token) {
    return
  }

  try {
    userStore.token = token
    await userStore.getUserInfo()
    ElMessage.success(route.query.github ? 'GitHub 登录成功' : '注册成功')
    router.replace('/')
  } catch (error) {
    ElMessage.error(error.message || t('login_failed'))
  }
}

const handleLogin = () => {
  loginFormRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true
      try {
        if (isClientLogin && defaultServerBaseUrl) {
          saveCodeMasterClientConfig({
            ...getCodeMasterClientConfig(),
            serverBaseUrl: defaultServerBaseUrl
          })
        }

        await userStore.login({
          userName: loginForm.username,
          password: loginForm.password
        })

        ElMessage.success(t('login_success'))
        router.push('/')
      } catch (error) {
        ElMessage.error(error.message || t('login_failed'))
      } finally {
        loading.value = false
      }
    }
  })
}

onMounted(() => {
  if (isClientLogin && defaultServerBaseUrl) {
    saveCodeMasterClientConfig({
      ...getCodeMasterClientConfig(),
      serverBaseUrl: defaultServerBaseUrl
    })
  }
  completeExternalLogin()
})
</script>

<style lang="scss" scoped>
.login-container {
  position: relative;
  min-height: 100vh;
  width: 100%;
  background:
    linear-gradient(118deg, rgba(20, 184, 166, 0.22) 0%, transparent 34%),
    linear-gradient(298deg, rgba(234, 179, 8, 0.16) 0%, transparent 32%),
    linear-gradient(135deg, #090d13 0%, #111827 42%, #16251f 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 32px;
  overflow: hidden;
}

.ambient-grid {
  position: absolute;
  inset: 0;
  background:
    linear-gradient(90deg, rgba(148, 163, 184, 0.08) 1px, transparent 1px),
    linear-gradient(0deg, rgba(148, 163, 184, 0.08) 1px, transparent 1px);
  background-size: 44px 44px;
  mask-image: linear-gradient(180deg, rgba(0, 0, 0, 0.95), transparent 88%);
  pointer-events: none;
}

.login-theme {
  position: fixed;
  top: 18px;
  right: 18px;
  z-index: 2;
  display: flex;
  align-items: center;
  padding: 4px;
  border: 1px solid rgba(148, 163, 184, 0.22);
  border-radius: 8px;
  background: rgba(15, 23, 42, 0.72);
  box-shadow: 0 18px 40px rgba(0, 0, 0, 0.32);
  backdrop-filter: blur(14px);
}

.login-shell {
  position: relative;
  display: grid;
  grid-template-columns: minmax(420px, 1fr) 430px;
  width: min(1080px, 100%);
  min-height: 620px;
  border: 1px solid rgba(148, 163, 184, 0.22);
  border-radius: 8px;
  overflow: hidden;
  background: rgba(15, 23, 42, 0.78);
  box-shadow: 0 34px 90px rgba(0, 0, 0, 0.46);
  backdrop-filter: blur(22px);

  @media (max-width: 900px) {
    grid-template-columns: 1fr;
    min-height: auto;
  }
}

.brand-panel {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  gap: 26px;
  padding: 42px;
  color: #fff;
  background:
    linear-gradient(135deg, rgba(8, 13, 22, 0.98), rgba(19, 35, 30, 0.92)),
    linear-gradient(135deg, rgba(20, 184, 166, 0.42), rgba(245, 158, 11, 0.2));
  border-right: 1px solid rgba(148, 163, 184, 0.18);

  &::before {
    content: "";
    position: absolute;
    inset: 0;
    background:
      linear-gradient(120deg, transparent 0 35%, rgba(20, 184, 166, 0.13) 35% 36%, transparent 36% 100%),
      linear-gradient(160deg, transparent 0 58%, rgba(234, 179, 8, 0.12) 58% 59%, transparent 59% 100%);
    pointer-events: none;
  }

  > * {
    position: relative;
    z-index: 1;
  }

  .brand-topline {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    color: rgba(226, 232, 240, 0.72);
    font-size: 12px;
    font-weight: 700;
    text-transform: uppercase;

    strong {
      color: #14b8a6;
      font-size: 12px;
    }
  }

  .brand-main {
    display: flex;
    align-items: flex-start;
    gap: 18px;
  }

  .brand-mark {
    display: inline-flex;
    width: 66px;
    height: 66px;
    flex: 0 0 auto;
    align-items: center;
    justify-content: center;
    border-radius: 8px;
    color: #0f172a;
    background: linear-gradient(135deg, #2dd4bf, #facc15);
    box-shadow: 0 18px 34px rgba(20, 184, 166, 0.32);
  }

  h1 {
    margin: 0;
    font-size: 46px;
    font-weight: 800;
    line-height: 1;
  }

  p {
    max-width: 420px;
    margin: 14px 0 0;
    color: rgba(226, 232, 240, 0.78);
    line-height: 1.7;
  }

  @media (max-width: 900px) {
    min-height: 260px;
    justify-content: center;
  }
}

.command-stack {
  display: grid;
  gap: 12px;

  div {
    display: grid;
    grid-template-columns: 44px 1fr;
    gap: 4px 14px;
    padding: 14px 16px;
    border: 1px solid rgba(148, 163, 184, 0.16);
    border-radius: 8px;
    background: rgba(15, 23, 42, 0.46);
  }

  span {
    grid-row: span 2;
    color: #facc15;
    font-size: 13px;
    font-weight: 800;
  }

  strong {
    color: #f8fafc;
    font-size: 15px;
  }

  em {
    color: rgba(203, 213, 225, 0.68);
    font-size: 12px;
    font-style: normal;
  }
}

.brand-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;

  div {
    padding: 12px;
    border-radius: 8px;
    background: rgba(2, 6, 23, 0.48);
    border: 1px solid rgba(148, 163, 184, 0.18);
  }

  span,
  strong {
    display: block;
  }

  span {
    color: rgba(203, 213, 225, 0.64);
    font-size: 12px;
  }

  strong {
    margin-top: 4px;
    font-size: 14px;
  }
}

.login-card {
  padding: 46px 40px 30px;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.96), rgba(248, 250, 252, 0.94)),
    var(--app-surface, #fff);
}

.access-line {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 26px;

  span {
    color: #0f766e;
    font-size: 12px;
    font-weight: 800;
  }

  i {
    height: 1px;
    flex: 1;
    background: linear-gradient(90deg, rgba(15, 118, 110, 0.45), transparent);
  }
}

.card-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 34px;

  h2 {
    margin: 0 0 8px;
    color: #0f172a;
    font-size: 30px;
    font-weight: 800;
  }

  p {
    margin: 0;
    color: var(--app-text-muted, #64748b);
    font-size: 13px;
  }
}

.mode-badge {
  padding: 4px 9px;
  border-radius: 6px;
  color: #0f766e;
  background: rgba(20, 184, 166, 0.1);
  border: 1px solid rgba(20, 184, 166, 0.28);
  font-size: 12px;
  font-weight: 800;
}

.login-form {
  :deep(.el-form-item) {
    margin-bottom: 18px;
  }

  :deep(.el-input__wrapper) {
    padding: 10px 14px;
    border-radius: 8px;
    background: #fff;
    box-shadow: 0 0 0 1px #cbd5e1 inset;
    transition: all 0.3s;

    &:hover {
      box-shadow: 0 0 0 1px #0f766e inset;
    }

    &.is-focus {
      box-shadow: 0 0 0 1px #0f766e inset, 0 0 0 3px rgba(20, 184, 166, 0.14);
    }
  }

  .password-icon {
    cursor: pointer;
    color: #889aa4;
    transition: color 0.3s;

    &:hover {
      color: #0f766e;
    }
  }

  .login-button {
    width: 100%;
    height: 48px;
    margin-top: 10px;
    font-size: 16px;
    font-weight: 800;
    border-radius: 8px;
    background: linear-gradient(135deg, #0f766e, #111827 58%, #ca8a04);
    border: none;
    transition: all 0.3s;

    &:hover {
      transform: translateY(-2px);
      box-shadow: 0 14px 26px rgba(15, 118, 110, 0.28);
    }

    &:active {
      transform: translateY(0);
    }
  }
}

.auth-links {
  display: flex;
  justify-content: center;
  gap: 14px;
  margin-top: 16px;

  :deep(.el-button) {
    color: #0f766e;
    font-weight: 700;
  }
}

.card-footer {
  margin-top: 30px;
  text-align: center;

  p {
    font-size: 12px;
    color: #64748b;
    margin: 0;
  }
}

@media (max-width: 640px) {
  .login-container {
    padding: 18px;
  }

  .brand-panel,
  .login-card {
    padding: 28px 22px;
  }

  .brand-panel {
    .brand-main {
      flex-direction: column;
    }

    h1 {
      font-size: 36px;
    }
  }

  .brand-grid {
    grid-template-columns: 1fr;
  }
}
</style>
