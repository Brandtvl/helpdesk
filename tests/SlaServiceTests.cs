using Helpdesk.Api.Data;
using Helpdesk.Api.Enums;
using Helpdesk.Api.Models;
using Helpdesk.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Helpdesk.Tests;

public class SlaServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.SlaConfigs.AddRange(
            new SlaConfig { Id = 1, Priority = Priority.Low,      ReactionHours = 8,  ResolutionHours = 72 },
            new SlaConfig { Id = 2, Priority = Priority.Medium,   ReactionHours = 4,  ResolutionHours = 24 },
            new SlaConfig { Id = 3, Priority = Priority.High,     ReactionHours = 2,  ResolutionHours = 8  },
            new SlaConfig { Id = 4, Priority = Priority.Critical, ReactionHours = 0,  ResolutionHours = 4  }
        );
        db.SaveChanges();
        return db;
    }

    // --- Расчёт дедлайна ---

    [Theory]
    [InlineData(Priority.Low,      72)]
    [InlineData(Priority.Medium,   24)]
    [InlineData(Priority.High,      8)]
    [InlineData(Priority.Critical,  4)]
    public async Task CalculateDeadline_ReturnsCorrectHours(Priority priority, int expectedHours)
    {
        var sla = new SlaService(CreateDb());
        var createdAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var deadline = await sla.CalculateDeadlineAsync(priority, createdAt);

        Assert.Equal(createdAt.AddHours(expectedHours), deadline);
    }

    // --- Пауза SLA (FR-24) ---

    [Fact]
    public void PauseSla_SetsPausedFlagAndTime()
    {
        var sla = new SlaService(CreateDb());
        var ticket = new Ticket { SlaPaused = false };

        sla.PauseSla(ticket);

        Assert.True(ticket.SlaPaused);
        Assert.NotNull(ticket.SlaPausedAt);
    }

    [Fact]
    public void PauseSla_WhenAlreadyPaused_DoesNothing()
    {
        var sla = new SlaService(CreateDb());
        var pausedAt = DateTime.UtcNow.AddMinutes(-10);
        var ticket = new Ticket { SlaPaused = true, SlaPausedAt = pausedAt };

        sla.PauseSla(ticket);

        Assert.Equal(pausedAt, ticket.SlaPausedAt); // не изменилось
    }

    [Fact]
    public void ResumeSla_ClearsPauseAndExtendsDeadline()
    {
        var sla = new SlaService(CreateDb());
        var deadline = DateTime.UtcNow.AddHours(2);
        var pausedAt = DateTime.UtcNow.AddMinutes(-30);
        var ticket = new Ticket
        {
            SlaPaused   = true,
            SlaPausedAt = pausedAt,
            SlaDeadline = deadline
        };

        sla.ResumeSla(ticket);

        Assert.False(ticket.SlaPaused);
        Assert.Null(ticket.SlaPausedAt);
        // дедлайн сдвинулся примерно на 30 минут вперёд
        Assert.True(ticket.SlaDeadline > deadline);
    }

    [Fact]
    public void ResumeSla_WhenNotPaused_DoesNothing()
    {
        var sla = new SlaService(CreateDb());
        var deadline = DateTime.UtcNow.AddHours(2);
        var ticket = new Ticket { SlaPaused = false, SlaDeadline = deadline };

        sla.ResumeSla(ticket);

        Assert.Equal(deadline, ticket.SlaDeadline);
    }

    // --- Нарушение SLA (FR-16) ---

    [Fact]
    public void CheckBreach_WhenDeadlineExceeded_SetsBreachedTrue()
    {
        var sla = new SlaService(CreateDb());
        var ticket = new Ticket
        {
            Status      = TicketStatus.InProgress,
            SlaDeadline = DateTime.UtcNow.AddHours(-1) // уже прошло
        };

        sla.CheckBreach(ticket);

        Assert.True(ticket.SlaBreached);
    }

    [Fact]
    public void CheckBreach_WhenDeadlineNotReached_SetsBreachedFalse()
    {
        var sla = new SlaService(CreateDb());
        var ticket = new Ticket
        {
            Status      = TicketStatus.InProgress,
            SlaDeadline = DateTime.UtcNow.AddHours(2) // ещё не прошло
        };

        sla.CheckBreach(ticket);

        Assert.False(ticket.SlaBreached);
    }

    [Theory]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    public void CheckBreach_WhenResolved_AlwaysFalse(TicketStatus status)
    {
        var sla = new SlaService(CreateDb());
        var ticket = new Ticket
        {
            Status      = status,
            SlaDeadline = DateTime.UtcNow.AddHours(-1) // просрочено, но решено
        };

        sla.CheckBreach(ticket);

        Assert.False(ticket.SlaBreached);
    }
}
