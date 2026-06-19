using Helpdesk.Api.Data;
using Helpdesk.Api.DTOs;
using Helpdesk.Api.Models;
using Helpdesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Controllers;

// FR-2:  категория выбирается из справочника, управляемого администратором
// FR-21: управление категориями - создание и удаление (только admin)
// ОБ-2:  GET доступен всем, POST/DELETE только admin
[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController(AppDbContext db, AuditService audit) : ControllerBase
{
    // GET /api/categories
    // FR-2: список категорий - нужен при создании обращения
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cats = await db.Categories.OrderBy(c => c.Name).ToListAsync();
        return Ok(cats.Select(c => new CategoryDto(c.Id, c.Name)));
    }

    // POST /api/categories
    // FR-21: добавление новой категории администратором
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Название категории обязательно" });

        var cat = new Category { Name = req.Name };
        db.Categories.Add(cat);

        // ОБ-7: журналируем создание категории
        audit.Log(User.GetUserId(), User.GetUsername(), "create_category", "category");
        await db.SaveChangesAsync();

        return StatusCode(201, new CategoryDto(cat.Id, cat.Name));
    }

    // DELETE /api/categories/{id}
    // FR-21: удаление категории администратором
    // нельзя удалить категорию если она используется в обращениях
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await db.Categories.FindAsync(id);
        if (cat is null) return NotFound(new { error = "Категория не найдена" });

        // проверяем что категория нигде не используется
        var inUse = await db.Tickets.AnyAsync(t => t.CategoryId == id);
        if (inUse) return Conflict(new { error = "Категория используется в обращениях" });

        db.Categories.Remove(cat);
        audit.Log(User.GetUserId(), User.GetUsername(), "delete_category", "category", id.ToString());
        await db.SaveChangesAsync();

        return Ok(new { deleted = true });
    }
}
