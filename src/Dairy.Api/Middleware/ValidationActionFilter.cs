using Dairy.Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dairy.Api.Middleware;

/// <summary>Validates every action argument that has a registered IValidator&lt;T&gt; and returns a 400 ApiResponse on failure.</summary>
public class ValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new List<ApiError>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var type = argument.GetType();
            if (type.IsPrimitive || type.IsEnum || argument is string || argument is decimal || argument is DateTime || argument is Guid)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(type);
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (result.IsValid) continue;

            foreach (var failure in result.Errors)
                errors.Add(new ApiError(FieldNames.ToCamelCase(failure.PropertyName), failure.ErrorMessage));
        }

        if (errors.Count > 0)
        {
            context.Result = new ObjectResult(ApiResponse<object?>.Fail("Validation failed.", errors))
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            return;
        }

        await next();
    }
}
