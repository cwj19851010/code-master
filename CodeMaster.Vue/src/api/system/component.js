import request from '@/utils/request'

// ==================== 组件分组 ====================
export function getGroupList() {
  return request({ url: '/codegen/componentgroup/getlist', method: 'get' })
}

export function getGroupById(id) {
  return request({ url: `/codegen/componentgroup/getbyid/${id}`, method: 'get' })
}

export function createGroup(data) {
  return request({ url: '/codegen/componentgroup/create', method: 'post', data })
}

export function updateGroup(id, data) {
  return request({ url: `/codegen/componentgroup/update/${id}`, method: 'put', data })
}

export function deleteGroup(id) {
  return request({ url: `/codegen/componentgroup/delete/${id}`, method: 'delete' })
}

// ==================== 组件 ====================
export function getComponentPagedList(params) {
  return request({ url: '/codegen/component/getpagedlist', method: 'get', params })
}

export function getComponentListByGroup(groupId) {
  return request({ url: `/codegen/component/getlistbygroup/${groupId}`, method: 'get' })
}

export function getComponentDetail(id) {
  return request({ url: `/codegen/component/getdetail/${id}`, method: 'get' })
}

export function createComponent(data) {
  return request({ url: '/codegen/component/create', method: 'post', data })
}

export function updateComponent(id, data) {
  return request({ url: `/codegen/component/update/${id}`, method: 'put', data })
}

export function deleteComponent(id) {
  return request({ url: `/codegen/component/delete/${id}`, method: 'delete' })
}

// ==================== 组件属性 ====================
export function getPropertyListByComponent(componentId) {
  return request({ url: `/codegen/componentproperty/getlistbycomponent/${componentId}`, method: 'get' })
}

export function createProperty(data) {
  return request({ url: '/codegen/componentproperty/create', method: 'post', data })
}

export function updateProperty(id, data) {
  return request({ url: `/codegen/componentproperty/update/${id}`, method: 'put', data })
}

export function deleteProperty(id) {
  return request({ url: `/codegen/componentproperty/delete/${id}`, method: 'delete' })
}

export function setPropertyCommon(data) {
  return request({ url: '/codegen/componentproperty/setcommon', method: 'post', data })
}

// ==================== 组件事件 ====================
export function getEventListByComponent(componentId) {
  return request({ url: `/codegen/componentevent/getlistbycomponent/${componentId}`, method: 'get' })
}

export function createEvent(data) {
  return request({ url: '/codegen/componentevent/create', method: 'post', data })
}

export function updateEvent(id, data) {
  return request({ url: `/codegen/componentevent/update/${id}`, method: 'put', data })
}

export function deleteEvent(id) {
  return request({ url: `/codegen/componentevent/delete/${id}`, method: 'delete' })
}

export function setEventCommon(data) {
  return request({ url: '/codegen/componentevent/setcommon', method: 'post', data })
}

// ==================== 组件插槽 ====================
export function getSlotListByComponent(componentId) {
  return request({ url: `/codegen/componentslot/getlistbycomponent/${componentId}`, method: 'get' })
}

export function createSlot(data) {
  return request({ url: '/codegen/componentslot/create', method: 'post', data })
}

export function updateSlot(id, data) {
  return request({ url: `/codegen/componentslot/update/${id}`, method: 'put', data })
}

export function deleteSlot(id) {
  return request({ url: `/codegen/componentslot/delete/${id}`, method: 'delete' })
}

// ==================== 组件暴露方法 ====================
export function getExposeListByComponent(componentId) {
  return request({ url: `/codegen/componentexpose/getlistbycomponent/${componentId}`, method: 'get' })
}

export function createExpose(data) {
  return request({ url: '/codegen/componentexpose/create', method: 'post', data })
}

export function updateExpose(id, data) {
  return request({ url: `/codegen/componentexpose/update/${id}`, method: 'put', data })
}

export function deleteExpose(id) {
  return request({ url: `/codegen/componentexpose/delete/${id}`, method: 'delete' })
}

// ==================== 属性面板（设计器用）====================
export function getFieldPropertyPanel(tag) {
  return request({ url: '/codegen/moduleentity/getfieldpropertypanel', method: 'get', params: { tag } })
}
