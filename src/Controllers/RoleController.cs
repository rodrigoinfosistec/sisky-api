using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiskyApi.Services;

namespace SiskyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    private readonly RoleService _roleService;

    public RoleController(RoleService roleService)
    {
        _roleService = roleService;
    }

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

    [HttpGet("{id}/permissions")]
    public async Task<IActionResult> GetPermissions(int id)
    {
        var result = await _roleService.GetPermissions(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AddPermission(int id, [FromBody] int permissionId)
    {
        var result = await _roleService.AddPermission(id, permissionId);
        if (!result) return BadRequest("Permissão já atribuída.");
        return NoContent();
    }

    [HttpDelete("{id}/permissions/{permissionId}")]
    public async Task<IActionResult> RemovePermission(int id, int permissionId)
    {
        var result = await _roleService.RemovePermission(id, permissionId);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] string name)
    {
        var result = await _roleService.Create(name);
        if (result is null) return BadRequest("Nome já existe.");
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] string name)
    {
        var result = await _roleService.Update(id, name);
        if (result is null) return BadRequest("Role não encontrada ou é do sistema.");
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _roleService.Delete(id);
        if (!result) return BadRequest("Role não encontrada ou é do sistema.");
        return NoContent();
    }
}