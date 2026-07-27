using Shouldly;
using SiskyApi.Modules.Auth.DTOs;
using SiskyApi.Modules.Auth.Validators;

namespace SiskyApi.Tests.Modules.Auth;

public class ForgotPasswordValidatorTests
{
    private readonly ForgotPasswordValidator _validator = new();

    [Fact]
    public async Task Validate_WhenEmailIsEmpty_ShouldFail()
    {
        var dto = new ForgotPasswordDto { Email = "" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WhenEmailIsInvalid_ShouldFail()
    {
        var dto = new ForgotPasswordDto { Email = "emailinvalido" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WhenEmailIsValid_ShouldPass()
    {
        var dto = new ForgotPasswordDto { Email = "usuario@email.com" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }
}