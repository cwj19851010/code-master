import { createApp } from 'vue'
import { createPinia } from 'pinia'
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/dark/css-vars.css'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'

import App from './App.vue'
import router from './router'
import directives from './directives'
import './styles/index.scss'
import i18n, { initI18n, t2 } from './i18n'
import { installTauriBridge } from './utils/tauriBridge'

const app = createApp(App)
const pinia = createPinia()
pinia.use(piniaPluginPersistedstate)

const bootstrap = async () => {
  await installTauriBridge()
  await initI18n()

  // 注册所有图标
  for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
    app.component(key, component)
  }

  app.use(pinia)
  app.use(router)
  app.use(ElementPlus)
  app.use(directives)
  app.use(i18n)

  // 将 t2 方法挂载到全局属性，方便在模板中使用
  app.config.globalProperties.$t2 = t2

  app.mount('#app')
}

bootstrap()
