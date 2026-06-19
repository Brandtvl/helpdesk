// базовый HTTP клиент - добавляет токен к каждому запросу

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

  const response = await fetch('/api' + url, {
    ...options,
    headers,
  })

  // если 401 - токен протух, выкидываем на логин
  if (response.status === 401) {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    window.location.href = '/login'
    return null
  }

  return response
}
