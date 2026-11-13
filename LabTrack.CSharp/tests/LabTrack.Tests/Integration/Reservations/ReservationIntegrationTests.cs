using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LabTrack.Tests.Integration.Reservations;

public class ReservationIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly LabTrackDbContext _dbContext;

    public ReservationIntegrationTests()
    {
        var provider = new ServiceCollection()
            .AddDbContext<LabTrackDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
            .BuildServiceProvider();

        _dbContext = provider.GetRequiredService<LabTrackDbContext>();
        _client = new HttpClient();
    }

    public async Task InitializeAsync() => await _dbContext.Database.EnsureCreatedAsync();

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task GetReservationById_WhenExists_ReturnsReservationWithDetails()
    {
        var reservationId = Guid.NewGuid();
        var labId = Guid.NewGuid();

        var reservation = new Reservation
        {
            Id = reservationId,
            LaboratoryId = labId,
            Purpose = "AI Workload Training Session",
            Status = "Approved",
        };

        await _dbContext.Reservations.AddAsync(reservation);
        await _dbContext.SaveChangesAsync();

        var fetchedReservation = await _dbContext.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        Assert.NotNull(fetchedReservation);
        Assert.Equal(reservationId, fetchedReservation.Id);
        Assert.Equal(labId, fetchedReservation.LaboratoryId);
        Assert.Equal("Approved", fetchedReservation.Status);
        Assert.Equal("AI Workload Training Session", fetchedReservation.Purpose);
    }

    [Fact]
    public async Task CreateReservation_WithValidPayload_PersistsReservationInDatabase()
    {
        var labId = Guid.NewGuid();

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            LaboratoryId = labId,
            Purpose = "Robotics Team Practice",
            Status = "Pending",
        };

        await _dbContext.Reservations.AddAsync(reservation);
        var affectedRows = await _dbContext.SaveChangesAsync();

        var persistedReservation = await _dbContext.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservation.Id);

        Assert.Equal(1, affectedRows);
        Assert.NotNull(persistedReservation);
        Assert.Equal("Pending", persistedReservation.Status);
        Assert.Equal(labId, persistedReservation.LaboratoryId);
    }

    [Fact]
    public async Task UpdateReservationStatus_WhenValid_UpdatesStatusCorrectly()
    {
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation
        {
            Id = reservationId,
            LaboratoryId = Guid.NewGuid(),
            Purpose = "Network Testing",
            Status = "Pending",
        };

        await _dbContext.Reservations.AddAsync(reservation);
        await _dbContext.SaveChangesAsync();

        reservation.Status = "Approved";
        _dbContext.Reservations.Update(reservation);
        await _dbContext.SaveChangesAsync();

        var updatedReservation = await _dbContext.Reservations.FindAsync(reservationId);

        Assert.NotNull(updatedReservation);
        Assert.Equal("Approved", updatedReservation.Status);
    }

    [Fact]
    public async Task GetReservationsByLaboratory_FiltersByLabIdCorrectly()
    {
        var targetLabId = Guid.NewGuid();
        var otherLabId = Guid.NewGuid();

        var reservations = new List<Reservation>
        {
            new() { Id = Guid.NewGuid(), LaboratoryId = targetLabId, Purpose = "Session A", Status = "Approved" },
            new() { Id = Guid.NewGuid(), LaboratoryId = targetLabId, Purpose = "Session B", Status = "Pending" },
            new() { Id = Guid.NewGuid(), LaboratoryId = otherLabId,  Purpose = "Other Lab", Status = "Approved" },
        };

        await _dbContext.Reservations.AddRangeAsync(reservations);
        await _dbContext.SaveChangesAsync();

        var labReservations = await _dbContext.Reservations
            .Where(r => r.LaboratoryId == targetLabId)
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(2, labReservations.Count);
        Assert.All(labReservations, r => Assert.Equal(targetLabId, r.LaboratoryId));
    }

    [Fact]
    public async Task DeleteReservation_WhenExists_RemovesFromDatabase()
    {
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation
        {
            Id = reservationId,
            LaboratoryId = Guid.NewGuid(),
            Purpose = "Cancelled Slot",
            Status = "Cancelled",
        };

        await _dbContext.Reservations.AddAsync(reservation);
        await _dbContext.SaveChangesAsync();

        _dbContext.Reservations.Remove(reservation);
        await _dbContext.SaveChangesAsync();

        var deletedReservation = await _dbContext.Reservations.FindAsync(reservationId);

        Assert.Null(deletedReservation);
    }
}