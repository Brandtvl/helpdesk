using Helpdesk.Api.Data;
using Helpdesk.Api.DTOs;
using Helpdesk.Api.Models;
using Helpdesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

// FR-11: комментарии двух видов - публичные (видны заявителю) и внутренние (только исполнители)
// ОБ-2: заявитель не может создавать внутренние комментарии и не видит чужие внутренние
[ApiController]
[Route("api/tickets/{ticketId:int}/comments")]
[Authorize]
public class CommentsController(AppDbContext db, AuditService audit) : ControllerBase
{
    // GET /api/tickets/{ticketId}/comments
    // FR-11: заявитель получает только публичные, исполнитель/админ видят все
    [HttpGet]
    public async Task<IActionResult> GetComments(int ticketId)
    {
        var role = User.GetRole();
        var query = db.Comments
            .Include(c => c.Author)
            .Where(c => c.TicketId == ticketId);

        // FR-11: фильтруем внутренние для заявителя
        if (role == "applicant")
            query = query.Where(c => !c.IsInternal);

        var comments = await query.OrderBy(c => c.CreatedAt).ToListAsync();

        return Ok(comments.Select(c => new CommentDto(
            c.Id, c.TicketId, c.AuthorId, c.Author?.Username,
            c.Text, c.IsInternal, c.CreatedAt.ToString("O"))));
    }

    // POST /api/tickets/{ticketId}/comments
    // FR-11: добавление комментария, isInternal=true только для executor/admin
    // ОБ-4: валидация текста комментария
    [HttpPost]
    public async Task<IActionResult> AddComment(int ticketId, [FromBody] CreateCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest(new { error = "Текст комментария обязателен" });

        var role = User.GetRole();

        // FR-11: заявитель не может создавать внутренние комментарии
        // ОБ-2: проверка на сервере, не доверяем фронтенду
        if (req.IsInternal && role == "applicant")
            return StatusCode(403, new { error = "Заявитель не может создавать внутренние комментарии" });

        var ticketExists = await db.Tickets.AnyAsync(t => t.Id == ticketId);
        if (!ticketExists) return NotFound(new { error = "Тикет не найден" });

        var userId = User.GetUserId();
        var comment = new Comment
        {
            TicketId   = ticketId,
            AuthorId   = userId,
            Text       = req.Text,
            IsInternal = req.IsInternal,
            CreatedAt  = DateTime.UtcNow
        };

        db.Comments.Add(comment);

        // ОБ-7: журналируем добавление комментария
        audit.Log(userId, User.GetUsername(), "add_comment", "ticket", ticketId.ToString());
        await db.SaveChangesAsync();

        await db.Entry(comment).Reference(c => c.Author).LoadAsync();

        return StatusCode(201, new CommentDto(
            comment.Id, comment.TicketId, comment.AuthorId,
            comment.Author?.Username, comment.Text, comment.IsInternal,
            comment.CreatedAt.ToString("O")));
    }
}
