# Проект 02 · Helpdesk — Алгоритм, типы данных, преобразования

Низамаев · Брандт

---

## 1. АЛГОРИТМ РАБОТЫ

### Вход в систему (`AuthService.LoginAsync`)

1. Получить входные данные: `email` (string), `password` (string).

2. Найти пользователя по email. **ЕСЛИ** не найден — вернуть 400.

```csharp
// AuthService.cs
var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
if (user == null)
    throw new UnauthorizedAccessException("Неверный email или пароль");
// EF Core генерирует параметризованный SQL — защита от SQL-инъекций (ОБ-8)
```

3. Проверить пароль. **ЕСЛИ** не совпадает — вернуть 400.

```csharp
// BCrypt.Verify сравнивает введённый пароль с хешем из БД (ОБ-8)
bool passwordOk = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
if (!passwordOk)
    throw new UnauthorizedAccessException("Неверный email или пароль");
```

4. Сформировать JWT-токен с id, именем и ролью пользователя.

```csharp
// GenerateToken() — кладём данные пользователя в claims токена
var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.Role, SerializeRole(user.Role)),
};
var token = new JwtSecurityToken(
    issuer: _config["Jwt:Issuer"],
    expires: DateTime.UtcNow.AddHours(24), // токен живёт 24 часа
    signingCredentials: creds              // подпись HMAC-SHA256
);
```

5. Вернуть `token`, `id`, `username`, `role`.

---

### Создание обращения (`TicketService.CreateAsync`)

1. Получить входные данные: `title` (string), `description` (string), `categoryId` (int), `priority` (string).

2. Проверка токена и роли происходит автоматически через атрибут на контроллере.

```csharp
// TicketsController.cs — [Authorize] проверяет JWT до вызова метода
[HttpPost]
[Authorize]
public async Task<IActionResult> Create([FromBody] CreateTicketRequest req) { ... }
```

3. Рассчитать дедлайн SLA по приоритету.

```csharp
// SlaService.CalculateDeadlineAsync()
var deadline = await _sla.CalculateDeadlineAsync(priority, now);
// берём норматив из таблицы SlaConfigs и прибавляем к текущему времени
```

4. Создать тикет, сохранить в БД, сформировать номер из Id.

```csharp
var ticket = new Ticket
{
    Title       = req.Title.Trim(),
    Description = req.Description.Trim(),
    CategoryId  = req.CategoryId,
    Priority    = priority,
    AuthorId    = authorId,
    Status      = TicketStatus.New,
    CreatedAt   = now,
    SlaDeadline = deadline
};
_db.Tickets.Add(ticket);
await _db.SaveChangesAsync();

ticket.Number = "#" + ticket.Id; // FR-5: номер формируется из реального Id
```

5. Записать в журнал аудита и вернуть TicketDto.

```csharp
_audit.Log(authorId, authorName, "create", "ticket", ticket.Id.ToString()); // ОБ-7
await _db.SaveChangesAsync();
return await MapToDto(ticket.Id);
```

---

### Смена статуса обращения (`TicketService.ChangeStatusAsync`)

1. Получить входные данные: `id` (int), `status` (string), `comment` (string?).

2. Загрузить тикет из БД вместе с блокирующими обращениями.

```csharp
var ticket = await _db.Tickets
    .Include(t => t.BlockedBy).ThenInclude(d => d.BlockedByTicket)
    .FirstOrDefaultAsync(t => t.Id == ticketId);
if (ticket == null)
    throw new KeyNotFoundException("Тикет не найден"); // → 404
```

3. Проверить допустимость перехода по машине состояний.

```csharp
var newStatus = TicketStateMachine.Parse(req.Status);
var oldStatus = ticket.Status;

if (!TicketStateMachine.CanTransition(oldStatus, newStatus))
    throw new InvalidOperationException("INVALID_TRANSITION"); // → 409
```

4. **ЕСЛИ** закрытие и есть открытые блокирующие тикеты — запретить (FR-23).

```csharp
if (newStatus == TicketStatus.Closed)
{
    bool hasOpenBlockers = ticket.BlockedBy
        .Any(d => d.BlockedByTicket.Status != TicketStatus.Closed
               && d.BlockedByTicket.Status != TicketStatus.Resolved);
    if (hasOpenBlockers)
        throw new InvalidOperationException("BLOCKED_BY_OPEN"); // → 409
}
```

5. Управление паузой SLA (FR-24).

```csharp
if (newStatus == TicketStatus.Waiting)
    _sla.PauseSla(ticket);          // ставим на паузу — фиксируем SlaPausedAt
else if (oldStatus == TicketStatus.Waiting)
    _sla.ResumeSla(ticket);         // снимаем паузу — сдвигаем SlaDeadline
```

6. **ЕСЛИ** берут в работу и исполнитель не назначен — автоназначение (FR-10).

```csharp
if (newStatus == TicketStatus.InProgress && ticket.AssigneeId == null)
{
    ticket.AssigneeId = userId;     // тот кто нажал кнопку становится исполнителем
    _db.HistoryEntries.Add(new HistoryEntry
    {
        Field = "assignee", OldValue = null, NewValue = username, ...
    });
}
```

7. Записать переход в историю и журнал аудита.

```csharp
_db.HistoryEntries.Add(new HistoryEntry
{
    Field    = "status",
    OldValue = TicketStateMachine.Serialize(oldStatus),
    NewValue = TicketStateMachine.Serialize(newStatus),
    Comment  = req.Comment,         // комментарий к переходу (FR-9)
    ...
});
_audit.Log(userId, username, "status:" + newStatus, "ticket", ticketId.ToString()); // ОБ-7
await _db.SaveChangesAsync();
```

---

### Получение списка обращений (`TicketService.GetListAsync`)

1. Получить параметры фильтрации: `status`, `priority`, `assigneeId`, `categoryId`, `search`, `slaBreached`, `page`, `pageSize`.

2. **ЕСЛИ** роль == `applicant` — показать только свои тикеты (ОБ-8).

```csharp
if (currentRole == "applicant")
    query = query.Where(t => t.AuthorId == currentUserId);
// исполнитель и администратор видят все тикеты
```

3. Применить остальные фильтры.

```csharp
if (!string.IsNullOrEmpty(search))
    query = query.Where(t => t.Title.Contains(search)      // FR-19: поиск
                          || t.Description.Contains(search));

if (slaBreached.HasValue)
    query = query.Where(t => t.SlaBreached == slaBreached.Value); // FR-16
```

4. Применить пагинацию и вернуть результат.

```csharp
var total = await query.CountAsync();
var items = await query
    .Skip((page - 1) * pageSize)  // пропускаем предыдущие страницы
    .Take(pageSize)                // берём нужное количество
    .ToListAsync();

return new PagedResult<TicketDto>(items.Select(MapDto), total, page, pageSize);
```

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
