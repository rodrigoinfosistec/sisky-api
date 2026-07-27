using Shouldly;
using SiskyApi.Shared.Constants;

namespace SiskyApi.Tests.Shared;

public class TicketPriorityTests
{
    [Fact]
    public void TicketPriority_ShouldHaveFourPriorities()
    {
        TicketPriority.All.Length.ShouldBe(4);
    }

    [Fact]
    public void TicketPriority_ShouldContainLow()
    {
        TicketPriority.All.ShouldContain(TicketPriority.Low);
    }

    [Fact]
    public void TicketPriority_ShouldContainMedium()
    {
        TicketPriority.All.ShouldContain(TicketPriority.Medium);
    }

    [Fact]
    public void TicketPriority_ShouldContainHigh()
    {
        TicketPriority.All.ShouldContain(TicketPriority.High);
    }

    [Fact]
    public void TicketPriority_ShouldContainUrgent()
    {
        TicketPriority.All.ShouldContain(TicketPriority.Urgent);
    }

    [Fact]
    public void TicketPriority_ValuesShouldBeLowercase()
    {
        foreach (var priority in TicketPriority.All)
        {
            priority.ShouldBe(priority.ToLower());
        }
    }
}