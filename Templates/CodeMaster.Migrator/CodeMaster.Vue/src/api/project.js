import request from '@/utils/request'

// 获取项目列表
export function getProjectList(data) {
  return request({
    url: '/api/system/project/getpagedlist',
    method: 'post',
    data
  })
}

// 获取项目详情
export function getProjectById(id) {
  return request({
    url: `/api/system/project/getbyid/${id}`,
    method: 'get'
  })
}

// 创建项目
export function createProject(data) {
  return request({
    url: '/api/system/project/create',
    method: 'post',
    data
  })
}

// 更新项目
export function updateProject(id, data) {
  return request({
    url: `/api/system/project/update/${id}`,
    method: 'put',
    data
  })
}

// 删除项目
export function deleteProject(id) {
  return request({
    url: `/api/system/project/delete/${id}`,
    method: 'delete'
  })
}

// 初始化项目
export function initializeProject(id) {
  return request({
    url: `/api/system/project/initialize/${id}`,
    method: 'post'
  })
}

// 启动项目
export function startProject(id) {
  return request({
    url: `/api/system/project/start/${id}`,
    method: 'post'
  })
}

// 停止项目
export function stopProject(id) {
  return request({
    url: `/api/system/project/stop/${id}`,
    method: 'post'
  })
}
