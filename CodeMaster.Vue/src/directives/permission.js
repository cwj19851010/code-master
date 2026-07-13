import { useUserStore } from '@/stores/user'
import { watch } from 'vue'

export default {
  mounted(el, binding) {
    const { value } = binding
    const userStore = useUserStore()

    const setVisible = (visible) => {
      el.style.display = visible ? '' : 'none'
      if (visible) {
        el.style.visibility = 'visible'
      }
    }

    const checkPermission = () => {
      const permissions = userStore.permissions || []

      if (Array.isArray(value) && value.length > 0) {
        const hasPermission = permissions.some(permission => value.includes(permission))
        setVisible(hasPermission)
        return hasPermission
      }

      if (value && typeof value === 'string') {
        const hasPermission = permissions.includes(value)
        setVisible(hasPermission)
        return hasPermission
      }

      console.warn('v-permission requires a permission key, for example v-permission="[\'system:user:add\']"')
      setVisible(false)
      return false
    }

    const stopWatch = watch(
      () => [userStore.userInfo, userStore.permissions],
      () => {
        checkPermission()
      },
      { deep: true, immediate: true }
    )

    el._permissionWatcher = stopWatch
  },

  unmounted(el) {
    if (el._permissionWatcher) {
      el._permissionWatcher()
    }
  }
}
