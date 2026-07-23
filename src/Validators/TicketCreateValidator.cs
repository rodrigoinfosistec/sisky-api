using FluentValidation;
using SiskyApi.Constants;
using SiskyApi.DTOs;

namespace SiskyApi.Validators;

public class TicketCreateValidator : AbstractValidator<TicketCreateDto>
{
    public TicketCreateValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Título é obrigatório.")
            .MinimumLength(5).WithMessage("Título deve ter no mínimo 5 caracteres.")
            .MaximumLength(100).WithMessage("Título deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória.")
            .MinimumLength(10).WithMessage("Descrição deve ter no mínimo 10 caracteres.");

        RuleFor(x => x.Priority)
            .Must(p => TicketPriority.All.Contains(p))
            .WithMessage($"Prioridade inválida. Use: {string.Join(", ", TicketPriority.All)}");
    }
}