using Shouldly;
using SiskyApi.Modules.Admin.DTOs;
using SiskyApi.Modules.Admin.Validators;

namespace SiskyApi.Tests.Modules.Admin;

public class TenantUpdateValidatorTests
{
    private readonly TenantUpdateValidator _validator = new();

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldFail()
    {
        var dto = new TenantUpdateDto { Name = "" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenNameIsTooShort_ShouldFail()
    {
        var dto = new TenantUpdateDto { Name = "ab" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenNameIsValid_ShouldPass()
    {
        var dto = new TenantUpdateDto { Name = "Tenant Válido" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }
}