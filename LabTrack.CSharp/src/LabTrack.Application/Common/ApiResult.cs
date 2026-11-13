namespace LabTrack.Application.Common;

public class ApiResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public IDictionary<string, string[]>? ValidationErrors { get; }

    protected ApiResult(
        bool isSuccess,
        T? value,
        string? errorCode,
        string? errorMessage,
        IDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
    }

    public static ApiResult<T> Success(T value) =>
        new(true, value, null, null);

    public static ApiResult<T> Failure(string errorCode, string errorMessage) =>
        new(false, default, errorCode, errorMessage);

    public static ApiResult<T> ValidationFailure(IDictionary<string, string[]> errors) =>
        new(false, default, "VALIDATION_FAILED", "One or more validation failures occurred.", errors);
}

public class ApiResult : ApiResult<object>
{
    private ApiResult(
        bool isSuccess,
        string? errorCode,
        string? errorMessage,
        IDictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, null, errorCode, errorMessage, validationErrors)
    {
    }

    public static ApiResult Success() =>
        new(true, null, null);

    public static new ApiResult Failure(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage);

    public static new ApiResult ValidationFailure(IDictionary<string, string[]> errors) =>
        new(false, "VALIDATION_FAILED", "One or more validation failures occurred.", errors);
}