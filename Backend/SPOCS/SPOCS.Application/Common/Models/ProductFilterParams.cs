namespace SPOCS.Application.Common.Models;

public class ProductFilterParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageIndex { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
    }

    public Guid? CategoryId { get; set; }
    public string? CategorySlug { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Supported: "price_asc", "price_desc", "name_asc", "name_desc", "newest"
    /// </summary>
    public string? SortBy { get; set; }
}
