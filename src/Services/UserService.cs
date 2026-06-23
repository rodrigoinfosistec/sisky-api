using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;
using SiskyApi.DTOs;
using SiskyApi.Models;

namespace SiskyApi.Services;

public class UserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponseDto<UserResponseDto>> GetAll(int page, int perPage, string sortBy = "name", string sortDir = "asc", string? search = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Name.ToLower().Contains(search.ToLower()) ||
                                    u.Email.ToLower().Contains(search.ToLower()));
        }

        query = sortBy switch
        {
            "name" => sortDir == "desc" ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
            "email" => sortDir == "desc" ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "createdAt" => sortDir == "desc" ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _ => sortDir == "desc" ? query.OrderByDescending(u => u.Id) : query.OrderBy(u => u.Id)
        };

        var total = await query.CountAsync();
        var lastPage = (int)Math.Ceiling((double)total / perPage);

        var users = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(user => new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponseDto<UserResponseDto>
        {
            Data = users,
            Total = total,
            Page = page,
            PerPage = perPage,
            LastPage = lastPage
        };
    }

    public async Task<UserResponseDto?> GetById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return null;

        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponseDto> Create(UserCreateDto dto)
    {
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponseDto?> Update(int id, UserUpdateDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return null;

        user.Name = dto.Name;
        user.Email = dto.Email;

        await _context.SaveChangesAsync();

        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> ChangePassword(int id, UserChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return false;
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password)) return false;
        if (dto.NewPassword != dto.NewPasswordConfirmation) return false;

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}