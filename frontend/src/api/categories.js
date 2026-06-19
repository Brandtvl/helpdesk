import { request } from './client'

export async function getCategories() {
  const res = await request('/categories')
  const data = await res.json()
  if (!res.ok) throw new Error(data.error || 'Ошибка')
  return data
}
