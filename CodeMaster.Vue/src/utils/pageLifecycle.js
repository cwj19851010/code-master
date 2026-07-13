import { onActivated } from 'vue'

export function resetReactiveForm(form, initialValues = {}) {
  Object.keys(form).forEach((key) => {
    delete form[key]
  })
  Object.assign(form, cloneValue(initialValues))
}

export function resetFormValidation(formRef) {
  setTimeout(() => {
    formRef?.value?.clearValidate?.()
  })
}

export function refreshOnReactivated(refresh) {
  let activatedOnce = false
  onActivated(() => {
    if (activatedOnce) {
      refresh()
      return
    }
    activatedOnce = true
  })
}

function cloneValue(value) {
  if (Array.isArray(value)) {
    return value.map((item) => cloneValue(item))
  }
  if (value && typeof value === 'object') {
    return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, cloneValue(item)]))
  }
  return value
}
