using FluentValidation;
using SiskyApi.DTOs;

namespace SiskyApi.Validators;

public class UserChangePasswordValidator : AbstractValidator<UserChangePasswordDto>
{
    public UserChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Senha atual é obrigatória.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória.")
            .MinimumLength(6).WithMessage("Nova senha deve ter no mínimo 6 caracteres.");

        RuleFor(x => x.NewPasswordConfirmation)
            .NotEmpty().WithMessage("Confirmação de senha é obrigatória.")
            .Equal(x => x.NewPassword).WithMessage("Confirmação de senha não confere.");
    }
}