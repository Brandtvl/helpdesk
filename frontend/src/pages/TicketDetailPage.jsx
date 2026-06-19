import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { getTicket, changeStatus } from '../api/tickets'
import { getComments, addComment } from '../api/comments'
import Header from '../components/Header'
import StatusBadge from '../components/StatusBadge'
import PriorityBadge from '../components/PriorityBadge'

// возможные переходы для каждого статуса (для кнопок в UI)
const transitions = {
  new:         [{ value: 'in_progress', label: 'Взять в работу' }],
  in_progress: [
    { value: 'waiting',  label: 'Ждём ответа' },
    { value: 'resolved', label: 'Решено' },
  ],
  waiting:     [{ value: 'in_progress', label: 'Возобновить' }],
  resolved:    [
    { value: 'closed',      label: 'Закрыть' },
    { value: 'in_progress', label: 'Переоткрыть' },
  ],
  closed:      [{ value: 'in_progress', label: 'Переоткрыть' }],
}

function TicketDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()

  const userStr = localStorage.getItem('user')
  const currentUser = userStr ? JSON.parse(userStr) : null

  const [ticket, setTicket] = useState(null)
  const [comments, setComments] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  // форма комментария
  const [commentText, setCommentText] = useState('')
  const [isInternal, setIsInternal] = useState(false)
  const [commentLoading, setCommentLoading] = useState(false)

  // смена статуса
  const [statusComment, setStatusComment] = useState('')
  const [statusError, setStatusError] = useState('')

  useEffect(() => {
    loadTicket()
    loadComments()
  }, [id])

  async function loadTicket() {
    setLoading(true)
    try {
      const data = await getTicket(id)
      setTicket(data)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  async function loadComments() {
    try {
      const data = await getComments(id)
      setComments(data)
    } catch (err) {
      console.error('Не удалось загрузить комментарии', err)
    }
  }

  async function handleStatusChange(newStatus) {
    setStatusError('')
    try {
      const updated = await changeStatus(id, newStatus, statusComment)
      setTicket(updated)
      setStatusComment('')
    } catch (err) {
      setStatusError(err.message)
    }
  }

  async function handleAddComment(e) {
    e.preventDefault()
    if (!commentText.trim()) return

    setCommentLoading(true)
    try {
      const comment = await addComment(id, commentText.trim(), isInternal)
      setComments([...comments, comment])
      setCommentText('')
      setIsInternal(false)
    } catch (err) {
      alert(err.message)
    } finally {
      setCommentLoading(false)
    }
  }

  function formatDate(iso) {
    if (!iso) return '—'
    const d = new Date(iso)
    return d.toLocaleDateString('ru-RU') + ' ' + d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
  }

  if (loading) return <div><Header /><div className="page-content">Загрузка...</div></div>
  if (error)   return <div><Header /><div className="page-content"><div className="error-msg">{error}</div></div></div>
  if (!ticket) return null

  const availableTransitions = transitions[ticket.status] || []

  return (
    <div>
      <Header />
      <div className="page-content">
        <div className="page-header">
          <button onClick={() => navigate('/tickets')} className="btn-back">
            ← К списку
          </button>
          <h1>{ticket.number} {ticket.title}</h1>
        </div>

        <div className="detail-layout">
          {/* Левая колонка - основная информация */}
          <div className="detail-main">
            <div className="card">
              <div className="ticket-meta-grid">
                <div className="meta-item">
                  <span className="meta-label">Статус</span>
                  <StatusBadge status={ticket.status} />
                </div>
                <div className="meta-item">
                  <span className="meta-label">Приоритет</span>
                  <PriorityBadge priority={ticket.priority} />
                </div>
                <div className="meta-item">
                  <span className="meta-label">Категория</span>
                  <span>{ticket.categoryName || '—'}</span>
                </div>
                <div className="meta-item">
                  <span className="meta-label">Автор</span>
                  <span>{ticket.authorName}</span>
                </div>
                <div className="meta-item">
                  <span className="meta-label">Исполнитель</span>
                  <span>{ticket.assigneeName || <span className="muted">Не назначен</span>}</span>
                </div>
                <div className="meta-item">
                  <span className="meta-label">Создано</span>
                  <span>{formatDate(ticket.createdAt)}</span>
                </div>
                <div className="meta-item">
                  <span className="meta-label">SLA дедлайн</span>
                  <span className={ticket.slaBreached ? 'sla-warn' : ''}>
                    {ticket.slaBreached ? '⚠ Просрочен! ' : ''}{formatDate(ticket.slaDeadline)}
                  </span>
                </div>
              </div>

              <div className="ticket-description">
                <h3>Описание</h3>
                <p>{ticket.description}</p>
              </div>
            </div>

            {/* Смена статуса */}
            {availableTransitions.length > 0 && currentUser?.role !== 'applicant' && (
              <div className="card">
                <h3>Сменить статус</h3>
                <div className="form-group">
                  <label>Комментарий к переходу</label>
                  <input
                    type="text"
                    value={statusComment}
                    onChange={e => setStatusComment(e.target.value)}
                    placeholder="Необязательно"
                  />
                </div>
                {statusError && <div className="error-msg">{statusError}</div>}
                <div className="status-buttons">
                  {availableTransitions.map(t => (
                    <button
                      key={t.value}
                      onClick={() => handleStatusChange(t.value)}
                      className="btn-secondary"
                    >
                      {t.label}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* Комментарии */}
            <div className="card">
              <h3>Комментарии ({comments.length})</h3>

              <div className="comments-list">
                {comments.length === 0 && (
                  <p className="muted">Комментариев пока нет</p>
                )}
                {comments.map(c => (
                  <div key={c.id} className={'comment' + (c.isInternal ? ' comment-internal' : '')}>
                    <div className="comment-header">
                      <span className="comment-author">{c.authorName}</span>
                      {c.isInternal && <span className="internal-tag">Внутренний</span>}
                      <span className="comment-date muted">{formatDate(c.createdAt)}</span>
                    </div>
                    <p className="comment-text">{c.text}</p>
                  </div>
                ))}
              </div>

              {/* Форма добавления комментария */}
              <form onSubmit={handleAddComment} className="comment-form">
                <textarea
                  value={commentText}
                  onChange={e => setCommentText(e.target.value)}
                  placeholder="Напишите комментарий..."
                  rows={3}
                />
                <div className="comment-form-footer">
                  {currentUser?.role !== 'applicant' && (
                    <label className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={isInternal}
                        onChange={e => setIsInternal(e.target.checked)}
                      />
                      Внутренний (не виден заявителю)
                    </label>
                  )}
                  <button type="submit" className="btn-primary" disabled={commentLoading || !commentText.trim()}>
                    {commentLoading ? 'Отправляем...' : 'Отправить'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default TicketDetailPage
