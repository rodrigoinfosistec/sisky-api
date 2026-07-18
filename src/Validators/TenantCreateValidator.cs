using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;
using SiskyApi.DTOs;

namespace SiskyApi.Validators;

public class TenantCreateValidator : AbstractValidator<TenantCreateDto>
{
    public TenantCreateValidator(AppDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres.");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomínio é obrigatório.")
            .MinimumLength(3).WithMessage("Subdomínio deve ter no mínimo 3 caracteres.")
            .Matches("^[a-z0-9-]+$").WithMessage("Subdomínio deve conter apenas letras minúsculas, números e hífens.")
            .MustAsync(async (subdomain, _) =>
                !await context.Tenants.AnyAsync(t => t.Subdomain == subdomain))
            .WithMessage("Subdomínio já cadastrado.");
    }
}