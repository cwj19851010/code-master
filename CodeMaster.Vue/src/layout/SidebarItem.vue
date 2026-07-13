<template>
  <template v-if="!item.hidden">
    <!-- 有子菜单：显示目录 -->
    <el-sub-menu v-if="item.children && item.children.length > 0" :index="resolvePath(item.path)">
      <template #title>
        <el-icon v-if="item.meta && item.meta.icon">
          <component :is="item.meta.icon" />
        </el-icon>
        <span>{{ t(item.meta?.title || '') }}</span>
      </template>

      <sidebar-item
        v-for="child in item.children"
        :key="child.path"
        :item="child"
        :base-path="resolvePath(item.path)"
      />
    </el-sub-menu>
    <!-- 无子菜单：叶子节点 -->
    <el-menu-item v-else :index="resolvePath(item.path)" @click="handleMenuClick(item)">
      <el-icon v-if="item.meta && item.meta.icon">
        <component :is="item.meta.icon" />
      </el-icon>
      <template #title>
        <span>{{ t(item.meta?.title || '') }}</span>
      </template>
    </el-menu-item>
  </template>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import path from 'path-browserify'
import { useI18n } from 'vue-i18n'

const props = defineProps({
  item: {
    type: Object,
    required: true
  },
  basePath: {
    type: String,
    default: ''
  }
})

const router = useRouter()
const onlyOneChild = ref(null)
const { t } = useI18n()

// 添加防抖标志
let isNavigating = false

const handleMenuClick = async (menuItem, event) => {
  // 阻止默认行为和事件冒泡
  if (event) {
    event.preventDefault()
    event.stopPropagation()
  }

  // 防止重复点击
  if (isNavigating) {
    return
  }

  const fullPath = resolvePath(menuItem.path)

  // 如果点击的是当前路由，不进行跳转
  if (router.currentRoute.value.path === fullPath) {
    return
  }

  try {
    isNavigating = true
    await router.push(fullPath)
  } catch (error) {
    console.error('路由跳转失败:', error)
  } finally {
    // 延迟重置标志，防止快速重复点击
    setTimeout(() => {
      isNavigating = false
    }, 500)
  }
}

const hasOneShowingChild = (children = [], parent) => {
  // 添加空值检查
  if (!children || !Array.isArray(children)) {
    children = []
  }

  const showingChildren = children.filter(item => {
    if (item.hidden) {
      return false
    } else {
      onlyOneChild.value = item
      return true
    }
  })

  if (showingChildren.length === 1) {
    return true
  }

  if (showingChildren.length === 0) {
    // 当没有可显示的子菜单时，使用父菜单本身，保留原始 path
    onlyOneChild.value = { ...parent, noShowingChildren: true }
    return true
  }

  return false
}

const resolvePath = (routePath) => {
  if (routePath.startsWith('/')) {
    return routePath
  }
  return path.resolve(props.basePath, routePath)
}
</script>
