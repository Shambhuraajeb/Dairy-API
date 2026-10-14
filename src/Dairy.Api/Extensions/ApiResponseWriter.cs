using System.Text.Json;
using System.Text.Json.Serialization;
using Dairy.Application.Common;

namespace Dairy.Api.Extensions;

/// <summary>Writes an ApiResponse failure body outside of MVC (middleware, auth events).</summary>
public static class ApiResponseWriter
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task WriteFailureAsync(HttpContext context, int statusCode, string message, IEnumerable<ApiError>? errors = null)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = ApiResponse<object?>.Fail(message, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
