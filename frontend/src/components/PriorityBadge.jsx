// значки приоритета
const labels = {
  low:      'Низкий',
  medium:   'Средний',
  high:     'Высокий',
  critical: 'Критический',
}

function PriorityBadge({ priority }) {
  return (
    <span className={'priority-badge priority-' + priority}>
      {labels[priority] || priority}
    </span>
  )
}

export default PriorityBadge
