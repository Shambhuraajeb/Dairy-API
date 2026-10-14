using Dairy.Application.BusinessRules;
using Dairy.Application.Requests.Orders;
using FluentValidation;

namespace Dairy.Application.Validators.Orders;

public class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
{
    public OrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Product is required.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m).When(x => x.UnitPrice.HasValue);
    }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.DeliveryDate).NotEmpty().WithMessage("Delivery date is required.")
            .Must(d => d.Date >= DateTime.Today).WithMessage("Delivery date cannot be in the past.");
        RuleFor(x => x.DeliverySlot).NotEmpty().MustBeOneOf(SalesValues.Slots);
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.Items).NotEmpty().WithMessage("An order requires at least one item.");
        RuleForEach(x => x.Items).SetValidator(new OrderItemRequestValidator());
    }
}

public class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerAddressId).GreaterThan(0).When(x => x.CustomerAddressId.HasValue);
        RuleFor(x => x.DeliveryDate).Must(d => d!.Value.Date >= DateTime.Today)
            .When(x => x.DeliveryDate.HasValue).WithMessage("Delivery date cannot be in the past.");
        RuleFor(x => x.DeliverySlot).MustBeOneOf(SalesValues.Slots);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(255);
    }
}
