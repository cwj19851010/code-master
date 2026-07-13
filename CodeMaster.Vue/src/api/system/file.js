import request from '@/utils/request'

/**
 * 分页查询文件
 */
export function getFileList(params) {
  return request({
    url: '/system/file/getpagedlist',
    method: 'get',
    params
  })
}

/**
 * 根据ID获取文件
 */
export function getFileById(id) {
  return request({
    url: `/system/file/getbyid/${id}`,
    method: 'get'
  })
}

/**
 * 删除文件
 */
export function deleteFile(id) {
  return request({
    url: `/system/file/delete/${id}`,
    method: 'delete'
  })
}

/**
 * 下载文件
 */
export function upload(file) {
  const fd = new FormData()
  fd.append('file', file)
  return request({ url: '/system/file/upload', method: 'post', data: fd, headers: { 'Content-Type': 'multipart/form-data' } })
}

export function downloadFile(id) {
  return request({
    url: `/system/file/downloadfile/${id}`,
    method: 'get',
    responseType: 'blob'
  })
}
