# Проект 02 · Helpdesk — Алгоритм, типы данных, преобразования

Низамаев · Брандт

---

## 1. АЛГОРИТМ РАБОТЫ

### Вход в систему

1. Получить входные данные: `email` (строка), `password` (строка).
2. **ЕСЛИ** пользователь с таким email не найден в БД **ТО**
   - Вернуть ошибку 400 «Неверный email или пароль»
   **ИНАЧЕ**
   - Продолжить
3. **ЕСЛИ** BCrypt.Verify(password, passwordHash) == false **ТО**
   - Вернуть ошибку 400 «Неверный email или пароль»
   **ИНАЧЕ**
   - Продолжить
4. Сформировать JWT-токен (см. часть «Преобразования», правило П-1).
5. Вернуть `token`, `id`, `username`, `role`.

---

### Создание обращения

1. Получить входные данные: `title`, `description`, `categoryId`, `priority` (см. часть «Типы данных»).
2. **ЕСЛИ** токен отсутствует или недействителен **ТО**
   - Вернуть ошибку 401 Unauthorized
   **ИНАЧЕ**
   - Продолжить
3. **ЕСЛИ** роль пользователя != `applicant` **ТО**
   - Вернуть ошибку 403 Forbidden
   **ИНАЧЕ**
   - Продолжить
4. **ЕСЛИ** `title` пустой или длина < 3 или > 200 **ТО**
   - Вернуть ошибку 400 Bad Request
   **ИНАЧЕ**
   - Продолжить
5. Создать запись Ticket: `status = new`, `createdAt = текущее время UTC`.
6. Рассчитать `slaDeadline` (см. «Преобразования», правило П-2).
7. Записать в историю: `field = status`, `oldValue = null`, `newValue = new`.
8. Сформировать итоговые данные по правилам части «Преобразования».
9. Вернуть TicketDto с кодом 201 Created.

---

### Смена статуса обращения

1. Получить входные данные: `id` (целое), `status` (перечисление), `comment` (строка).
2. **ЕСЛИ** токен отсутствует или недействителен **ТО**
   - Вернуть ошибку 401 Unauthorized
   **ИНАЧЕ**
   - Продолжить
3. Загрузить тикет по `id` из БД.
4. **ЕСЛИ** тикет не найден **ТО**
   - Вернуть ошибку 404 Not Found
   **ИНАЧЕ**
   - Продолжить
5. Проверить переход по таблице машины состояний (см. «Преобразования», правило П-3).
6. **ЕСЛИ** переход недопустим **ТО**
   - Вернуть ошибку 409 Conflict
   **ИНАЧЕ**
   - Применить новый статус
7. **ЕСЛИ** новый статус == `waiting` **ТО**
   - `slaPaused = true`, сохранить время паузы
   **ИНАЧЕ ЕСЛИ** предыдущий статус был `waiting` **ТО**
   - `slaPaused = false`, сдвинуть `slaDeadline` на время паузы
8. **ЕСЛИ** новый статус == `in_progress` и исполнитель не назначен **ТО**
   - `assigneeId = currentUserId` (автоназначение)
9. Записать в историю: `field = status`, `oldValue`, `newValue`, `comment`.
10. Записать в журнал аудита: субъект, действие, объект, время.
11. Вернуть обновлённый TicketDto с кодом 200 OK.

---

### Получение списка обращений

1. Получить параметры фильтрации: `status`, `priority`, `assigneeId`, `categoryId`, `search`, `slaBreached`, `page`, `pageSize`.
2. **ЕСЛИ** роль пользователя == `applicant` **ТО**
   - Добавить фильтр: `authorId == currentUserId` (только свои тикеты, ОБ-8)
   **ИНАЧЕ**
   - Показать все тикеты
3. Применить остальные фильтры если переданы.
4. Сформировать итоговые данные по правилам части «Преобразования».
5. Вернуть `items`, `total`, `page`, `pageSize`.

---

## 2. ТИПЫ ДАННЫХ

### Пользователь (User)

| № | Название поля | Тип C# |
|---|---|---|
| 01 | Id | int |
| 02 | Username | string |
| 03 | Email | string |
| 04 | PasswordHash | string |
| 05 | Role | enum Role |
| 06 | CreatedAt | DateTime |

### Обращение (Ticket)

| № | Название поля | Тип C# |
|---|---|---|
| 01 | Id | int |
| 02 | Number | string |
| 03 | Title | string |
| 04 | Description | string |
| 05 | Status | enum TicketStatus |
| 06 | Priority | enum Priority |
| 07 | CategoryId | int |
| 08 | AuthorId | int |
| 09 | AssigneeId | int? |
| 10 | CreatedAt | DateTime |
| 11 | SlaDeadline | DateTime |
| 12 | SlaBreached | bool |
| 13 | SlaPaused | bool |
| 14 | SlaPausedAt | DateTime? |
| 15 | SlaPausedTotal | TimeSpan |

### Комментарий (Comment)

| № | Название поля | Тип C# |
|---|---|---|
| 01 | Id | int |
| 02 | TicketId | int |
| 03 | AuthorId | int |
| 04 | Text | string |
| 05 | IsInternal | bool |
| 06 | CreatedAt | DateTime |

### Запись истории (HistoryEntry)

| № | Название поля | Тип C# |
|---|---|---|
| 01 | Id | int |
| 02 | TicketId | int |
| 03 | AuthorId | int |
| 04 | Field | string |
| 05 | OldValue | string? |
| 06 | NewValue | string |
| 07 | Comment | string? |
| 08 | CreatedAt | DateTime |

### Норматив SLA (SlaConfig)

| № | Название поля | Тип C# |
|---|---|---|
| 01 | Id | int |
| 02 | Priority | enum Priority |
| 03 | ReactionHours | int |
| 04 | ResolutionHours | int |

### Перечисления (enum)

```csharp
enum Role         { Applicant, Executor, Admin }
enum TicketStatus { New, InProgress, Waiting, Resolved, Closed }
enum Priority     { Low, Medium, High, Critical }
```

---

## 3. ПРЕОБРАЗОВАНИЯ

Слева — исходные поля и их типы, справа — итоговые, в центре — правило, по которому итог получается из исходных данных.

### П-1. Генерация JWT-токена при входе

| Исходные данные | | Правило преобразования | Итоговые данные | |
|---|---|---|---|---|
| **Поле** | **Тип C#** | **Как получается** | **Поле** | **Тип C#** |
| Id | int | Записывается в claim "sub" | token | string |
| Username | string | Записывается в claim "name" | — | — |
| Role | enum Role | Записывается в claim "role" | — | — |
| Jwt:Key (конфиг) | string | HMAC-SHA256 подпись, срок 24 часа | — | — |

### П-2. Расчёт SLA-дедлайна при создании тикета

| Исходные данные | | Правило преобразования | Итоговые данные | |
|---|---|---|---|---|
| **Поле** | **Тип C#** | **Как получается** | **Поле** | **Тип C#** |
| CreatedAt | DateTime | Дата создания тикета | SlaDeadline | DateTime |
| Priority | enum Priority | По приоритету берём ResolutionHours из SlaConfig | — | — |
| ResolutionHours | int | SlaDeadline = CreatedAt + TimeSpan.FromHours(ResolutionHours) | — | — |

### П-3. Проверка допустимости перехода статуса

| Исходные данные | | Правило преобразования | Итоговые данные | |
|---|---|---|---|---|
| **Поле** | **Тип C#** | **Как получается** | **Поле** | **Тип C#** |
| Status (текущий) | enum TicketStatus | Текущий статус тикета | allowed | bool |
| newStatus | enum TicketStatus | ЕСЛИ (current→new) есть в таблице переходов ТО true ИНАЧЕ false | — | — |

Таблица допустимых переходов:

| Из | В |
|---|---|
| New | InProgress |
| InProgress | Waiting, Resolved |
| Waiting | InProgress |
| Resolved | Closed, InProgress |
| Closed | InProgress |

### П-4. Хеширование пароля при регистрации

| Исходные данные | | Правило преобразования | Итоговые данные | |
|---|---|---|---|---|
| **Поле** | **Тип C#** | **Как получается** | **Поле** | **Тип C#** |
| password | string | BCrypt.Net.BCrypt.HashPassword(password, workFactor=12) | PasswordHash | string |

### П-5. Фильтрация тикетов по роли (ОБ-8)

| Исходные данные | | Правило преобразования | Итоговые данные | |
|---|---|---|---|---|
| **Поле** | **Тип C#** | **Как получается** | **Поле** | **Тип C#** |
| Role | enum Role | ЕСЛИ Role == Applicant ТО query.Where(t => t.AuthorId == userId) | items | IEnumerable\<Ticket\> |
| currentUserId | int | ИНАЧЕ возвращаем все тикеты | total | int |

### П-6. Расчёт среднего времени решения (отчёт)

| Исходные данные | | Правило преобразования | Итоговые данные | |
|---|---|---|---|---|
| **Поле** | **Тип C#** | **Как получается** | **Поле** | **Тип C#** |
| CreatedAt (история) | DateTime | Момент перехода в статус Resolved | avgHours | double |
| CreatedAt (тикет) | DateTime | (resolvedAt − ticketCreatedAt).TotalHours, среднее по всем | — | — |

### П-7. Пауза и возобновление SLA (FR-24)

| Исходные данные | | Правило преобразования | Итоговые данные | |
|---|---|---|---|---|
| **Поле** | **Тип C#** | **Как получается** | **Поле** | **Тип C#** |
| newStatus | enum TicketStatus | ЕСЛИ newStatus == Waiting ТО SlaPaused = true, SlaPausedAt = DateTime.UtcNow | SlaPaused | bool |
| SlaPausedAt | DateTime? | ЕСЛИ возврат в InProgress ТО SlaDeadline += (DateTime.UtcNow − SlaPausedAt) | SlaDeadline | DateTime |
