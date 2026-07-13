import request from '@/utils/request'

// 获取默认语言
export function getDefaultLang() {
  return request({
    url: '/system/lang/getdefault',
    method: 'get'
  })
}

// 获取启用语言列表
export function getEnabledLangs() {
  return request({
    url: '/system/lang/getenabledlist',
    method: 'get'
  })
}

// 获取语言文本映射
export function getI18nMap(langCode) {
  return request({
    url: '/system/langtext/geti18nmap',
    method: 'get',
    params: { langCode }
  })
}

// 语言管理
export function getPagedList(params) {
  return request({
    url: '/system/lang/getpagedlist',
    method: 'get',
    params
  })
}

export function create(data) {
  return request({
    url: '/system/lang/create',
    method: 'post',
    data
  })
}

export function update(id, data) {
  return request({
    url: `/system/lang/update/${id}`,
    method: 'put',
    data
  })
}

export function getById(id) {
  return request({
    url: `/system/lang/getbyid/${id}`,
    method: 'get'
  })
}

export function deleteById(id) {
  return request({
    url: `/system/lang/delete/${id}`,
    method: 'delete'
  })
}

// 语言文本管理
export function getTextPagedList(params) {
  return request({
    url: '/system/langtext/getpagedlist',
    method: 'get',
    params
  })
}

export function createText(data) {
  return request({
    url: '/system/langtext/create',
    method: 'post',
    data
  })
}

export function updateText(id, data) {
  return request({
    url: `/system/langtext/update/${id}`,
    method: 'put',
    data
  })
}

export function getTextById(id) {
  return request({
    url: `/system/langtext/getbyid/${id}`,
    method: 'get'
  })
}

export function deleteTextById(id) {
  return request({
    url: `/system/langtext/delete/${id}`,
    method: 'delete'
  })
}
