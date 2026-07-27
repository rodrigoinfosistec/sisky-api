using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SiskyApi.Shared.Data;
using SiskyApi.Modules.Users.DTOs;

namespace SiskyApi.Modules.Users.Validators;

public class UserCreateValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateValidator(IAppDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MustAsync(async (email, _) =>
                !await context.Users.AnyAsync(u => u.Email == email))
            .WithMessage("E-mail já cadastrado.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres.");
    }
}