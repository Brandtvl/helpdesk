// базовый HTTP клиент - добавляет токен к каждому запросу

// в проде берём URL бэкенда из env, локально используем прокси vite
const API_BASE = import.meta.env.VITE_API_URL || '/api'

function getToken() {
  return localStorage.getItem('token')
}

export async function request(url, options = {}) {
  const headers = {
    'Content-Type': 'application/json',
  }

  const token = getToken()
  if (token) {
    headers['Authorization'] = 'Bearer ' + token
  }

  // мёрджим пользовательские заголовки
  if (options.headers) {
    Object.assign(headers, options.headers)
  }

  const response = await fetch(API_BASE + url, {
    ...options,
    headers,
  })

  // если 401 на защищённых эндпоинтах - токен протух, выкидываем на логин
  // для /auth/* не редиректим - там 401 это просто "неверный пароль"
  if (response.status === 401 && !url.startsWith('/auth/')) {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    window.location.href = '/login'
    return null
  }

  return response
}
