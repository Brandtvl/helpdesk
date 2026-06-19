import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { getTicket, changeStatus } from '../api/tickets'
import { addComment } from '../api/comments'
import Header from '../components/Header'
import StatusBadge from '../components/StatusBadge'
import PriorityBadge from '../components/PriorityBadge'

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
  closed: [{ value: 'in_progress', label: 'Переоткрыть' }],
}

const STATUS_RU = {
  new: 'новое', in_progress: 'в работе', waiting: 'ожидание',
  resolved: 'решено', closed: 'закрыто',
}

const PRIORITY_RU = {
  low: 'низкий', medium: 'средний', high: 'высокий', critical: 'критический',
}

const FIELD_LABELS = {
  status: 'Статус', assignee: 'Исполнитель',
  priority: 'Приоритет', dependency: 'Связь',
}

function formatHistoryChange(h) {
  if (h.field === 'status') {
    var oldL = STATUS_RU[h.oldValue] || h.oldValue || '—'
    var newL = STATUS_RU[h.newValue] || h.newValue
    return oldL + ' → ' + newL
  }
  if (h.field === 'assignee') {
    var oldA = h.oldValue || 'не назначен'
    var newA = h.newValue || 'снят'
    return oldA + ' → ' + newA
  }
  if (h.field === 'priority') {
    var oldP = PRIORITY_RU[h.oldValue] || h.oldValue || '—'
    var newP = PRIORITY_RU[h.newValue] || h.newValue
    return oldP + ' → ' + newP
  }
  if (h.field === 'dependency') {
    var num = h.newValue.replace('blocker:', '#')
    return 'добавлена блокировка ' + num
  }
  return (h.oldValue || '—') + ' → ' + h.newValue
}

function TicketDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()

  const userStr = localStorage.getItem('user')
  const currentUser = userStr ? JSON.parse(userStr) : null

  const [ticket, setTicket] = useState(null)
  const [comments, setComments] = useState([])
  const [history, setHistory] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [commentText, setCommentText] = useState('')
  const [isInternal, setIsInternal] = useState(false)
  const [commentLoading, setCommentLoading] = useState(false)

  const [statusComment, setStatusComment] = useState('')
  const [statusError, setStatusError] = useState('')

  useEffect(function() {
    loadTicket()
  }, [id])

  async function loadTicket() {
    setLoading(true)
    setError('')
    try {
      var data = await getTicket(id)
      setTicket(data)
      setComments(data.comments || [])
      setHistory(data.history || [])
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  async function handleStatusChange(newStatus) {
    setStatusError('')
    try {
      var updated = await changeStatus(id, newStatus, statusComment)
      setTicket(updated)
      setStatusComment('')
      // перезагружаем чтобы получить свежую историю
      var fresh = await getTicket(id)
      setTicket(fresh)
      setComments(fresh.comments || [])
      setHistory(fresh.history || [])
    } catch (err) {
      setStatusError(err.message)
    }
  }

  async function handleAddComment(e) {
    e.preventDefault()
    if (!commentText.trim()) return
    setCommentLoading(true)
    try {
      var comment = await addComment(id, commentText.trim(), isInternal)
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
    var d = new Date(iso)
    return d.toLocaleDateString('ru-RU') + ' ' + d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
  }

  function formatTime(iso) {
    if (!iso) return ''
    var d = new Date(iso)
    return d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
  }

  if (loading) return <div><Header /><div className="page-content">Загрузка...</div></div>
  if (error)   return <div><Header /><div className="page-content"><div className="error-msg">{error}</div></div></div>
  if (!ticket) return null

  var availableTransitions = transitions[ticket.status] || []
  var hasBlockers = ticket.blockedByIds && ticket.blockedByIds.length > 0

  return (
    <div>
      <Header />
      <div className="page-content">
        <div className="page-header">
          <button onClick={() => navigate('/tickets')} className="btn-back">← К списку</button>
          <h1>{ticket.number} · {ticket.title}</h1>
        </div>

        <div className="detail-two-col">
          {/* Левая колонка */}
          <div className="detail-left">

            {/* Мета-информация */}
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

            {/* Блокировка FR-23 */}
            {hasBlockers && (
              <div className="blocker-warning">
                <strong>Блокировка (FR-23):</strong> закрытие недоступно — открыто блокирующее обращение{' '}
                {ticket.blockedByIds.map(function(bid) {
                  return (
                    <span
                      key={bid}
                      className="blocker-link"
                      onClick={() => navigate('/tickets/' + bid)}
                    >
                      #{bid}
                    </span>
                  )
                })}
              </div>
            )}

            {/* Смена статуса */}
            {availableTransitions.length > 0 && currentUser && currentUser.role !== 'applicant' && (
              <div className="card">
                <h3>Сменить статус</h3>
                <div className="form-group">
                  <label>Комментарий к переходу</label>
                  <input
                    type="text"
                    value={statusComment}
                    onChange={function(e) { setStatusComment(e.target.value) }}
                    placeholder="Необязательно"
                  />
                </div>
                {statusError && <div className="error-msg">{statusError}</div>}
                <div className="status-buttons">
                  {availableTransitions.map(function(t) {
                    return (
                      <button key={t.value} onClick={() => handleStatusChange(t.value)} className="btn-secondary">
                        {t.label}
                      </button>
                    )
                  })}
                </div>
              </div>
            )}

            {/* Переписка */}
            <div className="card">
              <h3>Переписка ({comments.length})</h3>
              <div className="comments-list">
                {comments.length === 0 && <p className="muted">Комментариев пока нет</p>}
                {comments.map(function(c) {
                  return (
                    <div key={c.id} className={'comment' + (c.isInternal ? ' comment-internal' : '')}>
                      <div className="comment-header">
                        <span className="comment-author">{c.authorName}</span>
                        {c.isInternal
                          ? <span className="internal-tag">внутренний</span>
                          : <span className="public-tag">публичный</span>
                        }
                        <span className="comment-date muted">{formatDate(c.createdAt)}</span>
                      </div>
                      <p className="comment-text">{c.text}</p>
                    </div>
                  )
                })}
              </div>
              <form onSubmit={handleAddComment} className="comment-form">
                <textarea
                  value={commentText}
                  onChange={function(e) { setCommentText(e.target.value) }}
                  placeholder="Напишите комментарий..."
                  rows={3}
                />
                <div className="comment-form-footer">
                  {currentUser && currentUser.role !== 'applicant' && (
                    <label className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={isInternal}
                        onChange={function(e) { setIsInternal(e.target.checked) }}
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

          {/* Правая колонка — история изменений */}
          <div className="detail-right">
            <div className="card history-card">
              <h3>История изменений</h3>
              {history.length === 0 && <p className="muted">Изменений пока нет</p>}
              <div className="history-list">
                {history.map(function(h) {
                  return (
                    <div key={h.id} className="history-entry">
                      <div className="history-top">
                        <span className="history-field">
                          {FIELD_LABELS[h.field] || h.field}
                        </span>
                        <span className="history-time muted">{formatTime(h.createdAt)}</span>
                      </div>
                      <div className="history-change">
                        {formatHistoryChange(h)}
                        {h.authorName && h.field !== 'dependency' && (
                          <span className="history-author"> · {h.authorName}</span>
                        )}
                      </div>
                      {h.comment && (
                        <div className="history-comment muted">{h.comment}</div>
                      )}
                    </div>
                  )
                })}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default TicketDetailPage
