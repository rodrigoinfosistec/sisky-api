using Shouldly;
using SiskyApi.Modules.Tickets.DTOs;
using SiskyApi.Modules.Tickets.Validators;

namespace SiskyApi.Tests.Modules.Tickets;

public class TicketCreateValidatorTests
{
    private readonly TicketCreateValidator _validator = new();

    [Fact]
    public async Task Validate_WhenTitleIsEmpty_ShouldFail()
    {
        // ARRANGE
        var dto = new TicketCreateDto
        {
            Title = "",
            Description = "Descrição com mais de 10 caracteres",
            Priority = "medium"
        };

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task Validate_WhenTitleIsTooShort_ShouldFail()
    {
        // ARRANGE
        var dto = new TicketCreateDto
        {
            Title = "abc",
            Description = "Descrição com mais de 10 caracteres",
            Priority = "medium"
        };

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task Validate_WhenPriorityIsInvalid_ShouldFail()
    {
        // ARRANGE
        var dto = new TicketCreateDto
        {
            Title = "Título válido",
            Description = "Descrição com mais de 10 caracteres",
            Priority = "invalida"
        };

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Priority");
    }

    [Fact]
    public async Task Validate_WhenAllFieldsAreValid_ShouldPass()
    {
        // ARRANGE
        var dto = new TicketCreateDto
        {
            Title = "Título válido com mais de 5 caracteres",
            Description = "Descrição com mais de 10 caracteres",
            Priority = "high"
        };

        // ACT
        var result = await _validator.ValidateAsync(dto);

        // ASSERT
        result.IsValid.ShouldBeTrue();
    }
}