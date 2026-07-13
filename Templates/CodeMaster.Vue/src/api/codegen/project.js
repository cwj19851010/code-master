import request from '@/utils/request'

/**
 * 获取项目列表（不分页）
 */
export function getList(data = {}) {
  return request({
    url: '/codegen/project/getlist',
    method: 'get',
    params: data
  })
}

/**
 * 获取项目分页列表
 */
export function getPagedList(data) {
  return request({
    url: '/codegen/project/getpagedlist',
    method: 'get',
    params: data
  })
}

/**
 * 根据ID获取项目
 */
export function getById(id) {
  return request({
    url: `/codegen/project/getbyid/${id}`,
    method: 'get'
  })
}

/**
 * 创建项目
 */
export function create(data) {
  return request({
    url: '/codegen/project/create',
    method: 'post',
    data
  })
}

/**
 * 更新项目
 */
export function update(id, data) {
  return request({
    url: `/codegen/project/update/${id}`,
    method: 'put',
    data
  })
}

/**
 * 删除项目
 */
export function deleteById(id) {
  return request({
    url: `/codegen/project/delete/${id}`,
    method: 'delete',
  })
}

/**
 * 初始化项目
 */
export function initializeProject(id) {
  return request({
    url: '/codegen/project/initialize',
    method: 'post',
    data: { id },
    timeout: 300000 // 5分钟超时
  })
}

/**
 * 启动项目
 */
export function startProject(id) {
  return request({
    url: `/codegen/project/start/${id}`,
    method: 'post'
  })
}

/**
 * 停止项目
 */
export function stopProject(id) {
  return request({
    url: `/codegen/project/stop/${id}`,
    method: 'post'
  })
}

/**
 * 导出模板
 */
export function exportTemplate(outputPath = '') {
  return request({
    url: '/codegen/project/exporttemplate',
    method: 'post',
    data: { outputPath }
  })
}

/**
 * 获取模板 Base64（用于客户端）
 */
export function getTemplateBase64() {
  return request({
    url: '/codegen/project/gettemplatebase64',
    method: 'get'
  })
}

/**
 * 获取客户端初始化数据（包含模板 Base64）
 */
export function getClientInitializeData(id) {
  return request({
    url: `/codegen/project/getclientinitializedata/${id}`,
    method: 'get'
  })
}

// ========== 分步初始化 API ==========

/**
 * 步骤1：解压模板并替换项目名称
 */
export function initializeStep1(data) {
  return request({
    url: '/codegen/project/initialize/step1',
    method: 'post',
    data,
    timeout: 60000 // 1分钟
  })
}

/**
 * 步骤2：生成解决方案文件
 */
export function initializeStep2(data) {
  return request({
    url: '/codegen/project/initialize/step2',
    method: 'post',
    data,
    timeout: 30000
  })
}

/**
 * 步骤3：更新数据库配置
 */
export function initializeStep3(data) {
  return request({
    url: '/codegen/project/initialize/step3',
    method: 'post',
    data,
    timeout: 30000
  })
}

/**
 * 步骤4：更新端口配置
 */
export function initializeStep4(data) {
  return request({
    url: '/codegen/project/initialize/step4',
    method: 'post',
    data,
    timeout: 30000
  })
}

/**
 * 步骤5：创建数据库迁移
 */
export function initializeStep5(data) {
  return request({
    url: '/codegen/project/initialize/step5',
    method: 'post',
    data,
    timeout: 120000 // 2分钟
  })
}

/**
 * 步骤6：应用数据库迁移
 */
export function initializeStep6(data) {
  return request({
    url: '/codegen/project/initialize/step6',
    method: 'post',
    data,
    timeout: 120000 // 2分钟
  })
}

/**
 * 步骤7：运行 dotnet restore
 */
export function initializeStep7(data) {
  return request({
    url: '/codegen/project/initialize/step7',
    method: 'post',
    data,
    timeout: 180000 // 3分钟
  })
}

/**
 * 步骤8：写入项目翻译
 */
export function initializeStep8(data) {
  return request({
    url: '/codegen/project/initialize/step8',
    method: 'post',
    data,
    timeout: 30000
  })
}

/**
 * 步骤9：运行 npm install
 */
export function initializeStep9(data) {
  return request({
    url: '/codegen/project/initialize/step9',
    method: 'post',
    data,
    timeout: 300000 // 5分钟
  })
}

/**
 * 步骤10：启动后端服务
 */
export function initializeStep10(data) {
  return request({
    url: '/codegen/project/initialize/step10',
    method: 'post',
    data,
    timeout: 60000 // 1分钟
  })
}

/**
 * 步骤11：启动前端服务
 */
export function initializeStep11(data) {
  return request({
    url: '/codegen/project/initialize/step11',
    method: 'post',
    data,
    timeout: 60000 // 1分钟
  })
}

/**
 * 获取初始化状态
 */
export function getInitializationState(id) {
  return request({
    url: `/codegen/project/${id}/initialization-state`,
    method: 'get'
  })
}
