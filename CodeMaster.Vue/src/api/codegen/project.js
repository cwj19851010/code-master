import request from '@/utils/request'
import { executeCodegenAction } from '@/utils/codegenExecution'

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
  return executeCodegenAction('initializeProject', { projectId: id, id }, () =>
    request({
      url: '/codegen/project/initialize',
      method: 'post',
      data: { id },
      timeout: 300000 // 5分钟超时
    })
  )
}

/**
 * 启动项目（旧版，整体启动）
 */
export function startProject(id) {
  return executeCodegenAction('startProject', { projectId: id, id }, () =>
    request({
      url: `/codegen/project/start/${id}`,
      method: 'post'
    })
  )
}

/**
 * 停止项目（旧版，整体停止）
 */
export function stopProject(id) {
  return executeCodegenAction('stopProject', { projectId: id, id }, () =>
    request({
      url: `/codegen/project/stop/${id}`,
      method: 'post'
    })
  )
}

/**
 * 独立启动前端（npm run dev）
 */
export function startFrontend(projectId) {
  return executeCodegenAction('startFrontend', { projectId }, () =>
    request({
      url: '/codegen/project/startfrontend',
      method: 'post',
      data: { projectId },
      timeout: 30000
    })
  )
}

/**
 * 独立启动后端（dotnet run）
 */
export function startBackend(projectId) {
  return executeCodegenAction('startBackend', { projectId }, () =>
    request({
      url: '/codegen/project/startbackend',
      method: 'post',
      data: { projectId },
      timeout: 30000
    })
  )
}

/**
 * 独立停止前端进程
 */
export function stopFrontend(projectId) {
  return executeCodegenAction('stopFrontend', { projectId }, () =>
    request({
      url: '/codegen/project/stopfrontend',
      method: 'post',
      data: { projectId },
      timeout: 15000
    })
  )
}

/**
 * 独立停止后端进程
 */
export function stopBackend(projectId) {
  return executeCodegenAction('stopBackend', { projectId }, () =>
    request({
      url: '/codegen/project/stopbackend',
      method: 'post',
      data: { projectId },
      timeout: 15000
    })
  )
}

/**
 * 查询前后端运行状态
 */
export function getProjectStatus(projectId) {
  return executeCodegenAction('getProjectStatus', { projectId }, () =>
    request({
      url: '/codegen/project/getstatus',
      method: 'post',
      data: { projectId }
    })
  )
}

/**
 * 执行数据库迁移（add-migration + database update）
 */
export function migrateDatabase(projectId) {
  return executeCodegenAction('migrateDatabase', { projectId }, () =>
    request({
      url: '/codegen/project/migratedatabase',
      method: 'post',
      data: { projectId },
      timeout: 300000 // 5分钟超时（迁移耗时较长）
    })
  )
}

/**
 * 编译目标项目（dotnet build）
 */
export function buildProject(projectId) {
  return executeCodegenAction('buildProject', { projectId }, () =>
    request({
      url: '/codegen/project/build',
      method: 'post',
      data: { projectId },
      timeout: 180000 // 3分钟超时
    })
  )
}

export function enhanceUiPage(data) {
  return executeCodegenAction('enhanceUiPage', data, () =>
    request({
      url: '/codegen/project/enhanceuipage',
      method: 'post',
      data,
      timeout: 120000
    })
  )
}

/**
 * 导出模板
 */
export function exportTemplate(outputPath = '') {
  return request({
    url: '/codegen/project/export-template',
    method: 'post',
    data: { outputPath }
  })
}

/**
 * 获取模板 Base64（用于客户端）
 */
export function getTemplateBase64() {
  return request({
    url: '/codegen/project/template-base64',
    method: 'get'
  })
}

/**
 * 下载服务端最新项目模板 ZIP
 */
export function downloadTemplate() {
  return request({
    url: '/codegen/project/template/download',
    method: 'get',
    responseType: 'blob',
    timeout: 120000
  })
}

export function downloadTemplateToLocal() {
  return executeCodegenAction('downloadTemplate', {}, () => {
    throw new Error('当前环境不支持本地模板下载')
  })
}

/**
 * 上传服务端项目模板 ZIP（仅宿主 Admin）
 */
export function uploadTemplate(file) {
  const formData = new FormData()
  formData.append('file', file)

  return request({
    url: '/codegen/project/template/upload',
    method: 'post',
    data: formData,
    timeout: 120000
  })
}

/**
 * 获取客户端初始化数据（包含模板 Base64）
 */
export function getClientInitializeData(id) {
  return request({
    url: `/codegen/project/${id}/client-init-data`,
    method: 'get'
  })
}

/**
 * 获取客户端本地执行代码生成所需的完整上下文快照
 */
export function getGenerationBundle(projectId) {
  return request({
    url: `/codegen/project/${projectId}/generation-bundle`,
    method: 'get',
    timeout: 60000
  })
}

// ========== 分步初始化 API ==========

/**
 * 步骤1：解压模板并替换项目名称
 */
export function initializeStep1(data) {
  return executeCodegenAction('initializeStep1', data, () =>
    request({
      url: '/codegen/project/initialize/step1',
      method: 'post',
      data,
      timeout: 60000 // 1分钟
    })
  )
}

/**
 * 步骤2：生成解决方案文件
 */
export function initializeStep2(data) {
  return executeCodegenAction('initializeStep2', data, () =>
    request({
      url: '/codegen/project/initialize/step2',
      method: 'post',
      data,
      timeout: 30000
    })
  )
}

/**
 * 步骤3：更新数据库配置
 */
export function initializeStep3(data) {
  return executeCodegenAction('initializeStep3', data, () =>
    request({
      url: '/codegen/project/initialize/step3',
      method: 'post',
      data,
      timeout: 30000
    })
  )
}

/**
 * 步骤4：更新端口配置
 */
export function initializeStep4(data) {
  return executeCodegenAction('initializeStep4', data, () =>
    request({
      url: '/codegen/project/initialize/step4',
      method: 'post',
      data,
      timeout: 30000
    })
  )
}

/**
 * 步骤5：创建数据库迁移
 */
export function initializeStep5(data) {
  return executeCodegenAction('initializeStep5', data, () =>
    request({
      url: '/codegen/project/initialize/step5',
      method: 'post',
      data,
      timeout: 120000 // 2分钟
    })
  )
}

/**
 * 步骤6：应用数据库迁移
 */
export function initializeStep6(data) {
  return executeCodegenAction('initializeStep6', data, () =>
    request({
      url: '/codegen/project/initialize/step6',
      method: 'post',
      data,
      timeout: 120000 // 2分钟
    })
  )
}

/**
 * 步骤7：运行 dotnet restore
 */
export function initializeStep7(data) {
  return executeCodegenAction('initializeStep7', data, () =>
    request({
      url: '/codegen/project/initialize/step7',
      method: 'post',
      data,
      timeout: 180000 // 3分钟
    })
  )
}

/**
 * 步骤8：写入项目翻译
 */
export function initializeStep8(data) {
  return executeCodegenAction('initializeStep8', data, () =>
    request({
      url: '/codegen/project/initialize/step8',
      method: 'post',
      data,
      timeout: 30000
    })
  )
}

/**
 * 步骤9：运行 npm install
 */
export function initializeStep9(data) {
  return executeCodegenAction('initializeStep9', data, () =>
    request({
      url: '/codegen/project/initialize/step9',
      method: 'post',
      data,
      timeout: 300000 // 5分钟
    })
  )
}

/**
 * 步骤10：启动后端服务
 */
export function initializeStep10(data) {
  return executeCodegenAction('initializeStep10', data, () =>
    request({
      url: '/codegen/project/initialize/step10',
      method: 'post',
      data,
      timeout: 60000 // 1分钟
    })
  )
}

/**
 * 步骤11：启动前端服务
 */
export function initializeStep11(data) {
  return executeCodegenAction('initializeStep11', data, () =>
    request({
      url: '/codegen/project/initialize/step11',
      method: 'post',
      data,
      timeout: 60000 // 1分钟
    })
  )
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

/**
 * 获取目标项目的字典类型列表
 */
export function getDictTypes(projectId) {
  return executeCodegenAction('getDictTypes', { projectId }, () =>
    request({
      url: `/codegen/project/getdicttypes/${projectId}`,
      method: 'get'
    })
  )
}
