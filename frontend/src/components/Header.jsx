import { Link, useNavigate } from 'react-router-dom'

function Header() {
  const navigate = useNavigate()

  const userStr = localStorage.getItem('user')
  const user = userStr ? JSON.parse(userStr) : null

  function handleLogout() {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    navigate('/login')
  }

  return (
    <header className="header">
      <div className="header-inner">
        <Link to="/tickets" className="logo">
          Helpdesk
        </Link>

        <nav className="header-nav">
          <Link to="/tickets">Обращения</Link>
          <Link to="/tickets/new">+ Создать</Link>
        </nav>

        <div className="header-user">
          {user && (
            <>
              <span className="username">{user.username}</span>
              <span className="role-badge">{user.role}</span>
              <button onClick={handleLogout} className="btn-logout">
                Выйти
              </button>
            </>
          )}
        </div>
      </div>
    </header>
  )
}

export default Header
