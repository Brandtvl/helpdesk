using Helpdesk.Api.Data;
using Helpdesk.Api.DTOs;
using Helpdesk.Api.Models;
using Helpdesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

// FR-4: прикрепление файлов к обращению
// файлы сохраняются на диск в wwwroot/uploads/, в БД храним только путь
[ApiController]
[Route("api/tickets/{ticketId:int}/files")]
[Authorize] // ОБ-1: только авторизованные
public class FilesController(AppDbContext db, IWebHostEnvironment env, AuditService audit) : ControllerBase
{
    // POST /api/tickets/{ticketId}/files
    // FR-4: загрузка файла multipart/form-data
    [HttpPost]
    public async Task<IActionResult> Upload(int ticketId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Файл не выбран" });

        // проверяем что тикет существует
        var ticketExists = await db.Tickets.AnyAsync(t => t.Id == ticketId);
        if (!ticketExists) return NotFound(new { error = "Тикет не найден" });

        // создаём папку uploads если ещё нет
        var uploadsDir = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);

        // FR-4: генерируем уникальное имя чтобы не было конфликтов
        var safeFilename = Path.GetFileName(file.FileName);
        var storedName = $"{Guid.NewGuid()}_{safeFilename}";
        var fullPath = Path.Combine(uploadsDir, storedName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream);

        var ticketFile = new TicketFile
        {
            TicketId   = ticketId,
            Filename   = safeFilename,
            StoredPath = storedName,
            UploadedAt = DateTime.UtcNow
        };

        db.TicketFiles.Add(ticketFile);

        // ОБ-7: журналируем загрузку файла
        audit.Log(User.GetUserId(), User.GetUsername(), "upload_file", "ticket", ticketId.ToString());
        await db.SaveChangesAsync();

        // возвращаем путь по которому фронт сможет скачать файл
        return StatusCode(201, new FileDto(ticketFile.Id, ticketFile.Filename, $"/uploads/{storedName}"));
    }
}
