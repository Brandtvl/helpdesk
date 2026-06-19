import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import Header from '../components/Header'
import { getCategories } from '../api/categories'
import {
  getUsers,
  createCategory,
  deleteCategory,
  getSlaConfigs,
  updateSla,
  getReportByStatus,
  getReportAvgResolution,
  getReportExecutorLoad,
  forceStatus,
} from '../api/admin'

const STATUS_LABELS = {
  new:         'Новое',
  in_progress: 'В работе',
  waiting:     'Ожидание',
  resolved:    'Решено',
  closed:      'Закрыто',
}

const PRIORITY_LABELS = {
  low:      'Низкий',
  medium:   'Средний',
  high:     'Высокий',
  critical: 'Критический',
}

const ROLE_LABELS = {
  applicant: 'Заявитель',
  executor:  'Исполнитель',
  admin:     'Администратор',
}

function AdminPage() {
  const navigate = useNavigate()

  // проверяем что пользователь — администратор
  const userStr = localStorage.getItem('user')
  const currentUser = userStr ? JSON.parse(userStr) : null

  const [activeTab, setActiveTab] = useState('users')

  // --- Пользователи ---
  const [users, setUsers] = useState([])
  const [usersLoading, setUsersLoading] = useState(false)
  const [usersError, setUsersError] = useState('')

  // --- Категории ---
  const [categories, setCategories] = useState([])
  const [newCatName, setNewCatName] = useState('')
  const [catError, setCatError] = useState('')
  const [catLoading, setCatLoading] = useState(false)

  // --- SLA ---
  const [slaConfigs, setSlaConfigs] = useState([])
  const [slaEdits, setSlaEdits] = useState({})
  const [slaError, setSlaError] = useState('')
  const [slaSaved, setSlaSaved] = useState('')

  // --- Отчёты ---
  const [reportByStatus, setReportByStatus] = useState([])
  const [reportAvg, setReportAvg] = useState(null)
  const [reportLoad, setReportLoad] = useState([])
  const [reportsLoading, setReportsLoading] = useState(false)
  const [reportsError, setReportsError] = useState('')

  // --- Принудительная смена ---
  const [forceTicketId, setForceTicketId] = useState('')
  const [forceNewStatus, setForceNewStatus] = useState('in_progress')
  const [forceReason, setForceReason] = useState('')
  const [forceError, setForceError] = useState('')
  const [forceSuccess, setForceSuccess] = useState('')
  const [forceLoading, setForceLoading] = useState(false)

  useEffect(() => {
    if (!currentUser || currentUser.role !== 'admin') {
      navigate('/tickets')
      return
    }
    loadTabData(activeTab)
  }, [activeTab])

  function loadTabData(tab) {
    if (tab === 'users')      loadUsers()
    if (tab === 'categories') loadCategories()
    if (tab === 'sla')        loadSla()
    if (tab === 'reports')    loadReports()
  }

  async function loadUsers() {
    setUsersLoading(true)
    setUsersError('')
    try {
      const data = await getUsers()
      setUsers(data)
    } catch (err) {
      setUsersError(err.message)
    } finally {
      setUsersLoading(false)
    }
  }

  async function loadCategories() {
    setCatError('')
    try {
      const data = await getCategories()
      setCategories(data)
    } catch (err) {
      setCatError(err.message)
    }
  }

  async function handleAddCategory(e) {
    e.preventDefault()
    if (!newCatName.trim()) return
    setCatLoading(true)
    setCatError('')
    try {
      const cat = await createCategory(newCatName.trim())
      setCategories([...categories, cat])
      setNewCatName('')
    } catch (err) {
      setCatError(err.message)
    } finally {
      setCatLoading(false)
    }
  }

  async function handleDeleteCategory(id) {
    if (!window.confirm('Удалить категорию?')) return
    setCatError('')
    try {
      await deleteCategory(id)
      setCategories(categories.filter(c => c.id !== id))
    } catch (err) {
      setCatError(err.message)
    }
  }

  async function loadSla() {
    setSlaError('')
    try {
      const data = await getSlaConfigs()
      setSlaConfigs(data)
      // заполняем форму редактирования текущими значениями
      var edits = {}
      data.forEach(function(s) {
        edits[s.priority] = { reactionHours: s.reactionHours, resolutionHours: s.resolutionHours }
      })
      setSlaEdits(edits)
    } catch (err) {
      setSlaError(err.message)
    }
  }

  async function handleSaveSla(priority) {
    setSlaError('')
    setSlaSaved('')
    var edit = slaEdits[priority]
    try {
      await updateSla(priority, Number(edit.reactionHours), Number(edit.resolutionHours))
      setSlaSaved('Сохранено: ' + PRIORITY_LABELS[priority])
      setTimeout(() => setSlaSaved(''), 3000)
    } catch (err) {
      setSlaError(err.message)
    }
  }

  async function loadReports() {
    setReportsLoading(true)
    setReportsError('')
    try {
      const [byStatus, avg, load] = await Promise.all([
        getReportByStatus(),
        getReportAvgResolution(),
        getReportExecutorLoad(),
      ])
      setReportByStatus(byStatus)
      setReportAvg(avg)
      setReportLoad(load)
    } catch (err) {
      setReportsError(err.message)
    } finally {
      setReportsLoading(false)
    }
  }

  async function handleForce(e) {
    e.preventDefault()
    setForceError('')
    setForceSuccess('')
    if (!forceTicketId || !forceReason.trim()) {
      setForceError('Заполните номер тикета и причину')
      return
    }
    setForceLoading(true)
    try {
      await forceStatus(forceTicketId, forceNewStatus, forceReason.trim())
      setForceSuccess('Статус тикета #' + forceTicketId + ' изменён на "' + STATUS_LABELS[forceNewStatus] + '"')
      setForceTicketId('')
      setForceReason('')
    } catch (err) {
      setForceError(err.message)
    } finally {
      setForceLoading(false)
    }
  }

  function formatDate(iso) {
    if (!iso) return '—'
    return new Date(iso).toLocaleDateString('ru-RU')
  }

  return (
    <div>
      <Header />
      <div className="page-content">
        <h1 style={{ marginBottom: '20px' }}>Панель администратора</h1>

        {/* Вкладки */}
        <div className="admin-tabs">
          {[
            { key: 'users',      label: 'Пользователи' },
            { key: 'categories', label: 'Категории' },
            { key: 'sla',        label: 'Нормативы SLA' },
            { key: 'reports',    label: 'Отчёты' },
            { key: 'force',      label: 'Принудительная смена' },
          ].map(function(tab) {
            return (
              <button
                key={tab.key}
                className={'admin-tab' + (activeTab === tab.key ? ' admin-tab-active' : '')}
                onClick={() => setActiveTab(tab.key)}
              >
                {tab.label}
              </button>
            )
          })}
        </div>

        {/* Пользователи */}
        {activeTab === 'users' && (
          <div className="card">
            <h2>Список пользователей</h2>
            {usersLoading && <p className="muted">Загрузка...</p>}
            {usersError && <div className="error-msg">{usersError}</div>}
            {!usersLoading && !usersError && (
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Имя пользователя</th>
                    <th>Email</th>
                    <th>Роль</th>
                    <th>Дата регистрации</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map(function(u) {
                    return (
                      <tr key={u.id}>
                        <td className="muted">{u.id}</td>
                        <td><strong>{u.username}</strong></td>
                        <td>{u.email}</td>
                        <td>
                          <span className={'role-badge role-' + u.role}>
                            {ROLE_LABELS[u.role] || u.role}
                          </span>
                        </td>
                        <td className="muted">{formatDate(u.createdAt)}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            )}
          </div>
        )}

        {/* Категории */}
        {activeTab === 'categories' && (
          <div className="card">
            <h2>Управление категориями</h2>
            {catError && <div className="error-msg">{catError}</div>}

            <form onSubmit={handleAddCategory} className="inline-form">
              <input
                type="text"
                placeholder="Название новой категории"
                value={newCatName}
                onChange={e => setNewCatName(e.target.value)}
                maxLength={100}
              />
              <button type="submit" className="btn-primary" disabled={catLoading || !newCatName.trim()}>
                {catLoading ? 'Добавляем...' : '+ Добавить'}
              </button>
            </form>

            <table className="admin-table" style={{ marginTop: '16px' }}>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Название</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {categories.map(function(cat) {
                  return (
                    <tr key={cat.id}>
                      <td className="muted">{cat.id}</td>
                      <td>{cat.name}</td>
                      <td>
                        <button
                          className="btn-danger-sm"
                          onClick={() => handleDeleteCategory(cat.id)}
                        >
                          Удалить
                        </button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* SLA */}
        {activeTab === 'sla' && (
          <div className="card">
            <h2>Нормативы SLA</h2>
            <p className="muted" style={{ marginBottom: '16px' }}>
              Время реакции и решения в часах для каждого приоритета.
            </p>
            {slaError && <div className="error-msg">{slaError}</div>}
            {slaSaved && <div className="success-msg">{slaSaved}</div>}

            <table className="admin-table">
              <thead>
                <tr>
                  <th>Приоритет</th>
                  <th>Реакция (ч)</th>
                  <th>Решение (ч)</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {slaConfigs.map(function(s) {
                  var edit = slaEdits[s.priority] || { reactionHours: s.reactionHours, resolutionHours: s.resolutionHours }
                  return (
                    <tr key={s.priority}>
                      <td><strong>{PRIORITY_LABELS[s.priority] || s.priority}</strong></td>
                      <td>
                        <input
                          type="number"
                          min="0"
                          value={edit.reactionHours}
                          onChange={function(e) {
                            setSlaEdits(prev => ({
                              ...prev,
                              [s.priority]: { ...prev[s.priority], reactionHours: e.target.value }
                            }))
                          }}
                          className="sla-input"
                        />
                      </td>
                      <td>
                        <input
                          type="number"
                          min="1"
                          value={edit.resolutionHours}
                          onChange={function(e) {
                            setSlaEdits(prev => ({
                              ...prev,
                              [s.priority]: { ...prev[s.priority], resolutionHours: e.target.value }
                            }))
                          }}
                          className="sla-input"
                        />
                      </td>
                      <td>
                        <button className="btn-secondary" onClick={() => handleSaveSla(s.priority)}>
                          Сохранить
                        </button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* Отчёты */}
        {activeTab === 'reports' && (
          <div>
            {reportsLoading && <p className="muted">Загрузка отчётов...</p>}
            {reportsError && <div className="error-msg">{reportsError}</div>}

            {!reportsLoading && !reportsError && (
              <div className="reports-grid">

                {/* Левый блок: по статусам + среднее время */}
                <div className="card">
                  <h2>Обращения по статусам · за период</h2>
                  {(function() {
                    var mainRows = reportByStatus.filter(function(r) { return r.status !== 'sla_breached' })
                    var slaRow   = reportByStatus.find(function(r)   { return r.status === 'sla_breached' })
                    var maxCount = Math.max(1, ...mainRows.map(function(r) { return r.count }))
                    return (
                      <div className="bar-chart">
                        {mainRows.map(function(r) {
                          return (
                            <div key={r.status} className="bar-row">
                              <span className="bar-label">{STATUS_LABELS[r.status] || r.status}</span>
                              <div className="bar-track">
                                <div
                                  className="bar-fill"
                                  style={{ width: (r.count / maxCount * 100) + '%' }}
                                />
                              </div>
                              <span className="bar-count">{r.count}</span>
                            </div>
                          )
                        })}
                        {slaRow && (
                          <div className="bar-row bar-row-sla">
                            <span className="bar-label">Просрочено по SLA</span>
                            <div className="bar-track">
                              <div
                                className="bar-fill bar-fill-sla"
                                style={{ width: (slaRow.count / maxCount * 100) + '%' }}
                              />
                            </div>
                            <span className="bar-count bar-count-sla">{slaRow.count}</span>
                          </div>
                        )}
                      </div>
                    )
                  })()}

                  {reportAvg !== null && (
                    <div className="avg-time-box">
                      <span className="avg-time-label">Среднее время решения</span>
                      <span className="avg-time-value">{reportAvg.avgHours} ч</span>
                      <span className="avg-time-note">от создания до статуса "Решено" (FR-20)</span>
                    </div>
                  )}
                </div>

                {/* Правый блок: нагрузка на исполнителей */}
                <div className="card">
                  <h2>Нагрузка на исполнителей</h2>
                  {reportLoad.length === 0 && <p className="muted">Нет назначенных тикетов</p>}
                  {(function() {
                    var maxLoad = Math.max(1, ...reportLoad.map(function(r) { return r.count }))
                    return (
                      <div className="bar-chart">
                        {reportLoad.map(function(r) {
                          return (
                            <div key={r.executorId} className="bar-row">
                              <span className="bar-label">{r.username}</span>
                              <div className="bar-track">
                                <div
                                  className="bar-fill"
                                  style={{ width: (r.count / maxLoad * 100) + '%' }}
                                />
                              </div>
                              <span className="bar-count">{r.count}</span>
                            </div>
                          )
                        })}
                      </div>
                    )
                  })()}

                  <div className="report-fr-note">
                    Среднее время решения и распределение по статусам строятся по фактической истории (FR-20).
                    Справочники и нормативы SLA настраиваются администратором (FR-21).
                  </div>
                </div>

              </div>
            )}
          </div>
        )}

        {/* Принудительная смена */}
        {activeTab === 'force' && (
          <div className="card form-card">
            <h2>Принудительная смена статуса</h2>
            <p className="muted" style={{ marginBottom: '16px' }}>
              Меняет статус тикета без проверки таблицы допустимых переходов. Используется когда тикет завис.
            </p>

            {forceError && <div className="error-msg">{forceError}</div>}
            {forceSuccess && <div className="success-msg">{forceSuccess}</div>}

            <form onSubmit={handleForce}>
              <div className="form-group">
                <label>Номер тикета (ID)</label>
                <input
                  type="number"
                  min="1"
                  placeholder="Например: 5"
                  value={forceTicketId}
                  onChange={e => setForceTicketId(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label>Новый статус</label>
                <select value={forceNewStatus} onChange={e => setForceNewStatus(e.target.value)}>
                  <option value="new">Новое</option>
                  <option value="in_progress">В работе</option>
                  <option value="waiting">Ожидание</option>
                  <option value="resolved">Решено</option>
                  <option value="closed">Закрыто</option>
                </select>
              </div>

              <div className="form-group">
                <label>Причина (обязательно)</label>
                <input
                  type="text"
                  placeholder="Объясните причину принудительной смены"
                  value={forceReason}
                  onChange={e => setForceReason(e.target.value)}
                />
              </div>

              <div className="form-actions">
                <button
                  type="submit"
                  className="btn-primary"
                  disabled={forceLoading || !forceTicketId || !forceReason.trim()}
                >
                  {forceLoading ? 'Меняем...' : 'Сменить статус'}
                </button>
              </div>
            </form>
          </div>
        )}
      </div>
    </div>
  )
}

export default AdminPage
