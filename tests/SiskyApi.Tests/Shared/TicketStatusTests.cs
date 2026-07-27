using Shouldly;
using SiskyApi.Shared.Constants;

namespace SiskyApi.Tests.Shared;

public class TicketStatusTests
{
    [Fact]
    public void TicketStatus_ShouldHaveFourStatuses()
    {
        // Verifica que temos exatamente 4 status
        TicketStatus.All.Length.ShouldBe(4);
    }

    [Fact]
    public void TicketStatus_ShouldContainOpen()
    {
        TicketStatus.All.ShouldContain(TicketStatus.Open);
    }

    [Fact]
    public void TicketStatus_ShouldContainInProgress()
    {
        TicketStatus.All.ShouldContain(TicketStatus.InProgress);
    }

    [Fact]
    public void TicketStatus_ShouldContainResolved()
    {
        TicketStatus.All.ShouldContain(TicketStatus.Resolved);
    }

    [Fact]
    public void TicketStatus_ShouldContainClosed()
    {
        TicketStatus.All.ShouldContain(TicketStatus.Closed);
    }

    [Fact]
    public void TicketStatus_ValuesShouldBeLowercase()
    {
        // Garante que os valores são lowercase — importante para o banco
        foreach (var status in TicketStatus.All)
        {
            status.ShouldBe(status.ToLower());
        }
    }
}