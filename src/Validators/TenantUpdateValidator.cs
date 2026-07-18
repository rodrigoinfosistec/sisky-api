using FluentValidation;
using SiskyApi.DTOs;

namespace SiskyApi.Validators;

public class TenantUpdateValidator : AbstractValidator<TenantUpdateDto>
{
    public TenantUpdateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres.");
    }
}