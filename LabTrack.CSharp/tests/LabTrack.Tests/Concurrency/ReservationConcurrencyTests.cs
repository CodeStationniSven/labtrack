using System.Collections.Concurrent;
using Xunit;

namespace LabTrack.Tests.Concurrency;

public class ReservationConcurrencyTests
{
    private class ReservationSlot
    {
        public Guid ReservationId { get; set; }
        public Guid AssetId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    private readonly ConcurrentBag<ReservationSlot> _existingReservations = new();
    private readonly object _lock = new();

    [Fact]
    public async Task ConcurrentReservations_OverlappingTimeSlot_ShouldAllowOnlyOne()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        int successfulBookings = 0;
        int failedBookings = 0;

        Task<bool> TryBookReservationAsync()
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    // Check for overlapping reservations for the specified asset
                    bool hasOverlap = _existingReservations.Any(r =>
                        r.AssetId == assetId &&
                        r.StartTime < endTime &&
                        r.EndTime > startTime);

                    if (hasOverlap)
                    {
                        Interlocked.Increment(ref failedBookings);
                        return false;
                    }

                    _existingReservations.Add(new ReservationSlot
                    {
                        ReservationId = Guid.NewGuid(),
                        AssetId = assetId,
                        StartTime = startTime,
                        EndTime = endTime
                    });

                    Interlocked.Increment(ref successfulBookings);
                    return true;
                }
            });
        }

        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ => TryBookReservationAsync()).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, successfulBookings);
        Assert.Equal(9, failedBookings);
        Assert.Single(_existingReservations);
    }

    [Fact]
    public async Task ConcurrentReservations_NonOverlappingTimeSlots_ShouldAllSucceed()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.AddDays(1);

        Task<bool> TryBookSlotAsync(int hourOffset)
        {
            return Task.Run(() =>
            {
                var start = baseTime.AddHours(hourOffset * 2);
                var end = start.AddHours(2);

                lock (_lock)
                {
                    bool hasOverlap = _existingReservations.Any(r =>
                        r.AssetId == assetId &&
                        r.StartTime < end &&
                        r.EndTime > start);

                    if (!hasOverlap)
                    {
                        _existingReservations.Add(new ReservationSlot
                        {
                            ReservationId = Guid.NewGuid(),
                            AssetId = assetId,
                            StartTime = start,
                            EndTime = end
                        });
                        return true;
                    }

                    return false;
                }
            });
        }

        // Act
        var tasks = Enumerable.Range(0, 5).Select(i => TryBookSlotAsync(i)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.True(results.All(r => r));
        Assert.Equal(5, _existingReservations.Count);
    }
}