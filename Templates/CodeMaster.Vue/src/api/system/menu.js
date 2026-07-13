import request from '@/utils/request'

// 获取用户路由菜单
export function getRoutes() {
  return request({
    url: '/auth/getRouters',
    method: 'get'
  })
}

// 获取菜单列表（扁平结构）
export function getList(params) {
  return request({
    url: '/system/menu/getlist',
    method: 'get',
    params
  })
}

// 获取菜单详情
export function getById(id) {
  return request({
    url: `/system/menu/getbyid/${id}`,
    method: 'get'
  })
}

// 创建菜单
export function create(data) {
  return request({
    url: '/system/menu/create',
    method: 'post',
    data
  })
}

// 更新菜单
export function update(id, data) {
  return request({
    url: `/system/menu/update/${id}`,
    method: 'put',
    data
  })
}

// 删除菜单
export function deleteById(id) {
  return request({
    url: `/system/menu/delete/${id}`,
    method: 'delete'
  })
}
