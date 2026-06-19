import { request } from './client'

export async function getTickets(params) {
  let url = '/tickets'

  if (params) {
    // убираем пустые значения из параметров
    const filtered = {}
    for (const key in params) {
      if (params[key] !== '' && params[key] !== null && params[key] !== undefined) {
        filtered[key] = params[key]
      }
    }
    const qs = new URLSearchParams(filtered).toString()
    if (qs) url = url + '?' + qs
  }

  const res = await request(url)
  const data = await res.json()

  if (!res.ok) throw new Error(data.error || 'Не удалось загрузить обращения')
  return data
}

export async function getMyTickets() {
  const res = await request('/tickets/my')
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка')
  return data
}

export async function getTicket(id) {
  const res = await request('/tickets/' + id)
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Обращение не найдено')
  return data
}

export async function createTicket(body) {
  const res = await request('/tickets', {
    method: 'POST',
    body: JSON.stringify(body),
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка создания обращения')
  return data
}

export async function changeStatus(id, status, comment) {
  const res = await request('/tickets/' + id + '/status', {
    method: 'PATCH',
    body: JSON.stringify({ status, comment: comment || '' }),
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка смены статуса')
  return data
}

export async function assignTicket(id, assigneeId) {
  const res = await request('/tickets/' + id + '/assignee', {
    method: 'PATCH',
    body: JSON.stringify({ assigneeId }),
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка назначения')
  return data
}
