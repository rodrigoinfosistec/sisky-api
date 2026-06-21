using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;
using SiskyApi.DTOs;

namespace SiskyApi.Validators;

public class UserUpdateValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateValidator(AppDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MustAsync(async (dto, email, _) =>
                !await context.Users.AnyAsync(u => u.Email == email && u.Id != dto.Id))
            .WithMessage("E-mail já cadastrado.");
    }
}