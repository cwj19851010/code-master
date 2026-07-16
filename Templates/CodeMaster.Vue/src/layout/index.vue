<template>
  <el-container class="layout-container">
    <el-aside :width="isCollapse ? '64px' : '200px'" class="sidebar">
      <div class="logo">
        <h2 v-if="!isCollapse">{{t('app_title')}}</h2>
        <h2 v-else>CM</h2>
      </div>
      <el-menu
        :default-active="activeMenu"
        :collapse="isCollapse"
        class="sidebar-menu"
      >
        <sidebar-item
          v-for="route in menuRoutes"
          :key="route.path"
          :item="route"
          :base-path="route.path"
        />
      </el-menu>
    </el-aside>

    <el-container>
      <el-header class="header">
        <div class="header-left">
          <el-icon @click="toggleSidebar" class="toggle-icon">
            <Fold v-if="!isCollapse" />
            <Expand v-else />
          </el-icon>
          <breadcrumb class="breadcrumb" />
        </div>
        <div class="header-right">
          <!-- 主题切换器 -->
          <theme-picker class="header-item" />
          <!-- 语言切换器 -->
          <language-switcher class="header-item" />

          <!-- 用户下拉菜单 -->
          <el-dropdown class="header-item">
            <span class="user-info">
              <el-avatar :size="32" :src="userInfo?.avatar || defaultAvatar" />
              <span class="username">{{ userInfo?.nickName || t('admin') }}</span>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item>{{ t('profile') }}</el-dropdown-item>
                <el-dropdown-item divided @click="handleLogout">{{ t('logout') }}</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <!-- 标签页导航 -->
      <tabs-view />

      <el-main class="main-content">
        <router-view v-slot="{ Component }">
          <keep-alive :include="cachedViews" :max="20">
            <component :is="Component" />
          </keep-alive>
        </router-view>
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Fold, Expand } from '@element-plus/icons-vue'
import SidebarItem from './SidebarItem.vue'
import ThemePicker from './components/ThemePicker.vue'
import LanguageSwitcher from './components/LanguageSwitcher.vue'
import Breadcrumb from '@/components/Breadcrumb/index.vue'
import TabsView from '@/components/TabsView/index.vue'
import { signalRService } from '@/utils/signalr'
import { useUserStore } from '@/stores/user'
import { usePermissionStore } from '@/stores/permission'
import { useSettingsStore } from '@/stores/settings'
import { useTagsViewStore } from '@/stores/tagsView'
import { useI18n } from 'vue-i18n'
import defaultAvatarImg from '@/assets/images/default-avatar.svg'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const permissionStore = usePermissionStore()
const settingsStore = useSettingsStore()
const tagsViewStore = useTagsViewStore()
const { t } = useI18n()

const isCollapse = ref(false)
const defaultAvatar = defaultAvatarImg

const userInfo = computed(() => userStore.userInfo)

const menuRoutes = computed(() => {
  // 使用 permission store 中的菜单
  return permissionStore.menus
})

const activeMenu = computed(() => {
  return route.path
})

const cachedViews = computed(() => tagsViewStore.cachedViews)

const toggleSidebar = () => {
  isCollapse.value = !isCollapse.value
  settingsStore.toggleSidebar()
}

const handleLogout = async () => {
  ElMessageBox.confirm(t('logout_confirm'), t('prompt'), {
    confirmButtonText: t('confirm'),
    cancelButtonText: t('cancel'),
    type: 'warning'
  }).then(async () => {
    // 断开 SignalR 连接
    await signalRService.disconnect()

    // 使用 userStore 的 logout 方法
    await userStore.logout()

    ElMessage.success(t('logout_success'))
    router.push('/login')
  }).catch(() => {})
}

// 组件挂载时连接 SignalR 并初始化主题
onMounted(async () => {
  const token = localStorage.getItem('token')
  if (token) {
    await signalRService.connect()
  }

  // 初始化主题
  settingsStore.initTheme()

  // 初始化侧边栏状态
  isCollapse.value = !settingsStore.isSidebarOpened
})

// 组件卸载时断开连接
onUnmounted(async () => {
  await signalRService.disconnect()
})
</script>

<style scoped lang="scss">
.layout-container {
  height: 100vh;
}

.sidebar {
  background: var(--sidebar-bg, linear-gradient(180deg, #1a1f3a 0%, #2d3561 100%));
  transition: width 0.3s;
  overflow-x: hidden;

  .logo {
    height: 60px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(0, 0, 0, 0.2);
    transition: all 0.3s;

    h2 {
      color: #fff;
      font-size: 20px;
      margin: 0;
      font-weight: 600;
      letter-spacing: 2px;
    }
  }

  .sidebar-menu {
    border: none;
    background: transparent;

    :deep(.el-menu-item),
    :deep(.el-sub-menu__title) {
      color: var(--sidebar-text-color, #b8c7e0);

      &:hover {
        background: var(--sidebar-hover-bg, rgba(64, 158, 255, 0.1)) !important;
      }

      &.is-active {
        color: var(--sidebar-active-text-color, #409eff) !important;
        background: var(--sidebar-hover-bg, rgba(64, 158, 255, 0.1)) !important;
      }
    }

    :deep(.el-sub-menu) {
      .el-menu {
        background: rgba(0, 0, 0, 0.1);
      }
    }
  }
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background-color: #fff;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
  padding: 0 20px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 10px;

    .toggle-icon {
      font-size: 20px;
      cursor: pointer;
      transition: all 0.3s;

      &:hover {
        color: #409eff;
      }
    }

    .breadcrumb {
      margin-left: 10px;
    }
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 20px;

    .header-item {
      display: flex;
      align-items: center;
      flex-shrink: 0;
    }

    .user-info {
      display: flex;
      align-items: center;
      cursor: pointer;

      .username {
        margin-left: 10px;
        font-size: 14px;
        color: #333;
      }
    }
  }
}

.main-content {
  background-color: #f0f2f5;
  padding: 20px;
  overflow-y: auto;
}

// 页面切换动画
.fade-transform-leave-active,
.fade-transform-enter-active {
  transition: all 0.3s;
}

.fade-transform-enter-from {
  opacity: 0;
  transform: translateX(-30px);
}

.fade-transform-leave-to {
  opacity: 0;
  transform: translateX(30px);
}
</style>
