import { defineStore } from 'pinia'
import { login as loginApi, getUserInfo as getUserInfoApi } from '@/api/auth'
import { usePermissionStore } from './permission'

export const useUserStore = defineStore('user', {
  state: () => ({
    token: '',
    userInfo: null,
    roles: [],
    permissions: []
  }),

  persist: {
    paths: ['token', 'userInfo', 'roles', 'permissions']
  },

  getters: {
    userId: (state) => state.userInfo?.id || null,
    userName: (state) => state.userInfo?.userName || '',
    nickName: (state) => state.userInfo?.nickName || '',
    avatar: (state) => state.userInfo?.avatar || '',
    deptId: (state) => state.userInfo?.deptId || null,
    dataScope: (state) => state.userInfo?.dataScope || 1,
    isAdmin: (state) => !!state.userInfo?.isAdmin,
    isHostAdmin: (state) => !!state.userInfo?.isHostAdmin,
    isTenantAdmin: (state) => !!state.userInfo?.isTenantAdmin
  },

  actions: {
    // 登录
    async login(loginForm) {
      try {
        // 响应拦截器已自动解包 data 字段，res 直接是 LoginResultDto
        const res = await loginApi(loginForm)
        if (!res?.accessToken) {
          throw new Error('登录失败：服务器未返回访问令牌')
        }

        this.token = res.accessToken
        this.userInfo = res.userInfo

        // 登录成功后立即获取用户完整信息（包括角色和权限）
        await this.getUserInfo()

        return res
      } catch (error) {
        throw error
      }
    },

    // 获取用户信息
    async getUserInfo() {
      try {
        // 响应拦截器已自动解包 data 字段，res 直接是 UserInfoResponseDto
        const res = await getUserInfoApi()
        if (!res?.user) {
          throw new Error('获取用户信息失败：服务器返回格式不正确')
        }

        this.userInfo = res.user
        this.roles = res.roles || []
        this.permissions = res.permissions || []
        return res
      } catch (error) {
        throw error
      }
    },

    // 退出登录
    logout() {
      this.token = ''
      this.userInfo = null
      this.roles = []
      this.permissions = []

      // 重置路由加载标志位
      const permissionStore = usePermissionStore()
      permissionStore.clearRoutes()
    },

    // 检查权限
    hasPermission(permission) {
      if (!permission) return true
      return this.permissions.includes(permission)
    },

    // 检查角色
    hasRole(role) {
      if (!role) return true
      return this.roles.includes(role)
    }
  }
})
