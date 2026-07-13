<template>
  <el-config-provider :locale="elementLocale">
    <router-view />
  </el-config-provider>
</template>

<script setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import zhCn from 'element-plus/dist/locale/zh-cn.mjs'
import en from 'element-plus/dist/locale/en.mjs'
import { useSettingsStore } from '@/stores/settings'

const { locale } = useI18n()
const settingsStore = useSettingsStore()

settingsStore.initTheme()

const elementLocale = computed(() => {
  return locale.value === 'en-US' ? en : zhCn
})
</script>

<style>
#app {
  height: 100vh;
  margin: 0;
  padding: 0;
}
</style>
