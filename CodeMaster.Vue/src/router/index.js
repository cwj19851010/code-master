import { createRouter, createWebHashHistory } from 'vue-router'
import Layout from '@/layout/index.vue'
import { useUserStore } from '@/stores/user'
import { usePermissionStore } from '@/stores/permission'

// 静态路由（不需要权限）
export const constantRoutes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/index.vue'),
    meta: { title: 'login' }
  },
  {
    path: '/redirect',
    component: Layout,
    hidden: true,
    children: [
      {
        path: '/redirect/:path(.*)',
        component: () => import('@/views/redirect/index.vue')
      }
    ]
  },
  {
    path: '/',
    component: Layout,
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/index.vue'),
        meta: { title: 'dashboard', icon: 'HomeFilled', affix: true }
      },
      {
        path: 'profile/mcp-token',
        name: 'McpToken',
        component: () => import('@/views/profile/mcpToken/index.vue'),
        meta: { title: 'MCP Token', hidden: true, noCache: true }
      },
      // EntityDesigner 路由由菜单数据库动态加载
    ]
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes: constantRoutes
})

// 添加一个标志位，防止路由守卫无限循环
let isAddingRoutes = false

// 使用 import.meta.glob 预加载所有视图组件
const modules = import.meta.glob('@/views/**/*.vue')

// 动态加载组件
const loadView = (view) => {
  if (!view) return null

  // 处理 Layout 组件
  if (view === 'Layout') {
    return Layout
  }

  // 构建完整路径
  let componentPath = `@/views/${view}.vue`

  // 如果路径不存在,尝试添加 index
  if (!modules[`/src/views/${view}.vue`]) {
    componentPath = `@/views/${view}/index.vue`
  }

  // 返回动态导入函数
  const matchedModule = modules[componentPath.replace('@', '/src')]
  if (matchedModule) {
    console.log(`[Router] ✓ 加载组件: ${view} -> ${componentPath}`)
    return matchedModule
  }

  console.error(`[Router] ✗ 组件未找到: ${view}, 尝试路径: ${componentPath}`)
  // 返回 null，让 Vue Router 处理
  return null
}

// 转换后端路由为前端路由格式
export function filterAsyncRoutes(routes) {
  const res = []

  console.log(`[filterAsyncRoutes] 开始处理 ${routes.length} 个路由`)

  routes.forEach(route => {
    const tmp = { ...route }

    console.log(`[filterAsyncRoutes] 处理路由: ${tmp.path}, component: ${tmp.component}`)

    // 递归处理子路由（在转换组件之前）
    if (tmp.children && tmp.children.length > 0) {
      console.log(`[filterAsyncRoutes] ${tmp.path} 有 ${tmp.children.length} 个子路由`)
      tmp.children = filterAsyncRoutes(tmp.children)
      console.log(`[filterAsyncRoutes] ${tmp.path} 处理���剩余 ${tmp.children.length} 个子路由`)

      // 如果是 Layout 路由且没有 redirect，自动设置 redirect 到第一个非隐藏的子路由
      if (tmp.component === 'Layout' && !tmp.redirect && tmp.children.length > 0) {
        const firstVisibleChild = tmp.children.find(child => !child.meta?.hidden)
        if (firstVisibleChild) {
          // 确保使用绝对路径
          const redirectPath = firstVisibleChild.path.startsWith('/')
            ? firstVisibleChild.path
            : `${tmp.path}/${firstVisibleChild.path}`
          tmp.redirect = redirectPath
          console.log(`[Router] 为 Layout ${tmp.path} 设置 redirect: ${tmp.redirect}`)
        }
      }
    }

    // 设置组件（在处理完 redirect 之后）
    if (tmp.component) {
      const originalComponent = tmp.component
      tmp.component = loadView(tmp.component)

      console.log(`[filterAsyncRoutes] ${tmp.path} 组件加载结果:`, tmp.component === null ? 'null' : tmp.component === undefined ? 'undefined' : typeof tmp.component)
      console.log(`[filterAsyncRoutes] ${tmp.path} 组件值检查: tmp.component =`, tmp.component, ', !tmp.component =', !tmp.component)

      // 如果组件加载失败（返回 null 或 undefined），跳过该路由
      console.log(`[filterAsyncRoutes] ${tmp.path} 准备检查 if (!tmp.component)...`)
      if (!tmp.component) {
        console.warn(`[Router] ⚠ 跳过路由 ${tmp.path}，因为组件 ${originalComponent} 加载失败`)
        return // 退出当前 forEach 回调，不执行后面的 res.push
      }
      console.log(`[filterAsyncRoutes] ${tmp.path} 通过组件检查，继续处理`)
    }

    // 确保路由有 name 属性（用于 keep-alive）
    if (!tmp.name && tmp.meta?.title) {
      tmp.name = tmp.meta.title
    }

    // 将 meta.hidden 提升到路由对象顶层（菜单组件需要）
    if (tmp.meta?.hidden !== undefined) {
      tmp.hidden = tmp.meta.hidden
    }

    console.log(`[filterAsyncRoutes] ✓ 添加路由: ${tmp.path}`)
    res.push(tmp)
  })

  console.log(`[filterAsyncRoutes] 完成，返回 ${res.length} 个路由`)
  return res
}

// 路由守卫
router.beforeEach(async (to, from, next) => {
  const userStore = useUserStore()
  const permissionStore = usePermissionStore()
  const token = userStore.token

  if (to.path === '/login') {
    // 如果已登录，跳转到首页
    if (token) {
      next('/')
    } else {
      next()
    }
    return
  }

  // 需要登录的页面
  if (!token) {
    next('/login')
    return
  }

  // 检查用户信息是否已加载
  if (!userStore.userInfo || !userStore.userInfo.userId) {
    try {
      await userStore.getUserInfo()
    } catch (error) {
      console.error('获取用户信息失败:', error)
      userStore.logout()
      next('/login')
      return
    }
  }

  // 使用 routesLoaded 标志位判断路由是否已加载
  if (!permissionStore.routesLoaded && !isAddingRoutes) {
    isAddingRoutes = true
    try {
      // 生成动态路由（这里会设置 routesLoaded = true）
      const accessRoutes = await permissionStore.generateRoutes()

      // 转换路由格式
      const routes = filterAsyncRoutes(accessRoutes)

      // 动态添加路由
      routes.forEach(route => {
        router.addRoute(route)
      })

      // 添加 404 路由（必须在最后）
      router.addRoute({
        path: '/:pathMatch(.*)*',
        redirect: '/404'
      })

      // 重置标志位
      isAddingRoutes = false

      // 使用 next(to.path) 而不是 next({ ...to, replace: true })
      next({ path: to.path, query: to.query, params: to.params, replace: true })
      return
    } catch (error) {
      console.error('路由守卫错误:', error)
      isAddingRoutes = false
      permissionStore.routesLoaded = false
      // 清除 token 并跳转到登录页
      userStore.logout()
      next('/login')
      return
    }
  }

  next()
})

export default router
