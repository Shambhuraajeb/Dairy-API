namespace Dairy.Application.DTOs.Products;

public class ProductCategoryDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class ProductDto
{
    public long Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long ProductCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public long UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TaxPercent { get; set; }
    public string? HsnCode { get; set; }
    public bool IsMilkBased { get; set; }
    public decimal? MilkLitersPerUnit { get; set; }
    public decimal DefaultPrice { get; set; }

    /// <summary>Effective Retail price today (price list, else DefaultPrice).</summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>Effective Distributor price today (price list, else DefaultPrice).</summary>
    public decimal DistributorPrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public int? ShelfLifeDays { get; set; }
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; }
}

public class ProductPriceDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Retail | Distributor.</summary>
    public string CustomerType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ProductPriceLookupDto
{
    public long ProductId { get; set; }
    public string CustomerType { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Price { get; set; }

    /// <summary>PriceList | Default.</summary>
    public string Source { get; set; } = string.Empty;
    public decimal TaxPercent { get; set; }
}
