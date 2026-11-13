namespace LabTrack.Api.Contracts;

public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data) => new()
    {
        Success = true,
        Data = data,
        Error = null
    };

    public static ApiResponse<T> Fail(string code, string message, IReadOnlyDictionary<string, string[]>? details = null) => new()
    {
        Success = false,
        Data = default,
        Error = new ApiError(code, message, details)
    };

    public static ApiResponse<T> Fail(ApiError error) => new()
    {
        Success = false,
        Data = default,
        Error = error
    };
}