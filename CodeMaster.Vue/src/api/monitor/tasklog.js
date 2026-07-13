import request from '@/utils/request'

// 查询任务日志列表
export function getPagedList(query) {
  return request({
    url: '/monitor/tasklog/getpagedlist',
    method: 'get',
    params: query
  })
}

// 查询任务日志详细
export function getById(id) {
  return request({
    url: `/monitor/tasklog/getbyid/${id}`,
    method: 'get'
  })
}

// 批量删除任务日志
export function deleteBatch(ids) {
  return request({
    url: '/monitor/tasklog/deletebatch',
    method: 'delete',
    data: ids
  })
}

// 清空任务日志
export function clear() {
  return request({
    url: '/monitor/tasklog/clear',
    method: 'delete'
  })
}
