<template>
  <div class="tabs-view-container">
    <el-scrollbar class="tabs-scrollbar">
      <div class="tabs-view-wrapper">
        <div
          v-for="tag in visitedViews"
          :key="tag.fullPath"
          :class="['tabs-view-item', isActive(tag) ? 'active' : '']"
          @click="handleClickTag(tag)"
          @contextmenu.prevent="openContextMenu(tag, $event)"
        >
          <span class="tabs-view-title">{{ t(tag.title) }}</span>
          <el-icon
            v-if="!isAffix(tag)"
            class="tabs-view-close"
            @click.stop="closeSelectedTag(tag)"
          >
            <Close />
          </el-icon>
        </div>
      </div>
    </el-scrollbar>

    <!-- 右键菜单 -->
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
      <li @click="closeOthersTags">
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
import { ref, computed, watch, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useTagsViewStore } from '@/stores/tagsView'
import { useI18n } from 'vue-i18n'
import { Close, Refresh, CircleClose } from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const tagsViewStore = useTagsViewStore()
const { t } = useI18n()

const contextMenuVisible = ref(false)
const contextMenuLeft = ref(0)
const contextMenuTop = ref(0)
const selectedTag = ref({})

const visitedViews = computed(() => tagsViewStore.visitedViews)

// 判断是否是当前激活的标签
const isActive = (tag) => {
  return tag.path === route.path && JSON.stringify(tag.query) === JSON.stringify(route.query)
}

// 判断是否是固定标签
const isAffix = (tag) => {
  return tag.meta && tag.meta.affix
}

// 添加标签
const addTags = () => {
  console.log('[TabsView] addTags 被调用, route.name:', route.name, 'route.path:', route.path)
  if (route.name) {
    tagsViewStore.addView(route)
  }
}

// 点击标签
const handleClickTag = (tag) => {
  console.log('[TabsView] handleClickTag 被调用, tag.path:', tag.path)
  router.push({ path: tag.path, query: tag.query })
}

// 关闭选中的标签
const closeSelectedTag = (tag) => {
  tagsViewStore.delView(tag).then(({ visitedViews }) => {
    if (isActive(tag)) {
      toLastView(visitedViews, tag)
    }
  })
}

// 刷新选中的标签
const refreshSelectedTag = (tag) => {
  tagsViewStore.delCachedView(tag).then(() => {
    nextTick(() => {
      router.replace({
        path: '/redirect' + tag.path,
        query: tag.query
      })
    })
  })
}

// 关闭其他标签
const closeOthersTags = () => {
  router.push(selectedTag.value)
  tagsViewStore.delOthersViews(selectedTag.value)
}

// 关闭所有标签
const closeAllTags = () => {
  tagsViewStore.delAllViews().then(({ visitedViews }) => {
    toLastView(visitedViews)
  })
}

// 跳转到最后一个视图
const toLastView = (visitedViews, view) => {
  const latestView = visitedViews.slice(-1)[0]
  if (latestView) {
    router.push(latestView.fullPath)
  } else {
    // 如果没有标签了，跳转到首页
    if (view.name === 'Dashboard') {
      router.replace({ path: '/redirect' + view.path })
    } else {
      router.push('/')
    }
  }
}

// 打开右键菜单
const openContextMenu = (tag, e) => {
  const menuMinWidth = 105
  const offsetLeft = e.currentTarget.getBoundingClientRect().left
  const offsetWidth = e.currentTarget.offsetWidth
  const maxLeft = window.innerWidth - menuMinWidth

  const left = offsetLeft + offsetWidth + 15
  if (left > maxLeft) {
    contextMenuLeft.value = maxLeft
  } else {
    contextMenuLeft.value = left
  }

  contextMenuTop.value = e.currentTarget.getBoundingClientRect().bottom + 5
  contextMenuVisible.value = true
  selectedTag.value = tag
}

// 关闭右键菜单
const closeContextMenu = () => {
  contextMenuVisible.value = false
}

// 初始化固定标签
const initAffixTags = () => {
  // 添加 dashboard 固定标签
  const dashboardRoute = {
    path: '/dashboard',
    name: 'Dashboard',
    fullPath: '/dashboard',
    meta: { title: 'dashboard', icon: 'HomeFilled', affix: true },
    query: {},
    params: {}
  }
  tagsViewStore.addView(dashboardRoute)
}

// 组件挂载时初始化固定标签
initAffixTags()

// 监听路由变化
watch(
  () => route.fullPath,
  () => {
    addTags()
  },
  { immediate: true }
)

// 监听点击事件，关闭右键菜单
watch(contextMenuVisible, (value) => {
  if (value) {
    document.body.addEventListener('click', closeContextMenu)
  } else {
    document.body.removeEventListener('click', closeContextMenu)
  }
})
</script>

<style scoped lang="scss">
.tabs-view-container {
  height: 40px;
  background: #fff;
  border-bottom: 1px solid #d8dce5;
  box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.12);
  position: relative;

  .tabs-scrollbar {
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
    padding: 0 10px;
    gap: 5px;
  }

  .tabs-view-item {
    display: inline-flex;
    align-items: center;
    height: 28px;
    padding: 0 12px;
    font-size: 12px;
    color: #495060;
    background: #fff;
    border: 1px solid #d8dce5;
    border-radius: 3px;
    cursor: pointer;
    white-space: nowrap;
    transition: all 0.3s;

    &:hover {
      color: #409eff;
      background: #ecf5ff;
      border-color: #b3d8ff;
    }

    &.active {
      color: #fff;
      background: #409eff;
      border-color: #409eff;

      .tabs-view-close {
        color: #fff;

        &:hover {
          background: rgba(255, 255, 255, 0.3);
        }
      }
    }

    .tabs-view-title {
      margin-right: 5px;
    }

    .tabs-view-close {
      width: 14px;
      height: 14px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 12px;
      transition: all 0.3s;

      &:hover {
        background: #e8eaec;
      }
    }
  }

  .context-menu {
    position: fixed;
    background: #fff;
    border-radius: 4px;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
    z-index: 3000;
    list-style: none;
    padding: 5px 0;
    margin: 0;
    font-size: 12px;
    color: #333;

    li {
      display: flex;
      align-items: center;
      padding: 8px 16px;
      cursor: pointer;
      gap: 8px;

      &:hover {
        background: #ecf5ff;
        color: #409eff;
      }

      .el-icon {
        font-size: 14px;
      }
    }
  }
}
</style>
