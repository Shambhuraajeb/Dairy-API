namespace Dairy.Application.Common;

public record ApiError(string Field, string Message);

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<ApiError> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T? data, string? message = null) =>
        new() { Success = true, Message = message ?? "OK", Data = data };

    public static ApiResponse<T> Fail(string message, IEnumerable<ApiError>? errors = null) =>
        new() { Success = false, Message = message, Data = default, Errors = errors?.ToList() ?? new List<ApiError>() };
}
