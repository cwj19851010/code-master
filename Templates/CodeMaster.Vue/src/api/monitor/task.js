import request from '@/utils/request'

// 查询任务列表
export function getPagedList(query) {
  return request({
    url: '/monitor/task/getpagedlist',
    method: 'get',
    params: query
  })
}

// 查询任务详细
export function getById(id) {
  return request({
    url: `/monitor/task/getbyid/${id}`,
    method: 'get'
  })
}

// 新增任务
export function create(data) {
  return request({
    url: '/monitor/task/create',
    method: 'post',
    data
  })
}

// 修改任务
export function update(id, data) {
  return request({
    url: `/monitor/task/update/${id}`,
    method: 'put',
    data
  })
}

// 删除任务
export function deleteById(id) {
  return request({
    url: `/monitor/task/delete/${id}`,
    method: 'delete'
  })
}

// 任务状态修改
export function changeTaskStatus(id, status) {
  return request({
    url: `/monitor/task/${status === 0 ? 'start' : 'pause'}/${id}`,
    method: 'post'
  })
}

// 立即执行任务
export function runTask(id) {
  return request({
    url: `/monitor/task/run/${id}`,
    method: 'post'
  })
}
