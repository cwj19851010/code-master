import request from '@/utils/request'


/**
 * 获取模块实体列表
 */
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
  return request({
    url: `/codegen/moduleentity/generatecode/${id}`,
    method: 'post'
  })
}
