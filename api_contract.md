# API-контракт · Helpdesk (Проект 02)

Низамаев (фронтенд) · Брандт (бэкенд)

## Общее

**Base URL (локально):** `http://localhost:8080/api`

**Base URL (продакшн):** `https://helpdesk-production-6466.up.railway.app/api`

Формат: JSON, кодировка UTF-8.

Защищённые запросы требуют заголовок:
```
Authorization: Bearer <jwt_token>
Content-Type: application/json
```

Коды ответов: 200 OK, 201 Created, 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict, 500 Internal Server Error.

---

## Модели данных

### Пользователь

```
id           — number
username     — string
email        — string
role         — "applicant" | "executor" | "admin"
createdAt    — ISO 8601
// passwordHash в ответах не передаётся
```

### Тикет

```
id           — number
number       — string ("#2041")
title        — string
description  — string
status       — string (см. статусы)
priority     — string (low | medium | high | critical)
categoryId   — number
categoryName — string
authorId     — number
authorName   — string
assigneeId   — number | null
assigneeName — string | null
createdAt    — ISO 8601
slaDeadline  — ISO 8601
slaBreached  — boolean
slaPaused    — boolean (true когда статус waiting, FR-24)
blockedByIds — number[]  (FR-23)
childIds     — number[]  (FR-23)
```

### Статусы и допустимые переходы

```
new         → in_progress
in_progress → waiting, resolved
waiting     → in_progress
resolved    → closed, in_progress (reopen)
closed      → in_progress (reopen)
```

Недопустимый переход → 409 Conflict.

### Приоритеты (SLA по умолчанию)

```
low      — реакция 8ч,    решение 72ч
medium   — реакция 4ч,    решение 24ч
high     — реакция 2ч,    решение 8ч
critical — реакция 30мин, решение 4ч
```

Нормативы меняет администратор через PUT /sla/{priority}.

### Комментарий

```
id          — number
ticketId    — number
authorId    — number
authorName  — string
text        — string
isInternal  — boolean (true = скрыт от заявителя)
createdAt   — ISO 8601
```

### Запись истории

```
id          — number
ticketId    — number
authorId    — number
authorName  — string
field       — "status" | "assignee" | "priority" | "dependency"
oldValue    — string | null
newValue    — string
comment     — string | null
createdAt   — ISO 8601
```

---

## Эндпоинты

### /auth

**POST /auth/register**

```json
// запрос
{ "username": "string", "email": "string", "password": "string", "role": "applicant|executor|admin" }

// ответ 201
{ "id": 1, "username": "string", "role": "applicant", "token": "string" }
```

**POST /auth/login**

```json
// запрос
{ "email": "string", "password": "string" }

// ответ 200
{ "token": "string", "user": { "id": 1, "username": "string", "role": "string" } }

// ответ 400
{ "error": "Неверный email или пароль" }
```

---

### /tickets

Заявитель (`applicant`) на GET /tickets получает только свои тикеты (ОБ-8).

**GET /tickets**

Query-параметры (все необязательны):
```
status, priority, assigneeId, categoryId, search,
slaBreached, page (default 1), pageSize (default 20),
sortBy (createdAt|priority|slaDeadline), sortDir (asc|desc)
```

Ответ 200:
```json
{ "items": [...], "total": 42, "page": 1, "pageSize": 20 }
```

**GET /tickets/my** — мои тикеты (заявитель = созданные, исполнитель = назначенные)

```json
{ "items": [...], "total": 5 }
```

**POST /tickets**

```json
// запрос
{ "title": "string (3–200)", "description": "string (до 5000)", "categoryId": 1, "priority": "high" }

// ответ 201 — тикет
```

**GET /tickets/{id}** — карточка тикета с комментариями и историей

```json
// ответ 200
{ ...ticket, "comments": [...], "history": [...] }
```

**PATCH /tickets/{id}/status**

```json
// запрос
{ "status": "in_progress", "comment": "необязательно" }

// ответ 200 — тикет | 409 — недопустимый переход
```

**PATCH /tickets/{id}/assignee** (executor, admin)

```json
// запрос
{ "assigneeId": 3 }  // null чтобы снять

// ответ 200 — тикет
```

**POST /tickets/bulk** (executor, admin)

```json
// запрос
{ "ids": [1, 2, 3], "action": "setStatus", "value": "resolved" }

// ответ 200
{ "updated": 3 }
```

---

### /tickets/{id}/comments

**GET** — список комментариев (внутренние скрыты от applicant на сервере)

**POST**

```json
// запрос
{ "text": "string", "isInternal": false }

// ответ 201 — комментарий
```

---

### /tickets/{id}/files

**POST** — multipart/form-data, поле `file`

```json
// ответ 201
{ "id": 1, "filename": "screenshot.png", "url": "/files/1/screenshot.png" }
```

---

### /tickets/{id}/history

**GET** — полная история изменений тикета

---

### /tickets/{id}/dependencies (FR-23)

**POST**

```json
// запрос
{ "blockedById": 5 }

// ответ 201
{ "ticketId": 10, "blockedById": 5 }
```

**DELETE /tickets/{id}/dependencies/{blockedById}** → 200 `{ "deleted": true }`

---

### /categories

**GET** → `[{ "id": 1, "name": "IT" }, ...]`

**POST** (admin) → `{ "name": "Безопасность" }` → 201 `{ "id": 6, "name": "Безопасность" }`

**DELETE /categories/{id}** (admin) → 200 `{ "deleted": true }`

---

### /sla

**GET** → `[{ "priority": "high", "reactionHours": 2, "resolutionHours": 8 }, ...]`

**PUT /sla/{priority}** (admin)

```json
// запрос
{ "reactionHours": 1, "resolutionHours": 6 }
```

---

### /reports (только admin)

**GET /reports/by-status?from=date&to=date**

```json
[
  { "status": "new", "count": 5 },
  { "status": "in_progress", "count": 3 },
  { "status": "sla_breached", "count": 2 }
]
```

**GET /reports/avg-resolution?from=date&to=date** → `{ "avgHours": 6.4 }`

**GET /reports/executor-load?from=date&to=date** → `[{ "executorId": 2, "username": "petrov", "count": 8 }, ...]`

---

### /admin (только admin)

**GET /admin/users** → `[{ "id": 1, "username": "string", "email": "string", "role": "string" }, ...]`

**PATCH /admin/tickets/{id}/force**

```json
// запрос
{ "status": "resolved", "assigneeId": 3, "reason": "по требованию руководства" }

// ответ 200 — тикет
```

---

## Формат ошибок

```json
{ "error": "Текст ошибки", "details": ["поле: описание"] }
```

---

## Примечания

- Все даты в ISO 8601: `"2024-05-01T10:00:00Z"`
- `passwordHash` никогда не передаётся в ответах
- Внутренние комментарии фильтруются на сервере, не на фронте
- SLA приостанавливается при переходе в `waiting` и возобновляется при `in_progress`
- Журнал действий пишется в `AuditLogs`: кто, что, над чем, когда (ОБ-7)
