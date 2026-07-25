using FluentValidation;
using SiskyApi.Modules.Tickets.DTOs;

namespace SiskyApi.Modules.Tickets.Validators;

public class TicketMessageCreateValidator : AbstractValidator<TicketMessageCreateDto>
{
    public TicketMessageCreateValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mensagem é obrigatória.")
            .MinimumLength(2).WithMessage("Mensagem deve ter no mínimo 2 caracteres.");
    }
}