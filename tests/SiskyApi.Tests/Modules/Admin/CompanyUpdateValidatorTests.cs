using Shouldly;
using SiskyApi.Modules.Admin.DTOs;
using SiskyApi.Modules.Admin.Validators;

namespace SiskyApi.Tests.Modules.Admin;

public class CompanyUpdateValidatorTests
{
    private readonly CompanyUpdateValidator _validator = new();

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldFail()
    {
        var dto = new CompanyUpdateDto { Name = "" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenPrimaryColorIsInvalidHex_ShouldFail()
    {
        var dto = new CompanyUpdateDto { Name = "Empresa Válida", PrimaryColor = "blue" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "PrimaryColor");
    }

    [Fact]
    public async Task Validate_WhenAllFieldsAreValid_ShouldPass()
    {
        var dto = new CompanyUpdateDto { Name = "Empresa Válida", PrimaryColor = "#111111" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }
}