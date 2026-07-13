import { defineStore } from 'pinia'
import { getRoutes } from '@/api/system/menu'

export const usePermissionStore = defineStore('permission', {
  state: () => ({
    routes: [],
    addRoutes: [],
    menuList: [],
    routesLoaded: false // 标志位，标记路由是否已加载
  }),

  persist: {
    paths: ['routes', 'addRoutes', 'menuList']
    // 注意：routesLoaded 不应该持久化，因为刷新页面后需要重新添加动态路由
  },

  getters: {
    // 获取所有路由
    allRoutes: (state) => state.routes,

    // 获取菜单列表
    menus: (state) => state.menuList
  },

  actions: {
    // 生成路由
    async generateRoutes() {
      try {
        // 响应拦截器已自动解包 data 字段，res 直接是路由数组
        const routes = await getRoutes()

        // 扁平化路由：将 MenuType=C 下的子路由提升到同级
        const flattenedRoutes = this.flattenRoutes(routes || [])

        this.addRoutes = flattenedRoutes
        this.routes = flattenedRoutes
        // menuList 需要提升 hidden 字段到顶层，供菜单组件使用
        this.menuList = this.processMenuList(flattenedRoutes)
        this.routesLoaded = true // 标记路由已加载
        return flattenedRoutes
      } catch (error) {
        console.error('生成路由失败:', error)
        throw error
      }
    },

    // 扁平化路由：将子路由提升到父级的 children 中
    flattenRoutes(routes) {
      console.log('[Permission] 原始路由数据 - 总数:', routes.length)

      // 第一步：将所有路由按 path 分组
      const routeMap = new Map()
      routes.forEach(route => {
        routeMap.set(route.path, { ...route, children: route.children || [] })
      })

      console.log('[Permission] 路由 Map 中的所有路径:', Array.from(routeMap.keys()))

      // 第二步：遍历所有路由，建立父子关系
      routes.forEach(route => {
        const pathParts = route.path.split('/').filter(p => p)

        // 如果路径有3层或更多（如 /system/user/add），尝试找到父路由
        if (pathParts.length >= 3 && route.component !== 'Layout') {
          // 构建可能的父路径：/system/user
          const parentPath = '/' + pathParts.slice(0, -1).join('/')
          console.log(`[Permission] 查找 ${route.path} 的父路由: ${parentPath}`)

          const parentRoute = routeMap.get(parentPath)

          if (parentRoute) {
            console.log(`[Permission] ✓ 找到父路由 ${parentPath}, component: ${parentRoute.component}`)
            if (parentRoute.component !== 'Layout') {
              // 找到父路由，将当前路由添加到父路由的 children 中
              if (!parentRoute.children) {
                parentRoute.children = []
              }
              parentRoute.children.push(route)
              route._addedToParent = true
              console.log(`[Permission] ✓ 将 ${route.path} 添加到 ${parentPath} 的 children 中`)
            }
          } else {
            console.log(`[Permission] ✗ 未找到父路由 ${parentPath}`)
          }
        }
      })

      // 第三步：收集顶层路由
      const topLevelRoutes = []
      routes.forEach(route => {
        if (!route._addedToParent) {
          const routeData = routeMap.get(route.path)
          if (routeData) {
            topLevelRoutes.push(routeData)
          }
        }
      })

      console.log('[Permission] 顶层路由数量:', topLevelRoutes.length)

      // 第四步：对 Layout 路由进行扁平化处理
      const result = []
      for (const route of topLevelRoutes) {
        if (route.component === 'Layout' && route.children) {
          const flattenedChildren = []

          for (const child of route.children) {
            // 处理孙子路由（add/edit/detail）
            if (child.children && child.children.length > 0) {
              console.log(`[Permission] 扁平化 ${child.path}, 子路由数量: ${child.children.length}`)
              // 将孙子路由添加到扁平列表
              child.children.forEach(grandChild => {
                const cleanGrandChild = { ...grandChild }
                delete cleanGrandChild.children
                flattenedChildren.push(cleanGrandChild)
              })
            }

            // 添加子路由本身（列表页），删除其 children 避免循环引用
            const cleanChild = { ...child }
            delete cleanChild.children
            flattenedChildren.push(cleanChild)
          }

          route.children = flattenedChildren
          console.log(`[Permission] Layout ${route.path} 最终 children 数量: ${flattenedChildren.length}`)
        }

        delete route._addedToParent
        result.push(route)
      }

      console.log('[Permission] 扁平化完成，最终路由数量:', result.length)
      return result
    },

    // 处理菜单列表，将 meta.hidden 提升到顶层，并过滤隐藏的菜单
    processMenuList(routes) {
      return routes.map(route => {
        const menu = { ...route }

        // 将 meta.hidden 提升到顶层
        if (menu.meta?.hidden !== undefined) {
          menu.hidden = menu.meta.hidden
        }

        // 递归处理子菜单，过滤掉隐藏的菜单
        if (menu.children && menu.children.length > 0) {
          // 先过滤再递归，避免无限循环
          const visibleChildren = menu.children.filter(child => {
            const childHidden = child.meta?.hidden !== undefined ? child.meta.hidden : child.hidden
            return !childHidden
          })
          menu.children = visibleChildren.length > 0 ? this.processMenuList(visibleChildren) : []
        }

        return menu
      }).filter(route => !route.hidden) // 只过滤顶层的隐藏路由
    },

    // 清空路由
    clearRoutes() {
      this.routes = []
      this.addRoutes = []
      this.menuList = []
      this.routesLoaded = false // 重置标志位
    }
  }
})
