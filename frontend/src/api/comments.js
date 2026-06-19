import { request } from './client'

export async function getComments(ticketId) {
  const res = await request('/tickets/' + ticketId + '/comments')
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка загрузки комментариев')
  return data
}

export async function addComment(ticketId, text, isInternal) {
  const res = await request('/tickets/' + ticketId + '/comments', {
    method: 'POST',
    body: JSON.stringify({ text, isInternal: isInternal || false }),
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка добавления комментария')
  return data
}
