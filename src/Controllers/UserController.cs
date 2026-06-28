using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiskyApi.Authorization;
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
    private readonly StorageService _storageService;

    public UserController(
        UserService userService,
        IValidator<UserCreateDto> createValidator,
        IValidator<UserUpdateDto> updateValidator,
        IValidator<UserChangePasswordDto> changePasswordValidator,
        StorageService storageService)
    {
        _userService = userService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _changePasswordValidator = changePasswordValidator;
        _storageService = storageService;
    }

    [RequirePermission("users.view")]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetById(id);
        if (user is null) return NotFound();

        var tenantId = User.FindFirstValue("tenant_id");
        var companyId = User.FindFirstValue("company_id");
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = User.FindAll("permission").Select(c => c.Value).ToList();

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.AvatarUrl,
            user.CreatedAt,
            TenantId = tenantId != null ? int.Parse(tenantId) : (int?)null,
            CompanyId = companyId != null ? int.Parse(companyId) : (int?)null,
            Roles = roles,
            Permissions = permissions
        });
    }

    [RequirePermission("users.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 15,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc",
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null)
    {
        var result = await _userService.GetAll(page, perPage, sortBy, sortDir, search, active);
        return Ok(result);
    }

    [RequirePermission("users.view")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetById(id);
        if (user is null) return NotFound();
        return Ok(user);
    }

    [RequirePermission("users.view")]
    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetDetails(int id)
    {
        var details = await _userService.GetUserDetails(id);
        if (details is null) return NotFound();
        return Ok(details);
    }

    [RequirePermission("users.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userService.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [RequirePermission("users.edit")]
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

    [RequirePermission("users.edit")]
    [HttpPatch("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] UserChangePasswordDto dto)
    {
        var validation = await _changePasswordValidator.ValidateAsync(dto);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _userService.ChangePassword(id, dto);
        if (!result) return BadRequest("Senha atual incorreta.");
        return NoContent();
    }

    [RequirePermission("users.edit")]
    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error, data) = await _userService.ToggleActive(id, currentUserId);
        if (!success) return BadRequest(error);
        return Ok(data);
    }

    [RequirePermission("users.delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.Delete(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [RequirePermission("users.edit")]
    [HttpPost("{id}/avatar")]
    public async Task<IActionResult> UpdateAvatar(int id)
    {
        var file = Request.Form.Files.FirstOrDefault();

        if (file is null || file.Length == 0)
            return BadRequest("Arquivo inválido.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Formato inválido. Use JPEG, PNG ou WebP.");

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest("Arquivo muito grande. Máximo 2MB.");

        using var stream = file.OpenReadStream();
        var url = await _userService.UpdateAvatar(id, stream, file.FileName, file.ContentType, _storageService);

        if (url is null) return NotFound();

        return Ok(new { avatarUrl = url });
    }

    [RequirePermission("users.view")]
    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var companies = await _userService.GetUserCompanies(userId);
        return Ok(companies);
    }

    [RequirePermission("users.edit")]
    [HttpPost("{id}/companies")]
    public async Task<IActionResult> AddCompany(int id, [FromBody] AddUserCompanyDto dto)
    {
        var result = await _userService.AddCompany(id, dto.CompanyId);
        if (!result) return BadRequest("Empresa já associada ou não encontrada.");
        return NoContent();
    }

    [RequirePermission("users.edit")]
    [HttpDelete("{id}/companies/{companyId}")]
    public async Task<IActionResult> RemoveCompany(int id, int companyId)
    {
        var result = await _userService.RemoveCompany(id, companyId);
        if (!result) return BadRequest("Empresa não encontrada ou é a empresa padrão.");
        return NoContent();
    }

    [RequirePermission("users.edit")]
    [HttpPatch("{id}/companies/{companyId}/default")]
    public async Task<IActionResult> SetDefaultCompany(int id, int companyId)
    {
        var result = await _userService.SetDefaultCompany(id, companyId);
        if (!result) return NotFound();
        return NoContent();
    }

    [RequirePermission("users.edit")]
    [HttpPost("{id}/companies/{companyId}/roles")]
    public async Task<IActionResult> AddRole(int id, int companyId, [FromBody] AddUserRoleDto dto)
    {
        var result = await _userService.AddRole(id, companyId, dto.RoleId);
        if (!result) return BadRequest("Role já atribuída ou não encontrada.");
        return NoContent();
    }

    [RequirePermission("users.edit")]
    [HttpDelete("{id}/companies/{companyId}/roles/{roleId}")]
    public async Task<IActionResult> RemoveRole(int id, int companyId, int roleId)
    {
        var result = await _userService.RemoveRole(id, companyId, roleId);
        if (!result) return NotFound();
        return NoContent();
    }
}