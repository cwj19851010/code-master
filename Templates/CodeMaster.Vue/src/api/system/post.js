import request from '@/utils/request'

export function getPagedList(params) {
  return request({
    url: '/system/post/getpagedlist',
    method: 'get',
    params
  })
}

export function getById(id) {
  return request({
    url: `/system/post/getbyid/${id}`,
    method: 'get'
  })
}

export function create(data) {
  return request({
    url: '/system/post/create',
    method: 'post',
    data
  })
}

export function update(id, data) {
  return request({
    url: `/system/post/update/${id}`,
    method: 'put',
    data
  })
}

export function deleteById(id) {
  return request({
    url: `/system/post/delete/${id}`,
    method: 'delete'
  })
}

export function deleteBatch(ids) {
  return request({
    url: '/system/post/deletebatch',
    method: 'post',
    data: { ids }
  })
}

export function getAllPostList() {
  return request({
    url: '/system/post/getalllist',
    method: 'get'
  })
}
