<template>
  <div class="login-container">
    <!-- 背景动画 -->
    <div class="background-animation">
      <div class="circle circle-1"></div>
      <div class="circle circle-2"></div>
      <div class="circle circle-3"></div>
      <div class="circle circle-4"></div>
    </div>

    <!-- 登录表单卡片 -->
    <div class="login-card">
      <div class="card-header">
        <div class="logo">
          <el-icon :size="50" color="#409eff"><Platform /></el-icon>
        </div>
        <h2 class="title">CodeMaster</h2>
        <p class="subtitle">{{ t('login_title') }}</p>
      </div>

      <el-form ref="loginFormRef" :model="loginForm" :rules="loginRules" class="login-form">
        <el-form-item prop="username">
          <el-input
            ref="username"
            v-model="loginForm.username"
            :placeholder="t('please_input_username')"
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
            :placeholder="t('please_input_password')"
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

      <div class="card-footer">
        <p>{{ t('copyright') }}</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { User, Lock, View, Hide, Platform } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import { useI18n } from 'vue-i18n'

const router = useRouter()
const userStore = useUserStore()
const loginFormRef = ref(null)
const { t } = useI18n()

const loginForm = reactive({
  username: 'admin',
  password: 'admin123'
})

const loginRules = {
  username: [{ required: true, trigger: 'blur', message: t('please_input_username') }],
  password: [{ required: true, trigger: 'blur', message: t('please_input_password') }]
}

const loading = ref(false)
const passwordType = ref('password')

const showPwd = () => {
  passwordType.value = passwordType.value === 'password' ? 'text' : 'password'
}

const handleLogin = () => {
  loginFormRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true
      try {
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
</script>

<style lang="scss" scoped>
.login-container {
  position: relative;
  min-height: 100vh;
  width: 100%;
  background: linear-gradient(135deg, #1a1f3a 0%, #2d3561 100%);
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
}

// 背景动画
.background-animation {
  position: absolute;
  width: 100%;
  height: 100%;
  overflow: hidden;

  .circle {
    position: absolute;
    border-radius: 50%;
    background: linear-gradient(135deg, rgba(64, 158, 255, 0.1) 0%, rgba(100, 120, 255, 0.1) 100%);
    animation: float 20s infinite ease-in-out;

    &.circle-1 {
      width: 300px;
      height: 300px;
      top: 10%;
      left: 10%;
      animation-delay: 0s;
    }

    &.circle-2 {
      width: 200px;
      height: 200px;
      top: 60%;
      left: 70%;
      animation-delay: 5s;
    }

    &.circle-3 {
      width: 250px;
      height: 250px;
      top: 30%;
      right: 10%;
      animation-delay: 10s;
    }

    &.circle-4 {
      width: 180px;
      height: 180px;
      bottom: 10%;
      left: 50%;
      animation-delay: 15s;
    }
  }
}

@keyframes float {
  0%, 100% {
    transform: translate(0, 0) scale(1);
    opacity: 0.3;
  }
  50% {
    transform: translate(50px, 50px) scale(1.1);
    opacity: 0.5;
  }
}

// 登录卡片
.login-card {
  position: relative;
  z-index: 1;
  width: 450px;
  padding: 50px 40px 30px;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(10px);
  border-radius: 20px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  animation: slideIn 0.5s ease-out;

  @media (max-width: 768px) {
    width: 90%;
    padding: 40px 30px 20px;
  }
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateY(-30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

// 卡片头部
.card-header {
  text-align: center;
  margin-bottom: 40px;

  .logo {
    margin-bottom: 20px;
    animation: pulse 2s infinite;
  }

  .title {
    font-size: 32px;
    font-weight: 700;
    color: #1a1f3a;
    margin: 0 0 10px 0;
    background: linear-gradient(135deg, #409eff 0%, #6478ff 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
  }

  .subtitle {
    font-size: 14px;
    color: #666;
    margin: 0;
  }
}

@keyframes pulse {
  0%, 100% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.05);
  }
}

// 登录表单
.login-form {
  :deep(.el-form-item) {
    margin-bottom: 24px;
  }

  :deep(.el-input__wrapper) {
    padding: 12px 15px;
    border-radius: 10px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
    transition: all 0.3s;

    &:hover {
      box-shadow: 0 4px 12px rgba(64, 158, 255, 0.15);
    }

    &.is-focus {
      box-shadow: 0 4px 12px rgba(64, 158, 255, 0.25);
    }
  }

  .password-icon {
    cursor: pointer;
    color: #889aa4;
    transition: color 0.3s;

    &:hover {
      color: #409eff;
    }
  }

  .login-button {
    width: 100%;
    height: 48px;
    margin-top: 10px;
    font-size: 16px;
    font-weight: 600;
    border-radius: 10px;
    background: linear-gradient(135deg, #409eff 0%, #6478ff 100%);
    border: none;
    transition: all 0.3s;

    &:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 20px rgba(64, 158, 255, 0.4);
    }

    &:active {
      transform: translateY(0);
    }
  }
}

// 卡片底部
.card-footer {
  margin-top: 30px;
  text-align: center;

  p {
    font-size: 12px;
    color: #999;
    margin: 0;
  }
}
</style>
