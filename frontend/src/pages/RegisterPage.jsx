import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { register } from '../api/auth'

function RegisterPage() {
  const navigate = useNavigate()

  const [form, setForm] = useState({
    username: '',
    email: '',
    password: '',
    role: 'applicant',
  })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  function handleChange(e) {
    setForm({ ...form, [e.target.name]: e.target.value })
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')

    if (!form.username || !form.email || !form.password) {
      setError('Заполните все поля')
      return
    }

    if (form.password.length < 6) {
      setError('Пароль должен быть не менее 6 символов')
      return
    }

    setLoading(true)
    try {
      const data = await register(form.username, form.email, form.password, form.role)
      localStorage.setItem('token', data.token)
      localStorage.setItem('user', JSON.stringify({ id: data.userId, username: data.username, role: data.role }))
      navigate('/tickets')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Helpdesk</h1>
        <h2>Регистрация</h2>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Имя пользователя</label>
            <input
              type="text"
              name="username"
              value={form.username}
              onChange={handleChange}
              placeholder="Введите имя"
            />
          </div>

          <div className="form-group">
            <label>Email</label>
            <input
              type="email"
              name="email"
              value={form.email}
              onChange={handleChange}
              placeholder="example@mail.ru"
            />
          </div>

          <div className="form-group">
            <label>Пароль</label>
            <input
              type="password"
              name="password"
              value={form.password}
              onChange={handleChange}
              placeholder="Минимум 6 символов"
            />
          </div>

          <div className="form-group">
            <label>Роль</label>
            <select name="role" value={form.role} onChange={handleChange}>
              <option value="applicant">Заявитель</option>
              <option value="executor">Исполнитель</option>
              <option value="admin">Администратор</option>
            </select>
          </div>

          {error && <div className="error-msg">{error}</div>}

          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? 'Создаём аккаунт...' : 'Зарегистрироваться'}
          </button>
        </form>

        <p className="auth-link">
          Уже есть аккаунт? <Link to="/login">Войти</Link>
        </p>
      </div>
    </div>
  )
}

export default RegisterPage
