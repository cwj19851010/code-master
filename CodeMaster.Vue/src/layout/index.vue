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
          <theme-picker class="header-item" />
          <language-switcher class="header-item" />

          <el-dropdown class="header-item">
            <span class="user-info">
              <el-avatar :size="32" :src="userInfo?.avatar || defaultAvatar" />
              <span class="username">{{ userInfo?.nickName || t('admin') }}</span>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click="router.push('/profile/mcp-token')">MCP Token</el-dropdown-item>
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

onMounted(async () => {
  if (userStore.token) {
    await signalRService.connect()
  }

  isCollapse.value = !settingsStore.isSidebarOpened
})

onUnmounted(async () => {
  await signalRService.disconnect()
})
</script>

<style scoped lang="scss">
.layout-container {
  height: 100vh;
  background: var(--app-bg, #f4f7fb);
  color: var(--app-text, #1f2937);
}

.sidebar {
  background: var(--sidebar-bg, #152238);
  border-right: 1px solid var(--sidebar-border, rgba(148, 163, 184, 0.16));
  transition: width 0.3s, background-color 0.2s ease;
  overflow-x: hidden;

  .logo {
    height: 60px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(0, 0, 0, 0.16);
    border-bottom: 1px solid var(--sidebar-border, rgba(148, 163, 184, 0.16));
    transition: all 0.3s;

    h2 {
      color: #fff;
      font-size: 20px;
      margin: 0;
      font-weight: 600;
      letter-spacing: 0;
    }
  }

  .sidebar-menu {
    border: none;
    background: transparent;

    :deep(.el-menu-item),
    :deep(.el-sub-menu__title) {
      color: var(--sidebar-text-color, #b8c7e0);
      height: 46px;
      line-height: 46px;
      margin: 4px 8px;
      border-radius: 8px;

      &:hover {
        background: var(--sidebar-hover-bg, rgba(64, 158, 255, 0.1)) !important;
      }

      &.is-active {
        color: var(--sidebar-active-text-color, #409eff) !important;
        background: var(--sidebar-active-bg, rgba(64, 158, 255, 0.16)) !important;
      }
    }

    :deep(.el-sub-menu) {
      .el-menu {
        background: transparent;
      }
    }
  }
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background-color: var(--app-header-bg, #fff);
  border-bottom: 1px solid var(--app-border, #dbe4ef);
  box-shadow: var(--app-header-shadow, 0 1px 8px rgba(15, 23, 42, 0.05));
  padding: 0 18px;
  backdrop-filter: blur(10px);
  color: var(--app-text, #1f2937);
  transition: background-color 0.2s ease, border-color 0.2s ease;

  .header-left {
    display: flex;
    align-items: center;
    gap: 10px;

    .toggle-icon {
      font-size: 20px;
      cursor: pointer;
      color: var(--app-text-muted, #64748b);
      transition: all 0.3s;

      &:hover {
        color: var(--app-primary, #2563eb);
      }
    }

    .breadcrumb {
      margin-left: 10px;
    }
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 10px;

    .header-item {
      display: flex;
      align-items: center;
      flex-shrink: 0;
    }

    .user-info {
      display: flex;
      align-items: center;
      cursor: pointer;
      padding: 4px 6px;
      border-radius: 8px;
      transition: background-color 0.2s ease;

      &:hover {
        background: rgba(var(--app-primary-rgb, 37, 99, 235), 0.08);
      }

      .username {
        margin-left: 10px;
        font-size: 14px;
        color: var(--app-text, #333);
      }
    }
  }
}

.main-content {
  background-color: var(--app-bg, #f4f7fb);
  padding: 18px;
  overflow-y: auto;
  transition: background-color 0.2s ease;
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
