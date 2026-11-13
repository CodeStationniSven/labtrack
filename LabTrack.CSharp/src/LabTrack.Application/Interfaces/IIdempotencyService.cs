namespace LabTrack.Application.Interfaces;

public interface IIdempotencyService
{
    Task<bool> RequestExistsAsync(
        string key, 
        CancellationToken cancellationToken = default);

    Task<TResponse?> GetResponseAsync<TResponse>(
        string key, 
        CancellationToken cancellationToken = default);

    Task SaveRequestAsync<TResponse>(
        string key, 
        TResponse response, 
        TimeSpan? expiry = null, 
        CancellationToken cancellationToken = default);
}