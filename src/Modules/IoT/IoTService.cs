using System.Text.Json;
using Bogus;
using Microsoft.EntityFrameworkCore;
using SiskyApi.Modules.IoT.DTOs;
using SiskyApi.Shared.Data;
using SiskyApi.Shared.Models;

namespace SiskyApi.Modules.IoT;

public class IoTService
{
    private readonly AppDbContext _context;

    public IoTService(AppDbContext context)
    {
        _context = context;
    }

    // Dispositivos
    public async Task<List<IoTDeviceResponseDto>> GetDevices(int tenantId)
    {
        return await _context.IoTDevices
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Name)
            .Select(d => new IoTDeviceResponseDto
            {
                Id = d.Id,
                TenantId = d.TenantId,
                Name = d.Name,
                Type = d.Type,
                Active = d.Active,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IoTDeviceResponseDto> CreateDevice(int tenantId, IoTDeviceCreateDto dto)
    {
        // Gera API Key única
        var rawKey = $"sk_{Guid.NewGuid():N}{Guid.NewGuid():N}";
        var hashedKey = BCrypt.Net.BCrypt.HashPassword(rawKey);

        var device = new IoTDevice
        {
            TenantId = tenantId,
            Name = dto.Name,
            Type = dto.Type,
            ApiKeyHash = hashedKey,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.IoTDevices.Add(device);
        await _context.SaveChangesAsync();

        return new IoTDeviceResponseDto
        {
            Id = device.Id,
            TenantId = device.TenantId,
            Name = device.Name,
            Type = device.Type,
            Active = device.Active,
            CreatedAt = device.CreatedAt,
            ApiKey = rawKey // retorna apenas na criação!
        };
    }

    public async Task<(bool Success, bool? Active)> ToggleDevice(int deviceId, int tenantId)
    {
        var device = await _context.IoTDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.TenantId == tenantId);
        if (device is null) return (false, null);

        device.Active = !device.Active;
        await _context.SaveChangesAsync();

        return (true, device.Active);
    }

    public async Task<(bool Success, string? Error)> DeleteDevice(int deviceId, int tenantId)
    {
        var device = await _context.IoTDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.TenantId == tenantId);
        if (device is null) return (false, "Dispositivo não encontrado.");

        var hasReadings = await _context.IoTReadings.AnyAsync(r => r.DeviceId == deviceId);
        if (hasReadings)
            return (false, "Este dispositivo possui leituras. Desative-o em vez de excluir.");

        _context.IoTDevices.Remove(device);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    // Leituras
    public async Task<List<IoTReadingResponseDto>> GetReadings(int tenantId, int? deviceId, string? type, int hours = 24)
    {
        var query = _context.IoTReadings
            .Include(r => r.Device)
            .Where(r => r.TenantId == tenantId &&
                        r.CreatedAt >= DateTime.UtcNow.AddHours(-hours))
            .AsQueryable();

        if (deviceId.HasValue)
            query = query.Where(r => r.DeviceId == deviceId);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(r => r.Type == type);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new IoTReadingResponseDto
            {
                Id = r.Id,
                DeviceId = r.DeviceId,
                DeviceName = r.Device.Name,
                Type = r.Type,
                Data = r.Data,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IoTReadingResponseDto?> CreateReading(int deviceId, int tenantId, IoTReadingCreateDto dto)
    {
        var device = await _context.IoTDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.TenantId == tenantId && d.Active);
        if (device is null) return null;

        var reading = new IoTReading
        {
            DeviceId = deviceId,
            TenantId = tenantId,
            Type = dto.Type,
            Data = JsonSerializer.Serialize(dto.Data),
            CreatedAt = DateTime.UtcNow
        };

        _context.IoTReadings.Add(reading);
        await _context.SaveChangesAsync();

        return new IoTReadingResponseDto
        {
            Id = reading.Id,
            DeviceId = reading.DeviceId,
            DeviceName = device.Name,
            Type = reading.Type,
            Data = reading.Data,
            CreatedAt = reading.CreatedAt
        };
    }

    // Seed de dados mockados
    public async Task SeedMockReadings(int deviceId, int tenantId)
    {
        var device = await _context.IoTDevices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.TenantId == tenantId);
        if (device is null) return;

        var faker = new Faker();
        var readings = new List<IoTReading>();

        for (int i = 0; i < 100; i++)
        {
            var createdAt = DateTime.UtcNow.AddHours(-24).AddMinutes(i * 15);

            var data = device.Type switch
            {
                "dht22" => JsonSerializer.Serialize(new
                {
                    temperature = Math.Round(faker.Random.Double(18, 35), 1),
                    humidity = Math.Round(faker.Random.Double(30, 90), 1)
                }),
                "hc_sr04" => JsonSerializer.Serialize(new
                {
                    distance = Math.Round(faker.Random.Double(2, 400), 1)
                }),
                _ => JsonSerializer.Serialize(new { value = Math.Round(faker.Random.Double(0, 100), 1) })
            };

            readings.Add(new IoTReading
            {
                DeviceId = deviceId,
                TenantId = tenantId,
                Type = device.Type,
                Data = data,
                CreatedAt = createdAt
            });
        }

        _context.IoTReadings.AddRange(readings);
        await _context.SaveChangesAsync();
    }

    public async Task ClearReadings(int deviceId, int tenantId)
    {
        var readings = await _context.IoTReadings
            .Where(r => r.DeviceId == deviceId && r.TenantId == tenantId)
            .ToListAsync();

        _context.IoTReadings.RemoveRange(readings);
        await _context.SaveChangesAsync();
    }

    // Validação de API Key
    public async Task<IoTDevice?> ValidateApiKey(string apiKey)
    {
        var devices = await _context.IoTDevices
            .Where(d => d.Active)
            .ToListAsync();

        return devices.FirstOrDefault(d => BCrypt.Net.BCrypt.Verify(apiKey, d.ApiKeyHash));
    }
}