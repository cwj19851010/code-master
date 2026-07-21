import request from '@/utils/request'
import { executeCodegenAction } from '@/utils/codegenExecution'

/**
 * 获取全部模块实体列表
 */
export function getList(data) {
  return request({
    url: '/codegen/moduleentity/getlist',
    method: 'get',
    params: data
  })
}

/**
 * 获取模块实体列表
 */
export function getReferenceEntities(data) {
  return request({
    url: '/codegen/moduleentity/getreferenceentities',
    method: 'get',
    params: data
  })
}

export function getPagedList(data) {
  return request({
    url: '/codegen/moduleentity/getpagedlist',
    method: 'get',
    params: data
  })
}

/**
 * 获取模块实体详情
 */
export function getById(id) {
  return request({
    url: `/codegen/moduleentity/getbyid/${id}`,
    method: 'get'
  })
}

/**
 * 创建模块实体
 */
export function create(data) {
  return request({
    url: '/codegen/moduleentity/create',
    method: 'post',
    data
  })
}

/**
 * 更新模块实体
 */
export function update(id, data) {
  return request({
    url: `/codegen/moduleentity/update/${id}`,
    method: 'put',
    data
  })
}

/**
 * 删除模块实体
 */
export function deleteById(id) {
  return request({
    url: `/codegen/moduleentity/delete/${id}`,
    method: 'delete'
  })
}

/**
 * 根据项目模块ID获取实体列表
 */
export function getByModuleId(moduleId) {
  return request({
    url: `/codegen/moduleentity/getbymoduleid/${moduleId}`,
    method: 'get'
  })
}

/**
 * 生成代码
 */
export function generateCode(id) {
  return executeCodegenAction('generateCode', { entityId: id, id }, () =>
    request({
      url: `/codegen/moduleentity/generatecode/${id}`,
      method: 'post'
    })
  )
}

/**
 * 增量生成代码
 */
export function generateIncrementalCode(id) {
  return executeCodegenAction('generateIncrementalCode', { entityId: id, id }, () =>
    request({
      url: `/codegen/moduleentity/generateincrementalcode/${id}`,
      method: 'post'
    })
  )
}

export function generateProjectCode(projectId, entityIds = []) {
  const data = { projectId, entityIds }
  return executeCodegenAction('generateProjectCode', data, () =>
    request({
      url: '/codegen/moduleentity/generateprojectcode',
      method: 'post',
      data
    })
  )
}

export function generateProjectIncrementalCode(projectId, entityIds = []) {
  const data = { projectId, entityIds }
  return executeCodegenAction('generateProjectIncrementalCode', data, () =>
    request({
      url: '/codegen/moduleentity/generateprojectincrementalcode',
      method: 'post',
      data
    })
  )
}

/**
 * 同步菜单到目标项目数据库
 */
export function syncMenu(id) {
  return executeCodegenAction('syncMenu', { entityId: id, id }, () =>
    request({
      url: `/codegen/moduleentity/syncmenutotarget/${id}`,
      method: 'post'
    })
  )
}

export function syncProjectMenus(projectId, entityIds = []) {
  const data = { projectId, entityIds }
  return executeCodegenAction('syncProjectMenus', data, () =>
    request({
      url: '/codegen/moduleentity/syncprojectmenustotarget',
      method: 'post',
      data
    })
  )
}

/**
 * 同步字段多语言到目标项目数据库
 */
export function syncLanguage(id) {
  return executeCodegenAction('syncLanguage', { entityId: id, id }, () =>
    request({
      url: `/codegen/moduleentity/synclanguagetotarget/${id}`,
      method: 'post'
    })
  )
}

export function syncProjectLanguages(projectId, entityIds = []) {
  const data = { projectId, entityIds }
  return executeCodegenAction('syncProjectLanguages', data, () =>
    request({
      url: '/codegen/moduleentity/syncprojectlanguagestotarget',
      method: 'post',
      data
    })
  )
}

/**
 * 获取页面模板内容（可视化设计器加载）
 */
export function getPageContent(id, pageType) {
  return executeCodegenAction('getPageContent', { entityId: id, id, pageType }, () =>
    request({
      url: `/codegen/moduleentity/getpagecontent/${id}`,
      method: 'get',
      params: { pageType }
    })
  )
}

/**
 * 保存页面模板内容（可视化设计器回写）
 */
/**
 * 删除子表关系（设计器联动）
 */
export function deleteChildRelation(id, childEntityName) {
  return request({
    url: `/codegen/moduleentity/deletechildrelation/${id}`,
    method: 'delete',
    params: { childEntityName }
  })
}

export function deleteEntityRelation(id, relationId) {
  return request({
    url: `/codegen/moduleentity/deleteentityrelation/${id}`,
    method: 'delete',
    params: { relationId }
  })
}

function normalizePageContentPayload(templateOrTreeJson) {
  if (templateOrTreeJson && typeof templateOrTreeJson === 'object') {
    return templateOrTreeJson
  }

  const isJson = typeof templateOrTreeJson === 'string' && templateOrTreeJson.trim().startsWith('[')
  return isJson ? { treeJson: templateOrTreeJson } : { templateHtml: templateOrTreeJson }
}

export function savePageContent(id, pageType, templateOrTreeJson) {
  const data = normalizePageContentPayload(templateOrTreeJson)
  return executeCodegenAction('savePageContent', { entityId: id, id, pageType, ...data }, () =>
    request({
      url: `/codegen/moduleentity/savepagecontent/${id}`,
      method: 'post',
      params: { pageType },
      data
    })
  )
}

/**
 * 获取页面 ScriptSection JSON（设计器 Script 标签页）
 */
export function getPageScript(id, pageType) {
  return executeCodegenAction('getPageScript', { entityId: id, id, pageType }, () =>
    request({
      url: `/codegen/moduleentity/getpagescript/${id}`,
      method: 'get',
      params: { pageType }
    })
  )
}

/**
 * 保存页面 ScriptSection JSON（设计器 Script 标签页回写）
 */
export function savePageScript(id, pageType, scriptJson) {
  return executeCodegenAction('savePageScript', { entityId: id, id, pageType, scriptJson }, () =>
    request({
      url: `/codegen/moduleentity/savepagescript/${id}`,
      method: 'post',
      params: { pageType },
      data: scriptJson
    })
  )
}

/**
 * 获取所有字段级 ScriptSection（key=gen_id → ScriptSection JSON）
 */
export function getFieldScripts(id, pageType) {
  return executeCodegenAction('getFieldScripts', { entityId: id, id, pageType }, () =>
    request({
      url: `/codegen/moduleentity/getfieldscripts/${id}`,
      method: 'get',
      params: { pageType }
    })
  )
}

/**
 * 保存所有字段级 ScriptSection
 */
export function saveFieldScripts(id, pageType, scriptsJson) {
  return executeCodegenAction('saveFieldScripts', { entityId: id, id, pageType, scripts: scriptsJson }, () =>
    request({
      url: `/codegen/moduleentity/savefieldscripts/${id}`,
      method: 'post',
      params: { pageType },
      data: scriptsJson
    })
  )
}

// ====== 模板管理 API ======

export function getPageTemplates() {
  return request({ url: '/codegen/moduleentity/getpagetemplates', method: 'get' })
}

export function savePageTemplate(data) {
  return request({ url: '/codegen/moduleentity/savepagetemplate', method: 'post', data })
}

export function getFieldControlTemplates() {
  return request({ url: '/codegen/moduleentity/getfieldcontroltemplates', method: 'get' })
}

export function saveFieldControlTemplate(data) {
  return request({ url: '/codegen/moduleentity/savefieldcontroltemplate', method: 'post', data })
}

export function getChildTemplates() {
  return request({ url: '/codegen/moduleentity/getchildtemplates', method: 'get' })
}

export function saveChildTemplate(data) {
  return request({ url: '/codegen/moduleentity/savechildtemplate', method: 'post', data })
}
