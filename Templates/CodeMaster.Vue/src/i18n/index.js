import { createI18n } from 'vue-i18n'
import { getDefaultLang, getI18nMap } from '@/api/system/lang'

const i18n = createI18n({
  legacy: false,
  locale: 'zh-CN',
  messages: {}
})

export async function loadLocaleMessages(langCode) {
  try {
    console.log('加载语言包:', langCode)
    const messages = await getI18nMap(langCode)
    console.log('语言包数据:', messages)
    i18n.global.setLocaleMessage(langCode, messages || {})
    i18n.global.locale.value = langCode
    localStorage.setItem('lang', langCode)
  } catch (error) {
    console.error('加载语言包失败:', error)
    i18n.global.setLocaleMessage(langCode, {})
    i18n.global.locale.value = langCode
    localStorage.setItem('lang', langCode)
  }
}

export async function initI18n() {
  const saved = localStorage.getItem('lang')
  if (saved) {
    await loadLocaleMessages(saved)
    return
  }

  try {
    const defaultLang = await getDefaultLang()
    const langCode = defaultLang?.langCode || 'zh-CN'
    await loadLocaleMessages(langCode)
  } catch (error) {
    await loadLocaleMessages('zh-CN')
  }
}

export default i18n
