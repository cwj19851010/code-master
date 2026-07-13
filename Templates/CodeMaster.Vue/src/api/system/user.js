import request from '@/utils/request'

// 获取用户分页列表
export function getPagedList(params) {
  return request({
    url: '/system/user/getpagedlist',
    method: 'get',
    params
  })
}

// 获取用户详情
export function getById(id) {
  return request({
    url: `/system/user/getbyid/${id}`,
    method: 'get'
  })
}

// 创建用户
export function create(data) {
  return request({
    url: '/system/user/create',
    method: 'post',
    data
  })
}

// 更新用户
export function update(id, data) {
  return request({
    url: `/system/user/update/${id}`,
    method: 'put',
    data
  })
}

// 删除用户
export function deleteById(id) {
  return request({
    url: `/system/user/delete/${id}`,
    method: 'delete'
  })
}
