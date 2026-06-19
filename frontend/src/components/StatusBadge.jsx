// значки статуса обращения
const labels = {
  new:         'Новое',
  in_progress: 'В работе',
  waiting:     'Ожидание',
  resolved:    'Решено',
  closed:      'Закрыто',
}

function StatusBadge({ status }) {
  return (
    <span className={'status-badge status-' + status}>
      {labels[status] || status}
    </span>
  )
}

export default StatusBadge
