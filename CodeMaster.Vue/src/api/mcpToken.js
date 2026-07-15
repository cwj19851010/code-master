import request from '@/utils/request'

export function listMcpTokens() {
  return request({
    url: '/mcp/token',
    method: 'get'
  })
}

export function createMcpToken(data) {
  return request({
    url: '/mcp/token',
    method: 'post',
    data
  })
}

export function revokeMcpToken(id) {
  return request({
    url: `/mcp/token/${id}`,
    method: 'delete'
  })
}
