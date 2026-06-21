namespace SiskyApi.DTOs;

public class PaginatedResponseDto<T>
{
    public List<T> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int LastPage { get; set; }
}