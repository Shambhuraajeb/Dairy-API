namespace Dairy.Application.Common;

/// <summary>Business rule violated -> HTTP 422.</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>Entity not found -> HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entity, object? id) : base($"{entity} with id '{id}' was not found.") { }
}

/// <summary>Authenticated but not allowed (e.g. ownership check) -> HTTP 403.</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to perform this action.") : base(message) { }
}

/// <summary>Authentication failed -> HTTP 401.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Authentication is required.") : base(message) { }
}

/// <summary>Conflict with current state (e.g. duplicate code) -> HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Request validation failed -> HTTP 400 with field errors.</summary>
public class RequestValidationException : Exception
{
    public RequestValidationException(IEnumerable<ApiError> errors) : base("Validation failed.")
    {
        Errors = errors.ToList();
    }

    public RequestValidationException(string field, string message) : this(new[] { new ApiError(field, message) }) { }

    public IReadOnlyList<ApiError> Errors { get; }
}
