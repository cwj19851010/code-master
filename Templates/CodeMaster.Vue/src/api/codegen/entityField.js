import request from '@/utils/request'

/**
 * 获取实体字段列表
 */
export function getPagedList(params) {
  return request({
    url: '/codegen/entityfield/getpagedlist',
    method: 'get',
    params
  })
}

/**
 * 获取实体字段详情
 */
export function getById(id) {
  return request({
    url: `/codegen/entityfield/getbyid/${id}`,
    method: 'get'
  })
}

/**
 * 创建实体字段
 */
export function create(data) {
  return request({
    url: '/codegen/entityfield/create',
    method: 'post',
    data
  })
}

/**
 * 更新实体字段
 */
export function update(id, data) {
  return request({
    url: `/codegen/entityfield/update/${id}`,
    method: 'put',
    data
  })
}

/**
 * 删除实体字段
 */
export function deleteById(id) {
  return request({
    url: `/codegen/entityfield/delete/${id}`,
    method: 'delete'
  })
}

/**
 * 根据模块实体ID获取字段列表
 */
export function getByEntityId(moduleEntityId) {
  return request({
    url: `/codegen/entityfield/getbyentityid/${moduleEntityId}`,
    method: 'get'
  })
}

/**
 * 批量更新字段
 */
export function updateBatch(data) {
  return request({
    url: '/codegen/entityfield/updatebatch',
    method: 'put',
    data
  })
}
