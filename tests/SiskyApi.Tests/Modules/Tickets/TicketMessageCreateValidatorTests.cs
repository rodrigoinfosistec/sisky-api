using Shouldly;
using SiskyApi.Modules.Tickets.DTOs;
using SiskyApi.Modules.Tickets.Validators;

namespace SiskyApi.Tests.Modules.Tickets;

public class TicketMessageCreateValidatorTests
{
    private readonly TicketMessageCreateValidator _validator = new();

    [Fact]
    public async Task Validate_WhenMessageIsEmpty_ShouldFail()
    {
        var dto = new TicketMessageCreateDto { Message = "" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Message");
    }

    [Fact]
    public async Task Validate_WhenMessageIsTooShort_ShouldFail()
    {
        var dto = new TicketMessageCreateDto { Message = "a" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Message");
    }

    [Fact]
    public async Task Validate_WhenMessageIsValid_ShouldPass()
    {
        var dto = new TicketMessageCreateDto { Message = "Mensagem válida" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }
}