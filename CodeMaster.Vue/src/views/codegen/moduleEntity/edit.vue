<template>
  <div class="app-container">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <div class="card-header">
          <span>编辑模块实体</span>
        </div>
      </template>

      <el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="所属项目" prop="projectId">
              <el-select v-model="form.projectId" placeholder="请选择项目" style="width: 100%" @change="handleProjectChange">
                <el-option v-for="p in projectList" :key="p.id" :label="getProjectOptionLabel(p)" :value="p.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="所属模块" prop="moduleId">
              <el-select v-model="form.moduleId" placeholder="请选择模块" style="width: 100%">
                <el-option v-for="m in moduleList" :key="m.id" :label="m.moduleDescription || m.moduleName" :value="m.id" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="实体名称" prop="name">
              <el-input v-model="form.name" placeholder="如：Customer" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="实体描述" prop="description">
              <el-input v-model="form.description" placeholder="如：客户" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="前端路由">
              <el-input v-model="form.frontendRoute" placeholder="如：/system/customer" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="排序">
              <el-input-number v-model="form.orderNum" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="选项">
              <el-checkbox v-model="form.hasPrimaryKey">主键</el-checkbox>
              <el-checkbox v-model="form.isTree">树形</el-checkbox>
              <el-checkbox v-model="form.isReadOnly">只读</el-checkbox>
              <el-checkbox v-model="form.hasDataPermission">数据权限</el-checkbox>
              <el-checkbox v-model="form.hasTenant">多租户</el-checkbox>
              <el-checkbox v-model="form.hasAudit">审计</el-checkbox>
              <el-checkbox v-model="form.hasSoftDelete">软删除</el-checkbox>
              <el-checkbox v-model="form.isChildTable">子表（不生成前端页面）</el-checkbox>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
    </el-card>

    <!-- 字段列表 -->
    <el-card shadow="never" style="margin-top: 20px">
      <template #header>
        <div class="card-header">
          <span>字段列表</span>
          <el-button type="primary" size="small" @click="handleAddField">新增字段</el-button>
        </div>
      </template>

      <!-- 字段筛选 -->
      <el-row :gutter="12" style="margin-bottom: 12px">
        <el-col :span="4">
          <el-select v-model="filterDeleted" placeholder="是否已删除" clearable style="width: 100%" size="small">
            <el-option label="全部" :value="null" />
            <el-option label="正常" :value="false" />
            <el-option label="已删除" :value="true" />
          </el-select>
        </el-col>
        <el-col :span="4">
          <el-select v-model="filterSystem" placeholder="是否系统" clearable style="width: 100%" size="small">
            <el-option label="全部" :value="null" />
            <el-option label="系统字段" :value="true" />
            <el-option label="自定义字段" :value="false" />
          </el-select>
        </el-col>
        <el-col :span="4">
          <el-input v-model="filterName" placeholder="字段名" clearable size="small" />
        </el-col>
        <el-col :span="4">
          <el-input v-model="filterDesc" placeholder="描述" clearable size="small" />
        </el-col>
      </el-row>

      <el-table
        ref="fieldTableRef"
        :data="displayFields"
        border
        style="width: 100%"
        size="small"
        :row-class-name="getFieldRowClassName"
      >
        <el-table-column label="" width="44" align="center">
          <template #default="{ row }">
            <span
              class="field-drag-handle"
              :class="{ disabled: row._deleted }"
              title="拖拽调整字段顺序"
              @mousedown.prevent="handleFieldPointerStart(row, $event)"
            >⋮⋮</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="字段名" width="130" />
        <el-table-column prop="description" label="描述" width="120" />
        <el-table-column prop="dataType" label="类型" width="90" />
        <el-table-column prop="formControlType" label="控件" width="110" />
        <el-table-column label="显示" width="200">
          <template #default="{ row }">
            <el-tag v-if="row.showInList" size="small" class="mx-1">列表</el-tag>
            <el-tag v-if="row.showInAddForm" size="small" type="success" class="mx-1">新增</el-tag>
            <el-tag v-if="row.showInEditForm" size="small" type="warning" class="mx-1">编辑</el-tag>
            <el-tag v-if="row.showInSearch" size="small" type="info" class="mx-1">搜索</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="标记" width="220">
          <template #default="{ row }">
            <el-tag v-if="row.isPrimaryKey" size="small" type="danger">主键</el-tag>
            <el-tag v-if="row.isRequired" size="small" type="warning">表单必填</el-tag>
            <el-tag v-if="row.isNullable" size="small" type="success">数据库可空</el-tag>
            <el-tag v-if="row.isSystemField" size="small" type="info">系统</el-tag>
            <el-tag v-if="row.isMultiple" size="small">多选</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="70">
          <template #default="{ row }">
            <el-tag v-if="row._status === 'new'" size="small" type="success">新增</el-tag>
            <el-tag v-else-if="row._status === 'modified'" size="small" type="warning">修改</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row, $index }">
            <el-button link type="primary" size="small" @click="handleEditField(row, $index)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDeleteField(row, $index)" :disabled="row.isSystemField">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 一对一组成关系 -->
    <el-card shadow="never" style="margin-top: 20px">
      <template #header>
        <div class="card-header">
          <div>
            <span>一对一组成关系</span>
            <span class="section-hint">组成实体随当前实体一起查询和保存，原有一对多关系不受影响</span>
          </div>
          <el-button type="primary" size="small" @click="handleAddEntityRelation">新增关系</el-button>
        </div>
      </template>

      <el-table :data="displayEntityRelations" border style="width: 100%" size="small">
        <el-table-column prop="relationName" label="属性名称" min-width="130" />
        <el-table-column label="组成实体" min-width="180">
          <template #default="{ row }">
            {{ row.targetEntityName || getEntityName(row.targetEntityId) }}
            <span v-if="row.targetEntityDescription" class="muted-text">（{{ row.targetEntityDescription }}）</span>
          </template>
        </el-table-column>
        <el-table-column label="字段对应" min-width="210">
          <template #default="{ row }">{{ row.sourceField }} → {{ row.targetField }}</template>
        </el-table-column>
        <el-table-column label="必填" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isRequired ? 'success' : 'info'" size="small">{{ row.isRequired ? '是' : '否' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="空值处理" width="110">
          <template #default="{ row }">{{ getDeleteBehaviorLabel(row.deleteBehavior) }}</template>
        </el-table-column>
        <el-table-column prop="orderNum" label="排序" width="80" />
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag v-if="row._status === 'new'" size="small" type="success">新增</el-tag>
            <el-tag v-else-if="row._status === 'modified'" size="small" type="warning">修改</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row, $index }">
            <el-button link type="primary" size="small" @click="handleEditEntityRelation(row, $index)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDeleteEntityRelation(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="displayEntityRelations.length === 0" description="暂无一对一组成关系" :image-size="60" />
    </el-card>

    <!-- 一对多关系 -->
    <el-card shadow="never" style="margin-top: 20px">
      <template #header>
        <div class="card-header">
          <span>一对多关系</span>
          <el-button type="primary" size="small" @click="handleAddRelation">新增关系</el-button>
        </div>
      </template>

      <el-table :data="displayRelations" border style="width: 100%" size="small">
        <el-table-column prop="masterField" label="主表字段" width="150" />
        <el-table-column prop="childEntityName" label="子表名" width="150" />
        <el-table-column prop="childForeignKey" label="子表外键" width="150" />
        <el-table-column label="状态" width="80">
          <template #default="{ row }">
            <el-tag v-if="row._status === 'new'" size="small" type="success">新增</el-tag>
            <el-tag v-else-if="row._status === 'modified'" size="small" type="warning">修改</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row, $index }">
            <el-button link type="primary" size="small" @click="handleEditRelation(row, $index)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDeleteRelation(row, $index)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 提交 -->
    <div style="margin-top: 20px; text-align: center">
      <el-button type="primary" @click="handleSubmit" :loading="submitLoading">保存</el-button>
      <el-button @click="handleBack">取消</el-button>
    </div>

    <!-- 字段编辑弹框 -->
    <el-dialog v-model="fieldDialogVisible" :title="fieldDialogTitle" width="900px" @close="handleFieldDialogClose">
      <el-form ref="fieldFormRef" :model="fieldForm" :rules="fieldRules" label-width="120px">
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="字段名" prop="name">
              <el-input v-model="fieldForm.name" placeholder="如 UserName" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="描述" prop="description">
              <el-input v-model="fieldForm.description" placeholder="如 用户名" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="数据类型" prop="dataType">
              <el-select v-model="fieldForm.dataType" style="width: 100%">
                <el-option label="string" value="string" />
                <el-option label="int" value="int" />
                <el-option label="long" value="long" />
                <el-option label="decimal" value="decimal" />
                <el-option label="bool" value="bool" />
                <el-option label="DateTime" value="DateTime" />
                <el-option label="double" value="double" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="控件类型" prop="formControlType">
              <el-select v-model="fieldForm.formControlType" style="width: 100%">
                <el-option label="单行文本" value="input" />
                <el-option label="多行文本" value="textarea" />
                <el-option label="数字" value="number" />
                <el-option label="下拉选择" value="select" />
                <el-option label="开关" value="switch" />
                <el-option label="复选框" value="checkbox" />
                <el-option label="复选框组" value="checkbox-group" />
                <el-option label="单选按钮组" value="radio-group" />
                <el-option label="日期" value="date" />
                <el-option label="日期时间" value="datetime" />
                <el-option label="附件上传" value="file" />
                <el-option label="图片上传" value="image" />
                <el-option label="富文本" value="editor" />
                <el-option label="关联表选择" value="select-table" />
                <el-option label="级联选择" value="cascader" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="最大长度">
              <el-input-number v-model="fieldForm.maxLength" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="排序">
              <el-input-number v-model="fieldForm.orderNum" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="字段选项">
              <el-checkbox v-model="fieldForm.isPrimaryKey">主键</el-checkbox>
              <el-checkbox v-model="fieldForm.isRequired">表单/接口必填</el-checkbox>
              <el-checkbox v-model="fieldForm.isNullable">数据库可空</el-checkbox>
              <el-checkbox v-model="fieldForm.isIgnore">忽略映射</el-checkbox>
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 验证规则 -->
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="最小值">
              <el-input v-model="fieldForm.minValue" placeholder="如 0" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="最大值">
              <el-input v-model="fieldForm.maxValue" placeholder="如 100" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="验证类型">
              <el-checkbox v-model="fieldForm.isEmail" style="margin-right:12px">邮箱</el-checkbox>
              <el-checkbox v-model="fieldForm.isPhone">手机号</el-checkbox>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="16">
            <el-form-item label="正则表达式">
              <el-input v-model="fieldForm.regexPattern" placeholder="如 ^[A-Z]{3}-\d{4}$" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="显示配置">
              <el-checkbox v-model="fieldForm.showInList">列表</el-checkbox>
              <el-checkbox v-model="fieldForm.showInDetail">详情</el-checkbox>
              <el-checkbox v-model="fieldForm.showInAddForm">新增表单</el-checkbox>
              <el-checkbox v-model="fieldForm.showInEditForm">编辑表单</el-checkbox>
              <el-checkbox v-model="fieldForm.showInSearch">搜索条件</el-checkbox>
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 数据源配置：select / checkbox-group / radio-group -->
        <template v-if="['select', 'checkbox-group', 'radio-group'].includes(fieldForm.formControlType)">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="数据源">
                <el-select v-model="fieldForm.selectDataSource" style="width: 100%">
                  <el-option label="枚举" value="enum" />
                  <el-option label="字典" value="dict" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="16">
              <el-form-item label="选项数据">
                <el-select v-if="fieldForm.selectDataSource === 'dict'" v-model="fieldForm.selectOptions" placeholder="选择字典类型" style="width: 100%">
                  <el-option v-for="item in dictTypeList" :key="item.dictType" :label="`${item.dictName} (${item.dictType})`" :value="item.dictType" />
                </el-select>
                <el-input v-else v-model="fieldForm.selectOptions" placeholder='JSON 如 [{"value":1,"label":"男"}]' />
              </el-form-item>
            </el-col>
          </el-row>
        </template>

        <!-- 多选开关：checkbox-group / select / select-table / cascader -->
        <template v-if="['select', 'checkbox-group', 'select-table', 'cascader'].includes(fieldForm.formControlType)">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="是否多选">
                <el-switch v-model="fieldForm.isMultiple" :disabled="fieldForm.formControlType === 'checkbox-group'" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row v-if="fieldForm.formControlType === 'select-table' && fieldForm.relatedEntityName">
            <el-col :span="24">
              <el-form-item label="选择结果映射">
                <div class="mapping-editor">
                  <div class="mapping-toolbar">
                    <span class="form-help">选择一条数据后，把关联实体字段复制到当前实体的普通字段中。</span>
                    <el-button size="small" type="primary" plain @click="handleAddResultMapping">新增映射</el-button>
                  </div>
                  <el-table :data="resultMappingRows" border size="small" empty-text="未配置时保持原 select-table 行为">
                    <el-table-column label="来源字段" min-width="180">
                      <template #default="{ row }">
                        <el-select v-model="row.sourceField" placeholder="关联实体字段" style="width: 100%">
                          <el-option v-for="f in relatedEntityFields" :key="f.name" :label="`${f.name}（${f.description}）`" :value="f.name" />
                        </el-select>
                      </template>
                    </el-table-column>
                    <el-table-column label="写入当前字段" min-width="180">
                      <template #default="{ row }">
                        <el-select v-model="row.targetField" placeholder="当前实体字段" style="width: 100%">
                          <el-option v-for="f in mappingTargetFieldOptions" :key="f.name" :label="`${f.name}（${f.description}）`" :value="f.name" />
                        </el-select>
                      </template>
                    </el-table-column>
                    <el-table-column label="操作" width="80" align="center">
                      <template #default="{ $index }">
                        <el-button link type="danger" @click="resultMappingRows.splice($index, 1)">删除</el-button>
                      </template>
                    </el-table-column>
                  </el-table>
                </div>
              </el-form-item>
            </el-col>
          </el-row>
        </template>

        <!-- 关联表配置：select-table / cascader -->
        <template v-if="['select-table', 'cascader'].includes(fieldForm.formControlType)">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="关联表">
                <el-select v-model="fieldForm.relatedEntityName" style="width: 100%" @change="handleRelatedEntityChange">
                  <el-option v-for="e in referenceEntities" :key="e.isBuiltin ? e.name : e.id" :label="getReferenceEntityLabel(e)" :value="e.name" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="关联ID字段">
                <el-select v-model="fieldForm.relatedEntityIdField" placeholder="选择字段" clearable style="width: 100%">
                  <el-option v-for="f in relatedEntityFields" :key="f.name" :label="`${f.name}（${f.description}）`" :value="f.name" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="显示字段">
                <el-select v-model="displayFieldsArray" multiple placeholder="选择字段" clearable style="width: 100%">
                  <el-option v-for="f in relatedEntityFields" :key="f.name" :label="`${f.name}（${f.description}）`" :value="f.name" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
        </template>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="默认值">
              <el-input v-model="fieldForm.defaultValue" placeholder="可选" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="列宽(px)">
              <el-input-number v-model="fieldForm.listWidth" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="显示条件">
              <el-input v-model="fieldForm.showCondition" placeholder="如 form.isEnabled === true" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="列表排序">
              <el-switch v-model="fieldForm.isSortable" />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 字段类别 -->
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="字段类别">
              <el-select v-model="fieldForm.fieldCategory" style="width: 100%">
                <el-option label="普通字段" value="Normal" />
                <el-option label="计算字段" value="Computed" />
                <el-option label="统计字段" value="Aggregate" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 计算公式（计算字段） -->
        <template v-if="fieldForm.fieldCategory === 'Computed'">
          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item label="计算公式">
                <el-input v-model="fieldForm.formula" placeholder="如 [Price]*[Quantity]" style="width: 100%" />
                <div style="color: #999; font-size: 12px; margin-top: 4px">用 [字段名] 引用同表字段，如 [Price]*[Quantity]*(1-[Discount]/100)</div>
              </el-form-item>
            </el-col>
          </el-row>
        </template>

        <!-- 统计配置（统计字段） -->
        <template v-if="fieldForm.fieldCategory === 'Aggregate'">
          <el-row :gutter="20">
            <el-col :span="8">
              <el-form-item label="统计类型">
                <el-select v-model="fieldForm.aggregateType" style="width: 100%">
                  <el-option label="数值累加" value="Sum" />
                  <el-option label="平均值" value="Avg" />
                  <el-option label="字符串拼接" value="Concat" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="统计子表">
                <el-select v-model="fieldForm.aggregateChildEntityId" style="width: 100%" @change="handleAggChildEntityChange">
                  <el-option v-for="r in displayRelations" :key="r.childEntityId" :label="r.childEntityName" :value="r.childEntityId" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="子表字段">
                <el-select v-model="fieldForm.aggregateChildFieldName" style="width: 100%">
                  <el-option v-for="f in aggChildFieldOptions" :key="f.name" :label="`${f.name}（${f.description}）`" :value="f.name" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20" v-if="fieldForm.aggregateType === 'Concat'">
            <el-col :span="8">
              <el-form-item label="分隔符">
                <el-input v-model="fieldForm.aggregateSeparator" placeholder="如 , " />
              </el-form-item>
            </el-col>
          </el-row>
        </template>
      </el-form>

      <template #footer>
        <el-button @click="fieldDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleFieldSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 一对多关系弹框 -->
    <el-dialog v-model="relationDialogVisible" :title="relationDialogTitle" width="600px">
      <el-form ref="relationFormRef" :model="relationForm" :rules="relationRules" label-width="120px">
        <el-form-item label="主表字段" prop="masterField">
          <el-select v-model="relationForm.masterField" placeholder="选择主表字段" style="width: 100%">
            <el-option v-for="f in masterFieldOptions" :key="f.name" :label="`${f.name}（${f.description}）`" :value="f.name" />
          </el-select>
        </el-form-item>
        <el-form-item label="子表" prop="childEntityId">
          <el-select v-model="relationForm.childEntityId" placeholder="选择子表" style="width: 100%" @change="handleChildEntityChange">
            <el-option v-for="e in allEntities" :key="e.id" :label="`${e.name}（${e.description}）`" :value="e.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="子表外键" prop="childForeignKey">
          <el-select v-model="relationForm.childForeignKey" placeholder="选择子表外键字段" style="width: 100%">
            <el-option v-for="f in childFieldOptions" :key="f.name" :label="`${f.name}（${f.description}）`" :value="f.name" />
          </el-select>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="relationForm.orderNum" :min="0" style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="relationDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleRelationSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 一对一组成关系弹框 -->
    <el-dialog v-model="entityRelationDialogVisible" :title="entityRelationDialogTitle" width="680px">
      <el-form ref="entityRelationFormRef" :model="entityRelationForm" :rules="entityRelationRules" label-width="120px">
        <el-alert
          title="目标实体字段将沿用它自己的列表、表单和详情显示配置；保存时作为当前实体的组成部分一起处理。"
          type="info"
          :closable="false"
          show-icon
          style="margin-bottom: 18px"
        />
        <el-form-item label="属性名称" prop="relationName">
          <el-input v-model="entityRelationForm.relationName" placeholder="如 Detail、ShippingAddress" />
        </el-form-item>
        <el-form-item label="当前实体字段" prop="sourceField">
          <el-select v-model="entityRelationForm.sourceField" placeholder="选择当前实体字段" style="width: 100%">
            <el-option v-for="f in entitySourceFieldOptions" :key="f.name" :label="`${f.name}（${f.description}）`" :value="f.name" />
          </el-select>
        </el-form-item>
        <el-form-item label="组成实体" prop="targetEntityId">
          <el-select v-model="entityRelationForm.targetEntityId" placeholder="选择组成实体" style="width: 100%" @change="handleEntityRelationTargetChange">
            <el-option v-for="e in entityRelationTargetOptions" :key="e.id" :label="`${e.name}（${e.description}）`" :value="e.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="组成实体外键" prop="targetField">
          <el-select v-model="entityRelationForm.targetField" placeholder="选择唯一外键字段" style="width: 100%">
            <el-option
              v-for="f in entityRelationTargetFieldOptions"
              :key="f.name"
              :label="`${f.name}（${f.description}）${f.isPrimaryKey ? ' 主键' : (f.isUnique ? ' 唯一' : '')}`"
              :value="f.name"
            />
          </el-select>
          <div class="form-help">外键必须与当前实体字段类型一致；建议在生成项目中为该外键建立唯一索引。</div>
        </el-form-item>
        <el-form-item label="关系选项">
          <el-checkbox v-model="entityRelationForm.isRequired">必须填写组成数据</el-checkbox>
        </el-form-item>
        <el-form-item v-if="!entityRelationForm.isRequired" label="提交空值时" prop="deleteBehavior">
          <el-radio-group v-model="entityRelationForm.deleteBehavior">
            <el-radio :value="1">删除已有组成数据</el-radio>
            <el-radio :value="2">保留已有组成数据</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="entityRelationForm.orderNum" :min="0" style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="entityRelationDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleEntityRelationSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getById, update, getList as getEntityList, getReferenceEntities } from '@/api/codegen/moduleEntity'
import { getByProjectId } from '@/api/codegen/projectModule'
import { getList as getProjectList, getDictTypes } from '@/api/codegen/project'

const router = useRouter()
const route = useRoute()
const formRef = ref(null)
const fieldTableRef = ref(null)
const fieldFormRef = ref(null)
const relationFormRef = ref(null)
const entityRelationFormRef = ref(null)
const loading = ref(false)
const submitLoading = ref(false)
const projectList = ref([])
const moduleList = ref([])
const dictTypeList = ref([])
const allEntities = ref([])
const referenceEntities = ref([])
const childFieldOptions = ref([])
const entityRelationTargetFieldOptions = ref([])
const resultMappingRows = ref([])

const form = reactive({
  id: null, projectId: null, moduleId: null,
  name: '', description: '', tableName: '',
  hasPrimaryKey: true, isTree: false, isReadOnly: false,
  hasDataPermission: false, hasTenant: false,
  hasAudit: true, hasSoftDelete: true,
  generateFrontend: true, frontendRoute: '', menuIcon: '', orderNum: 0,
  isChildTable: false
})

const rules = {
  projectId: [{ required: true, message: '请选择项目', trigger: 'change' }],
  moduleId: [{ required: true, message: '请选择模块', trigger: 'change' }],
  name: [{ required: true, message: '请输入实体名称', trigger: 'blur' }],
  description: [{ required: true, message: '请输入实体描述', trigger: 'blur' }]
}

// ===== 字段变更追踪 =====
const originalFields = ref([])
const newFields = ref([])
const updatedFields = ref([])
const deletedFieldIds = ref([])
const FIELD_ORDER_STEP = 10000

// ===== 字段筛选 =====
const filterDeleted = ref(false)
const filterSystem = ref(null)
const filterName = ref('')
const filterDesc = ref('')
const draggingFieldKey = ref('')
const draggingOverFieldKey = ref('')
const fieldOrderDirty = ref(false)
let fieldPointerActive = false
let fieldClientKeySeed = 1

const hasFieldFilters = computed(() => {
  return filterDeleted.value !== false ||
    filterSystem.value !== null ||
    !!filterName.value ||
    !!filterDesc.value
})

const displayFields = computed(() => {
  const fields = []
  // 收集 newFields 的字段名，用于去重（newFields 字段优先级更高）
  const newFieldNames = new Set(newFields.value.map(f => f.name))
  originalFields.value.forEach(f => {
    const isDeleted = deletedFieldIds.value.includes(f.id)
    // 筛选已删除状态
    if (filterDeleted.value != null && isDeleted !== filterDeleted.value) return
    // 去重：如果 newFields 中已有同名字段，跳过已删除的原始字段
    if (isDeleted && newFieldNames.has(f.name)) return
    if (!isDeleted) {
      const upd = updatedFields.value.find(u => u.id === f.id)
      fields.push(upd ? { ...upd, ...(isFieldOrderOnlyUpdate(upd) ? {} : { _status: 'modified' }) } : { ...f })
    } else {
      fields.push({ ...f, _deleted: true })
    }
  })
  newFields.value.forEach(f => fields.push({ ...f, _status: 'new' }))

  // 筛选系统字段、字段名、描述
  let list = fields
  if (filterSystem.value != null) {
    list = list.filter(f => f.isSystemField === filterSystem.value)
  }
  if (filterName.value) {
    const kw = filterName.value.toLowerCase()
    list = list.filter(f => (f.name || '').toLowerCase().includes(kw))
  }
  if (filterDesc.value) {
    const kw = filterDesc.value.toLowerCase()
    list = list.filter(f => (f.description || '').toLowerCase().includes(kw))
  }
  return list.sort((a, b) => (a.orderNum || 0) - (b.orderNum || 0))
})

// 主表字段选项（用于一对多关系选择）
const masterFieldOptions = computed(() => displayFields.value)
const mappingTargetFieldOptions = computed(() =>
  displayFields.value.filter(field => !field._deleted && field.fieldCategory !== 'Computed')
)

// ===== 一对多关系变更追踪 =====
function createFieldClientKey() {
  return `field_${Date.now()}_${fieldClientKeySeed++}`
}

function getFieldRowKey(field) {
  if (!field) return ''
  return field.id ? `id:${field.id}` : `new:${field._clientKey || field.name || ''}`
}

function cleanFieldUiState(field) {
  const { _status, _deleted, _clientKey, ...rest } = field
  return rest
}

function getNextFieldOrderNum() {
  const orderNums = [
    ...originalFields.value.filter(f => !deletedFieldIds.value.includes(f.id)),
    ...newFields.value
  ].map(f => Number(f.orderNum || 0))

  return orderNums.length > 0 ? Math.max(...orderNums) + FIELD_ORDER_STEP : FIELD_ORDER_STEP
}

function upsertUpdatedFieldOrder(row, orderNum) {
  if (!row?.id) return

  const original = originalFields.value.find(f => f.id === row.id)
  const idx = updatedFields.value.findIndex(f => f.id === row.id)
  const merged = cleanFieldUiState({
    ...defaultFieldForm,
    ...(original || {}),
    ...(idx >= 0 ? updatedFields.value[idx] : {}),
    ...row,
    id: row.id,
    moduleEntityId: form.id,
    orderNum
  })

  if (idx >= 0) updatedFields.value[idx] = merged
  else updatedFields.value.push(merged)
}

function updateNewFieldOrder(row, orderNum) {
  const rowKey = getFieldRowKey(row)
  const idx = newFields.value.findIndex(f => getFieldRowKey(f) === rowKey)
  if (idx >= 0) newFields.value[idx].orderNum = orderNum
}

function applyFieldDragOrder(sourceKey, targetKey, placement = 'before') {
  const ordered = displayFields.value.filter(f => !f._deleted)
  const prevKeys = ordered.map(getFieldRowKey)
  const from = ordered.findIndex(f => getFieldRowKey(f) === sourceKey)
  const to = ordered.findIndex(f => getFieldRowKey(f) === targetKey)
  if (from < 0 || to < 0 || from === to) return

  const [moving] = ordered.splice(from, 1)
  const targetIndex = ordered.findIndex(f => getFieldRowKey(f) === targetKey)
  const insertIndex = placement === 'after' ? targetIndex + 1 : targetIndex
  ordered.splice(insertIndex, 0, moving)

  const nextKeys = ordered.map(getFieldRowKey)
  if (nextKeys.every((key, index) => key === prevKeys[index])) return

  applyMinimalFieldOrder(ordered, sourceKey)
  fieldOrderDirty.value = true
}

function setFieldOrder(field, orderNum) {
  if (getFieldOrderNum(field) === orderNum) return
  if (field.id) upsertUpdatedFieldOrder(field, orderNum)
  else updateNewFieldOrder(field, orderNum)
}

function getFieldOrderNum(field) {
  return Number(field?.orderNum ?? 0)
}

function applyMinimalFieldOrder(ordered, movedKey) {
  const movedIndex = ordered.findIndex(f => getFieldRowKey(f) === movedKey)
  if (movedIndex < 0) return

  const moving = ordered[movedIndex]
  const prev = movedIndex > 0 ? ordered[movedIndex - 1] : null
  const next = movedIndex < ordered.length - 1 ? ordered[movedIndex + 1] : null

  if (!prev && !next) return

  if (!prev) {
    setFieldOrder(moving, getFieldOrderNum(next) - FIELD_ORDER_STEP)
    return
  }

  if (!next) {
    setFieldOrder(moving, getFieldOrderNum(prev) + FIELD_ORDER_STEP)
    return
  }

  const prevOrder = getFieldOrderNum(prev)
  const nextOrder = getFieldOrderNum(next)
  if (nextOrder - prevOrder > 1) {
    setFieldOrder(moving, Math.floor((prevOrder + nextOrder) / 2))
    return
  }

  let expectedOrder = prevOrder + 1
  setFieldOrder(moving, expectedOrder)

  for (let i = movedIndex + 1; i < ordered.length; i++) {
    expectedOrder += 1
    if (getFieldOrderNum(ordered[i]) >= expectedOrder) break
    setFieldOrder(ordered[i], expectedOrder)
  }
}

function normalizeFieldOrderNumbers(ordered = displayFields.value.filter(f => !f._deleted)) {
  ordered.forEach((field, index) => {
    setFieldOrder(field, (index + 1) * FIELD_ORDER_STEP)
  })
}

function normalizeFieldCompareValue(value) {
  return value === undefined ? null : value
}

function isFieldOrderOnlyUpdate(field) {
  if (!field?.id) return false
  const original = originalFields.value.find(f => f.id === field.id)
  if (!original) return false

  const ignoredKeys = new Set(['orderNum', 'createTime', 'updateTime', 'deleteTime'])
  const current = cleanFieldUiState(field)
  const baseline = cleanFieldUiState({
    ...defaultFieldForm,
    ...original,
    id: field.id,
    moduleEntityId: form.id
  })
  const keys = new Set([...Object.keys(current), ...Object.keys(baseline)])
  for (const key of keys) {
    if (ignoredKeys.has(key)) continue
    if (JSON.stringify(normalizeFieldCompareValue(current[key])) !== JSON.stringify(normalizeFieldCompareValue(baseline[key]))) {
      return false
    }
  }

  return getFieldOrderNum(current) !== getFieldOrderNum(baseline)
}

function getFieldDragTargetByPoint(clientX, clientY) {
  const element = document.elementFromPoint(clientX, clientY)
  const rowEl = element?.closest?.('tr.el-table__row')
  if (!rowEl) return null

  const rowContainer = rowEl.parentElement
  const rows = rowContainer ? Array.from(rowContainer.querySelectorAll('tr.el-table__row')) : []
  const rowIndex = rows.indexOf(rowEl)
  if (rowIndex < 0) return null

  const rect = rowEl.getBoundingClientRect()
  return {
    row: displayFields.value[rowIndex],
    placement: clientY > rect.top + rect.height / 2 ? 'after' : 'before'
  }
}

function cleanupFieldPointerDrag() {
  if (!fieldPointerActive) return
  document.removeEventListener('mousemove', handleFieldPointerMove)
  document.removeEventListener('mouseup', handleFieldPointerEnd)
  document.removeEventListener('mouseleave', handleFieldPointerCancel)
  document.body.classList.remove('field-dragging')
  fieldPointerActive = false
}

function handleFieldPointerStart(row, event) {
  if (row._deleted) return
  if (hasFieldFilters.value) {
    ElMessage.warning('请先清空字段筛选后再拖拽排序')
    return
  }

  event.preventDefault()
  cleanupFieldPointerDrag()
  draggingFieldKey.value = getFieldRowKey(row)
  draggingOverFieldKey.value = ''
  fieldPointerActive = true
  document.body.classList.add('field-dragging')
  document.addEventListener('mousemove', handleFieldPointerMove)
  document.addEventListener('mouseup', handleFieldPointerEnd)
  document.addEventListener('mouseleave', handleFieldPointerCancel)
}

function handleFieldPointerMove(event) {
  if (!draggingFieldKey.value) return
  event.preventDefault()
  const target = getFieldDragTargetByPoint(event.clientX, event.clientY)
  if (!target?.row || target.row._deleted) {
    draggingOverFieldKey.value = ''
    return
  }

  draggingOverFieldKey.value = getFieldRowKey(target.row)
}

function handleFieldPointerEnd(event) {
  if (!draggingFieldKey.value) {
    cleanupFieldPointerDrag()
    return
  }

  event.preventDefault()
  const target = getFieldDragTargetByPoint(event.clientX, event.clientY)
  if (target?.row && !target.row._deleted) {
    applyFieldDragOrder(draggingFieldKey.value, getFieldRowKey(target.row), target.placement)
  }
  handleFieldPointerCancel()
}

function handleFieldPointerCancel() {
  cleanupFieldPointerDrag()
  draggingFieldKey.value = ''
  draggingOverFieldKey.value = ''
}

function getFieldRowClassName({ row }) {
  return draggingOverFieldKey.value && getFieldRowKey(row) === draggingOverFieldKey.value
    ? 'field-drag-over'
    : ''
}

const originalRelations = ref([])
const newRelations = ref([])
const updatedRelations = ref([])
const deletedRelationIds = ref([])

const displayRelations = computed(() => {
  const rels = []
  originalRelations.value.forEach(r => {
    if (!deletedRelationIds.value.includes(r.id)) {
      const upd = updatedRelations.value.find(u => u.id === r.id)
      rels.push(upd ? { ...upd, _status: 'modified' } : { ...r })
    }
  })
  newRelations.value.forEach(r => rels.push({ ...r, _status: 'new' }))
  return rels
})

// ===== 一对一组成关系变更追踪 =====
const originalEntityRelations = ref([])
const newEntityRelations = ref([])
const updatedEntityRelations = ref([])
const deletedEntityRelationIds = ref([])

const displayEntityRelations = computed(() => {
  const relations = []
  originalEntityRelations.value.forEach(relation => {
    if (!deletedEntityRelationIds.value.includes(relation.id)) {
      const updated = updatedEntityRelations.value.find(item => item.id === relation.id)
      relations.push(updated ? { ...relation, ...updated, _status: 'modified' } : { ...relation })
    }
  })
  newEntityRelations.value.forEach(relation => relations.push({ ...relation, _status: 'new' }))
  return relations.sort((a, b) => (a.orderNum || 0) - (b.orderNum || 0))
})

const entitySourceFieldOptions = computed(() =>
  displayFields.value.filter(field => !field._deleted && !field.isIgnore && field.isPrimaryKey)
)

const entityRelationTargetOptions = computed(() =>
  allEntities.value.filter(entity => entity.id !== form.id && (!form.projectId || entity.projectId === form.projectId))
)

// ===== 字段弹框 =====
const fieldDialogVisible = ref(false)
const fieldDialogTitle = ref('新增字段')
const editingFieldIndex = ref(-1)
const editingFieldId = ref(null)
const editingFieldIsNew = ref(false)
const editingFieldOriginalName = ref('')

const defaultFieldForm = {
  name: '', description: '', dataType: 'string', maxLength: 0, defaultValue: '',
  formControlType: 'input', isPrimaryKey: false, isRequired: false,
  isNullable: false, isIgnore: false, isSystemField: false, isMultiple: false,
  showInList: true, showInDetail: true, showInAddForm: true, showInEditForm: true, showInSearch: false,
  selectDataSource: null, selectOptions: null,
  relatedEntityName: null, relatedEntityIdField: null, relatedEntityDisplayFields: null, resultMappings: null,
  listWidth: null, orderNum: 0,
  fieldCategory: 'Normal', formula: null, aggregateType: null,
  aggregateChildEntityId: null, aggregateChildFieldName: null, aggregateSeparator: null,
  showCondition: null, isSortable: false,
  minValue: null, maxValue: null, isEmail: false, isPhone: false, regexPattern: null
}
const fieldForm = reactive({ ...defaultFieldForm })

const fieldRules = {
  name: [{ required: true, message: '请输入字段名', trigger: 'blur' }],
  description: [{ required: true, message: '请输入描述', trigger: 'blur' }],
  dataType: [{ required: true, message: '请选择数据类型', trigger: 'change' }]
}

// ===== 关系弹框 =====
const aggChildFieldOptions = ref([])
const relationDialogVisible = ref(false)
const relationDialogTitle = ref('新增关系')
const editingRelationIndex = ref(-1)
const editingRelationId = ref(null)
const editingRelationIsNew = ref(false)

const relationForm = reactive({
  masterField: '', childEntityId: null, childEntityName: '', childForeignKey: '', orderNum: 0
})
const relationRules = {
  masterField: [{ required: true, message: '请选择主表字段', trigger: 'change' }],
  childEntityId: [{ required: true, message: '请选择子表', trigger: 'change' }],
  childForeignKey: [{ required: true, message: '请选择子表外键', trigger: 'change' }]
}

const entityRelationDialogVisible = ref(false)
const entityRelationDialogTitle = ref('新增一对一组成关系')
const editingEntityRelationIndex = ref(-1)
const editingEntityRelationId = ref(null)
const editingEntityRelationIsNew = ref(false)
const editingEntityRelationClientKey = ref('')
const defaultEntityRelationForm = {
  targetEntityId: null,
  relationName: '',
  sourceField: '',
  targetField: '',
  cardinality: 1,
  ownership: 1,
  isRequired: false,
  deleteBehavior: 1,
  orderNum: 0
}
const entityRelationForm = reactive({ ...defaultEntityRelationForm })
const entityRelationRules = {
  relationName: [
    { required: true, message: '请输入属性名称', trigger: 'blur' },
    { pattern: /^[A-Za-z][A-Za-z0-9_]*$/, message: '属性名称必须是合法的 C# / JavaScript 标识符', trigger: 'blur' }
  ],
  sourceField: [{ required: true, message: '请选择当前实体字段', trigger: 'change' }],
  targetEntityId: [{ required: true, message: '请选择组成实体', trigger: 'change' }],
  targetField: [{ required: true, message: '请选择组成实体唯一外键', trigger: 'change' }],
  deleteBehavior: [{ required: true, message: '请选择空值处理方式', trigger: 'change' }]
}

// ===== 系统字段定义 =====
const systemFieldDefs = {
  hasPrimaryKey: [
    { name: 'Id', description: '主键', dataType: 'long', isPrimaryKey: true, isRequired: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ],
  isTree: [
    { name: 'ParentId', description: '父级ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: true, showInEditForm: true, showInDetail: false, showInSearch: false },
    { name: 'Ancestors', description: '祖级列表', dataType: 'string?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ],
  hasTenant: [
    { name: 'TenantId', description: '租户ID', dataType: 'long', isRequired: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ],
  hasDataPermission: [
    { name: 'DeptId', description: '部门ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'DeptAncestors', description: '部门祖级', dataType: 'string?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'CreateUserId', description: '创建人ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ],
  hasAudit: [
    { name: 'CreateUserId', description: '创建人ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'CreateBy', description: '创建人', dataType: 'string?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'CreateTime', description: '创建时间', dataType: 'DateTime', isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'UpdateUserId', description: '更新人ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'UpdateBy', description: '更新人', dataType: 'string?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'UpdateTime', description: '更新时间', dataType: 'DateTime?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ],
  hasSoftDelete: [
    { name: 'IsDeleted', description: '是否删除', dataType: 'bool', isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'DeleteTime', description: '删除时间', dataType: 'DateTime?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'DeleteBy', description: '删除人', dataType: 'string?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false },
    { name: 'DeleteUserId', description: '删除人ID', dataType: 'long?', isNullable: true, isSystemField: true, showInList: false, showInAddForm: false, showInEditForm: false, showInDetail: false, showInSearch: false }
  ]
}

// ===== 勾选/取消勾选自动字段管理 =====
function addSystemFields(configKey) {
  const defs = systemFieldDefs[configKey] || []
  defs.forEach(def => {
    // 基于原始数据检查是否存在（不受筛选影响）
    const existsInOriginals = originalFields.value.some(f => f.name === def.name && !deletedFieldIds.value.includes(f.id))
    const existsInNew = newFields.value.some(f => f.name === def.name)
    if (existsInOriginals || existsInNew) return
    // 检查是否在 deletedFieldIds 中（已删除的恢复）
    const deletedField = originalFields.value.find(f => f.name === def.name && deletedFieldIds.value.includes(f.id))
    if (deletedField) {
      deletedFieldIds.value = deletedFieldIds.value.filter(id => id !== deletedField.id)
    } else {
      newFields.value.push({
        ...defaultFieldForm,
        ...def,
        formControlType: 'input',
        moduleEntityId: form.id,
        orderNum: getNextFieldOrderNum(),
        _clientKey: createFieldClientKey()
      })
    }
  })
}

// 获取其他已勾选选项所需的字段名集合（防止误删共享字段如 CreateUserId）
function getFieldNamesNeededByOtherConfigs(currentKey) {
  const allKeys = ['hasPrimaryKey', 'isTree', 'hasTenant', 'hasDataPermission', 'hasAudit', 'hasSoftDelete']
  const names = new Set()
  allKeys.forEach(key => {
    if (key !== currentKey && form[key]) {
      (systemFieldDefs[key] || []).forEach(d => names.add(d.name))
    }
  })
  return names
}

function removeSystemFields(configKey) {
  const defs = systemFieldDefs[configKey] || []
  const allNames = defs.map(d => d.name)
  // 过滤掉其他已勾选选项仍需要共享的字段
  const stillNeeded = getFieldNamesNeededByOtherConfigs(configKey)
  const namesToRemove = allNames.filter(n => !stillNeeded.has(n))
  if (namesToRemove.length === 0) return
  // 删除新增的系统字段
  newFields.value = newFields.value.filter(f => !namesToRemove.includes(f.name) || !f.isSystemField)
  // 删除原始的系统字段（软删除）
  originalFields.value.forEach(f => {
    if (namesToRemove.includes(f.name) && f.isSystemField && !deletedFieldIds.value.includes(f.id)) {
      deletedFieldIds.value.push(f.id)
    }
  })
}

watch(() => form.hasPrimaryKey, (val, old) => { if (old === undefined) return; val ? addSystemFields('hasPrimaryKey') : removeSystemFields('hasPrimaryKey') })
watch(() => form.isTree, (val, old) => { if (old === undefined) return; val ? addSystemFields('isTree') : removeSystemFields('isTree') })
watch(() => form.hasTenant, (val, old) => { if (old === undefined) return; val ? addSystemFields('hasTenant') : removeSystemFields('hasTenant') })
watch(() => form.hasDataPermission, (val, old) => { if (old === undefined) return; val ? addSystemFields('hasDataPermission') : removeSystemFields('hasDataPermission') })
watch(() => form.hasAudit, (val, old) => { if (old === undefined) return; val ? addSystemFields('hasAudit') : removeSystemFields('hasAudit') })
watch(() => form.hasSoftDelete, (val, old) => { if (old === undefined) return; val ? addSystemFields('hasSoftDelete') : removeSystemFields('hasSoftDelete') })

// 能力组合约束：无主键只能作为只读查询模型；树形实体必须有主键。
watch(() => form.hasPrimaryKey, (val) => {
  if (!val) {
    form.isTree = false
    form.isReadOnly = true
  }
})
watch(() => form.isTree, (val) => { if (val) form.hasPrimaryKey = true })
watch(() => form.isReadOnly, (val) => { if (!val && !form.hasPrimaryKey) form.hasPrimaryKey = true })

// checkbox-group 强制多选
watch(() => fieldForm.formControlType, (val) => {
  if (val === 'checkbox-group') fieldForm.isMultiple = true
  else if (val === 'radio-group') fieldForm.isMultiple = false
})

// select-table / cascader 默认不显示字段本身，只显示关联表的显示字段
watch(() => fieldForm.formControlType, (val) => {
  if (val === 'select-table' || val === 'cascader') {
    fieldForm.showInList = false
    fieldForm.showInDetail = true
    fieldForm.showInAddForm = true
    fieldForm.showInEditForm = true
  }
})

// 数据源切到字典时加载字典类型
watch(() => fieldForm.selectDataSource, (val) => { if (val === 'dict') loadDictTypes() })

watch(() => fieldForm.fieldCategory, (val) => {
  if (val === 'Computed') {
    fieldForm.aggregateType = null
    fieldForm.aggregateChildEntityId = null
    fieldForm.aggregateChildFieldName = null
    fieldForm.aggregateSeparator = null
  } else if (val === 'Aggregate') {
    fieldForm.formula = null
    fieldForm.aggregateType ||= 'Sum'
  } else {
    fieldForm.formula = null
    fieldForm.aggregateType = null
    fieldForm.aggregateChildEntityId = null
    fieldForm.aggregateChildFieldName = null
    fieldForm.aggregateSeparator = null
  }
})

function validateCalculatedFieldForm() {
  const dataType = String(fieldForm.dataType || '').replace(/\?$/, '').toLowerCase()
  const numericTypes = ['byte', 'short', 'int', 'long', 'float', 'double', 'decimal']
  if (fieldForm.fieldCategory === 'Computed') {
    if (!numericTypes.includes(dataType)) {
      ElMessage.warning('计算字段必须使用数值类型')
      return false
    }
    if (!fieldForm.formula || !/\[[A-Za-z_]\w*\]/.test(fieldForm.formula)) {
      ElMessage.warning('计算公式必须使用 [字段名] 引用至少一个字段')
      return false
    }
  }
  if (fieldForm.fieldCategory === 'Aggregate') {
    if (!fieldForm.aggregateType || !fieldForm.aggregateChildEntityId || !fieldForm.aggregateChildFieldName) {
      ElMessage.warning('统计字段必须选择统计类型、统计子表和子表字段')
      return false
    }
    if (fieldForm.aggregateType === 'Concat') {
      if (!['string', 'text'].includes(dataType)) {
        ElMessage.warning('字符串拼接统计字段必须使用 string 或 text 类型')
        return false
      }
    } else if (!numericTypes.includes(dataType)) {
      ElMessage.warning('求和或平均值统计字段必须使用数值类型')
      return false
    }
  }
  return true
}

// ===== 生命周期 =====
onMounted(async () => {
  await loadProjectList()
  await loadData()
  await loadAllEntities()
  await loadReferenceEntities()
})

onBeforeUnmount(() => {
  handleFieldPointerCancel()
})

async function loadProjectList() {
  try {
    const res = await getProjectList()
    projectList.value = Array.isArray(res) ? res : (res?.items || [])
  } catch (e) { console.error(e) }
}

function getProjectOptionLabel(project) {
  return project?.description || project?.displayName || '未设置项目描述'
}

async function loadModuleList() {
  if (!form.projectId) {
    moduleList.value = []
    return
  }
  try {
    const res = await getByProjectId(form.projectId)
    moduleList.value = Array.isArray(res) ? res : []
  } catch (e) { console.error(e) }
}

function handleProjectChange() {
  form.moduleId = null
  loadModuleList()
  loadReferenceEntities()
  dictTypeList.value = []
}

async function loadDictTypes() {
  if (!form.projectId) return
  try {
    dictTypeList.value = await getDictTypes(form.projectId)
  } catch (e) {
    console.error('加载字典类型失败:', e)
  }
}

async function loadAllEntities() {
  try {
    const res = await getEntityList({})
    allEntities.value = Array.isArray(res) ? res : (res?.items || [])
  } catch (e) { console.error(e) }
}

async function loadReferenceEntities() {
  try {
    const res = await getReferenceEntities(form.projectId ? { projectId: form.projectId } : {})
    referenceEntities.value = Array.isArray(res) ? res : (res?.items || [])
  } catch (e) { console.error(e) }
}

function getReferenceEntityLabel(entity) {
  const text = `${entity.name}（${entity.description}）`
  return entity.isBuiltin ? `${text} 内置` : text
}

async function loadData() {
  loading.value = true
  try {
    const data = await getById(route.query.id)
    Object.keys(form).forEach(k => { if (data[k] !== undefined) form[k] = data[k] })
    // 加载当前项目下的模块列表
    await loadModuleList()
    originalFields.value = (data.fields || []).map(f => ({ ...defaultFieldForm, ...f }))
    newFields.value = []
    updatedFields.value = []
    deletedFieldIds.value = []
    fieldOrderDirty.value = false
    originalRelations.value = data.oneToManyRelations || []
    originalEntityRelations.value = data.entityRelations || []
    newEntityRelations.value = []
    updatedEntityRelations.value = []
    deletedEntityRelationIds.value = []
  } catch (e) {
    ElMessage.error('加载数据失败')
    router.back()
  } finally { loading.value = false }
}

// ===== 字段操作 =====
function handleAddField() {
  fieldDialogTitle.value = '新增字段'
  editingFieldIndex.value = -1
  editingFieldId.value = null
  editingFieldIsNew.value = true
  Object.assign(fieldForm, { ...defaultFieldForm })
  resultMappingRows.value = []
  fieldDialogVisible.value = true
}

function handleEditField(row, index) {
  fieldDialogTitle.value = '编辑字段'
  editingFieldIndex.value = index
  editingFieldId.value = row.id || null
  editingFieldIsNew.value = row._status === 'new'
  editingFieldOriginalName.value = row.name || ''
  Object.assign(fieldForm, row)
  resultMappingRows.value = parseResultMappings(fieldForm.resultMappings)
  // 关联表：加载关联实体字段列表（保留已有配置）
  if (['select-table', 'cascader'].includes(fieldForm.formControlType) && fieldForm.relatedEntityName) {
    handleRelatedEntityChange(fieldForm.relatedEntityName, true)
  }
  fieldDialogVisible.value = true
}

function handleDeleteField(row) {
  if (row._status === 'new') {
    const i = newFields.value.findIndex(f => f.name === row.name && f.orderNum === row.orderNum)
    if (i >= 0) newFields.value.splice(i, 1)
  } else if (row.id) {
    deletedFieldIds.value.push(row.id)
    updatedFields.value = updatedFields.value.filter(f => f.id !== row.id)
  }
}

async function handleFieldSubmit() {
  try {
    await fieldFormRef.value.validate()
    if (!validateCalculatedFieldForm()) return
    if (!validateResultMappings()) return
    fieldForm.resultMappings = resultMappingRows.value.length > 0
      ? JSON.stringify(resultMappingRows.value)
      : null
    const data = { ...fieldForm }
    delete data._status
    delete data._deleted

    // 同名字段：改名时 >0 重复，未改名时 >1 重复
    const allActive = [...originalFields.value.filter(f => !deletedFieldIds.value.includes(f.id)), ...newFields.value]
    const sameCount = allActive.filter(f => f.name === data.name).length
    const maxOk = data.name === editingFieldOriginalName.value ? 1 : 0
    if (data.name && sameCount > maxOk) {
      ElMessage.warning(`字段名「${data.name}」已存在，不能重复添加`)
      return
    }

    const previousField = editingFieldIndex.value >= 0 ? displayFields.value[editingFieldIndex.value] : null
    if (previousField && getFieldOrderNum(previousField) !== Number(data.orderNum || 0)) {
      fieldOrderDirty.value = true
    }

    if (editingFieldIsNew.value && editingFieldIndex.value >= 0) {
      // 编辑新增字段 → 保持新增状态
      const idx = newFields.value.findIndex(f => {
        if (data._clientKey && f._clientKey === data._clientKey) return true
        return f.name === displayFields.value[editingFieldIndex.value]?.name
      })
      if (idx >= 0) Object.assign(newFields.value[idx], data)
    } else if (editingFieldId.value) {
      // 编辑已保存字段 → 变为更新状态
      const idx = updatedFields.value.findIndex(f => f.id === editingFieldId.value)
      const fData = { ...data, id: editingFieldId.value, moduleEntityId: form.id }
      if (idx >= 0) updatedFields.value[idx] = fData
      else updatedFields.value.push(fData)
    } else {
      // 新增
      newFields.value.push({
        ...data,
        moduleEntityId: form.id,
        orderNum: data.orderNum || getNextFieldOrderNum(),
        _clientKey: data._clientKey || createFieldClientKey()
      })
    }
    fieldDialogVisible.value = false
  } catch (e) { if (e !== false) console.error(e) }
}

function handleFieldDialogClose() {
  Object.assign(fieldForm, { ...defaultFieldForm })
  resultMappingRows.value = []
  fieldFormRef.value?.clearValidate()
}

const relatedEntityFields = ref([])

function parseResultMappings(value) {
  try {
    const rows = JSON.parse(value || '[]')
    return Array.isArray(rows) ? rows.map(row => ({ ...row })) : []
  } catch {
    return []
  }
}

function handleAddResultMapping() {
  resultMappingRows.value.push({ sourceField: '', targetField: '' })
}

function validateResultMappings() {
  if (resultMappingRows.value.length === 0) return true
  if (fieldForm.formControlType !== 'select-table') {
    ElMessage.error('只有关联表选择控件可以配置选择结果映射')
    return false
  }
  if (fieldForm.isMultiple) {
    ElMessage.error('多选控件暂不支持把一条记录映射到普通字段')
    return false
  }

  const sourceNames = new Set(relatedEntityFields.value.map(field => field.name))
  const targetFields = new Map(mappingTargetFieldOptions.value.map(field => [field.name, field]))
  const usedTargets = new Set()
  for (const mapping of resultMappingRows.value) {
    if (!mapping.sourceField || !sourceNames.has(mapping.sourceField)) {
      ElMessage.error('选择结果映射中存在无效的来源字段')
      return false
    }
    if (!mapping.targetField || !targetFields.has(mapping.targetField)) {
      ElMessage.error('选择结果映射中存在无效的当前实体字段')
      return false
    }
    if (usedTargets.has(mapping.targetField)) {
      ElMessage.error(`字段「${mapping.targetField}」被重复映射`)
      return false
    }
    usedTargets.add(mapping.targetField)

    const source = relatedEntityFields.value.find(field => field.name === mapping.sourceField)
    const target = targetFields.get(mapping.targetField)
    const normalizeType = value => String(value || '').replace(/\?$/, '').toLowerCase()
    if (source && target && normalizeType(source.dataType) !== normalizeType(target.dataType)) {
      ElMessage.error(`映射字段类型不一致：${source.dataType} 与 ${target.dataType}`)
      return false
    }
  }
  return true
}

// 显示字段 JSON 数组 ↔ 多选数组
const displayFieldsArray = computed({
  get: () => {
    try { return JSON.parse(fieldForm.relatedEntityDisplayFields || '[]') } catch { return [] }
  },
  set: (val) => { fieldForm.relatedEntityDisplayFields = JSON.stringify(val) }
})
async function handleRelatedEntityChange(name, keepExisting = false) {
  if (!keepExisting) {
    fieldForm.relatedEntityIdField = null
    fieldForm.relatedEntityDisplayFields = null
    fieldForm.selectOptions = null
    fieldForm.resultMappings = null
    resultMappingRows.value = []
  }
  relatedEntityFields.value = []
  if (!name) return
  const entity = referenceEntities.value.find(e => e.name === name)
  if (!entity) return
  if (entity.isBuiltin) {
    relatedEntityFields.value = (entity.fields || []).filter(f => !f.isSystemField || f.isPrimaryKey)
    if (!keepExisting) {
      fieldForm.relatedEntityIdField = entity.valueField || 'Id'
      fieldForm.relatedEntityDisplayFields = JSON.stringify(entity.displayFields || [])
      fieldForm.selectDataSource = 'api'
      fieldForm.selectOptions = entity.selectOptions || null
    }
    return
  }
  try {
    const detail = await getById(entity.id)
    relatedEntityFields.value = (detail.fields || []).filter(f => !f.isSystemField || f.isPrimaryKey)
  } catch (e) { console.error('加载关联实体字段失败:', e) }
}

// ===== 关系操作 =====
function handleAddRelation() {
  relationDialogTitle.value = '新增关系'
  editingRelationIndex.value = -1
  editingRelationId.value = null
  editingRelationIsNew.value = true
  Object.assign(relationForm, { masterField: '', childEntityId: null, childEntityName: '', childForeignKey: '', orderNum: 0 })
  childFieldOptions.value = []
  relationDialogVisible.value = true
}

function handleEditRelation(row, index) {
  relationDialogTitle.value = '编辑关系'
  editingRelationIndex.value = index
  editingRelationId.value = row.id || null
  editingRelationIsNew.value = row._status === 'new'
  Object.assign(relationForm, row)
  // 加载子表字段
  handleChildEntityChange(row.childEntityId)
  relationDialogVisible.value = true
}

function handleDeleteRelation(row) {
  if (row._status === 'new') {
    const i = newRelations.value.findIndex(r => r.childEntityId === row.childEntityId && r.masterField === row.masterField)
    if (i >= 0) newRelations.value.splice(i, 1)
  } else if (row.id) {
    deletedRelationIds.value.push(row.id)
    updatedRelations.value = updatedRelations.value.filter(r => r.id !== row.id)
  }
}

async function handleAggChildEntityChange(entityId) {
  aggChildFieldOptions.value = []
  if (!entityId) return
  try {
    const data = await getById(entityId)
    aggChildFieldOptions.value = (data.fields || []).filter(f => !f.isSystemField)
  } catch (e) { console.error(e) }
}

async function handleChildEntityChange(entityId) {
  if (!entityId) { childFieldOptions.value = []; return }
  try {
    const data = await getById(entityId)
    childFieldOptions.value = data.fields || []
    const entity = allEntities.value.find(e => e.id === entityId)
    if (entity) relationForm.childEntityName = entity.name
  } catch (e) { childFieldOptions.value = [] }
}

async function handleRelationSubmit() {
  try {
    await relationFormRef.value.validate()
    const data = { ...relationForm }
    delete data._status

    if (editingRelationIsNew.value && editingRelationIndex.value >= 0) {
      const idx = newRelations.value.findIndex((r, i) => i === editingRelationIndex.value - originalRelations.value.filter(o => !deletedRelationIds.value.includes(o.id)).length)
      if (idx >= 0) Object.assign(newRelations.value[idx], data)
    } else if (editingRelationId.value) {
      const idx = updatedRelations.value.findIndex(r => r.id === editingRelationId.value)
      const rData = { ...data, id: editingRelationId.value }
      if (idx >= 0) updatedRelations.value[idx] = rData
      else updatedRelations.value.push(rData)
    } else {
      newRelations.value.push({ ...data })
    }
    relationDialogVisible.value = false
  } catch (e) { if (e !== false) console.error(e) }
}

function getEntityName(entityId) {
  return allEntities.value.find(entity => entity.id === entityId)?.name || ''
}

function getDeleteBehaviorLabel(value) {
  return Number(value) === 2 ? '保留' : Number(value) === 3 ? '阻止' : '删除'
}

function getNextEntityRelationOrderNum() {
  const values = displayEntityRelations.value.map(item => Number(item.orderNum || 0))
  return values.length > 0 ? Math.max(...values) + 10 : 10
}

function handleAddEntityRelation() {
  entityRelationDialogTitle.value = '新增一对一组成关系'
  editingEntityRelationIndex.value = -1
  editingEntityRelationId.value = null
  editingEntityRelationIsNew.value = true
  editingEntityRelationClientKey.value = ''
  Object.assign(entityRelationForm, { ...defaultEntityRelationForm, orderNum: getNextEntityRelationOrderNum() })
  entityRelationTargetFieldOptions.value = []
  entityRelationDialogVisible.value = true
}

async function handleEditEntityRelation(row, index) {
  entityRelationDialogTitle.value = '编辑一对一组成关系'
  editingEntityRelationIndex.value = index
  editingEntityRelationId.value = row.id || null
  editingEntityRelationIsNew.value = row._status === 'new'
  editingEntityRelationClientKey.value = row._clientKey || ''
  Object.assign(entityRelationForm, { ...defaultEntityRelationForm, ...row })
  await loadEntityRelationTargetFields(row.targetEntityId)
  entityRelationDialogVisible.value = true
}

function handleDeleteEntityRelation(row) {
  if (row._status === 'new') {
    const index = newEntityRelations.value.findIndex(item => item._clientKey === row._clientKey)
    if (index >= 0) newEntityRelations.value.splice(index, 1)
    return
  }

  if (row.id && !deletedEntityRelationIds.value.includes(row.id)) {
    deletedEntityRelationIds.value.push(row.id)
    updatedEntityRelations.value = updatedEntityRelations.value.filter(item => item.id !== row.id)
  }
}

async function handleEntityRelationTargetChange(entityId) {
  entityRelationForm.targetField = ''
  const target = allEntities.value.find(entity => entity.id === entityId)
  if (target && !entityRelationForm.relationName) {
    entityRelationForm.relationName = target.name
  }
  await loadEntityRelationTargetFields(entityId)
}

async function loadEntityRelationTargetFields(entityId) {
  entityRelationTargetFieldOptions.value = []
  if (!entityId) return
  try {
    const target = await getById(entityId)
    entityRelationTargetFieldOptions.value = (target.fields || []).filter(field => !field.isIgnore)
  } catch (error) {
    console.error('加载组成实体字段失败:', error)
  }
}

function validateEntityRelationCompatibility() {
  const source = entitySourceFieldOptions.value.find(field => field.name === entityRelationForm.sourceField)
  const target = entityRelationTargetFieldOptions.value.find(field => field.name === entityRelationForm.targetField)
  if (!source || !target) {
    ElMessage.error('请选择有效的对应字段')
    return false
  }

  const normalizeType = value => String(value || '').replace(/\?$/, '').toLowerCase()
  if (normalizeType(source.dataType) !== normalizeType(target.dataType)) {
    ElMessage.error(`字段类型不一致：${source.dataType} 与 ${target.dataType}`)
    return false
  }

  const duplicate = displayEntityRelations.value.some(item =>
    item.id !== editingEntityRelationId.value &&
    (!editingEntityRelationClientKey.value || item._clientKey !== editingEntityRelationClientKey.value) &&
    item.targetEntityId === entityRelationForm.targetEntityId &&
    item.targetField === entityRelationForm.targetField
  )
  if (duplicate) {
    ElMessage.error('该组成实体外键已被其他一对一关系使用')
    return false
  }
  return true
}

async function handleEntityRelationSubmit() {
  try {
    await entityRelationFormRef.value.validate()
    if (!validateEntityRelationCompatibility()) return

    const target = allEntities.value.find(entity => entity.id === entityRelationForm.targetEntityId)
    const data = {
      ...entityRelationForm,
      targetEntityName: target?.name || '',
      targetEntityDescription: target?.description || '',
      cardinality: 1,
      ownership: 1,
      deleteBehavior: entityRelationForm.isRequired ? 1 : entityRelationForm.deleteBehavior
    }
    delete data._status

    if (editingEntityRelationIsNew.value && editingEntityRelationIndex.value >= 0) {
      const index = newEntityRelations.value.findIndex(item => item._clientKey === editingEntityRelationClientKey.value)
      if (index >= 0) newEntityRelations.value[index] = { ...data, _clientKey: editingEntityRelationClientKey.value }
    } else if (editingEntityRelationId.value) {
      const relation = { ...data, id: editingEntityRelationId.value }
      const index = updatedEntityRelations.value.findIndex(item => item.id === editingEntityRelationId.value)
      if (index >= 0) updatedEntityRelations.value[index] = relation
      else updatedEntityRelations.value.push(relation)
    } else {
      newEntityRelations.value.push({ ...data, _clientKey: `relation_${Date.now()}` })
    }

    entityRelationDialogVisible.value = false
  } catch (error) {
    if (error !== false) console.error(error)
  }
}

// ===== 提交 =====
async function handleSubmit() {
  try {
    await formRef.value.validate()
    submitLoading.value = true

    if (fieldOrderDirty.value) {
      normalizeFieldOrderNumbers()
    }

    const submitData = {
      ...form,
      newFields: newFields.value.map(cleanFieldUiState),
      updatedFields: updatedFields.value.map(cleanFieldUiState),
      deletedFieldIds: deletedFieldIds.value,
      newRelations: newRelations.value.map(r => { const { _status, ...rest } = r; return rest }),
      updatedRelations: updatedRelations.value.map(r => { const { _status, ...rest } = r; return rest }),
      deletedRelationIds: deletedRelationIds.value,
      newEntityRelations: newEntityRelations.value.map(r => { const { _status, _clientKey, targetEntityName, targetEntityDescription, ...rest } = r; return rest }),
      updatedEntityRelations: updatedEntityRelations.value.map(r => { const { _status, targetEntityName, targetEntityDescription, ...rest } = r; return rest }),
      deletedEntityRelationIds: deletedEntityRelationIds.value
    }

    await update(form.id, submitData)
    ElMessage.success('更新成功')
    router.back()
  } catch (e) {
    if (e !== false) { ElMessage.error('更新失败'); console.error(e) }
  } finally { submitLoading.value = false }
}

function handleBack() { router.back() }
</script>

<style scoped lang="scss">
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.mx-1 {
  margin: 0 2px;
}
.section-hint {
  color: var(--el-text-color-secondary);
  font-size: 12px;
  margin-left: 12px;
}
.muted-text,
.form-help {
  color: var(--el-text-color-secondary);
}
.mapping-editor {
  width: 100%;
}
.mapping-toolbar {
  align-items: center;
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
}
.form-help {
  font-size: 12px;
  line-height: 1.5;
  margin-top: 4px;
}
.field-drag-handle {
  color: #8a96a8;
  cursor: grab;
  display: inline-flex;
  font-weight: 700;
  letter-spacing: 1px;
  line-height: 1;
  touch-action: none;
  user-select: none;
}
.field-drag-handle:active {
  cursor: grabbing;
}
.field-drag-handle.disabled {
  cursor: not-allowed;
  opacity: 0.35;
}
:deep(.field-drag-over td) {
  background-color: var(--el-color-primary-light-9) !important;
}
:global(body.field-dragging),
:global(body.field-dragging *) {
  cursor: grabbing !important;
  user-select: none !important;
}
</style>
