```markdown
# 📚 Документация API Helpdesk System

**Версия:** 1.0  
**Дата:** 2024-05-01  
**Статус:** Актуально  

---

## 📑 Оглавление

- [Общее описание](#-общее-описание)
- [Быстрый старт](#-быстрый-старт)
- [Аутентификация](#-аутентификация)
- [Модели данных](#-модели-данных)
- [Эндпоинты](#-эндпоинты)
  - [Тикеты](#-тикеты-tickets)
  - [Комментарии](#-комментарии-comments)
  - [Категории](#-категории-categories)
  - [SLA](#-sla)
  - [Отчеты](#-отчеты-reports)
  - [Администрирование](#-администрирование-admin)
- [Бизнес-логика](#-бизнес-логика)
- [Ошибки](#-ошибки)
- [Примеры использования](#-примеры-использования)

---

## 🌐 Общее описание

### Базовые URL

| Окружение | URL |
|-----------|-----|
| **Локальная разработка** | `http://localhost:8080/api` |
| **Продакшн** | `https://helpdesk-production-6466.up.railway.app/api` |

### Основные характеристики

| Параметр | Значение |
|----------|----------|
| **Формат** | JSON |
| **Кодировка** | UTF-8 |
| **Авторизация** | JWT Bearer Token |
| **Язык интерфейса** | Русский |
| **Технические поля** | Английский |

### Заголовки запросов

```http
Authorization: Bearer <jwt_token>
Content-Type: application/json
Accept: application/json
```

### Коды ответов HTTP

| Код | Описание | Пример использования |
|-----|----------|---------------------|
| **200** | Успешный GET/PUT/PATCH | Получение списка тикетов |
| **201** | Успешное создание (POST) | Создание нового тикета |
| **400** | Неверный запрос/валидация | Неправильный формат данных |
| **401** | Не авторизован | Отсутствует JWT токен |
| **403** | Доступ запрещен | Недостаточно прав |
| **404** | Ресурс не найден | Тикет не существует |
| **409** | Конфликт | Недопустимый переход статуса |
| **500** | Внутренняя ошибка | Ошибка сервера |

---

## 🚀 Быстрый старт

### 1. Регистрация пользователя

```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "alex_engineer",
    "email": "alex@company.com",
    "password": "SecurePass123!",
    "role": "executor"
  }'
```

**Ожидаемый ответ:**

```json
{
  "id": 1,
  "username": "alex_engineer",
  "role": "executor",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 2. Вход в систему

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "alex@company.com",
    "password": "SecurePass123!"
  }'
```

**Ожидаемый ответ:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "alex_engineer",
    "role": "executor"
  }
}
```

### 3. Создание тикета

```bash
curl -X POST http://localhost:8080/api/tickets \
  -H "Authorization: Bearer <your_jwt_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Проблема с доступом к базе данных",
    "description": "Не могу подключиться к PostgreSQL после обновления...",
    "categoryId": 1,
    "priority": "high"
  }'
```

---

## 🔐 Аутентификация

### POST /auth/register

Регистрация нового пользователя в системе.

**Запрос:**

| Поле | Тип | Описание | Обязательность |
|------|-----|----------|----------------|
| `username` | string | Логин пользователя (3-50 символов) | ✅ Да |
| `email` | string | Email адрес | ✅ Да |
| `password` | string | Пароль (минимум 8 символов) | ✅ Да |
| `role` | string | Роль пользователя | ✅ Да |

**Допустимые роли:**
- `applicant` - Заявитель
- `executor` - Исполнитель
- `admin` - Администратор

**Пример запроса:**

```json
{
  "username": "john_doe",
  "email": "john@company.com",
  "password": "SecurePass123!",
  "role": "applicant"
}
```

**Ответ (201 Created):**

```json
{
  "id": 1,
  "username": "john_doe",
  "role": "applicant",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6ImpvaG5fZG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
}
```

**Ошибки:**

| Код | Причина |
|-----|---------|
| 400 | Невалидные данные (email, длина пароля и т.д.) |
| 409 | Пользователь с таким email уже существует |

---

### POST /auth/login

Вход в систему и получение JWT токена.

**Запрос:**

```json
{
  "email": "john@company.com",
  "password": "SecurePass123!"
}
```

**Ответ (200 OK):**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "john_doe",
    "role": "applicant"
  }
}
```

**Ошибка (400):**

```json
{
  "error": "Неверный email или пароль"
}
```

---

## 📊 Модели данных

### Пользователь (User)

```typescript
interface User {
  id: number;                    // Уникальный идентификатор
  username: string;              // Логин пользователя
  email: string;                 // Email
  role: "applicant" | "executor" | "admin";  // Роль
  createdAt: string;             // ISO 8601 дата создания
  // passwordHash НИКОГДА не передается в ответах!
}
```

**Пример:**

```json
{
  "id": 1,
  "username": "john_doe",
  "email": "john@company.com",
  "role": "applicant",
  "createdAt": "2024-05-01T10:00:00Z"
}
```

---

### Тикет (Ticket)

```typescript
interface Ticket {
  id: number;                    // Уникальный ID
  number: string;                // "#2041" - человекочитаемый номер
  title: string;                 // 3-200 символов
  description: string;           // до 5000 символов
  status: TicketStatus;          // Текущий статус
  priority: Priority;            // Приоритет
  categoryId: number;            // ID категории
  categoryName: string;          // Название категории
  authorId: number;              // ID автора
  authorName: string;            // Имя автора
  assigneeId: number | null;     // ID исполнителя
  assigneeName: string | null;   // Имя исполнителя
  createdAt: string;             // ISO 8601
  slaDeadline: string;           // Дедлайн по SLA
  slaBreached: boolean;          // Просрочен ли SLA
  slaPaused: boolean;            // Приостановлен ли SLA
  blockedByIds: number[];        // ID блокирующих тикетов
  childIds: number[];            // ID заблокированных тикетов
}

type TicketStatus = "new" | "in_progress" | "waiting" | "resolved" | "closed";
type Priority = "low" | "medium" | "high" | "critical";
```

**Пример:**

```json
{
  "id": 42,
  "number": "#2041",
  "title": "Не работает принтер",
  "description": "При попытке печати выдает ошибку 0x0001",
  "status": "in_progress",
  "priority": "high",
  "categoryId": 3,
  "categoryName": "Оргтехника",
  "authorId": 1,
  "authorName": "john_doe",
  "assigneeId": 2,
  "assigneeName": "alex_engineer",
  "createdAt": "2024-05-01T10:00:00Z",
  "slaDeadline": "2024-05-01T18:00:00Z",
  "slaBreached": false,
  "slaPaused": false,
  "blockedByIds": [38, 39],
  "childIds": [45, 46]
}
```

---

### Комментарий (Comment)

```typescript
interface Comment {
  id: number;                    // Уникальный ID
  ticketId: number;              // ID тикета
  authorId: number;              // ID автора
  authorName: string;            // Имя автора
  text: string;                  // Текст комментария
  isInternal: boolean;           // Внутренний/публичный
  createdAt: string;             // ISO 8601
}
```

**Пример:**

```json
{
  "id": 1,
  "ticketId": 42,
  "authorId": 2,
  "authorName": "alex_engineer",
  "text": "Проверяю драйвер принтера",
  "isInternal": false,
  "createdAt": "2024-05-01T10:30:00Z"
}
```

---

### Запись истории (History)

```typescript
interface HistoryEntry {
  id: number;                    // Уникальный ID
  ticketId: number;              // ID тикета
  authorId: number;              // ID автора изменения
  authorName: string;            // Имя автора
  field: HistoryField;           // Измененное поле
  oldValue: string | null;       // Старое значение
  newValue: string;              // Новое значение
  comment: string | null;        // Комментарий к изменению
  createdAt: string;             // ISO 8601
}

type HistoryField = "status" | "assignee" | "priority" | "dependency";
```

**Пример:**

```json
{
  "id": 1,
  "ticketId": 42,
  "authorId": 2,
  "authorName": "alex_engineer",
  "field": "status",
  "oldValue": "new",
  "newValue": "in_progress",
  "comment": "Начинаю диагностику",
  "createdAt": "2024-05-01T10:30:00Z"
}
```

---

### Категория (Category)

```typescript
interface Category {
  id: number;                    // Уникальный ID
  name: string;                  // Название категории
}
```

**Пример:**

```json
{
  "id": 1,
  "name": "IT Support"
}
```

---

### SLA Конфигурация (SlaConfig)

```typescript
interface SlaConfig {
  priority: Priority;            // Приоритет
  reactionHours: number;         // Часы на реакцию
  resolutionHours: number;       // Часы на решение
}
```

**Пример:**

```json
{
  "priority": "critical",
  "reactionHours": 0.5,
  "resolutionHours": 4
}
```

---

## 🎯 Эндпоинты

### 📌 Тикеты (/tickets)

---

#### GET /tickets

Получить список тикетов с фильтрацией и пагинацией.

**Доступ:** Авторизованные пользователи

**Параметры запроса (query):**

| Параметр | Тип | Описание | Значение по умолчанию |
|----------|-----|----------|----------------------|
| `status` | string | Фильтр по статусу | - |
| `priority` | string | Фильтр по приоритету | - |
| `assigneeId` | number | Фильтр по исполнителю | - |
| `categoryId` | number | Фильтр по категории | - |
| `search` | string | Поиск по title/description | - |
| `slaBreached` | boolean | Только просроченные | `false` |
| `page` | number | Номер страницы | `1` |
| `pageSize` | number | Размер страницы | `20` |
| `sortBy` | string | Поле сортировки | `createdAt` |
| `sortDir` | string | Направление сортировки (`asc`/`desc`) | `asc` |

**Допустимые значения сортировки:**
- `createdAt` - по дате создания
- `priority` - по приоритету
- `slaDeadline` - по дедлайну SLA

**Ответ (200 OK):**

```json
{
  "items": [
    {
      "id": 42,
      "number": "#2041",
      "title": "Не работает принтер",
      "status": "in_progress",
      "priority": "high"
    }
  ],
  "total": 42,
  "page": 1,
  "pageSize": 20
}
```

**Особенности:**
- Для `applicant` возвращаются ТОЛЬКО его тикеты (фильтрация на сервере)
- Для `executor` и `admin` возвращаются все тикеты

**Примеры запросов:**

```bash
# Получить все тикеты в работе с высоким приоритетом
GET /api/tickets?status=in_progress&priority=high

# Поиск по тексту
GET /api/tickets?search=принтер

# Только просроченные SLA
GET /api/tickets?slaBreached=true

# Сортировка по дедлайну (сначала срочные)
GET /api/tickets?sortBy=slaDeadline&sortDir=asc

# Вторая страница, 50 записей
GET /api/tickets?page=2&pageSize=50
```

---

#### GET /tickets/my

Получить "мои" тикеты.

**Доступ:** Авторизованные пользователи

**Особенности:**
- Для `applicant`: возвращает созданные им тикеты
- Для `executor`: возвращает назначенные ему тикеты
- Для `admin`: возвращает все тикеты (как GET /tickets)

**Ответ:** Аналогичен GET /tickets

**Пример:**

```bash
GET /api/tickets/my
```

---

#### POST /tickets

Создать новый тикет.

**Доступ:** Авторизованные пользователи

**Запрос:**

| Поле | Тип | Описание | Обязательность |
|------|-----|----------|----------------|
| `title` | string | Заголовок (3-200 символов) | ✅ Да |
| `description` | string | Описание (до 5000 символов) | ❌ Нет |
| `categoryId` | number | ID категории | ✅ Да |
| `priority` | string | Приоритет | ✅ Да |

**Пример запроса:**

```json
{
  "title": "Не работает VPN",
  "description": "Не могу подключиться к корпоративной сети с домашнего компьютера",
  "categoryId": 1,
  "priority": "high"
}
```

**Ответ (201 Created):**

```json
{
  "id": 42,
  "number": "#2041",
  "title": "Не работает VPN",
  "description": "Не могу подключиться к корпоративной сети с домашнего компьютера",
  "status": "new",
  "priority": "high",
  "categoryId": 1,
  "categoryName": "IT Support",
  "authorId": 1,
  "authorName": "john_doe",
  "assigneeId": null,
  "assigneeName": null,
  "createdAt": "2024-05-01T10:00:00Z",
  "slaDeadline": "2024-05-01T18:00:00Z",
  "slaBreached": false,
  "slaPaused": false,
  "blockedByIds": [],
  "childIds": []
}
```

**Автоматические действия:**
1. Генерация номера: `#{id}` (после сохранения в БД)
2. Установка статуса: `"new"`
3. Расчет SLA дедлайна: `createdAt + resolutionHours(priority)`
4. Создание записи в истории
5. Логирование действия (AuditLog)

**Ошибки:**

| Код | Причина |
|-----|---------|
| 400 | Невалидные данные (короткий title, неверный priority и т.д.) |
| 404 | Категория не найдена |

---

#### GET /tickets/{id}

Получить полную карточку тикета с комментариями и историей.

**Доступ:** Авторизованные пользователи

**Параметры пути:**
- `id` - ID тикета

**Ответ (200 OK):**

```json
{
  "id": 42,
  "number": "#2041",
  "title": "Не работает VPN",
  "description": "Не могу подключиться к корпоративной сети с домашнего компьютера",
  "status": "in_progress",
  "priority": "high",
  "categoryId": 1,
  "categoryName": "IT Support",
  "authorId": 1,
  "authorName": "john_doe",
  "assigneeId": 2,
  "assigneeName": "alex_engineer",
  "createdAt": "2024-05-01T10:00:00Z",
  "slaDeadline": "2024-05-01T18:00:00Z",
  "slaBreached": false,
  "slaPaused": false,
  "blockedByIds": [],
  "childIds": [],
  "comments": [
    {
      "id": 1,
      "ticketId": 42,
      "authorId": 2,
      "authorName": "alex_engineer",
      "text": "Начинаю диагностику",
      "isInternal": false,
      "createdAt": "2024-05-01T10:30:00Z"
    }
  ],
  "history": [
    {
      "id": 1,
      "ticketId": 42,
      "authorId": 1,
      "authorName": "john_doe",
      "field": "status",
      "oldValue": null,
      "newValue": "new",
      "comment": null,
      "createdAt": "2024-05-01T10:00:00Z"
    },
    {
      "id": 2,
      "ticketId": 42,
      "authorId": 2,
      "authorName": "alex_engineer",
      "field": "status",
      "oldValue": "new",
      "newValue": "in_progress",
      "comment": "Начинаю диагностику",
      "createdAt": "2024-05-01T10:30:00Z"
    }
  ]
}
```

**Особенности:**
- Для `applicant` скрываются внутренние комментарии (`isInternal: true`)
- Для `executor` и `admin` показываются все комментарии

**Ошибки:**

| Код | Причина |
|-----|---------|
| 404 | Тикет не найден |

---

#### PATCH /tickets/{id}/status

Изменить статус тикета.

**Доступ:** Executor, Admin

**Запрос:**

| Поле | Тип | Описание | Обязательность |
|------|-----|----------|----------------|
| `status` | string | Новый статус | ✅ Да |
| `comment` | string | Комментарий к изменению | ❌ Нет |

**Пример запроса:**

```json
{
  "status": "in_progress",
  "comment": "Начинаю работу над проблемой"
}
```

**Ответ (200 OK):** Полный объект тикета

**Ошибка 409 Conflict:**

```json
{
  "error": "Недопустимый переход статуса",
  "details": {
    "from": "new",
    "to": "resolved",
    "allowed": ["in_progress"]
  }
}
```

**Допустимые переходы:**

```
new → in_progress
in_progress → waiting, resolved
waiting → in_progress
resolved → closed, in_progress (reopen)
closed → in_progress (reopen)
```

**Автоматические действия:**

1. **При переходе в `in_progress`:**
   - Если исполнитель не назначен → назначается текущий пользователь
   - Запись в историю

2. **При переходе в `waiting`:**
   - Приостановка SLA (`slaPaused = true`)
   - Сохранение времени приостановки

3. **При переходе из `waiting` в `in_progress`:**
   - Возобновление SLA (пересчет дедлайна)
   - Снятие паузы

4. **При переходе `resolved → in_progress` (reopen):**
   - Снятие исполнителя (опционально)

**Примеры:**

```bash
# Взять в работу
PATCH /api/tickets/42/status
{
  "status": "in_progress",
  "comment": "Беру в работу"
}

# Запросить информацию
PATCH /api/tickets/42/status
{
  "status": "waiting",
  "comment": "Ожидаю ответ от заявителя"
}

# Решить проблему
PATCH /api/tickets/42/status
{
  "status": "resolved",
  "comment": "Проблема решена"
}

# Закрыть тикет
PATCH /api/tickets/42/status
{
  "status": "closed",
  "comment": "Заявитель подтвердил решение"
}
```

---

#### PATCH /tickets/{id}/assignee

Назначить исполнителя на тикет.

**Доступ:** Executor, Admin

**Запрос:**

| Поле | Тип | Описание | Обязательность |
|------|-----|----------|----------------|
| `assigneeId` | number \| null | ID исполнителя или null (снять) | ✅ Да |

**Пример запроса:**

```json
{
  "assigneeId": 3
}
```

**Ответ (200 OK):** Полный объект тикета

**Автоматические действия:**
1. Проверка существования пользователя
2. Обновление поля `assigneeId`
3. Запись в историю (field: "assignee")
4. Логирование в AuditLog

**Ошибки:**

| Код | Причина |
|-----|---------|
| 404 | Исполнитель не найден |
| 403 | Недостаточно прав |

**Примеры:**

```bash
# Назначить исполнителя
PATCH /api/tickets/42/assignee
{
  "assigneeId": 3
}

# Снять исполнителя
PATCH /api/tickets/42/assignee
{
  "assigneeId": null
}
```

---

#### POST /tickets/bulk

Массовое обновление тикетов.

**Доступ:** Executor, Admin

**Запрос:**

| Поле | Тип | Описание | Обязательность |
|------|-----|----------|----------------|
| `ids` | number[] | Массив ID тикетов | ✅ Да |
| `action` | string | Действие: `"setStatus"` | ✅ Да |
| `value` | string | Новое значение | ✅ Да |

**Пример запроса:**

```json
{
  "ids": [1, 2, 3],
  "action": "setStatus",
  "value": "resolved"
}
```

**Ответ (200 OK):**

```json
{
  "updated": 3
}
```

---

### 📌 Комментарии (/tickets/{id}/comments)

---

#### GET /tickets/{id}/comments

Получить список комментариев к тикету.

**Доступ:** Авторизованные пользователи

**Параметры пути:**
- `id` - ID тикета

**Ответ (200 OK):**

```json
[
  {
    "id": 1,
    "ticketId": 42,
    "authorId": 2,
    "authorName": "alex_engineer",
    "text": "Проверяю логи сервера",
    "isInternal": false,
    "createdAt": "2024-05-01T10:30:00Z"
  },
  {
    "id": 2,
    "ticketId": 42,
    "authorId": 2,
    "authorName": "alex_engineer",
    "text": "Обнаружена проблема в конфигурации",
    "isInternal": true,
    "createdAt": "2024-05-01T10:35:00Z"
  }
]
```

**Особенности:**
- Для `applicant` скрываются внутренние комментарии (`isInternal: true`)
- Для `executor` и `admin` показываются все комментарии

---

#### POST /tickets/{id}/comments

Добавить комментарий к тикету.

**Доступ:** Авторизованные пользователи

**Запрос:**

| Поле | Тип | Описание | Обязательность |
|------|-----|----------|----------------|
| `text` | string | Текст комментария | ✅ Да |
| `isInternal` | boolean | Внутренний/публичный | ❌ Нет (default: false) |

**Пример запроса:**

```json
{
  "text": "Проблема решена, рекомендую закрыть тикет",
  "isInternal": false
}
```

**Ответ (201 Created):**

```json
{
  "id": 3,
  "ticketId": 42,
  "authorId": 2,
  "authorName": "alex_engineer",
  "text": "Проблема решена, рекомендую закрыть тикет",
  "isInternal": false,
  "createdAt": "2024-05-01T11:00:00Z"
}
```

**Особенности:**
- Внутренние комментарии (`isInternal: true`) видны только исполнителям и администраторам
- Публичные комментарии видны всем

---

### 📌 Категории (/categories)

---

#### GET /categories

Получить список всех категорий.

**Доступ:** Авторизованные пользователи

**Ответ (200 OK):**

```json
[
  { "id": 1, "name": "IT Support" },
  { "id": 2, "name": "Network" },
  { "id": 3, "name": "Hardware" },
  { "id": 4, "name": "Software" },
  { "id": 5, "name": "Security" }
]
```

---

#### POST /categories

Создать новую категорию.

**Доступ:** Только Admin

**Запрос:**

| Поле | Тип | Описание | Обязательность |
|------|-----|----------|----------------|
| `name` | string | Название категории | ✅ Да |

**Пример запроса:**

```json
{
  "name": "Безопасность"
}
```

**Ответ (201 Created):**

```json
{
  "id": 6,
  "name": "Безопасность"
}
```

---

#### DELETE /categories/{id}

Удалить категорию.

**Доступ:** Только Admin

**Параметры пути:**
- `id` - ID категории

**Ответ (200 OK):**

```json
{
  "deleted": true
}
```

**Ошибки:**

| Код | Причина |
|-----|---------|
| 409 | Категория используется в тикетах |

---

### 📌 SLA (/sla)

---

#### GET /sla

Получить текущие SLA нормативы.

**Доступ:** Авторизованные пользователи

**Ответ (200 OK):**

```json
[
  {
    "priority": "low",
    "reactionHours": 8,
    "resolutionHours": 72
  },
  {
    "priority": "medium",
    "reactionHours": 4,
    "resolutionHours": 24
  },
  {
    "priority": "high",
    "reactionHours": 2,
    "resolutionHours": 8
  },
  {
    "priority": "critical",
    "reactionHours": 0.5,
    "resolutionHours": 4
  }
]
```

---

#### PUT /sla/{priority}

Изменить SLA нормативы.

**Доступ:** Только Admin

**Параметры пути:**
- `priority` - Приоритет (`low`, `medium`, `high`, `critical`)

**Запрос:**

| Поле | Тип | Описание | Обязательность |
|------|-----|----------|----------------|
| `reactionHours` | number | Часы на реакцию | ✅ Да |
| `resolutionHours` | number | Часы на решение | ✅ Да |

**Пример запроса:**

```json
{
  "reactionHours": 1,
  "resolutionHours": 6
}
```

**Ответ (200 OK):**

```json
{
  "priority": "high",
  "reactionHours": 1,
  "resolutionHours": 6
}
```

**Особенности:**
- Изменения применяются к новым тикетам
- Существующие тикеты не пересчитываются

---

### 📌 Отчеты (/reports)

**Доступ:** Только Admin

---

#### GET /reports/by-status

Отчет по статусам тикетов.

**Параметры запроса (query):**

| Параметр | Тип | Описание | Обязательность |
|----------|-----|----------|----------------|
| `from` | string | Дата начала (ISO 8601) | ✅ Да |
| `to` | string | Дата окончания (ISO 8601) | ✅ Да |

**Пример:**

```bash
GET /api/reports/by-status?from=2024-01-01&to=2024-01-31
```

**Ответ (200 OK):**

```json
[
  { "status": "new", "count": 5 },
  { "status": "in_progress", "count": 3 },
  { "status": "waiting", "count": 2 },
  { "status": "resolved", "count": 10 },
  { "status": "closed", "count": 8 },
  { "status": "sla_breached", "count": 2 }
]
```

---

#### GET /reports/avg-resolution

Среднее время решения тикетов.

**Параметры запроса (query):**

| Параметр | Тип | Описание | Обязательность |
|----------|-----|----------|----------------|
| `from` | string | Дата начала (ISO 8601) | ✅ Да |
| `to` | string | Дата окончания (ISO 8601) | ✅ Да |

**Ответ (200 OK):**

```json
{
  "avgHours": 6.4
}
```

---

#### GET /reports/executor-load

Нагрузка на исполнителей.

**Параметры запроса (query):**

| Параметр | Тип | Описание | Обязательность |
|----------|-----|----------|----------------|
| `from` | string | Дата начала (ISO 8601) | ✅ Да |
| `to` | string | Дата окончания (ISO 8601) | ✅ Да |

**Ответ (200 OK):**

```json
[
  {
    "executorId": 2,
    "username": "alex_engineer",
    "count": 8
  },
  {
    "executorId": 5,
    "username": "support_team",
    "count": 3
  }
]
```

---

### 📌 Администрирование (/admin)

**Доступ:** Только Admin

---

#### GET /admin/users

Получить список всех пользователей.

**Ответ (200 OK):**

```json
[
  {
    "id": 1,
    "username": "john_doe",
    "email": "john@company.com",
    "role": "applicant"
  },
  {
    "id": 2,
    "username": "alex_engineer",
    "email": "alex@company.com",
    "role": "executor"
  },
  {
    "id": 3,
    "username": "admin",
    "email": "admin@company.com",
    "role": "admin"
  }
]
```

---

#### PATCH /admin/tickets/{id}/force

Принудительное изменение тикета (обход бизнес-правил).

**Параметры пути:**
- `id` - ID тикета

**Запрос:**

| Поле | Тип | Описание | Обязательность |
|------|-----|----------|----------------|
| `status` | string | Новый статус | ❌ Нет |
| `assigneeId` | number | ID исполнителя | ❌ Нет |
| `reason` | string | Причина принудительного изменения | ✅ Да |

**Пример запроса:**

```json
{
  "status": "resolved",
  "assigneeId": 3,
  "reason": "Инцидент закрыт по регламенту №42"
}
```

**Ответ (200 OK):** Полный объект тикета

**Особенности:**
- Обходит стандартные проверки статусов
- Всегда записывается причина в историю
- Логируется в AuditLog как "force_update"

---

## 🧠 Бизнес-логика

### Статусы и допустимые переходы

```
new → in_progress → resolved → closed
           ↓           ↑
        waiting ──────┘
```

**Матрица переходов:**

| Из \\ В | new | in_progress | waiting | resolved | closed |
|---------|-----|-------------|---------|----------|--------|
| **new** | - | ✅ | ❌ | ❌ | ❌ |
| **in_progress** | ❌ | - | ✅ | ✅ | ❌ |
| **waiting** | ❌ | ✅ | - | ❌ | ❌ |
| **resolved** | ❌ | ✅ | ❌ | - | ✅ |
| **closed** | ❌ | ✅ | ❌ | ❌ | - |

### SLA Расчет

**Нормативы по умолчанию:**

| Приоритет | Реакция (часов) | Решение (часов) |
|-----------|----------------|-----------------|
| **low** | 8 | 72 |
| **medium** | 4 | 24 |
| **high** | 2 | 8 |
| **critical** | 0.5 | 4 |

**Алгоритм расчета:**

```csharp
// При создании тикета
var slaConfig = await slaService.GetConfig(priority);
ticket.SlaDeadline = ticket.CreatedAt.AddHours(slaConfig.ResolutionHours);

// При переходе в waiting
ticket.SlaPaused = true;
ticket.SlaPausedAt = DateTime.UtcNow;

// При выходе из waiting
var pausedDuration = DateTime.UtcNow - ticket.SlaPausedAt;
ticket.SlaDeadline = ticket.SlaDeadline.Add(pausedDuration);
ticket.SlaPaused = false;
ticket.SlaPausedAt = null;

// Проверка просрочки
if (DateTime.UtcNow > ticket.SlaDeadline && !ticket.SlaPaused)
{
    ticket.SlaBreached = true;
}
```

### Автоматические действия

| Событие | Действие |
|---------|----------|
| **Создание тикета** | Генерация номера, установка статуса "new", расчет SLA |
| **Переход в "in_progress"** | Автоназначение исполнителя (если не назначен) |
| **Переход в "waiting"** | Приостановка SLA |
| **Выход из "waiting"** | Возобновление SLA (пересчет дедлайна) |
| **Изменение статуса** | Запись в историю |
| **Назначение исполнителя** | Запись в историю |

---

## 🚨 Ошибки

### Формат ошибок

```json
{
  "error": "Текст ошибки",
  "details": [
    "поле1: описание",
    "поле2: описание"
  ]
}
```

### Типичные ошибки

**400 Bad Request - Ошибка валидации:**

```json
{
  "error": "Ошибка валидации",
  "details": [
    "title: Длина должна быть от 3 до 200 символов",
    "priority: Допустимые значения: low, medium, high, critical"
  ]
}
```

**401 Unauthorized - Нет токена или он недействителен:**

```json
{
  "error": "Требуется авторизация"
}
```

**403 Forbidden - Недостаточно прав:**

```json
{
  "error": "Недостаточно прав для выполнения операции"
}
```

**404 Not Found - Ресурс не найден:**

```json
{
  "error": "Тикет с ID 999 не найден"
}
```

**409 Conflict - Недопустимый переход статуса:**

```json
{
  "error": "Недопустимый переход статуса",
  "details": {
    "from": "new",
    "to": "resolved",
    "allowed": ["in_progress"]
  }
}
```

---

## 💡 Примеры использования

### Полный цикл работы с тикетом

#### 1. Регистрация пользователя (Applicant)

```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "john_doe",
    "email": "john@company.com",
    "password": "Pass123!",
    "role": "applicant"
  }'
```

#### 2. Вход и получение токена

```bash
TOKEN=$(curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@company.com",
    "password": "Pass123!"
  }' | jq -r '.token')
```

#### 3. Создание тикета

```bash
curl -X POST http://localhost:8080/api/tickets \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Не работает VPN",
    "description": "Не могу подключиться к корпоративной сети",
    "categoryId": 1,
    "priority": "high"
  }'
```

#### 4. Вход исполнителя

```bash
TOKEN=$(curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "support@company.com",
    "password": "Support123!"
  }' | jq -r '.token')
```

#### 5. Взять тикет в работу

```bash
curl -X PATCH http://localhost:8080/api/tickets/1/status \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "in_progress",
    "comment": "Начинаю диагностику"
  }'
```

#### 6. Добавить комментарий

```bash
curl -X POST http://localhost:8080/api/tickets/1/comments \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Проблема в настройках маршрутизатора",
    "isInternal": true
  }'
```

#### 7. Запросить информацию (переход в waiting)

```bash
curl -X PATCH http://localhost:8080/api/tickets/1/status \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "waiting",
    "comment": "Запросил дополнительные данные у пользователя"
  }'
```

#### 8. Продолжить работу (выход из waiting)

```bash
curl -X PATCH http://localhost:8080/api/tickets/1/status \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "in_progress",
    "comment": "Продолжаю работу, данные получены"
  }'
```

#### 9. Решить проблему

```bash
curl -X PATCH http://localhost:8080/api/tickets/1/status \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "resolved",
    "comment": "Проблема решена, VPN работает"
  }'
```

#### 10. Закрыть тикет (заявитель)

```bash
curl -X PATCH http://localhost:8080/api/tickets/1/status \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "closed",
    "comment": "Все работает, спасибо!"
  }'
```

---

### Администрирование

#### Получение отчетов

```bash
# Отчет по статусам
curl -X GET "http://localhost:8080/api/reports/by-status?from=2024-01-01&to=2024-01-31" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Среднее время решения
curl -X GET "http://localhost:8080/api/reports/avg-resolution?from=2024-01-01&to=2024-01-31" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Нагрузка на исполнителей
curl -X GET "http://localhost:8080/api/reports/executor-load?from=2024-01-01&to=2024-01-31" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

#### Управление SLA

```bash
# Изменить SLA для критических тикетов
curl -X PUT http://localhost:8080/api/sla/critical \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "reactionHours": 0.5,
    "resolutionHours": 2
  }'
```

#### Принудительное изменение

```bash
curl -X PATCH http://localhost:8080/api/admin/tickets/1/force \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "resolved",
    "reason": "Инцидент закрыт по регламенту"
  }'
```

---

### Фильтрация и поиск

```bash
# Поиск по статусу и приоритету
curl -X GET "http://localhost:8080/api/tickets?status=in_progress&priority=high" \
  -H "Authorization: Bearer $TOKEN"

# Поиск по тексту
curl -X GET "http://localhost:8080/api/tickets?search=VPN" \
  -H "Authorization: Bearer $TOKEN"

# Только просроченные SLA
curl -X GET "http://localhost:8080/api/tickets?slaBreached=true" \
  -H "Authorization: Bearer $TOKEN"

# Сортировка по дедлайну
curl -X GET "http://localhost:8080/api/tickets?sortBy=slaDeadline&sortDir=asc" \
  -H "Authorization: Bearer $TOKEN"

# Пагинация
curl -X GET "http://localhost:8080/api/tickets?page=2&pageSize=50" \
  -H "Authorization: Bearer $TOKEN"
```

---

### Работа с зависимостями

```bash
# Блокировка тикета
curl -X POST http://localhost:8080/api/tickets/10/dependencies \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "blockedById": 5
  }'

# Получение тикета с зависимостями
curl -X GET http://localhost:8080/api/tickets/10 \
  -H "Authorization: Bearer $TOKEN"

# Удаление зависимости
curl -X DELETE http://localhost:8080/api/tickets/10/dependencies/5 \
  -H "Authorization: Bearer $TOKEN"
```

---

## 📝 Примечания

### Важные правила

1. **Всегда используйте UTC время** (`DateTime.UtcNow`)
2. **Никогда не возвращайте `passwordHash`** в ответах
3. **Фильтрация внутренних комментариев** на сервере, а не на клиенте
4. **Все проверки прав** на сервере
5. **Валидация всех входных данных** (даже если фронтенд уже проверил)

### Безопасность

- JWT токены подписываются HMAC-SHA256
- Срок жизни токена - 24 часа
- Пароли хешируются BCrypt (соль встроена)
- Все эндпоинты кроме `/auth/*` требуют авторизации

### Технические детали

- Все даты в формате ISO 8601: `"2024-05-01T10:00:00Z"`
- Максимальный размер файла: 10MB
- Допустимые типы файлов: изображения, PDF, DOCX
- Максимум 100 тикетов в массовой операции

---

**Версия документации:** 1.0  
**Дата обновления:** 2024-05-01  
**Поддержка:** helpdesk@company.com