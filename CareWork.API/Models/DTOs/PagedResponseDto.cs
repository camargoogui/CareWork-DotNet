namespace CareWork.API.Models.DTOs;

public class PagedResponseDto<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    public LinksDto Links { get; set; } = new();
}

public class LinksDto
{
    public string? Self { get; set; }
    public string? First { get; set; }
    public string? Last { get; set; }
    public string? Previous { get; set; }
    public string? Next { get; set; }
}

