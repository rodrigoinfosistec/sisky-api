using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiskyApi.Services;

namespace SiskyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DepartmentController : ControllerBase
{
    private readonly DepartmentService _departmentService;

    public DepartmentController(DepartmentService departmentService)
    {
        _departmentService = departmentService;
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
    public async Task<IActionResult> Create([FromBody] string name)
    {
        var result = await _departmentService.Create(name);
        if (result is null) return BadRequest("Já existe um departamento com este nome.");
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] string name)
    {
        var result = await _departmentService.Update(id, name);
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