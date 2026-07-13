import request from '@/utils/request'

// 获取在线用户列表
export const getOnlineUsers = () => {
  return request({
    url: '/monitor/online/list',
    method: 'get'
  })
}

// 强制用户下线
export const forceOffline = (userId) => {
  return request({
    url: `/monitor/online/force-offline/${userId}`,
    method: 'post'
  })
}
