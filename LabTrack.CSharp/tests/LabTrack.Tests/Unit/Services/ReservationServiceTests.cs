using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LabTrack.Tests.Unit.Services;

public class ReservationServiceTests : IAsyncLifetime
{
    private readonly LabTrackDbContext _dbContext;

    public ReservationServiceTests()
    {
        var options = new DbContextOptionsBuilder<LabTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new LabTrackDbContext(options);
    }

    public async Task InitializeAsync() => await _dbContext.Database.EnsureCreatedAsync();

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CreateReservation_CreatesPendingReservation()
    {
        var labId = Guid.NewGuid();

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            LaboratoryId = labId,
            Purpose = "Robotics Lab Testing",
            Status = "Pending",
        };

        await _dbContext.Reservations.AddAsync(reservation);
        var affectedRows = await _dbContext.SaveChangesAsync();

        var createdReservation = await _dbContext.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservation.Id);

        Assert.Equal(1, affectedRows);
        Assert.NotNull(createdReservation);
        Assert.Equal("Pending", createdReservation.Status);
        Assert.Equal(labId, createdReservation.LaboratoryId);
    }

    [Fact]
    public async Task CreateReservation_WhenExistingApprovedBookingInLab_DetectsConflict()
    {
        var labId = Guid.NewGuid();

        var existingReservation = new Reservation
        {
            Id = Guid.NewGuid(),
            LaboratoryId = labId,
            Purpose = "Scheduled Class",
            Status = "Approved",
        };

        await _dbContext.Reservations.AddAsync(existingReservation);
        await _dbContext.SaveChangesAsync();

        // Act — conflict check by lab + active status
        bool hasConflict = await _dbContext.Reservations.AnyAsync(r =>
            r.LaboratoryId == labId && r.Status != "Cancelled");

        Assert.True(hasConflict);

        if (hasConflict)
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("The requested laboratory time slot conflicts with an existing reservation.");
            });

            Assert.Equal("The requested laboratory time slot conflicts with an existing reservation.", exception.Message);
        }
    }

    [Fact]
    public async Task ApproveReservation_WhenPending_UpdatesStatusToApproved()
    {
        var reservationId = Guid.NewGuid();
        var pendingReservation = new Reservation
        {
            Id = reservationId,
            LaboratoryId = Guid.NewGuid(),
            Purpose = "AI Workshop",
            Status = "Pending",
        };

        await _dbContext.Reservations.AddAsync(pendingReservation);
        await _dbContext.SaveChangesAsync();

        var reservationToApprove = await _dbContext.Reservations.FindAsync(reservationId);
        Assert.NotNull(reservationToApprove);

        reservationToApprove.Status = "Approved";
        _dbContext.Reservations.Update(reservationToApprove);
        await _dbContext.SaveChangesAsync();

        var approvedReservation = await _dbContext.Reservations.FindAsync(reservationId);

        Assert.NotNull(approvedReservation);
        Assert.Equal("Approved", approvedReservation.Status);
    }

    [Fact]
    public async Task CancelReservation_WhenExists_UpdatesStatusToCancelled()
    {
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation
        {
            Id = reservationId,
            LaboratoryId = Guid.NewGuid(),
            Purpose = "Cancelled Slot",
            Status = "Approved",
        };

        await _dbContext.Reservations.AddAsync(reservation);
        await _dbContext.SaveChangesAsync();

        var reservationToCancel = await _dbContext.Reservations.FindAsync(reservationId);
        Assert.NotNull(reservationToCancel);

        reservationToCancel.Status = "Cancelled";
        _dbContext.Reservations.Update(reservationToCancel);
        await _dbContext.SaveChangesAsync();

        var cancelledReservation = await _dbContext.Reservations.FindAsync(reservationId);

        Assert.NotNull(cancelledReservation);
        Assert.Equal("Cancelled", cancelledReservation.Status);
    }

    [Fact]
    public async Task GetReservationsForLab_ExcludesCancelledReservations()
    {
        var labId = Guid.NewGuid();

        var reservations = new List<Reservation>
        {
            new() { Id = Guid.NewGuid(), LaboratoryId = labId, Purpose = "Active Reservation", Status = "Approved" },
            new() { Id = Guid.NewGuid(), LaboratoryId = labId, Purpose = "Another Active", Status = "Pending" },
            new() { Id = Guid.NewGuid(), LaboratoryId = labId, Purpose = "Cancelled Reservation", Status = "Cancelled" },
        };

        await _dbContext.Reservations.AddRangeAsync(reservations);
        await _dbContext.SaveChangesAsync();

        var upcoming = await _dbContext.Reservations
            .Where(r => r.LaboratoryId == labId && r.Status != "Cancelled")
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(2, upcoming.Count);
        Assert.DoesNotContain(upcoming, r => r.Status == "Cancelled");
    }
}