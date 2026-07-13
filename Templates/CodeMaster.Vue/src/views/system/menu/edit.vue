<template>
  <div class="app-container">
    <el-card shadow="never">
      <div class="mb-20">
        <el-button @click="handleBack" :icon="ArrowLeft">{{ t('back') }}</el-button>
      </div>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="120px"
        v-loading="loading"
      >
        <el-form-item :label="t('parentMenu')" prop="parentId">
          <el-tree-select
            v-model="form.parentId"
            :data="menuTreeOptions"
            :props="{ label: 'menuName', value: 'id' }"
            check-strictly
            clearable
            :placeholder="t('pleaseSelect')"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item :label="t('menuType')" prop="menuType">
          <el-radio-group v-model="form.menuType">
            <el-radio label="M">{{ t('directory') }}</el-radio>
            <el-radio label="C">{{ t('menu') }}</el-radio>
            <el-radio label="F">{{ t('button') }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('menuName')" prop="menuName">
          <el-input v-model="form.menuName" :placeholder="t('pleaseEnter')" />
        </el-form-item>
        <el-form-item :label="t('titleKey')" prop="titleKey">
          <el-input v-model="form.titleKey" :placeholder="t('pleaseEnter')" />
        </el-form-item>
        <el-form-item :label="t('orderNum')" prop="orderNum">
          <el-input-number v-model="form.orderNum" :min="0" :max="9999" style="width: 100%" />
        </el-form-item>
        <el-form-item :label="t('icon')" prop="icon" v-if="form.menuType !== 'F'">
          <el-input v-model="form.icon" :placeholder="t('pleaseEnter')" />
        </el-form-item>
        <el-form-item :label="t('path')" prop="path" v-if="form.menuType !== 'F'">
          <el-input v-model="form.path" :placeholder="t('pleaseEnter')" />
        </el-form-item>
        <el-form-item :label="t('component')" prop="component" v-if="form.menuType === 'C'">
          <el-input v-model="form.component" :placeholder="t('pleaseEnter')" />
        </el-form-item>
        <el-form-item :label="t('perms')" prop="perms">
          <el-input v-model="form.perms" :placeholder="t('pleaseEnter')" />
        </el-form-item>
        <el-form-item :label="t('visible')" prop="visible">
          <el-radio-group v-model="form.visible">
            <el-radio :label="0">{{ t('show') }}</el-radio>
            <el-radio :label="1">{{ t('hide') }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('status')" prop="status">
          <el-radio-group v-model="form.status">
            <el-radio :label="0">{{ t('normal') }}</el-radio>
            <el-radio :label="1">{{ t('disabled') }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('isCache')" prop="isCache" v-if="form.menuType === 'C'">
          <el-radio-group v-model="form.isCache">
            <el-radio :label="0">{{ t('cache') }}</el-radio>
            <el-radio :label="1">{{ t('noCache') }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('menuScope')" prop="menuScope">
          <el-radio-group v-model="form.menuScope">
            <el-radio :label="0">{{ t('hostOnly') }}</el-radio>
            <el-radio :label="1">{{ t('tenantOnly') }}</el-radio>
            <el-radio :label="2">{{ t('shared') }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('remark')" prop="remark">
          <el-input v-model="form.remark" type="textarea" :rows="3" :placeholder="t('pleaseEnter')" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSubmit">{{ t('save') }}</el-button>
          <el-button @click="handleBack">{{ t('cancel') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ArrowLeft } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { getList, getById, update } from '@/api/system/menu'
import { buildTree } from '@/utils/tree'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()

const loading = ref(false)
const formRef = ref(null)
const menuTreeOptions = ref([])

const form = reactive({
  id: null,
  parentId: null,
  menuName: '',
  titleKey: '',
  menuType: 'M',
  orderNum: 0,
  icon: '',
  path: '',
  component: '',
  perms: '',
  visible: 0,
  status: 0,
  isCache: 0,
  menuScope: 2,
  remark: ''
})

const rules = {
  menuName: [
    { required: true, message: t2('please_input', ''), trigger: 'blur' }
  ],
  menuType: [
    { required: true, message: t2('please_select', ''), trigger: 'change' }
  ],
  orderNum: [
    { required: true, message: t2('please_input', ''), trigger: 'blur' }
  ]
}

// 获取菜单树选项
const getMenuTreeOptions = async () => {
  try {
    const data = await getList()
    menuTreeOptions.value = buildTree(data || [], { idKey: 'id', parentIdKey: 'parentId' })
  } catch (error) {
    console.error('获取菜单树失败:', error)
  }
}

// 加载菜单详情
const loadMenuDetail = async () => {
  const id = route.query.id
  if (!id) {
    ElMessage.error(t('invalidId'))
    router.push('/system/menu')
    return
  }

  loading.value = true
  try {
    const data = await getById(id)
    Object.assign(form, {
      id: data.id,
      parentId: data.parentId,
      menuName: data.menuName,
      titleKey: data.titleKey,
      menuType: data.menuType,
      orderNum: data.orderNum,
      icon: data.icon,
      path: data.path,
      component: data.component,
      perms: data.perms,
      visible: data.visible,
      status: data.status,
      isCache: data.isCache,
      menuScope: data.menuScope,
      remark: data.remark
    })
  } catch (error) {
    console.error('加载菜单详情失败:', error)
    ElMessage.error(t('loadFailed'))
  } finally {
    loading.value = false
  }
}

// 提交表单
const handleSubmit = async () => {
  try {
    await formRef.value.validate()

    await update(form.id, {
      newParentId: form.parentId,
      menuName: form.menuName,
      titleKey: form.titleKey,
      menuType: form.menuType,
      orderNum: form.orderNum,
      icon: form.icon,
      path: form.path,
      component: form.component,
      perms: form.perms,
      visible: form.visible,
      status: form.status,
      isCache: form.isCache,
      menuScope: form.menuScope,
      remark: form.remark
    })

    ElMessage.success(t('updateSuccess'))
    router.push('/system/menu')
  } catch (error) {
    if (error !== false) {
      console.error('提交失败:', error)
      ElMessage.error(t('saveFailed'))
    }
  }
}

// 返回
const handleBack = () => {
  router.push('/system/menu')
}

onMounted(async () => {
  await getMenuTreeOptions()
  await loadMenuDetail()
})
</script>

<style scoped>
.mb-20 {
  margin-bottom: 20px;
}
</style>
