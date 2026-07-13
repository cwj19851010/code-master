/**
 * 将扁平数组转换为树形结构
 * @param {Array} list - 扁平数组
 * @param {Object} options - 配置选项
 * @param {string} options.idKey - ID字段名，默认'id'
 * @param {string} options.parentIdKey - 父ID字段名，默认'parentId'
 * @param {string} options.childrenKey - 子节点字段名，默认'children'
 * @returns {Array} 树形结构数组
 */
export function buildTree(list, options = {}) {
  const {
    idKey = 'id',
    parentIdKey = 'parentId',
    childrenKey = 'children'
  } = options

  if (!list || !Array.isArray(list)) {
    return []
  }

  // 创建ID映射表
  const map = {}
  const result = []

  // 第一遍遍历：创建映射表
  list.forEach(item => {
    map[item[idKey]] = { ...item, [childrenKey]: [] }
  })

  // 第二遍遍历：构建树形结构
  list.forEach(item => {
    const node = map[item[idKey]]
    const parentId = item[parentIdKey]

    // 如果没有父节点或父节点不存在，则为顶级节点
    if (!parentId || !map[parentId]) {
      result.push(node)
    } else {
      // 添加到父节点的children中
      map[parentId][childrenKey].push(node)
    }
  })

  return result
}

/**
 * 树形结构转扁平数组
 * @param {Array} tree - 树形结构数组
 * @param {string} childrenKey - 子节点字段名，默认'children'
 * @returns {Array} 扁平数组
 */
export function flattenTree(tree, childrenKey = 'children') {
  const result = []

  function traverse(nodes) {
    nodes.forEach(node => {
      const { [childrenKey]: children, ...rest } = node
      result.push(rest)
      if (children && children.length > 0) {
        traverse(children)
      }
    })
  }

  traverse(tree)
  return result
}

/**
 * 查找树节点
 * @param {Array} tree - 树形结构数组
 * @param {Function} predicate - 查找条件函数
 * @param {string} childrenKey - 子节点字段名，默认'children'
 * @returns {Object|null} 找到的节点或null
 */
export function findTreeNode(tree, predicate, childrenKey = 'children') {
  for (const node of tree) {
    if (predicate(node)) {
      return node
    }
    if (node[childrenKey] && node[childrenKey].length > 0) {
      const found = findTreeNode(node[childrenKey], predicate, childrenKey)
      if (found) {
        return found
      }
    }
  }
  return null
}

/**
 * 过滤树节点
 * @param {Array} tree - 树形结构数组
 * @param {Function} predicate - 过滤条件函数
 * @param {string} childrenKey - 子节点字段名，默认'children'
 * @returns {Array} 过滤后的树形结构
 */
export function filterTree(tree, predicate, childrenKey = 'children') {
  return tree
    .filter(node => {
      if (node[childrenKey] && node[childrenKey].length > 0) {
        node[childrenKey] = filterTree(node[childrenKey], predicate, childrenKey)
      }
      return predicate(node) || (node[childrenKey] && node[childrenKey].length > 0)
    })
    .map(node => ({ ...node }))
}
