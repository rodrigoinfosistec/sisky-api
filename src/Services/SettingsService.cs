using Microsoft.EntityFrameworkCore;
using SiskyApi.Data;
using SiskyApi.Models;

namespace SiskyApi.Services;

public class SettingsService
{
    private readonly AppDbContext _context;

    public SettingsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<string, string>> GetAll()
    {
        var settings = await _context.Settings.ToListAsync();
        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task<string?> Get(string key)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task Set(string key, string value)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);

        if (setting is null)
        {
            _context.Settings.Add(new Setting
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task SetMany(Dictionary<string, string> values)
    {
        foreach (var (key, value) in values)
            await Set(key, value);
    }
}