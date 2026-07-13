<template>
  <div class="editor-preview-wrap">
    <div :id="toolbarId" class="editor-toolbar-preview"></div>
    <div :id="editorId" class="editor-content-preview"></div>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted } from 'vue'

const props = defineProps({ fieldName: { type: String, default: 'editor' } })
const toolbarId = computed(() => props.fieldName + 'Toolbar')
const editorId = computed(() => props.fieldName + 'Editor')
let editorInstance = null

onMounted(async () => {
  try {
    // 动态加载 wangeditor
    const { createEditor, createToolbar } = await import('@wangeditor/editor')
    await import('@wangeditor/editor/dist/css/style.css')

    // 等 DOM 就绪
    await new Promise(r => setTimeout(r, 100))

    editorInstance = createEditor({
      selector: '#' + editorId.value,
      html: '<p>富文本编辑器预览</p>',
      config: { placeholder: '富文本内容预览...' }
    })
    createToolbar({
      editor: editorInstance,
      selector: '#' + toolbarId.value,
      config: { excludeKeys: ['group-video'] }
    })
  } catch (e) {
    console.warn('wangeditor 加载失败:', e.message)
  }
})

onUnmounted(() => {
  if (editorInstance) { editorInstance.destroy(); editorInstance = null }
})

defineOptions({ name: 'EditorPreview' })
</script>

<style>
.editor-preview-wrap {
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  overflow: hidden;
}
.editor-toolbar-preview { border-bottom: 1px solid #eee; }
.editor-content-preview { min-height: 120px; }
</style>
