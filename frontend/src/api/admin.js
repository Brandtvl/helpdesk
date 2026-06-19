import { request } from './client'

export async function getUsers() {
  const res = await request('/admin/users')
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка загрузки пользователей')
  return data
}

export async function createCategory(name) {
  const res = await request('/categories', {
    method: 'POST',
    body: JSON.stringify({ name }),
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка создания категории')
  return data
}

export async function deleteCategory(id) {
  const res = await request('/categories/' + id, { method: 'DELETE' })
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка удаления категории')
  return data
}

export async function getSlaConfigs() {
  const res = await request('/sla')
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка загрузки нормативов')
  return data
}

export async function updateSla(priority, reactionHours, resolutionHours) {
  const res = await request('/sla/' + priority, {
    method: 'PUT',
    body: JSON.stringify({ reactionHours, resolutionHours }),
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка обновления норматива')
  return data
}

export async function getReportByStatus() {
  const res = await request('/reports/by-status')
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка')
  return data
}

export async function getReportAvgResolution() {
  const res = await request('/reports/avg-resolution')
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка')
  return data
}

export async function getReportExecutorLoad() {
  const res = await request('/reports/executor-load')
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка')
  return data
}

export async function forceStatus(ticketId, status, reason) {
  const res = await request('/admin/tickets/' + ticketId + '/force', {
    method: 'PATCH',
    body: JSON.stringify({ status, reason }),
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка принудительной смены')
  return data
}
