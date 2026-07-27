using Shouldly;
using SiskyApi.Modules.Auth.DTOs;
using SiskyApi.Modules.Auth.Validators;

namespace SiskyApi.Tests.Modules.Auth;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public async Task Validate_WhenEmailIsEmpty_ShouldFail()
    {
        var dto = new LoginDto { Email = "", Password = "senha123" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WhenEmailIsInvalid_ShouldFail()
    {
        var dto = new LoginDto { Email = "emailinvalido", Password = "senha123" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WhenPasswordIsEmpty_ShouldFail()
    {
        var dto = new LoginDto { Email = "usuario@email.com", Password = "" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_WhenAllFieldsAreValid_ShouldPass()
    {
        var dto = new LoginDto { Email = "usuario@email.com", Password = "senha123" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }
}