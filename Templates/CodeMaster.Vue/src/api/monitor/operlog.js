import request from '@/utils/request'

// 获取操作日志分页列表
export function getPagedList(params) {
  return request({
    url: '/monitor/operlog/getpagedlist',
    method: 'get',
    params
  })
}

// 根据ID获取操作日志详情
export function getById(id) {
  return request({
    url: `/monitor/operlog/getbyid/${id}`,
    method: 'get'
  })
}

// 删除操作日志
export function deleteById(id) {
  return request({
    url: `/monitor/operlog/delete/${id}`,
    method: 'delete'
  })
}

// 批量删除操作日志
export function deleteBatch(ids) {
  return request({
    url: '/monitor/operlog/deletebatch',
    method: 'delete',
    data: ids
  })
}

// 清空操作日志
export function clear() {
  return request({
    url: '/monitor/operlog/clear',
    method: 'delete'
  })
}
