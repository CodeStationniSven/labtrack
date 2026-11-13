namespace LabTrack.Api.Contracts;

public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Details = null
)
{
    public static ApiError Create(string code, string message, IReadOnlyDictionary<string, string[]>? details = null)
        => new(code, message, details);
}