using Dairy.Application.BusinessRules;
using Dairy.Application.Requests.Products;
using FluentValidation;

namespace Dairy.Application.Validators.Products;

public class CreateProductCategoryRequestValidator : AbstractValidator<CreateProductCategoryRequest>
{
    public CreateProductCategoryRequestValidator()
    {
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public class UpdateProductCategoryRequestValidator : AbstractValidator<UpdateProductCategoryRequest>
{
    public UpdateProductCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.ProductCode).MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.ProductCategoryId).GreaterThan(0).WithMessage("Category is required.");
        RuleFor(x => x.UnitId).GreaterThan(0).WithMessage("Unit is required.");
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.TaxPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.HsnCode).MaximumLength(20);
        RuleFor(x => x.MilkLitersPerUnit).GreaterThan(0m).When(x => x.MilkLitersPerUnit.HasValue);
        RuleFor(x => x.DefaultPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrentStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ShelfLifeDays).GreaterThan(0).When(x => x.ShelfLifeDays.HasValue);
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.ProductCode).NotEmpty().WithMessage("Product code is required.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(150);
        RuleFor(x => x.ProductCategoryId).GreaterThan(0).WithMessage("Category is required.");
        RuleFor(x => x.UnitId).GreaterThan(0).WithMessage("Unit is required.");
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.TaxPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.HsnCode).MaximumLength(20);
        RuleFor(x => x.MilkLitersPerUnit).GreaterThan(0m).When(x => x.MilkLitersPerUnit.HasValue);
        RuleFor(x => x.DefaultPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrentStock).GreaterThanOrEqualTo(0m).When(x => x.CurrentStock.HasValue);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ShelfLifeDays).GreaterThan(0).When(x => x.ShelfLifeDays.HasValue);
    }
}

public class CreateProductPriceRequestValidator : AbstractValidator<CreateProductPriceRequest>
{
    public CreateProductPriceRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Product is required.");
        RuleFor(x => x.CustomerType).NotEmpty().MustBeOneOf(SalesValues.PriceCustomerTypes);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EffectiveFrom).NotEmpty().WithMessage("Effective-from date is required.");
        RuleFor(x => x.EffectiveTo).GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue).WithMessage("Effective-to date cannot be before the effective-from date.");
    }
}
