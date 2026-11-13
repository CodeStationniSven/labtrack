namespace LabTrack.Application.Common;

public class ServiceResult<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? Code { get; }
    public string? Message { get; }
    public IDictionary<string, string[]>? ValidationErrors { get; }

    protected ServiceResult(
        bool isSuccess,
        T? data,
        string? code,
        string? message,
        IDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        Code = code;
        Message = message;
        ValidationErrors = validationErrors;
    }

    public static ServiceResult<T> Success(T data) =>
        new(true, data, null, null);

    public static ServiceResult<T> Failure(string code, string message) =>
        new(false, default, code, message);

    public static ServiceResult<T> Failure(string code, string message, IDictionary<string, string[]> validationErrors) =>
        new(false, default, code, message, validationErrors);

    public static ServiceResult<T> NotFound(string message = "The requested resource was not found.") =>
        new(false, default, ErrorCode.ResourceNotFound, message);

    public static ServiceResult<T> Unauthorized(string message = "Unauthorized access.") =>
        new(false, default, ErrorCode.UnauthorizedAccess, message);

    public static ServiceResult<T> Forbidden(string message = "Access to this resource is forbidden.") =>
        new(false, default, ErrorCode.Forbidden, message);

    public ServiceResult<TOut> Map<TOut>(Func<T, TOut> mapFunc)
    {
        if (!IsSuccess || Data is null)
        {
            return ServiceResult<TOut>.Failure(Code ?? ErrorCode.InternalServerError, Message ?? "An error occurred.", ValidationErrors!);
        }

        return ServiceResult<TOut>.Success(mapFunc(Data));
    }
}

public class ServiceResult : ServiceResult<object>
{
    private ServiceResult(
        bool isSuccess,
        string? code,
        string? message,
        IDictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, null, code, message, validationErrors)
    {
    }

    public static ServiceResult Success() =>
        new(true, null, null);

    public static new ServiceResult Failure(string code, string message) =>
        new(false, code, message);

    public static new ServiceResult Failure(string code, string message, IDictionary<string, string[]> validationErrors) =>
        new(false, code, message, validationErrors);

    public static new ServiceResult NotFound(string message = "The requested resource was not found.") =>
        new(false, ErrorCode.ResourceNotFound, message);

    public static new ServiceResult Unauthorized(string message = "Unauthorized access.") =>
        new(false, ErrorCode.UnauthorizedAccess, message);

    public static new ServiceResult Forbidden(string message = "Access to this resource is forbidden.") =>
        new(false, ErrorCode.Forbidden, message);
}