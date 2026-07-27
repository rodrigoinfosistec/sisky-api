using Shouldly;
using SiskyApi.Modules.Admin.DTOs;
using SiskyApi.Modules.Admin.Validators;

namespace SiskyApi.Tests.Modules.Admin;

public class CompanyCreateValidatorTests
{
    private readonly CompanyCreateValidator _validator = new();

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldFail()
    {
        var dto = new CompanyCreateDto { Name = "" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenNameIsTooShort_ShouldFail()
    {
        var dto = new CompanyCreateDto { Name = "a" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenPrimaryColorIsInvalidHex_ShouldFail()
    {
        var dto = new CompanyCreateDto { Name = "Empresa Válida", PrimaryColor = "red" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "PrimaryColor");
    }

    [Fact]
    public async Task Validate_WhenPrimaryColorIsValidHex_ShouldPass()
    {
        var dto = new CompanyCreateDto { Name = "Empresa Válida", PrimaryColor = "#2563eb" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_WhenPrimaryColorIsNull_ShouldPass()
    {
        var dto = new CompanyCreateDto { Name = "Empresa Válida", PrimaryColor = null };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_WhenNameIsValid_ShouldPass()
    {
        var dto = new CompanyCreateDto { Name = "Empresa Válida" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }
}