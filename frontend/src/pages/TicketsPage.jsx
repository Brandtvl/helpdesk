import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { getTickets } from '../api/tickets'
import Header from '../components/Header'
import StatusBadge from '../components/StatusBadge'
import PriorityBadge from '../components/PriorityBadge'

function TicketsPage() {
  const navigate = useNavigate()

  const [tickets, setTickets] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  // фильтры
  const [statusFilter, setStatusFilter] = useState('')
  const [priorityFilter, setPriorityFilter] = useState('')
  const [searchText, setSearchText] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 20

  useEffect(() => {
    loadTickets()
  }, [statusFilter, priorityFilter, page])

  async function loadTickets() {
    setLoading(true)
    setError('')
    try {
      const params = {
        page,
        pageSize,
        status: statusFilter,
        priority: priorityFilter,
        search: searchText,
      }
      const data = await getTickets(params)
      setTickets(data.items || [])
      setTotal(data.total || 0)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  function handleSearch(e) {
    e.preventDefault()
    setPage(1)
    loadTickets()
  }

  function formatDate(iso) {
    if (!iso) return '—'
    const d = new Date(iso)
    return d.toLocaleDateString('ru-RU') + ' ' + d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
  }

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div>
      <Header />
      <div className="page-content">
        <div className="page-header">
          <h1>Обращения <span className="total-count">({total})</span></h1>
          <button onClick={() => navigate('/tickets/new')} className="btn-primary">
            + Создать
          </button>
        </div>

        {/* Фильтры */}
        <div className="filters">
          <form onSubmit={handleSearch} className="search-form">
            <input
              type="text"
              placeholder="Поиск по теме..."
              value={searchText}
              onChange={e => setSearchText(e.target.value)}
            />
            <button type="submit" className="btn-secondary">Найти</button>
          </form>

          <select value={statusFilter} onChange={e => { setStatusFilter(e.target.value); setPage(1) }}>
            <option value="">Все статусы</option>
            <option value="new">Новое</option>
            <option value="in_progress">В работе</option>
            <option value="waiting">Ожидание</option>
            <option value="resolved">Решено</option>
            <option value="closed">Закрыто</option>
          </select>

          <select value={priorityFilter} onChange={e => { setPriorityFilter(e.target.value); setPage(1) }}>
            <option value="">Все приоритеты</option>
            <option value="low">Низкий</option>
            <option value="medium">Средний</option>
            <option value="high">Высокий</option>
            <option value="critical">Критический</option>
          </select>
        </div>

        {/* Список */}
        {loading && <div className="loading">Загрузка...</div>}
        {error && <div className="error-msg">{error}</div>}

        {!loading && !error && (
          <>
            {tickets.length === 0 ? (
              <div className="empty-state">
                <p>Обращений не найдено</p>
              </div>
            ) : (
              <div className="tickets-table-wrap">
                <table className="tickets-table">
                  <thead>
                    <tr>
                      <th>Номер</th>
                      <th>Тема</th>
                      <th>Статус</th>
                      <th>Приоритет</th>
                      <th>Исполнитель</th>
                      <th>Создано</th>
                      <th>SLA</th>
                    </tr>
                  </thead>
                  <tbody>
                    {tickets.map(ticket => (
                      <tr
                        key={ticket.id}
                        onClick={() => navigate('/tickets/' + ticket.id)}
                        className={'ticket-row' + (ticket.slaBreached ? ' sla-breached' : '')}
                      >
                        <td className="ticket-number">{ticket.number}</td>
                        <td className="ticket-title">{ticket.title}</td>
                        <td><StatusBadge status={ticket.status} /></td>
                        <td><PriorityBadge priority={ticket.priority} /></td>
                        <td>{ticket.assigneeName || <span className="muted">—</span>}</td>
                        <td className="muted">{formatDate(ticket.createdAt)}</td>
                        <td>
                          {ticket.slaBreached
                            ? <span className="sla-warn">Просрочен</span>
                            : formatDate(ticket.slaDeadline)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {/* Пагинация */}
            {totalPages > 1 && (
              <div className="pagination">
                <button
                  onClick={() => setPage(p => p - 1)}
                  disabled={page === 1}
                  className="btn-secondary"
                >
                  ← Назад
                </button>
                <span>Стр. {page} из {totalPages}</span>
                <button
                  onClick={() => setPage(p => p + 1)}
                  disabled={page === totalPages}
                  className="btn-secondary"
                >
                  Вперёд →
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}

export default TicketsPage
