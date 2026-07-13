import request from '@/utils/request'

// 获取登录日志分页列表
export function getPagedList(params) {
  return request({
    url: '/monitor/loginlog/getpagedlist',
    method: 'get',
    params
  })
}

// 根据ID获取登录日志详情
export function getById(id) {
  return request({
    url: `/monitor/loginlog/getbyid/${id}`,
    method: 'get'
  })
}

// 删除登录日志
export function deleteById(id) {
  return request({
    url: `/monitor/loginlog/delete/${id}`,
    method: 'delete'
  })
}

// 批量删除登录日志
export function deleteBatch(ids) {
  return request({
    url: '/monitor/loginlog/deletebatch',
    method: 'delete',
    data: ids
  })
}

// 清空登录日志
export function clear() {
  return request({
    url: '/monitor/loginlog/clear',
    method: 'delete'
  })
}
