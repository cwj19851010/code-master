import request from '@/utils/request'

/**
 * 分页查询租户
 */
export function getPagedList(params) {
  return request({
    url: '/system/tenant/getpagedlist',
    method: 'get',
    params
  })
}

/**
 * 根据ID获取租户
 */
export function getById(id) {
  return request({
    url: `/system/tenant/getbyid/${id}`,
    method: 'get'
  })
}

/**
 * 创建租户
 */
export function create(data) {
  return request({
    url: '/system/tenant/create',
    method: 'post',
    data
  })
}

/**
 * 更新租户
 */
export function update(id, data) {
  return request({
    url: `/system/tenant/update/${id}`,
    method: 'put',
    data
  })
}

/**
 * 删除租户
 */
export function deleteById(id) {
  return request({
    url: `/system/tenant/delete/${id}`,
    method: 'delete'
  })
}

/**
 * 测试数据库连接
 */
export function testConnection(data) {
  return request({
    url: '/system/tenant/testconnection',
    method: 'post',
    data
  })
}
