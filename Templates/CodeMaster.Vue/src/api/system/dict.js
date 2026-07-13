import request from '@/utils/request'

// 字典类型
export function getTypePagedList(params) {
  return request({
    url: '/system/dicttype/getpagedlist',
    method: 'get',
    params
  })
}

export function createType(data) {
  return request({
    url: '/system/dicttype/create',
    method: 'post',
    data
  })
}

export function updateType(id, data) {
  return request({
    url: `/system/dicttype/update/${id}`,
    method: 'put',
    data
  })
}

export function getTypeById(id) {
  return request({
    url: `/system/dicttype/getbyid/${id}`,
    method: 'get'
  })
}

export function deleteTypeById(id) {
  return request({
    url: `/system/dicttype/delete/${id}`,
    method: 'delete'
  })
}

// 字典数据
export function getDataPagedList(params) {
  return request({
    url: '/system/dictdata/getpagedlist',
    method: 'get',
    params
  })
}

export function getDataListByType(dictType) {
  return request({ url: '/system/dictdata/getlistbytype', method: 'get', params: { dictType } })
}

export function getDataById(id) {
  return request({
    url: `/system/dictdata/getbyid/${id}`,
    method: 'get'
  })
}

export function createData(data) {
  return request({
    url: '/system/dictdata/create',
    method: 'post',
    data
  })
}

export function updateData(id, data) {
  return request({
    url: `/system/dictdata/update/${id}`,
    method: 'put',
    data
  })
}

export function deleteDataById(id) {
  return request({
    url: `/system/dictdata/delete/${id}`,
    method: 'delete'
  })
}
