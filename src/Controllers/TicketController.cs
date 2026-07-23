using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiskyApi.DTOs;
using SiskyApi.Services;

namespace SiskyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/ticket")]
public class TicketController : ControllerBase
{
    private readonly TicketService _ticketService;
    private readonly IValidator<TicketCreateDto> _createValidator;
    private readonly IValidator<TicketMessageCreateDto> _messageValidator;

    public TicketController(
        TicketService ticketService,
        IValidator<TicketCreateDto> createValidator,
        IValidator<TicketMessageCreateDto> messageValidator)
    {
        _ticketService = ticketService;
        _createValidator = createValidator;
        _messageValidator = messageValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 15,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? search = null)
    {
        var result = await _ticketService.GetAll(page, perPage, status, priority, search);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ticket = await _ticketService.GetById(id);
        if (ticket is null) return NotFound();
        return Ok(ticket);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TicketCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ticket = await _ticketService.Create(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> AddMessage(int id, [FromBody] TicketMessageCreateDto dto)
    {
        var validation = await _messageValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var message = await _ticketService.AddMessage(id, dto, userId);
        if (message is null) return NotFound();
        return Ok(message);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var (success, error) = await _ticketService.UpdateStatus(id, status);
        if (!success) return BadRequest(error);
        return Ok();
    }
}