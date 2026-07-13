<template>
  <div class="app-container">
    <el-card shadow="never">
      <template #header>
        <div class="card-header">
          <span>{{ t($route.meta.title) }}</span>
          <!-- <el-button @click="handleBack" :icon="Back">{{ t('back') }}</el-button> -->
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="120px"
        class="detail-form"
      >
        <el-form-item :label="t('name')" prop="roleName">
          <el-input v-model="form.roleName" :placeholder="t2('please_input', 'name')" />
        </el-form-item>
        <el-form-item :label="t('code')" prop="roleKey">
          <el-input v-model="form.roleKey" :placeholder="t2('please_input', 'code')" />
        </el-form-item>
        <el-form-item :label="t('sort')" prop="roleSort">
          <el-input-number v-model="form.roleSort" :min="0" :max="999" />
        </el-form-item>
        <el-form-item :label="t('status')" prop="status">
          <el-radio-group v-model="form.status">
            <el-radio :label="0">{{ t('normal') }}</el-radio>
            <el-radio :label="1">{{ t('disabled') }}</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="t('menu_permission')" prop="menuIds">
          <el-tree
            ref="menuTreeRef"
            :data="menuTree"
            :props="{ label: 'menuName', children: 'children' }"
            node-key="id"
            show-checkbox
            default-expand-all
            :check-strictly="false"
          />
        </el-form-item>
        <el-form-item :label="t('remark')" prop="remark">
          <el-input v-model="form.remark" type="textarea" :rows="3" :placeholder="t2('please_input', 'remark')" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="submitLoading">{{ t('save') }}</el-button>
          <el-button @click="handleBack">{{ t('cancel') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { resetReactiveForm, resetFormValidation } from '@/utils/pageLifecycle'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Back } from '@element-plus/icons-vue'
import { create } from '@/api/system/role'
import { getList as getMenuList } from '@/api/system/menu'
import { useI18n } from 'vue-i18n'
import { t2 } from '@/i18n'
import { buildTree } from '@/utils/tree'

const router = useRouter()
const { t } = useI18n()
const formRef = ref(null)
const menuTreeRef = ref(null)
const submitLoading = ref(false)
const menuTree = ref([])

const form = reactive({
  roleName: '',
  roleKey: '',
  roleSort: 0,
  status: 0,
  remark: '',
  menuIds: []
})

const initialForm = JSON.parse(JSON.stringify(form))

const rules = {
  roleName: [
    { required: true, message: t2('please_input', 'name'), trigger: 'blur' },
    { min: 2, max: 20, message: t2('length_range', '2_20'), trigger: 'blur' }
  ],
  roleKey: [
    { required: true, message: t2('please_input', 'code'), trigger: 'blur' },
    { min: 2, max: 20, message: t2('length_range', '2_20'), trigger: 'blur' }
  ]
}

const loadMenuTree = async () => {
  try {
    const data = await getMenuList({})
    menuTree.value = buildTree(data.items || data || [])
  } catch (error) {
    ElMessage.error(t('load_menu_failed'))
  }
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    submitLoading.value = true

    // 获取选中的菜单ID（包括半选中的父节点）
    const checkedKeys = menuTreeRef.value.getCheckedKeys()
    const halfCheckedKeys = menuTreeRef.value.getHalfCheckedKeys()
    form.menuIds = [...checkedKeys, ...halfCheckedKeys]

    await create(form)
    ElMessage.success(t('create_success'))
    resetReactiveForm(form, initialForm)
    resetFormValidation(formRef)
    router.back()
  } catch (error) {
    if (error !== false) {
      ElMessage.error(t('create_failed'))
    }
  } finally {
    submitLoading.value = false
  }
}

const handleBack = () => {
  router.back()
}

onMounted(() => {
  loadMenuTree()
})
</script>

<style scoped lang="scss">
@import '@/styles/list-page.scss';

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
