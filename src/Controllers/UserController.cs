using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiskyApi.DTOs;
using SiskyApi.Services;

namespace SiskyApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly IValidator<UserCreateDto> _createValidator;
    private readonly IValidator<UserUpdateDto> _updateValidator;
    private readonly IValidator<UserChangePasswordDto> _changePasswordValidator;

    public UserController(
        UserService userService,
        IValidator<UserCreateDto> createValidator,
        IValidator<UserUpdateDto> updateValidator,
        IValidator<UserChangePasswordDto> changePasswordValidator)
    {
        _userService = userService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new { id, name, email });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int perPage = 15)
    {
        var result = await _userService.GetAll(page, perPage);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetById(id);
        if (user is null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userService.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
    {
        dto.Id = id;
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userService.Update(id, dto);
        if (user is null) return NotFound();
        return Ok(user);
    }

    [HttpPatch("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] UserChangePasswordDto dto)
    {
        var validation = await _changePasswordValidator.ValidateAsync(dto);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _userService.ChangePassword(id, dto);
        if (!result) return BadRequest("Senha atual incorreta.");
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.Delete(id);
        if (!result) return NotFound();
        return NoContent();
    }
}