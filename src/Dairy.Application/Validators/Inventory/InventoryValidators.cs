using Dairy.Application.Requests.Inventory;
using FluentValidation;

namespace Dairy.Application.Validators.Inventory;

public class CreateItemCategoryRequestValidator : AbstractValidator<CreateItemCategoryRequest>
{
    public CreateItemCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public class UpdateItemCategoryRequestValidator : AbstractValidator<UpdateItemCategoryRequest>
{
    public UpdateItemCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public abstract class InventoryItemRequestRules<T> : AbstractValidator<T> where T : CreateInventoryItemRequest
{
    protected InventoryItemRequestRules()
    {
        RuleFor(x => x.ItemCode).NotEmpty().WithMessage("Item code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.ItemCategoryId).GreaterThan(0).WithMessage("Category is required.");
        RuleFor(x => x.UnitId).GreaterThan(0).WithMessage("Unit is required.");
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0).WithMessage("Reorder level cannot be negative.");
        RuleFor(x => x.MaxStockLevel).GreaterThanOrEqualTo(0).When(x => x.MaxStockLevel.HasValue)
            .WithMessage("Max stock level cannot be negative.");
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).WithMessage("Unit cost cannot be negative.");
        RuleFor(x => x.TaxPercent).InclusiveBetween(0m, 100m).WithMessage("Tax percent must be between 0 and 100.");
        RuleFor(x => x.ShelfLifeDays).GreaterThan(0).When(x => x.ShelfLifeDays.HasValue)
            .WithMessage("Shelf life must be greater than zero.");
        RuleFor(x => x.PreferredSupplierId).GreaterThan(0).When(x => x.PreferredSupplierId.HasValue);
        RuleFor(x => x.StorageLocation).MaximumLength(100);
        RuleFor(x => x.Barcode).MaximumLength(50);
    }
}

public class CreateInventoryItemRequestValidator : InventoryItemRequestRules<CreateInventoryItemRequest> { }

public class UpdateInventoryItemRequestValidator : InventoryItemRequestRules<UpdateInventoryItemRequest> { }

public class RecordConsumptionRequestValidator : AbstractValidator<RecordConsumptionRequest>
{
    public RecordConsumptionRequestValidator()
    {
        RuleFor(x => x.InventoryItemId).GreaterThan(0).WithMessage("Item is required.");
        RuleFor(x => x.InventoryBatchId).GreaterThan(0).When(x => x.InventoryBatchId.HasValue);
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.Purpose).MaximumLength(150);
        RuleFor(x => x.Purpose).NotEmpty().When(x => x.ExpiredOverride)
            .WithMessage("A purpose/reason is required when overriding expired stock.");
    }
}

public class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.InventoryItemId).GreaterThan(0).WithMessage("Item is required.");
        RuleFor(x => x.InventoryBatchId).GreaterThan(0).When(x => x.InventoryBatchId.HasValue);
        RuleFor(x => x.QuantityDelta).NotEqual(0).WithMessage("Adjustment quantity must be non-zero.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required for stock adjustments.").MaximumLength(255);
    }
}

public class PurchaseLineRequestValidator : AbstractValidator<PurchaseLineRequest>
{
    public PurchaseLineRequestValidator()
    {
        RuleFor(x => x.InventoryItemId).GreaterThan(0).WithMessage("Item is required.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).When(x => x.UnitCost.HasValue)
            .WithMessage("Unit cost cannot be negative.");
        RuleFor(x => x.BatchNumber).MaximumLength(50);
        RuleFor(x => x.ExpiryDate).Must((x, expiry) => expiry!.Value >= x.ManufactureDate!.Value)
            .When(x => x.ExpiryDate.HasValue && x.ManufactureDate.HasValue)
            .WithMessage("Expiry date cannot be before the manufacture date.");
    }
}

public class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Supplier is required.");
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty().WithMessage("A purchase requires at least one item.");
        RuleForEach(x => x.Items).SetValidator(new PurchaseLineRequestValidator());
    }
}

public class CancelPurchaseRequestValidator : AbstractValidator<CancelPurchaseRequest>
{
    public CancelPurchaseRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A cancellation reason is required.").MaximumLength(255);
    }
}
