import { createI18n } from 'vue-i18n'
import { getDefaultLang, getI18nMap } from '@/api/system/lang'

const i18n = createI18n({
  legacy: false,
  locale: 'zh-CN',
  messages: {}
})

/**
 * 组合翻译方法 - 支持2个参数组合
 * @param {string} key1 - 第一个翻译键（如 'please_input'）
 * @param {string} key2 - 第二个翻译键（如 'username'）
 * @returns {string} - 组合后的翻译文本
 *
 * 示例：
 * t2('please_input', 'username') => "请输入用户名" / "Please input username"
 * t2('please_select', 'role') => "请选择角色" / "Please select role"
 */
export function t2(key1, key2) {
  const { t } = i18n.global
  const text1 = t(key1)
  const text2 = t(key2)

  // 如果是中文，直接拼接
  if (i18n.global.locale.value === 'zh-CN') {
    return text1 + text2
  }

  // 如果是英文，用空格连接
  return text1 + ' ' + text2
}

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
