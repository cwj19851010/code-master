import { defineStore } from 'pinia'

export const useSettingsStore = defineStore('settings', {
  state: () => ({
    theme: 'tech-blue',
    sidebarOpened: true,
    language: 'zh-CN'
  }),

  persist: {
    paths: ['theme', 'sidebarOpened', 'language']
  },

  getters: {
    currentTheme: (state) => state.theme,
    isSidebarOpened: (state) => state.sidebarOpened,
    currentLanguage: (state) => state.language
  },

  actions: {
    // 切换主题
    changeTheme(theme) {
      this.theme = theme
      this.applyTheme(theme)
    },

    // 应用主题
    applyTheme(theme) {
      const themes = {
        'tech-blue': {
          '--sidebar-bg': 'linear-gradient(180deg, #1a1f3a 0%, #2d3561 100%)',
          '--sidebar-text-color': '#b8c7e0',
          '--sidebar-active-text-color': '#409eff',
          '--sidebar-hover-bg': 'rgba(64, 158, 255, 0.1)'
        },
        'dark': {
          '--sidebar-bg': 'linear-gradient(180deg, #141414 0%, #1f1f1f 100%)',
          '--sidebar-text-color': '#a0a0a0',
          '--sidebar-active-text-color': '#00d4ff',
          '--sidebar-hover-bg': 'rgba(0, 212, 255, 0.1)'
        },
        'business-gray': {
          '--sidebar-bg': 'linear-gradient(180deg, #2c3e50 0%, #34495e 100%)',
          '--sidebar-text-color': '#bdc3c7',
          '--sidebar-active-text-color': '#3498db',
          '--sidebar-hover-bg': 'rgba(52, 152, 219, 0.1)'
        }
      }

      const themeVars = themes[theme] || themes['tech-blue']
      const root = document.documentElement

      Object.keys(themeVars).forEach(key => {
        root.style.setProperty(key, themeVars[key])
      })
    },

    // 切换侧边栏
    toggleSidebar() {
      this.sidebarOpened = !this.sidebarOpened
    },

    // 切换语言
    changeLanguage(language) {
      this.language = language
    },

    // 初始化主题
    initTheme() {
      this.applyTheme(this.theme)
    }
  }
})
