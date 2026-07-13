import { defineStore } from 'pinia'

export const appThemes = [
  {
    name: 'tech-blue',
    labelKey: 'theme_tech_blue',
    icon: 'Sunny',
    variables: {
      '--sidebar-bg': '#152238',
      '--sidebar-border': 'rgba(148, 163, 184, 0.16)',
      '--sidebar-text-color': '#c6d3e6',
      '--sidebar-active-text-color': '#ffffff',
      '--sidebar-active-bg': 'rgba(59, 130, 246, 0.22)',
      '--sidebar-hover-bg': 'rgba(96, 165, 250, 0.14)',
      '--app-bg': '#f4f7fb',
      '--app-surface': '#ffffff',
      '--app-surface-soft': '#f8fafc',
      '--app-border': '#dbe4ef',
      '--app-text': '#1f2937',
      '--app-text-muted': '#64748b',
      '--app-primary': '#2563eb',
      '--app-primary-rgb': '37, 99, 235',
      '--app-accent': '#0f766e',
      '--app-warning': '#b7791f',
      '--app-header-bg': 'rgba(255, 255, 255, 0.94)',
      '--app-card-shadow': '0 10px 30px rgba(31, 41, 55, 0.08)',
      '--app-header-shadow': '0 1px 8px rgba(15, 23, 42, 0.05)',
      '--el-color-primary': '#2563eb',
      '--el-color-primary-light-3': '#4f83f1',
      '--el-color-primary-light-5': '#8fb3fa',
      '--el-color-primary-light-7': '#bfd3fd',
      '--el-color-primary-light-8': '#d5e3fe',
      '--el-color-primary-light-9': '#eaf1ff',
      '--el-color-primary-dark-2': '#1d4ed8'
    }
  },
  {
    name: 'dark',
    labelKey: 'theme_dark',
    icon: 'Moon',
    dark: true,
    variables: {
      '--sidebar-bg': '#0d1420',
      '--sidebar-border': 'rgba(148, 163, 184, 0.14)',
      '--sidebar-text-color': '#a8b3c5',
      '--sidebar-active-text-color': '#e0f2fe',
      '--sidebar-active-bg': 'rgba(56, 189, 248, 0.16)',
      '--sidebar-hover-bg': 'rgba(56, 189, 248, 0.12)',
      '--app-bg': '#0f141b',
      '--app-surface': '#171d25',
      '--app-surface-soft': '#1f2732',
      '--app-border': '#2f3a48',
      '--app-text': '#e5e7eb',
      '--app-text-muted': '#94a3b8',
      '--app-primary': '#38bdf8',
      '--app-primary-rgb': '56, 189, 248',
      '--app-accent': '#34d399',
      '--app-warning': '#f59e0b',
      '--app-header-bg': 'rgba(23, 29, 37, 0.94)',
      '--app-card-shadow': '0 16px 34px rgba(0, 0, 0, 0.26)',
      '--app-header-shadow': '0 1px 10px rgba(0, 0, 0, 0.24)',
      '--el-color-primary': '#38bdf8',
      '--el-color-primary-light-3': '#67cdfa',
      '--el-color-primary-light-5': '#9be1fc',
      '--el-color-primary-light-7': '#c3effe',
      '--el-color-primary-light-8': '#d7f5fe',
      '--el-color-primary-light-9': '#ecfbff',
      '--el-color-primary-dark-2': '#0ea5e9'
    }
  },
  {
    name: 'business-gray',
    labelKey: 'theme_business_gray',
    icon: 'Cloudy',
    variables: {
      '--sidebar-bg': '#303a45',
      '--sidebar-border': 'rgba(203, 213, 225, 0.18)',
      '--sidebar-text-color': '#d1d9e0',
      '--sidebar-active-text-color': '#ffffff',
      '--sidebar-active-bg': 'rgba(22, 163, 74, 0.2)',
      '--sidebar-hover-bg': 'rgba(34, 197, 94, 0.12)',
      '--app-bg': '#f3f5f7',
      '--app-surface': '#ffffff',
      '--app-surface-soft': '#f7f9fb',
      '--app-border': '#d8dee6',
      '--app-text': '#24303b',
      '--app-text-muted': '#667281',
      '--app-primary': '#2563eb',
      '--app-primary-rgb': '37, 99, 235',
      '--app-accent': '#16a34a',
      '--app-warning': '#b7791f',
      '--app-header-bg': 'rgba(255, 255, 255, 0.94)',
      '--app-card-shadow': '0 10px 26px rgba(36, 48, 59, 0.08)',
      '--app-header-shadow': '0 1px 8px rgba(36, 48, 59, 0.05)',
      '--el-color-primary': '#2563eb',
      '--el-color-primary-light-3': '#4f83f1',
      '--el-color-primary-light-5': '#8fb3fa',
      '--el-color-primary-light-7': '#bfd3fd',
      '--el-color-primary-light-8': '#d5e3fe',
      '--el-color-primary-light-9': '#eaf1ff',
      '--el-color-primary-dark-2': '#1d4ed8'
    }
  }
]

const defaultTheme = 'tech-blue'
const findTheme = (theme) => appThemes.find(item => item.name === theme) || appThemes[0]

export const useSettingsStore = defineStore('settings', {
  state: () => ({
    theme: defaultTheme,
    sidebarOpened: true,
    language: 'zh-CN'
  }),

  persist: {
    paths: ['theme', 'sidebarOpened', 'language']
  },

  getters: {
    currentTheme: (state) => state.theme,
    themeOptions: () => appThemes,
    isSidebarOpened: (state) => state.sidebarOpened,
    currentLanguage: (state) => state.language
  },

  actions: {
    changeTheme(theme) {
      const nextTheme = findTheme(theme).name
      this.theme = nextTheme
      this.applyTheme(nextTheme)
    },

    applyTheme(theme) {
      const themeConfig = findTheme(theme)
      const root = document.documentElement

      Object.entries(themeConfig.variables).forEach(([key, value]) => {
        root.style.setProperty(key, value)
      })

      root.dataset.theme = themeConfig.name
      root.classList.toggle('dark', Boolean(themeConfig.dark))
    },

    toggleSidebar() {
      this.sidebarOpened = !this.sidebarOpened
    },

    changeLanguage(language) {
      this.language = language
    },

    initTheme() {
      this.applyTheme(this.theme)
    }
  }
})
