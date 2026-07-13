function pad(value) {
  return String(value).padStart(2, '0')
}

function normalizeUtcInput(value) {
  if (typeof value !== 'string') return value
  const text = value.trim()
  if (!text) return text

  // Backend UTC values should include Z/offset. Treat legacy ISO values without
  // timezone as UTC too, so old PostgreSQL/MySQL rows display consistently.
  const hasTimezone = /(?:z|[+-]\d{2}:?\d{2})$/i.test(text)
  const looksIsoDateTime = /^\d{4}-\d{2}-\d{2}[T\s]\d{2}:\d{2}/.test(text)
  return looksIsoDateTime && !hasTimezone ? `${text.replace(' ', 'T')}Z` : text
}

export function toDate(value) {
  if (!value) return null
  const date = value instanceof Date ? value : new Date(normalizeUtcInput(value))
  return Number.isNaN(date.getTime()) ? null : date
}

export function formatDateTime(value, withSeconds = true) {
  const date = toDate(value)
  if (!date) return ''

  const time = withSeconds
    ? `${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
    : `${pad(date.getHours())}:${pad(date.getMinutes())}`

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${time}`
}

export function formatDate(value) {
  const date = toDate(value)
  if (!date) return ''
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

export function toUtcIsoString(value) {
  const date = toDate(value)
  return date ? date.toISOString() : null
}

export function toUtcRange(beginTime, endTime) {
  return {
    beginTime: toUtcIsoString(beginTime),
    endTime: toUtcIsoString(endTime)
  }
}
