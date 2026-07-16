import { defineStore } from 'pinia'

const normalizeQuery = (query = {}) => {
  const result = {}
  Object.keys(query || {}).sort().forEach(key => {
    const value = query[key]
    if (value !== undefined) {
      result[key] = value
    }
  })
  return result
}

const getViewKey = (view = {}) => {
  const query = normalizeQuery(view.query)
  return `${view.path || ''}?${JSON.stringify(query)}`
}

const toTagView = (view) => ({
  name: view.name,
  path: view.path,
  title: view.meta?.title || view.title || 'no-name',
  query: normalizeQuery(view.query),
  params: view.params || {},
  fullPath: view.fullPath || view.path,
  meta: { ...(view.meta || {}) }
})

export const useTagsViewStore = defineStore('tagsView', {
  state: () => ({
    visitedViews: [],
    cachedViews: []
  }),

  persist: {
    paths: ['visitedViews', 'cachedViews']
  },

  getters: {
    getVisitedViews: (state) => state.visitedViews,
    getCachedViews: (state) => state.cachedViews
  },

  actions: {
    addView(view) {
      this.addVisitedView(view)
      this.addCachedView(view)
    },

    addVisitedView(view) {
      if (!view?.path || view.meta?.hideTab) return

      const tagView = toTagView(view)
      const exists = this.visitedViews.some(item => getViewKey(item) === getViewKey(tagView))
      if (exists) return

      this.visitedViews.push(tagView)
    },

    addCachedView(view) {
      if (!view?.name) return
      if (view?.meta?.noCache) {
        this.delCachedView(view)
        return
      }
      if (!this.cachedViews.includes(view.name)) {
        this.cachedViews.push(view.name)
      }
    },

    delView(view) {
      this.delVisitedView(view)
      this.delCachedView(view)
      return Promise.resolve(this.getSnapshot())
    },

    delVisitedView(view) {
      const targetKey = getViewKey(view)
      this.visitedViews = this.visitedViews.filter(item => getViewKey(item) !== targetKey)
    },

    delCachedView(view) {
      if (!view?.name) return Promise.resolve(this.getSnapshot())

      const index = this.cachedViews.indexOf(view.name)
      if (index > -1) {
        this.cachedViews.splice(index, 1)
      }

      return Promise.resolve(this.getSnapshot())
    },

    delOthersViews(view) {
      this.delOthersVisitedViews(view)
      this.delOthersCachedViews(view)
      return Promise.resolve(this.getSnapshot())
    },

    delOthersVisitedViews(view) {
      const targetKey = getViewKey(view)
      this.visitedViews = this.visitedViews.filter(item => item.meta?.affix || getViewKey(item) === targetKey)
    },

    delOthersCachedViews(view) {
      this.cachedViews = view?.name && this.cachedViews.includes(view.name) ? [view.name] : []
    },

    delAllViews() {
      this.delAllVisitedViews()
      this.delAllCachedViews()
      return Promise.resolve(this.getSnapshot())
    },

    delAllVisitedViews() {
      this.visitedViews = this.visitedViews.filter(item => item.meta?.affix)
    },

    delAllCachedViews() {
      this.cachedViews = []
    },

    updateVisitedView(view) {
      const targetKey = getViewKey(view)
      const index = this.visitedViews.findIndex(item => getViewKey(item) === targetKey)
      if (index > -1) {
        this.visitedViews[index] = { ...this.visitedViews[index], ...toTagView(view) }
      }
    },

    getSnapshot() {
      return {
        visitedViews: [...this.visitedViews],
        cachedViews: [...this.cachedViews]
      }
    }
  }
})
