import request from '@/utils/request'

// 获取角色列表
export function getPagedList(params) {
  return request({
    url: '/system/role/getpagedlist',
    method: 'get',
    params
  })
}

// 获取角色详情
export function getById(id) {
  return request({
    url: `/system/role/getbyid/${id}`,
    method: 'get'
  })
}

// 创建角色
export function create(data) {
  return request({
    url: '/system/role/create',
    method: 'post',
    data
  })
}

// 更新角色
export function update(id, data) {
  return request({
    url: `/system/role/update/${id}`,
    method: 'put',
    data
  })
}

// 删除角色
export function deleteById(id) {
  return request({
    url: `/system/role/delete/${id}`,
    method: 'delete'
  })
}

// 批量删除角色
export function deleteBatch(ids) {
  return request({
    url: '/system/role/deletebatch',
    method: 'post',
    data: { ids }
  })
}

// 获取角色已分配的菜单ID列表
export function getRoleMenuIds(roleId) {
  return request({
    url: `/system/role/getrolemenuids/${roleId}`,
    method: 'get'
  })
}

// 分配菜单给角色
export function assignMenus(roleId, menuIds) {
  return request({
    url: `/system/role/assignmenus/${roleId}`,
    method: 'post',
    data: menuIds
  })
}
