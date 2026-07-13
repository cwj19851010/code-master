import request from '@/utils/request'

// 获取部门列表
export function getList(params) {
  return request({
    url: '/system/dept/getlist',
    method: 'get',
    params
  })
}

// 获取部门树（使用 getlist 接口，前端自行构建树结构）
export function getTree() {
  return request({
    url: '/system/dept/getlist',
    method: 'get'
  })
}

// 获取部门详情
export function getById(id) {
  return request({
    url: `/system/dept/getbyid/${id}`,
    method: 'get'
  })
}

// 创建部门
export function create(data) {
  return request({
    url: '/system/dept/create',
    method: 'post',
    data
  })
}

// 更新部门
export function update(id, data) {
  return request({
    url: `/system/dept/update/${id}`,
    method: 'put',
    data
  })
}

// 删除部门
export function deleteById(id) {
  return request({
    url: `/system/dept/delete/${id}`,
    method: 'delete'
  })
}
