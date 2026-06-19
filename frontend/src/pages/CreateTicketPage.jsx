import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { createTicket } from '../api/tickets'
import { getCategories } from '../api/categories'
import Header from '../components/Header'

function CreateTicketPage() {
  const navigate = useNavigate()

  const [categories, setCategories] = useState([])
  const [form, setForm] = useState({
    title: '',
    description: '',
    categoryId: '',
    priority: 'medium',
  })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    getCategories().then(data => {
      setCategories(data)
      if (data.length > 0) {
        setForm(prev => ({ ...prev, categoryId: data[0].id }))
      }
    }).catch(err => {
      console.error('Не удалось загрузить категории', err)
    })
  }, [])

  function handleChange(e) {
    setForm({ ...form, [e.target.name]: e.target.value })
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')

    if (!form.title.trim()) {
      setError('Укажите тему обращения')
      return
    }

    if (!form.description.trim()) {
      setError('Укажите описание')
      return
    }

    setLoading(true)
    try {
      const ticket = await createTicket({
        title: form.title.trim(),
        description: form.description.trim(),
        categoryId: parseInt(form.categoryId),
        priority: form.priority,
      })
      navigate('/tickets/' + ticket.id)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <Header />
      <div className="page-content">
        <div className="page-header">
          <button onClick={() => navigate('/tickets')} className="btn-back">
            ← Назад
          </button>
          <h1>Новое обращение</h1>
        </div>

        <div className="card form-card">
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Тема *</label>
              <input
                type="text"
                name="title"
                value={form.title}
                onChange={handleChange}
                placeholder="Кратко опишите проблему"
                maxLength={200}
              />
            </div>

            <div className="form-group">
              <label>Описание *</label>
              <textarea
                name="description"
                value={form.description}
                onChange={handleChange}
                placeholder="Подробно опишите проблему..."
                rows={5}
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Категория</label>
                <select name="categoryId" value={form.categoryId} onChange={handleChange}>
                  {categories.map(cat => (
                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label>Приоритет</label>
                <select name="priority" value={form.priority} onChange={handleChange}>
                  <option value="low">Низкий</option>
                  <option value="medium">Средний</option>
                  <option value="high">Высокий</option>
                  <option value="critical">Критический</option>
                </select>
              </div>
            </div>

            {error && <div className="error-msg">{error}</div>}

            <div className="form-actions">
              <button type="button" onClick={() => navigate('/tickets')} className="btn-secondary">
                Отмена
              </button>
              <button type="submit" className="btn-primary" disabled={loading}>
                {loading ? 'Создаём...' : 'Создать обращение'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

export default CreateTicketPage
