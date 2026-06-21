using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;
using SiskyApi.DTOs;

namespace SiskyApi.Validators;

public class UserCreateValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateValidator(AppDbContext context)
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