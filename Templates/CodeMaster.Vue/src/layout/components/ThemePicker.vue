<template>
  <el-dropdown trigger="click" @command="handleThemeChange">
    <el-tooltip :content="t('theme_switch')" placement="bottom">
      <el-icon :size="20" class="theme-icon">
        <Sunny v-if="currentTheme === 'tech-blue'" />
        <Moon v-else-if="currentTheme === 'dark'" />
        <Cloudy v-else />
      </el-icon>
    </el-tooltip>
    <template #dropdown>
      <el-dropdown-menu>
        <el-dropdown-item command="tech-blue" :class="{ active: currentTheme === 'tech-blue' }">
          <el-icon><Sunny /></el-icon>
          <span>{{ t('theme_tech_blue') }}</span>
        </el-dropdown-item>
        <el-dropdown-item command="dark" :class="{ active: currentTheme === 'dark' }">
          <el-icon><Moon /></el-icon>
          <span>{{ t('theme_dark') }}</span>
        </el-dropdown-item>
        <el-dropdown-item command="business-gray" :class="{ active: currentTheme === 'business-gray' }">
          <el-icon><Cloudy /></el-icon>
          <span>{{ t('theme_business_gray') }}</span>
        </el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<script setup>
import { computed } from 'vue'
import { Sunny, Moon, Cloudy } from '@element-plus/icons-vue'
import { useSettingsStore } from '@/stores/settings'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'

const settingsStore = useSettingsStore()
const { t } = useI18n()

const currentTheme = computed(() => settingsStore.currentTheme)

const handleThemeChange = (theme) => {
  settingsStore.changeTheme(theme)

  const themeKeyMap = {
    'tech-blue': 'theme_tech_blue',
    'dark': 'theme_dark',
    'business-gray': 'theme_business_gray'
  }

  ElMessage.success(t('theme_switch_success', { theme: t(themeKeyMap[theme]) }))
}
</script>

<style scoped lang="scss">
.theme-icon {
  cursor: pointer;
  transition: all 0.3s;

  &:hover {
    color: #409eff;
    transform: rotate(20deg);
  }
}

:deep(.el-dropdown-menu__item) {
  display: flex;
  align-items: center;
  gap: 8px;

  &.active {
    color: #409eff;
    background-color: #ecf5ff;
  }
}
</style>
