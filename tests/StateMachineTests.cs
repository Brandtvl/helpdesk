using Helpdesk.Api.Enums;
using Helpdesk.Api.Services;
using Xunit;

namespace Helpdesk.Tests;

public class StateMachineTests
{
    // --- Допустимые переходы ---

    [Fact]
    public void New_CanTransitionTo_InProgress()
        => Assert.True(TicketStateMachine.CanTransition(TicketStatus.New, TicketStatus.InProgress));

    [Fact]
    public void InProgress_CanTransitionTo_Waiting()
        => Assert.True(TicketStateMachine.CanTransition(TicketStatus.InProgress, TicketStatus.Waiting));

    [Fact]
    public void InProgress_CanTransitionTo_Resolved()
        => Assert.True(TicketStateMachine.CanTransition(TicketStatus.InProgress, TicketStatus.Resolved));

    [Fact]
    public void Waiting_CanTransitionTo_InProgress()
        => Assert.True(TicketStateMachine.CanTransition(TicketStatus.Waiting, TicketStatus.InProgress));

    [Fact]
    public void Resolved_CanTransitionTo_Closed()
        => Assert.True(TicketStateMachine.CanTransition(TicketStatus.Resolved, TicketStatus.Closed));

    [Fact]
    public void Resolved_CanReopen_ToInProgress()
        => Assert.True(TicketStateMachine.CanTransition(TicketStatus.Resolved, TicketStatus.InProgress));

    [Fact]
    public void Closed_CanReopen_ToInProgress()
        => Assert.True(TicketStateMachine.CanTransition(TicketStatus.Closed, TicketStatus.InProgress));

    // --- Недопустимые переходы ---

    [Fact]
    public void New_CannotTransitionTo_Resolved()
        => Assert.False(TicketStateMachine.CanTransition(TicketStatus.New, TicketStatus.Resolved));

    [Fact]
    public void New_CannotTransitionTo_Closed()
        => Assert.False(TicketStateMachine.CanTransition(TicketStatus.New, TicketStatus.Closed));

    [Fact]
    public void New_CannotTransitionTo_Waiting()
        => Assert.False(TicketStateMachine.CanTransition(TicketStatus.New, TicketStatus.Waiting));

    [Fact]
    public void Waiting_CannotTransitionTo_Resolved()
        => Assert.False(TicketStateMachine.CanTransition(TicketStatus.Waiting, TicketStatus.Resolved));

    [Fact]
    public void Waiting_CannotTransitionTo_Closed()
        => Assert.False(TicketStateMachine.CanTransition(TicketStatus.Waiting, TicketStatus.Closed));

    [Fact]
    public void Closed_CannotTransitionTo_Resolved()
        => Assert.False(TicketStateMachine.CanTransition(TicketStatus.Closed, TicketStatus.Resolved));

    [Fact]
    public void Resolved_CannotTransitionTo_Waiting()
        => Assert.False(TicketStateMachine.CanTransition(TicketStatus.Resolved, TicketStatus.Waiting));

    // --- Парсинг строк ---

    [Theory]
    [InlineData("new",         TicketStatus.New)]
    [InlineData("in_progress", TicketStatus.InProgress)]
    [InlineData("waiting",     TicketStatus.Waiting)]
    [InlineData("resolved",    TicketStatus.Resolved)]
    [InlineData("closed",      TicketStatus.Closed)]
    public void Parse_ValidString_ReturnsCorrectStatus(string input, TicketStatus expected)
        => Assert.Equal(expected, TicketStateMachine.Parse(input));

    [Fact]
    public void Parse_InvalidString_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => TicketStateMachine.Parse("unknown_status"));

    // --- Сериализация ---

    [Theory]
    [InlineData(TicketStatus.New,        "new")]
    [InlineData(TicketStatus.InProgress, "in_progress")]
    [InlineData(TicketStatus.Waiting,    "waiting")]
    [InlineData(TicketStatus.Resolved,   "resolved")]
    [InlineData(TicketStatus.Closed,     "closed")]
    public void Serialize_ReturnsCorrectString(TicketStatus status, string expected)
        => Assert.Equal(expected, TicketStateMachine.Serialize(status));
}
