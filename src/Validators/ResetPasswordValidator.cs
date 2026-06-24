using FluentValidation;
using SiskyApi.DTOs;

namespace SiskyApi.Validators;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token é obrigatório.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória.")
            .MinimumLength(6).WithMessage("Nova senha deve ter no mínimo 6 caracteres.");

        RuleFor(x => x.NewPasswordConfirmation)
            .NotEmpty().WithMessage("Confirmação de senha é obrigatória.")
            .Equal(x => x.NewPassword).WithMessage("Confirmação de senha não confere.");
    }
}