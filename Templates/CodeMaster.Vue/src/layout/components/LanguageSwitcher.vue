<template>
  <el-select v-model="currentLang" size="small" class="language-selector" @change="handleChange">
    <el-option
      v-for="lang in langList"
      :key="lang.langCode"
      :label="lang.langName"
      :value="lang.langCode"
    />
  </el-select>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getEnabledLangs } from '@/api/system/lang'
import { loadLocaleMessages } from '@/i18n'

const currentLang = ref(localStorage.getItem('lang') || 'zh-CN')
const langList = ref([])

const handleChange = async (langCode) => {
  console.log('[LanguageSwitcher] handleChange 被调用, langCode:', langCode)
  await loadLocaleMessages(langCode)
}

onMounted(async () => {
  console.log('[LanguageSwitcher] onMounted 开始')
  try {
    const list = await getEnabledLangs()
    console.log('[LanguageSwitcher] 获取到语言列表:', list)
    langList.value = list || []
    if (langList.value.length === 0) return

    const exists = langList.value.some(x => x.langCode === currentLang.value)
    if (!exists) {
      const defaultLang = langList.value.find(x => x.isDefault === 1) || langList.value[0]
      currentLang.value = defaultLang.langCode
    }

    // 确保加载当前语言的语言包
    console.log('[LanguageSwitcher] 准备加载语言包:', currentLang.value)
    await loadLocaleMessages(currentLang.value)
    console.log('[LanguageSwitcher] onMounted 完成')
  } catch (error) {
    console.error('初始化语言选择器失败:', error)
  }
})
</script>

<style scoped>
.language-selector {
  width: 140px !important;
}

.language-selector :deep(.el-select__wrapper) {
  width: 140px !important;
}

.language-selector :deep(.el-input) {
  width: 140px !important;
}

.language-selector :deep(.el-input__wrapper) {
  width: 140px !important;
}
</style>
