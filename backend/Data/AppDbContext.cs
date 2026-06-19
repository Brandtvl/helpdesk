using Helpdesk.Api.Enums;
using Helpdesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Data;

// ОБ-3: единая точка доступа к данным через EF Core
// ОБ-8: пароли хэшируются до сохранения, в БД они никогда не хранятся открытым текстом
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<HistoryEntry> HistoryEntries => Set<HistoryEntry>();
    public DbSet<SlaConfig> SlaConfigs => Set<SlaConfig>();
    public DbSet<TicketFile> TicketFiles => Set<TicketFile>();
    public DbSet<TicketDependency> TicketDependencies => Set<TicketDependency>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();  // ОБ-7: журнал аудита

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // FR-23: зависимости тикетов — составной ключ чтобы одна пара не могла
        // встречаться дважды в таблице
        modelBuilder.Entity<TicketDependency>()
            .HasKey(d => new { d.TicketId, d.BlockedById });

        modelBuilder.Entity<TicketDependency>()
            .HasOne(d => d.Ticket)
            .WithMany(t => t.BlockedBy)
            .HasForeignKey(d => d.TicketId)
            .OnDelete(DeleteBehavior.Restrict);  // без каскадного удаления

        modelBuilder.Entity<TicketDependency>()
            .HasOne(d => d.BlockedByTicket)
            .WithMany(t => t.Blocking)
            .HasForeignKey(d => d.BlockedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict чтобы не было циклических CASCADE DELETE в SQLite
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Author)
            .WithMany(u => u.CreatedTickets)
            .HasForeignKey(t => t.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);  // при удалении юзера исполнитель снимается

        // ОБ-3: хранение enum-ов как строк — читаемее в БД и нет проблем при миграциях
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Ticket>()
            .Property(t => t.Status)
            .HasConversion<string>();  // FR-6: статус хранится строкой

        modelBuilder.Entity<Ticket>()
            .Property(t => t.Priority)
            .HasConversion<string>();  // FR-3: приоритет хранится строкой

        modelBuilder.Entity<SlaConfig>()
            .Property(s => s.Priority)
            .HasConversion<string>();

        // уникальность на уровне БД — дополнительная защита
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();

        // FR-5: номер обращения уникален в системе
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.Number).IsUnique();

        // FR-14: стартовые нормативы SLA по приоритетам
        // при первом запуске EF применит их через миграцию
        modelBuilder.Entity<SlaConfig>().HasData(
            new SlaConfig { Id = 1, Priority = Priority.Low,      ReactionHours = 8,  ResolutionHours = 72 },
            new SlaConfig { Id = 2, Priority = Priority.Medium,   ReactionHours = 4,  ResolutionHours = 24 },
            new SlaConfig { Id = 3, Priority = Priority.High,     ReactionHours = 2,  ResolutionHours = 8  },
            new SlaConfig { Id = 4, Priority = Priority.Critical, ReactionHours = 0,  ResolutionHours = 4  }
        );

        // FR-2: базовые категории, чтобы сразу можно было создавать обращения
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Общие вопросы" },
            new Category { Id = 2, Name = "Техническая поддержка" },
            new Category { Id = 3, Name = "Отчётность" },
            new Category { Id = 4, Name = "Доступы и права" }
        );
    }
}
