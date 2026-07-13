<template>
  <div class="tabs-view-container">
    <button class="tabs-nav-button" type="button" title="Scroll left" @click="scrollTabs(-240)">
      <el-icon><ArrowLeft /></el-icon>
    </button>

    <el-scrollbar ref="scrollbarRef" class="tabs-scrollbar">
      <div ref="tabsWrapperRef" class="tabs-view-wrapper">
        <div
          v-for="tag in visitedViews"
          :key="getTagKey(tag)"
          :class="['tabs-view-item', isActive(tag) ? 'active' : '']"
          @click="handleClickTag(tag)"
          @contextmenu.prevent="openContextMenu(tag, $event)"
        >
          <span class="tabs-view-title">{{ t(tag.title) }}</span>
          <button
            v-if="!isAffix(tag)"
            class="tabs-view-close"
            type="button"
            title="Close"
            @click.stop="closeSelectedTag(tag)"
          >
            <el-icon><Close /></el-icon>
          </button>
        </div>
      </div>
    </el-scrollbar>

    <div class="tabs-actions">
      <button class="tabs-action-button" type="button" title="Scroll right" @click="scrollTabs(240)">
        <el-icon><ArrowRight /></el-icon>
      </button>
      <button class="tabs-action-button" type="button" title="Refresh" @click="refreshSelectedTag(activeTag)">
        <el-icon><Refresh /></el-icon>
      </button>
      <button
        class="tabs-action-button"
        type="button"
        title="Close current"
        :disabled="!activeTag || isAffix(activeTag)"
        @click="closeSelectedTag(activeTag)"
      >
        <el-icon><Close /></el-icon>
      </button>
      <el-dropdown trigger="click" @command="handleActionCommand">
        <button class="tabs-action-button" type="button" title="More">
          <el-icon><MoreFilled /></el-icon>
        </button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="refresh">
              <el-icon><Refresh /></el-icon>
              {{ t('refresh') }}
            </el-dropdown-item>
            <el-dropdown-item command="close-current" :disabled="!activeTag || isAffix(activeTag)">
              <el-icon><Close /></el-icon>
              {{ t('close') }}
            </el-dropdown-item>
            <el-dropdown-item command="close-others">
              <el-icon><CircleClose /></el-icon>
              {{ t('closeOthers') }}
            </el-dropdown-item>
            <el-dropdown-item command="close-all">
              <el-icon><CircleClose /></el-icon>
              {{ t('closeAll') }}
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>

    <ul
      v-show="contextMenuVisible"
      :style="{ left: contextMenuLeft + 'px', top: contextMenuTop + 'px' }"
      class="context-menu"
    >
      <li @click="refreshSelectedTag(selectedTag)">
        <el-icon><Refresh /></el-icon>
        {{ t('refresh') }}
      </li>
      <li v-if="!isAffix(selectedTag)" @click="closeSelectedTag(selectedTag)">
        <el-icon><Close /></el-icon>
        {{ t('close') }}
      </li>
      <li @click="closeOthersTags(selectedTag)">
        <el-icon><CircleClose /></el-icon>
        {{ t('closeOthers') }}
      </li>
      <li @click="closeAllTags">
        <el-icon><CircleClose /></el-icon>
        {{ t('closeAll') }}
      </li>
    </ul>
  </div>
</template>

<script setup>
import { ref, computed, watch, nextTick, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useTagsViewStore } from '@/stores/tagsView'
import { useI18n } from 'vue-i18n'
import { ArrowLeft, ArrowRight, CircleClose, Close, MoreFilled, Refresh } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const tagsViewStore = useTagsViewStore()
const { t } = useI18n()

const scrollbarRef = ref(null)
const tabsWrapperRef = ref(null)
const contextMenuVisible = ref(false)
const contextMenuLeft = ref(0)
const contextMenuTop = ref(0)
const selectedTag = ref({})

const visitedViews = computed(() => tagsViewStore.visitedViews)
const activeTag = computed(() => visitedViews.value.find(tag => isActive(tag)) || visitedViews.value[0])

const normalizeQuery = (query = {}) => {
  const result = {}
  Object.keys(query || {}).sort().forEach(key => {
    const value = query[key]
    if (value !== undefined) result[key] = value
  })
  return result
}

const getTagKey = (tag = {}) => `${tag.path || ''}?${JSON.stringify(normalizeQuery(tag.query))}`
const isSameTag = (a, b) => getTagKey(a) === getTagKey(b)
const isActive = (tag) => tag.path === route.path && JSON.stringify(normalizeQuery(tag.query)) === JSON.stringify(normalizeQuery(route.query))
const isAffix = (tag) => Boolean(tag?.meta?.affix)

const getRouteLocation = (tag) => ({
  path: tag.path,
  query: tag.query || {},
  params: tag.params || {}
})

const addTags = () => {
  if (route.name) {
    tagsViewStore.addView(route)
  }
}

const handleClickTag = (tag) => {
  router.push(getRouteLocation(tag))
}

const closeSelectedTag = async (tag) => {
  if (!tag || isAffix(tag)) return

  const isCurrent = isActive(tag)
  const tagIndex = visitedViews.value.findIndex(item => isSameTag(item, tag))
  const { visitedViews: restViews } = await tagsViewStore.delView(tag)

  if (isCurrent) {
    toNearestView(restViews, tagIndex)
  }
}

const refreshSelectedTag = async (tag) => {
  if (!tag?.path) return

  await tagsViewStore.delCachedView(tag)
  await nextTick()
  closeContextMenu()

  router.replace({
    path: `/redirect${tag.path}`,
    query: tag.query || {}
  })
}

const closeOthersTags = async (tag = activeTag.value) => {
  if (!tag?.path) return

  await router.push(getRouteLocation(tag))
  await tagsViewStore.delOthersViews(tag)
  closeContextMenu()
}

const closeAllTags = async () => {
  const { visitedViews: restViews } = await tagsViewStore.delAllViews()
  closeContextMenu()
  toNearestView(restViews, 0)
}

const toNearestView = (views, closedIndex = 0) => {
  const nextView = views[Math.min(closedIndex, views.length - 1)] || views[views.length - 1]

  if (nextView) {
    router.push(getRouteLocation(nextView))
    return
  }

  router.push('/')
}

const handleActionCommand = (command) => {
  const handlers = {
    refresh: () => refreshSelectedTag(activeTag.value),
    'close-current': () => closeSelectedTag(activeTag.value),
    'close-others': () => closeOthersTags(activeTag.value),
    'close-all': closeAllTags
  }

  handlers[command]?.()
}

const scrollTabs = (distance) => {
  const wrap = scrollbarRef.value?.wrapRef
  if (!wrap) return
  scrollbarRef.value.setScrollLeft(wrap.scrollLeft + distance)
}

const moveToCurrentTag = () => {
  nextTick(() => {
    const wrapper = tabsWrapperRef.value
    const scrollbar = scrollbarRef.value
    const wrap = scrollbar?.wrapRef
    if (!wrapper || !wrap || !scrollbar) return

    const activeEl = wrapper.querySelector('.tabs-view-item.active')
    if (!activeEl) return

    const activeLeft = activeEl.offsetLeft
    const activeRight = activeLeft + activeEl.offsetWidth
    const visibleLeft = wrap.scrollLeft
    const visibleRight = visibleLeft + wrap.clientWidth

    if (activeLeft < visibleLeft) {
      scrollbar.setScrollLeft(activeLeft - 12)
    } else if (activeRight > visibleRight) {
      scrollbar.setScrollLeft(activeRight - wrap.clientWidth + 12)
    }
  })
}

const openContextMenu = (tag, event) => {
  const menuWidth = 132
  const maxLeft = window.innerWidth - menuWidth - 8
  const left = Math.min(event.clientX, maxLeft)

  contextMenuLeft.value = Math.max(left, 8)
  contextMenuTop.value = event.clientY + 4
  contextMenuVisible.value = true
  selectedTag.value = tag
}

const closeContextMenu = () => {
  contextMenuVisible.value = false
}

const initAffixTags = () => {
  tagsViewStore.addView({
    path: '/dashboard',
    name: 'Dashboard',
    fullPath: '/dashboard',
    query: {},
    params: {},
    meta: { title: 'dashboard', icon: 'HomeFilled', affix: true }
  })
}

initAffixTags()

watch(
  () => route.fullPath,
  () => {
    addTags()
    moveToCurrentTag()
  },
  { immediate: true }
)

watch(contextMenuVisible, (visible) => {
  if (visible) {
    document.body.addEventListener('click', closeContextMenu)
  } else {
    document.body.removeEventListener('click', closeContextMenu)
  }
})

onBeforeUnmount(() => {
  document.body.removeEventListener('click', closeContextMenu)
})
</script>

<style scoped lang="scss">
.tabs-view-container {
  height: 40px;
  display: flex;
  align-items: center;
  background: var(--app-surface, #fff);
  border-bottom: 1px solid var(--app-border, #d8dce5);
  box-shadow: var(--app-header-shadow, 0 1px 3px 0 rgba(0, 0, 0, 0.08));
  position: relative;
  transition: background-color 0.2s ease, border-color 0.2s ease;
}

.tabs-scrollbar {
  flex: 1;
  min-width: 0;
  height: 100%;

  :deep(.el-scrollbar__wrap) {
    overflow-x: auto !important;
    overflow-y: hidden;
  }

  :deep(.el-scrollbar__view) {
    height: 100%;
  }
}

.tabs-view-wrapper {
  display: flex;
  align-items: center;
  height: 100%;
  width: max-content;
  min-width: 100%;
  padding: 0 8px;
  gap: 6px;
}

.tabs-view-item {
  display: inline-flex;
  align-items: center;
  max-width: 150px;
  height: 28px;
  padding: 0 8px 0 12px;
  font-size: 12px;
  color: var(--app-text-muted, #495060);
  background: var(--app-surface, #fff);
  border: 1px solid var(--app-border, #d8dce5);
  border-radius: 6px;
  cursor: pointer;
  white-space: nowrap;
  transition: color 0.2s ease, background-color 0.2s ease, border-color 0.2s ease;

  &:hover {
    color: var(--app-primary, #409eff);
    background: rgba(var(--app-primary-rgb, 37, 99, 235), 0.08);
    border-color: rgba(var(--app-primary-rgb, 37, 99, 235), 0.24);

    .tabs-view-close {
      opacity: 1;
    }
  }

  &.active {
    color: #fff;
    background: var(--app-primary, #409eff);
    border-color: var(--app-primary, #409eff);

    .tabs-view-close {
      color: #fff;
      opacity: 1;

      &:hover {
        background: rgba(255, 255, 255, 0.3);
      }
    }
  }
}

.tabs-view-title {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tabs-view-close,
.tabs-action-button,
.tabs-nav-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: 0;
  background: transparent;
  cursor: pointer;
}

.tabs-view-close {
  width: 16px;
  height: 16px;
  flex: 0 0 16px;
  margin-left: 6px;
  color: currentColor;
  border-radius: 50%;
  opacity: 0.74;
  transition: all 0.2s ease;

  &:hover {
    background: rgba(var(--app-primary-rgb, 37, 99, 235), 0.12);
  }
}

.tabs-actions {
  height: 100%;
  display: flex;
  align-items: center;
  gap: 2px;
  padding: 0 10px 0 8px;
  border-left: 1px solid var(--app-border, #d8dce5);
  background: var(--app-surface, #fff);
  box-shadow: -8px 0 14px rgba(15, 23, 42, 0.04);
  z-index: 1;
}

.tabs-action-button,
.tabs-nav-button {
  width: 30px;
  height: 30px;
  color: var(--app-text-muted, #64748b);
  border-radius: 7px;
  transition: all 0.2s ease;

  &:hover:not(:disabled) {
    color: var(--app-primary, #409eff);
    background: rgba(var(--app-primary-rgb, 37, 99, 235), 0.08);
  }

  &:disabled {
    cursor: not-allowed;
    opacity: 0.38;
  }
}

.tabs-nav-button {
  flex: 0 0 32px;
  border-right: 1px solid var(--app-border, #d8dce5);
  border-radius: 0;
}

.context-menu {
  position: fixed;
  background: var(--app-surface, #fff);
  border: 1px solid var(--app-border, #d8dce5);
  border-radius: 8px;
  box-shadow: var(--app-card-shadow, 0 2px 12px 0 rgba(0, 0, 0, 0.1));
  z-index: 3000;
  list-style: none;
  padding: 5px 0;
  margin: 0;
  font-size: 12px;
  color: var(--app-text, #333);

  li {
    display: flex;
    align-items: center;
    min-width: 132px;
    padding: 8px 14px;
    cursor: pointer;
    gap: 8px;

    &:hover {
      background: rgba(var(--app-primary-rgb, 37, 99, 235), 0.08);
      color: var(--app-primary, #409eff);
    }

    .el-icon {
      font-size: 14px;
    }
  }
}
</style>
