using FluentValidation;
using SiskyApi.Modules.IoT.DTOs;

namespace SiskyApi.Modules.IoT.Validators;

public class IoTDeviceCreateValidator : AbstractValidator<IoTDeviceCreateDto>
{
    private static readonly string[] ValidTypes = { "dht22", "hc_sr04", "custom" };

    public IoTDeviceCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Tipo é obrigatório.")
            .Must(t => ValidTypes.Contains(t))
            .WithMessage($"Tipo inválido. Tipos aceitos: {string.Join(", ", ValidTypes)}.");
    }
}