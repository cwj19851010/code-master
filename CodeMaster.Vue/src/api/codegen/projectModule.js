import request from '@/utils/request'
import { executeCodegenAction } from '@/utils/codegenExecution'

/**
 * 获取全部项目模块列表
 */
export function getList(data = {}) {
  return request({
    url: '/codegen/projectmodule/getlist',
    method: 'get',
    params: data
  })
}

/**
 * 根据项目ID获取模块列表
 */
export function getByProjectId(projectId) {
  return request({
    url: `/codegen/projectmodule/getbyprojectid/${projectId}`,
    method: 'get'
  })
}

/**
 * 获取项目模块列表
 */
export function getPagedList(data) {
  return request({
    url: '/codegen/projectmodule/getpagedlist',
    method: 'get',
    params: data
  })
}

/**
 * 获取项目模块详情
 */
export function getById(id) {
  return request({
    url: `/codegen/projectmodule/getbyid/${id}`,
    method: 'get'
  })
}

/**
 * 创建项目模块
 */
export function create(data) {
  return request({
    url: '/codegen/projectmodule/create',
    method: 'post',
    data
  })
}

/**
 * 更新项目模块
 */
export function update(id, data) {
  return request({
    url: `/codegen/projectmodule/update/${id}`,
    method: 'put',
    data
  })
}

/**
 * 删除项目模块
 */
export function deleteById(id) {
  return request({
    url: `/codegen/projectmodule/delete/${id}`,
    method: 'delete'
  })
}

/**
 * 更新模块到菜单（服务端模式）
 */
export function updateModuleToMenu(moduleId) {
  return executeCodegenAction('syncModuleToMenu', { moduleId }, () =>
    request({
      url: `/codegen/projectmodule/syncmoduletomenu/${moduleId}`,
      method: 'post'
    })
  )
}
