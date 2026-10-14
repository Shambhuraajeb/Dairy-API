using System.Text.Json;
using Dairy.Api.Extensions;
using Dairy.Application.Common;
using MySqlConnector;

namespace Dairy.Api.Middleware;

/// <summary>Maps exceptions to ApiResponse bodies. Business/DB-rule errors are 4xx; everything else is a generic 500.</summary>
public class ExceptionHandlingMiddleware
{
    private const string GenericError = "An unexpected error occurred. Please try again or contact support.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499; // client closed request
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Unhandled exception after the response started. TraceId {TraceId}", context.TraceIdentifier);
                throw;
            }

            var (status, message, errors) = Map(ex);

            if (status >= 500)
                _logger.LogError(ex, "Unhandled exception. TraceId {TraceId} {Method} {Path}", context.TraceIdentifier, context.Request.Method, context.Request.Path);
            else
                _logger.LogInformation("Request failed with {Status}: {Message} ({ExceptionType})", status, message, ex.GetType().Name);

            await ApiResponseWriter.WriteFailureAsync(context, status, message, errors);
        }
    }

    private static (int Status, string Message, IEnumerable<ApiError>? Errors) Map(Exception ex)
    {
        switch (ex)
        {
            case RequestValidationException v:
                return (StatusCodes.Status400BadRequest, v.Message, v.Errors);
            case BusinessRuleException:
                return (StatusCodes.Status422UnprocessableEntity, ex.Message, null);
            case NotFoundException:
                return (StatusCodes.Status404NotFound, ex.Message, null);
            case ForbiddenException:
                return (StatusCodes.Status403Forbidden, ex.Message, null);
            case UnauthorizedException:
                return (StatusCodes.Status401Unauthorized, ex.Message, null);
            case ConflictException:
                return (StatusCodes.Status409Conflict, ex.Message, null);
            case BadHttpRequestException b:
                return (b.StatusCode, "The request is invalid.", null);
            case JsonException:
                return (StatusCodes.Status400BadRequest, "The request body is not valid JSON.", null);
        }

        var mysql = FindMySqlException(ex);
        if (mysql is not null)
        {
            if (mysql.SqlState == "45000")
                return (StatusCodes.Status422UnprocessableEntity, mysql.Message, null);

            switch ((int)mysql.ErrorCode)
            {
                case 1062:
                    return (StatusCodes.Status409Conflict, "Duplicate value. A record with the same unique value already exists.", null);
                case 1451:
                    return (StatusCodes.Status409Conflict, "This record is in use by other records and cannot be deleted or changed.", null);
                case 1452:
                    return (StatusCodes.Status400BadRequest, "A referenced record does not exist or is invalid.", null);
            }
        }

        return (StatusCodes.Status500InternalServerError, GenericError, null);
    }

    private static MySqlException? FindMySqlException(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is MySqlException m) return m;
            ex = ex.InnerException;
        }
        return null;
    }
}
