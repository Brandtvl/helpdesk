# Helpdesk — система учёта обращений

Проект 02. Низамаев (фронтенд) · Брандт (бэкенд)

## Что это

Система для подачи и обработки обращений (тикетов). Три роли: заявитель, исполнитель, администратор. Бэкенд на ASP.NET Core 10 + SQLite, фронтенд на React + Vite.

```
helpdesk/
  backend/    — C# Web API
  frontend/   — React SPA
  tests/      — xUnit тесты (37 штук)
  Dockerfile  — для деплоя на Railway
  start.bat   — запуск локально одним кликом
```

## Запуск локально

Двойной клик на `start.bat` — откроет два окна и браузер.

Или вручную:

```bash
# терминал 1
cd backend
dotnet run        # http://localhost:8080

# терминал 2
cd frontend
npm install
npm run dev       # http://localhost:5173
```

При первом запуске бэкенд сам создаёт базу данных, применяет миграции и добавляет начальные данные (категории, SLA-нормативы).

## Тестовые аккаунты

| Роль | Email | Пароль |
|---|---|---|
| Администратор | admin@helpdesk.ru | admin123 |
| Исполнитель | executor@helpdesk.ru | exec123 |

Заявители регистрируются сами через `/register`.

## Продакшн

Бэкенд: https://helpdesk-production-6466.up.railway.app

Фронтенд: развёрнут на web.ru

## API

Base URL локально: `http://localhost:8080/api`

Авторизация: `Authorization: Bearer <token>`

Роли:
- `applicant` — видит только свои тикеты, создаёт обращения
- `executor` — видит все тикеты, берёт в работу, меняет статус
- `admin` — всё выше + управление пользователями, категориями, SLA, отчёты

Основные эндпоинты:

| Метод | Путь | Описание |
|---|---|---|
| POST | /auth/register | Регистрация |
| POST | /auth/login | Вход |
| GET | /tickets | Список тикетов с фильтрами |
| POST | /tickets | Создать тикет |
| GET | /tickets/my | Мои тикеты |
| GET | /tickets/{id} | Карточка тикета |
| PATCH | /tickets/{id}/status | Сменить статус |
| PATCH | /tickets/{id}/assignee | Назначить исполнителя |
| POST | /tickets/bulk | Массовые операции |
| GET | /tickets/{id}/comments | Комментарии |
| POST | /tickets/{id}/comments | Добавить комментарий |
| GET | /tickets/{id}/history | История изменений |
| POST | /tickets/{id}/files | Прикрепить файл |
| POST | /tickets/{id}/dependencies | Добавить блокировку (FR-23) |
| DELETE | /tickets/{id}/dependencies/{blockerId} | Убрать блокировку |
| GET | /categories | Категории |
| POST | /categories | Создать категорию (admin) |
| DELETE | /categories/{id} | Удалить категорию (admin) |
| GET | /sla | SLA-нормативы |
| PUT | /sla/{priority} | Обновить норматив (admin) |
| GET | /reports/by-status | Отчёт по статусам |
| GET | /reports/avg-resolution | Среднее время решения |
| GET | /reports/executor-load | Нагрузка на исполнителей |
| GET | /admin/users | Список пользователей (admin) |
| PATCH | /admin/tickets/{id}/force | Принудительная смена (admin) |

Машина состояний тикета:

```
new → in_progress → waiting → in_progress
                 → resolved → closed
                            → in_progress (reopen)
closed → in_progress (reopen)
```

Недопустимый переход возвращает 409 Conflict.

SLA-нормативы по умолчанию:

| Приоритет | Реакция | Решение |
|---|---|---|
| low | 8 ч | 72 ч |
| medium | 4 ч | 24 ч |
| high | 2 ч | 8 ч |
| critical | 30 мин | 4 ч |

SLA приостанавливается когда тикет в статусе `waiting` (FR-24).

## Тесты

```bash
cd tests
dotnet test
```
