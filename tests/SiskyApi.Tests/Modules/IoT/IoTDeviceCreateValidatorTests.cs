using Shouldly;
using SiskyApi.Modules.IoT.DTOs;
using SiskyApi.Modules.IoT.Validators;

namespace SiskyApi.Tests.Modules.IoT;

public class IoTDeviceCreateValidatorTests
{
    private readonly IoTDeviceCreateValidator _validator = new();

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldFail()
    {
        var dto = new IoTDeviceCreateDto { Name = "", Type = "dht22" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenNameIsTooShort_ShouldFail()
    {
        var dto = new IoTDeviceCreateDto { Name = "ab", Type = "dht22" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenNameIsTooLong_ShouldFail()
    {
        var dto = new IoTDeviceCreateDto { Name = new string('a', 101), Type = "dht22" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenTypeIsEmpty_ShouldFail()
    {
        var dto = new IoTDeviceCreateDto { Name = "Sensor Sala 1", Type = "" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Type");
    }

    [Fact]
    public async Task Validate_WhenTypeIsInvalid_ShouldFail()
    {
        var dto = new IoTDeviceCreateDto { Name = "Sensor Sala 1", Type = "invalid_type" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Type");
    }

    [Fact]
    public async Task Validate_WhenTypeIsDht22_ShouldPass()
    {
        var dto = new IoTDeviceCreateDto { Name = "Sensor Sala 1", Type = "dht22" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_WhenTypeIsHcSr04_ShouldPass()
    {
        var dto = new IoTDeviceCreateDto { Name = "Sensor Entrada", Type = "hc_sr04" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_WhenTypeIsCustom_ShouldPass()
    {
        var dto = new IoTDeviceCreateDto { Name = "Sensor Custom", Type = "custom" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validate_WhenAllFieldsAreValid_ShouldPass()
    {
        var dto = new IoTDeviceCreateDto { Name = "Sensor Sala 1", Type = "dht22" };
        var result = await _validator.ValidateAsync(dto);
        result.IsValid.ShouldBeTrue();
    }
}