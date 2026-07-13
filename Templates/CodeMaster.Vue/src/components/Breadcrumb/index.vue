<template>
  <el-breadcrumb class="app-breadcrumb" separator="/">
    <transition-group name="breadcrumb">
      <el-breadcrumb-item v-for="(item, index) in breadcrumbs" :key="item.path">
        <span
          v-if="item.redirect === 'noRedirect' || index === breadcrumbs.length - 1"
          class="no-redirect"
        >
          {{ t(item.meta.title) }}
        </span>
        <a v-else @click.prevent="handleLink(item)">
          {{ t(item.meta.title) }}
        </a>
      </el-breadcrumb-item>
    </transition-group>
  </el-breadcrumb>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const breadcrumbs = ref([])

const getBreadcrumb = () => {
  console.log('[Breadcrumb] getBreadcrumb 被调用, route.path:', route.path)
  let matched = route.matched.filter(item => item.meta && item.meta.title)
  console.log('[Breadcrumb] matched routes:', matched.length)

  // 如果不是首页，添加首页到面包屑
  const first = matched[0]
  if (!isDashboard(first)) {
    console.log('[Breadcrumb] 不是 dashboard，添加 dashboard 到面包屑')
    matched = [{ path: '/dashboard', meta: { title: 'dashboard' }, redirect: 'noRedirect' }].concat(matched)
  }

  breadcrumbs.value = matched.filter(item => {
    return item.meta && item.meta.title && item.meta.breadcrumb !== false
  })
  console.log('[Breadcrumb] 最终面包屑数量:', breadcrumbs.value.length)
}

const isDashboard = (route) => {
  const name = route && route.name
  if (!name) {
    return false
  }
  return name.trim().toLowerCase() === 'dashboard'
}

const handleLink = (item) => {
  console.log('[Breadcrumb] handleLink 被调用, item:', item)
  const { redirect, path } = item
  if (redirect) {
    console.log('[Breadcrumb] 有 redirect，跳转到:', redirect)
    router.push(redirect)
    return
  }
  console.log('[Breadcrumb] 无 redirect，跳转到:', path)
  router.push(path)
}

watch(
  () => route.path,
  () => {
    getBreadcrumb()
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
.app-breadcrumb {
  display: inline-block;
  font-size: 14px;
  line-height: 50px;
  margin-left: 8px;

  .no-redirect {
    color: #97a8be;
    cursor: text;
  }

  a {
    color: #409eff;
    font-weight: normal;
    cursor: pointer;
    text-decoration: none;

    &:hover {
      color: #66b1ff;
    }
  }
}

.breadcrumb-enter-active,
.breadcrumb-leave-active {
  transition: all 0.3s;
}

.breadcrumb-enter-from,
.breadcrumb-leave-active {
  opacity: 0;
  transform: translateX(20px);
}

.breadcrumb-leave-active {
  position: absolute;
}
</style>
