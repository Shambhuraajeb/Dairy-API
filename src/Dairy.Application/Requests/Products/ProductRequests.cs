using Dairy.Application.Common;

namespace Dairy.Application.Requests.Products;

public class ProductCategoryQuery : PagedQuery
{
    public bool? IsActive { get; set; }
}

public class CreateProductCategoryRequest
{
    /// <summary>Optional; auto-generated (PCAT001 ...) when blank.</summary>
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateProductCategoryRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Null = unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

public class ProductQuery : PagedQuery
{
    public long? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsMilkBased { get; set; }

    /// <summary>true = only products at or below their reorder level.</summary>
    public bool? LowStock { get; set; }
}

public class CreateProductRequest
{
    /// <summary>Optional; auto-generated (PRD0001 ...) when blank.</summary>
    public string? ProductCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public long ProductCategoryId { get; set; }
    public long UnitId { get; set; }
    public string? Description { get; set; }
    public decimal TaxPercent { get; set; }
    public string? HsnCode { get; set; }
    public bool IsMilkBased { get; set; }
    public decimal? MilkLitersPerUnit { get; set; }
    public decimal DefaultPrice { get; set; }

    /// <summary>Opening stock for stock-tracked (non-milk) products.</summary>
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public int? ShelfLifeDays { get; set; }
}

public class UpdateProductRequest
{
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long ProductCategoryId { get; set; }
    public long UnitId { get; set; }
    public string? Description { get; set; }
    public decimal TaxPercent { get; set; }
    public string? HsnCode { get; set; }
    public bool IsMilkBased { get; set; }
    public decimal? MilkLitersPerUnit { get; set; }
    public decimal DefaultPrice { get; set; }

    /// <summary>Null = unchanged; set to correct the finished-goods stock level.</summary>
    public decimal? CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public int? ShelfLifeDays { get; set; }

    /// <summary>Null = unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

public class ProductPriceQuery : PagedQuery
{
    public long? ProductId { get; set; }

    /// <summary>Retail | Distributor.</summary>
    public string? CustomerType { get; set; }

    /// <summary>true = only prices effective today.</summary>
    public bool? CurrentOnly { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateProductPriceRequest
{
    public long ProductId { get; set; }

    /// <summary>Retail | Distributor.</summary>
    public string CustomerType { get; set; } = "Retail";
    public decimal Price { get; set; }
    public DateTime EffectiveFrom { get; set; }

    /// <summary>Null = open ended.</summary>
    public DateTime? EffectiveTo { get; set; }
}
