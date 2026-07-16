import { defineStore } from 'pinia'

export const useTagsViewStore = defineStore('tagsView', {
  state: () => ({
    visitedViews: [], // 访问过的视图
    cachedViews: [] // 需要缓存的视图
  }),

  persist: {
    paths: ['visitedViews', 'cachedViews']
  },

  getters: {
    // 获取所有访问过的视图
    getVisitedViews: (state) => state.visitedViews,
    // 获取所有缓存的视图
    getCachedViews: (state) => state.cachedViews
  },

  actions: {
    // 添加视图
    addView(view) {
      this.addVisitedView(view)
      this.addCachedView(view)
    },

    // 添加访问过的视图
    addVisitedView(view) {
      // 检查是否已存在（包括参数）
      const exists = this.visitedViews.some(v => {
        if (v.path !== view.path) return false
        // 比较查询参数
        const vQuery = JSON.stringify(v.query || {})
        const viewQuery = JSON.stringify(view.query || {})
        return vQuery === viewQuery
      })

      if (exists) return

      this.visitedViews.push({
        name: view.name,
        path: view.path,
        title: view.meta?.title || 'no-name',
        query: view.query,
        params: view.params,
        fullPath: view.fullPath,
        meta: { ...view.meta }
      })
    },

    // 添加缓存视图
    addCachedView(view) {
      if (!view?.name) return

      // 如果路由配置了不缓存，则不添加
      if (view.meta?.noCache) {
        this.delCachedView(view)
        return
      }

      // 如果已经缓存，则不重复添加
      if (this.cachedViews.includes(view.name)) return

      this.cachedViews.push(view.name)
    },

    // 删除视图
    delView(view) {
      return new Promise((resolve) => {
        this.delVisitedView(view)
        this.delCachedView(view)
        resolve({
          visitedViews: [...this.visitedViews],
          cachedViews: [...this.cachedViews]
        })
      })
    },

    // 删除访问过的视图
    delVisitedView(view) {
      for (const [i, v] of this.visitedViews.entries()) {
        if (v.path === view.path && JSON.stringify(v.query) === JSON.stringify(view.query)) {
          this.visitedViews.splice(i, 1)
          break
        }
      }
    },

    // 删除缓存视图
    delCachedView(view) {
      const index = this.cachedViews.indexOf(view.name)
      if (index > -1) {
        this.cachedViews.splice(index, 1)
      }
    },

    // 删除其他视图
    delOthersViews(view) {
      return new Promise((resolve) => {
        this.delOthersVisitedViews(view)
        this.delOthersCachedViews(view)
        resolve({
          visitedViews: [...this.visitedViews],
          cachedViews: [...this.cachedViews]
        })
      })
    },

    // 删除其他访问过的视图
    delOthersVisitedViews(view) {
      this.visitedViews = this.visitedViews.filter(v => {
        return v.meta?.affix || (v.path === view.path && JSON.stringify(v.query) === JSON.stringify(view.query))
      })
    },

    // 删除其他缓存视图
    delOthersCachedViews(view) {
      const index = this.cachedViews.indexOf(view.name)
      if (index > -1) {
        this.cachedViews = this.cachedViews.slice(index, index + 1)
      } else {
        this.cachedViews = []
      }
    },

    // 删除所有视图
    delAllViews() {
      return new Promise((resolve) => {
        this.delAllVisitedViews()
        this.delAllCachedViews()
        resolve({
          visitedViews: [...this.visitedViews],
          cachedViews: [...this.cachedViews]
        })
      })
    },

    // 删除所有访问过的视图
    delAllVisitedViews() {
      // 保留固定的标签
      this.visitedViews = this.visitedViews.filter(tag => tag.meta?.affix)
    },

    // 删除所有缓存视图
    delAllCachedViews() {
      this.cachedViews = []
    },

    // 更新访问过的视图
    updateVisitedView(view) {
      for (let v of this.visitedViews) {
        if (v.path === view.path) {
          v = Object.assign(v, view)
          break
        }
      }
    }
  }
})
