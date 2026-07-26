using FluentValidation;
using SiskyApi.Modules.Admin.DTOs;

namespace SiskyApi.Modules.Admin.Validators;

public class CompanyUpdateValidator : AbstractValidator<CompanyUpdateDto>
{
    public CompanyUpdateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.PrimaryColor)
            .Matches("^#([A-Fa-f0-9]{6})$").WithMessage("Cor primária deve ser um hex válido (ex: #111111).")
            .When(x => !string.IsNullOrEmpty(x.PrimaryColor));
    }
}