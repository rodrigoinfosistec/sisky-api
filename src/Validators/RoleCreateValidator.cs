using FluentValidation;
using SiskyApi.DTOs;

namespace SiskyApi.Validators;

public class RoleCreateValidator : AbstractValidator<RoleCreateDto>
{
    public RoleCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");
    }
}