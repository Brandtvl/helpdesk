# Helpdesk — система учёта обращений (Проект 02)

**Состав:** Низамаев (фронт, C#) · Брандт (бэк)

---

## Архитектура

```
helpdesk/
  backend/   — ASP.NET Core 10 Web API (C#)
  tests/     — xUnit тесты бизнес-логики
  frontend/  — клиентская часть (напарник)
```

Бэкенд и фронтенд — **независимые приложения**, общаются через REST API.

---

## Стек бэкенда

| Технология | Назначение |
|---|---|
| ASP.NET Core 10 | HTTP-сервер, роутинг, авторизация |
| Entity Framework Core + SQLite | Хранение данных |
| JWT Bearer | Аутентификация |
| BCrypt.Net | Хеширование паролей |
| xUnit | Автотесты |

---

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

---

## Запуск бэкенда

```bash
cd backend
dotnet run
```

API будет доступно на: `http://localhost:5292`

При первом запуске автоматически:
- Создаётся база данных `helpdesk.db`
- Применяются миграции
- Добавляются начальные данные (SLA-нормативы, базовые категории)

---

## Запуск тестов

```bash
cd tests
dotnet test
```

Запускает 37 тестов: машина состояний, SLA-логика.

---

## API

**Base URL:** `http://localhost:5292/api`

**Авторизация:** Bearer JWT в заголовке:
```
Authorization: Bearer <token>
```

### Роли
| Значение | Роль |
|---|---|
| `applicant` | Заявитель |
| `executor` | Исполнитель |
| `admin` | Администратор |

### Основные эндпоинты

| Метод | Путь | Описание |
|---|---|---|
| POST | `/auth/register` | Регистрация |
| POST | `/auth/login` | Вход, получение токена |
| GET | `/tickets` | Список тикетов (фильтры, пагинация) |
| POST | `/tickets` | Создать тикет |
| GET | `/tickets/{id}` | Карточка тикета |
| PATCH | `/tickets/{id}/status` | Сменить статус |
| PATCH | `/tickets/{id}/assignee` | Назначить исполнителя |
| POST | `/tickets/bulk` | Массовые операции |
| GET | `/tickets/my` | Мои тикеты |
| GET | `/tickets/{id}/comments` | Комментарии |
| POST | `/tickets/{id}/comments` | Добавить комментарий |
| GET | `/tickets/{id}/history` | История изменений |
| POST | `/tickets/{id}/files` | Загрузить файл |
| POST | `/tickets/{id}/dependencies` | Добавить блокировку (FR-23) |
| DELETE | `/tickets/{id}/dependencies/{blockerId}` | Удалить блокировку |
| GET | `/categories` | Категории |
| POST | `/categories` | Создать категорию (admin) |
| DELETE | `/categories/{id}` | Удалить категорию (admin) |
| GET | `/sla` | SLA-нормативы |
| PUT | `/sla/{priority}` | Обновить норматив (admin) |
| GET | `/reports/by-status` | Отчёт по статусам |
| GET | `/reports/avg-resolution` | Среднее время решения |
| GET | `/reports/executor-load` | Нагрузка на исполнителей |
| GET | `/admin/users` | Список пользователей (admin) |
| PATCH | `/admin/tickets/{id}/force` | Принудительная смена (admin) |

### Статусы тикета
```
new → in_progress → waiting → in_progress
                 → resolved → closed
                            → in_progress (reopen)
closed → in_progress (reopen)
```
Недопустимый переход возвращает **409 Conflict**.

### Приоритеты и SLA по умолчанию
| Приоритет | Реакция | Решение |
|---|---|---|
| `low` | 8 ч | 72 ч |
| `medium` | 4 ч | 24 ч |
| `high` | 2 ч | 8 ч |
| `critical` | 30 мин | 4 ч |

---

## Пример: регистрация и создание тикета

```bash
# 1. Регистрация
curl -X POST http://localhost:5292/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"ivan","email":"ivan@example.com","password":"123456","role":"applicant"}'

# 2. Вход
curl -X POST http://localhost:5292/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"ivan@example.com","password":"123456"}'

# 3. Создать тикет (token из шага 2)
curl -X POST http://localhost:5292/api/tickets \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"title":"Не работает принтер","description":"Принтер не печатает","categoryId":2,"priority":"high"}'
```
