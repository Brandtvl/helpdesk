import { request } from './client'

export async function login(email, password) {
  const res = await request('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })

  const data = await res.json()

  if (!res.ok) {
    throw new Error(data.error || 'Ошибка входа')
  }

  return data
}

export async function register(username, email, password, role) {
  const res = await request('/auth/register', {
    method: 'POST',
    body: JSON.stringify({ username, email, password, role }),
  })

  const data = await res.json()

  if (!res.ok) {
    throw new Error(data.error || 'Ошибка регистрации')
  }

  return data
}
