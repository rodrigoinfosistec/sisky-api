using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiskyApi.DTOs;
using SiskyApi.Services;

namespace SiskyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DepartmentController : ControllerBase
{
    private readonly DepartmentService _departmentService;
    private readonly IValidator<DepartmentCreateDto> _createValidator;
    private readonly IValidator<DepartmentUpdateDto> _updateValidator;

    public DepartmentController(
        DepartmentService departmentService,
        IValidator<DepartmentCreateDto> createValidator,
        IValidator<DepartmentUpdateDto> updateValidator)
    {
        _departmentService = departmentService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 15,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc",
        [FromQuery] string? search = null)
    {
        var result = await _departmentService.GetAll(page, perPage, sortBy, sortDir, search);
        return Ok(result);
    }

    [HttpGet("simple")]
    public async Task<IActionResult> GetAllSimple()
    {
        var result = await _departmentService.GetAllSimple();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DepartmentCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _departmentService.Create(dto.Name);
        if (result is null) return BadRequest("Já existe um departamento com este nome.");
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DepartmentUpdateDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _departmentService.Update(id, dto.Name);
        if (result is null) return NotFound("Departamento não encontrado.");
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _departmentService.Delete(id);
        if (!success) return BadRequest(error);
        return NoContent();
    }
}