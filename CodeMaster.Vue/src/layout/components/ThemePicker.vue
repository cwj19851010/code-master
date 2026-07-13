<template>
  <el-dropdown trigger="click" @command="handleThemeChange">
    <button class="theme-trigger" type="button" :aria-label="t('theme_switch')" :title="t('theme_switch')">
      <el-icon :size="18">
        <component :is="activeIcon" />
      </el-icon>
    </button>
    <template #dropdown>
      <el-dropdown-menu class="theme-menu">
        <el-dropdown-item
          v-for="theme in themeOptions"
          :key="theme.name"
          :command="theme.name"
          :class="{ active: currentTheme === theme.name }"
        >
          <span class="theme-swatch" :style="{ background: getSwatch(theme.name) }" />
          <span>{{ t(theme.labelKey) }}</span>
          <el-icon v-if="currentTheme === theme.name" class="theme-check">
            <Check />
          </el-icon>
        </el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<script setup>
import { computed } from 'vue'
import { Check, Cloudy, Moon, Sunny } from '@element-plus/icons-vue'
import { useSettingsStore } from '@/stores/settings'
import { ElMessage } from 'element-plus'
import { useI18n } from 'vue-i18n'

const iconMap = {
  Sunny,
  Moon,
  Cloudy
}

const swatchMap = {
  'tech-blue': 'linear-gradient(135deg, #2563eb 0%, #0f766e 100%)',
  dark: 'linear-gradient(135deg, #111827 0%, #38bdf8 100%)',
  'business-gray': 'linear-gradient(135deg, #3f4c57 0%, #16a34a 100%)'
}

const settingsStore = useSettingsStore()
const { t } = useI18n()

const currentTheme = computed(() => settingsStore.currentTheme)
const themeOptions = computed(() => settingsStore.themeOptions)
const activeTheme = computed(() => themeOptions.value.find(item => item.name === currentTheme.value) || themeOptions.value[0])
const activeIcon = computed(() => iconMap[activeTheme.value?.icon] || Sunny)

const getSwatch = (theme) => swatchMap[theme] || swatchMap['tech-blue']

const handleThemeChange = (theme) => {
  if (theme === currentTheme.value) return

  settingsStore.changeTheme(theme)
  const nextTheme = themeOptions.value.find(item => item.name === theme)
  ElMessage.success(t('theme_switch_success', { theme: t(nextTheme?.labelKey || 'theme_tech_blue') }))
}
</script>

<style scoped lang="scss">
.theme-trigger {
  width: 34px;
  height: 34px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: var(--app-text-muted, #64748b);
  background: transparent;
  border: 1px solid transparent;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;

  &:hover {
    color: var(--app-primary, #2563eb);
    background: rgba(var(--app-primary-rgb, 37, 99, 235), 0.08);
    border-color: rgba(var(--app-primary-rgb, 37, 99, 235), 0.16);
  }

  &:focus-visible {
    outline: 2px solid rgba(var(--app-primary-rgb, 37, 99, 235), 0.36);
    outline-offset: 2px;
  }
}

:deep(.theme-menu .el-dropdown-menu__item) {
  min-width: 160px;
  display: flex;
  align-items: center;
  gap: 10px;

  &.active {
    color: var(--app-primary, #2563eb);
    background-color: rgba(var(--app-primary-rgb, 37, 99, 235), 0.08);
  }
}

.theme-swatch {
  width: 18px;
  height: 18px;
  border-radius: 50%;
  box-shadow: 0 0 0 1px rgba(148, 163, 184, 0.35) inset;
}

.theme-check {
  margin-left: auto;
}
</style>
