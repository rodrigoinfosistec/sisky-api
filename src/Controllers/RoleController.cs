using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using SiskyApi.Authorization;
using SiskyApi.DTOs;
using SiskyApi.Services;

namespace SiskyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    private readonly RoleService _roleService;
    private readonly IValidator<RoleCreateDto> _createValidator;
    private readonly IValidator<RoleUpdateDto> _updateValidator;

    public RoleController(
        RoleService roleService,
        IValidator<RoleCreateDto> createValidator,
        IValidator<RoleUpdateDto> updateValidator)
    {
        _roleService = roleService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [RequirePermission("users.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 15,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc",
        [FromQuery] string? search = null)
    {
        var result = await _roleService.GetAll(page, perPage, sortBy, sortDir, search);
        return Ok(result);
    }

    [RequirePermission("users.view")]
    [HttpGet("{id}/permissions")]
    public async Task<IActionResult> GetPermissions(int id)
    {
        var result = await _roleService.GetPermissions(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [RequirePermission("users.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _roleService.Create(dto.Name);
        if (result is null) return BadRequest("Nome já existe.");
        return Ok(result);
    }

    [RequirePermission("users.edit")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RoleUpdateDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _roleService.Update(id, dto.Name);
        if (result is null) return BadRequest("Role não encontrada ou é do sistema.");
        return Ok(result);
    }

    [RequirePermission("users.delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _roleService.Delete(id);
        if (!success) return BadRequest(error);
        return NoContent();
    }

    [RequirePermission("users.edit")]
    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AddPermission(int id, [FromBody] int permissionId)
    {
        var result = await _roleService.AddPermission(id, permissionId);
        if (!result) return BadRequest("Permissão já atribuída.");
        return NoContent();
    }

    [RequirePermission("users.edit")]
    [HttpDelete("{id}/permissions/{permissionId}")]
    public async Task<IActionResult> RemovePermission(int id, int permissionId)
    {
        var result = await _roleService.RemovePermission(id, permissionId);
        if (!result) return NotFound();
        return NoContent();
    }
}