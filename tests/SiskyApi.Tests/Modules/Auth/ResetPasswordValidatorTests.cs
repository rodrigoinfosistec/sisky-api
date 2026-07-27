using Shouldly;
using SiskyApi.Modules.Auth.DTOs;
using SiskyApi.Modules.Auth.Validators;

namespace SiskyApi.Tests.Modules.Auth;

public class ResetPasswordValidatorTests
{
    private readonly ResetPasswordValidator _validator = new();

    [Fact]
    public async Task Validate_WhenTokenIsEmpty_ShouldFail()
    {
        var dto = new ResetPasswordDto { Token = "", NewPassword = "Senha@123", NewPasswordConfirmation = "Senha@123" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Token");
    }

    [Fact]
    public async Task Validate_WhenNewPasswordIsEmpty_ShouldFail()
    {
        var dto = new ResetPasswordDto { Token = "token-valido", NewPassword = "", NewPasswordConfirmation = "" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public async Task Validate_WhenPasswordConfirmationDoesNotMatch_ShouldFail()
    {
        var dto = new ResetPasswordDto { Token = "token-valido", NewPassword = "Senha@123", NewPasswordConfirmation = "SenhaDiferente" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "NewPasswordConfirmation");
    }

    [Fact]
    public async Task Validate_WhenAllFieldsAreValid_ShouldPass()
    {
        var dto = new ResetPasswordDto { Token = "token-valido", NewPassword = "Senha@123", NewPasswordConfirmation = "Senha@123" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }
}