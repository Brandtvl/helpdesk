import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import TicketsPage from './pages/TicketsPage'
import TicketDetailPage from './pages/TicketDetailPage'
import CreateTicketPage from './pages/CreateTicketPage'

// защищённый маршрут - если нет токена, редирект на логин
function PrivateRoute({ children }) {
  const token = localStorage.getItem('token')
  if (!token) {
    return <Navigate to="/login" replace />
  }
  return children
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route
          path="/tickets"
          element={<PrivateRoute><TicketsPage /></PrivateRoute>}
        />
        <Route
          path="/tickets/new"
          element={<PrivateRoute><CreateTicketPage /></PrivateRoute>}
        />
        <Route
          path="/tickets/:id"
          element={<PrivateRoute><TicketDetailPage /></PrivateRoute>}
        />

        {/* по умолчанию - на список обращений */}
        <Route path="*" element={<Navigate to="/tickets" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
