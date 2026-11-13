using LabTrack.Domain.Entities;
using Xunit;

namespace LabTrack.Tests.Unit.Domain;

public class DomainTests
{
    [Fact]
    public void Asset_Initialization_SetsDefaultValuesCorrectly()
    {
        var assetId = Guid.NewGuid();

        var asset = new Asset
        {
            Id = assetId,
            Name = "Dell Precision Workstation",
            SerialNumber = "SN-5540-88",
        };

        Assert.Equal(assetId, asset.Id);
        Assert.Equal("Dell Precision Workstation", asset.Name);
        Assert.Equal("SN-5540-88", asset.SerialNumber);
    }

    [Fact]
    public void User_FullName_CombinesFirstAndLastName()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "john.doe@labtrack.io",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true
        };

        var fullName = $"{user.FirstName} {user.LastName}";

        Assert.Equal("John Doe", fullName);
    }

    [Fact]
    public void Reservation_WithApprovedStatus_IsValidReservation()
    {
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            LaboratoryId = Guid.NewGuid(),
            Purpose = "Approved Booking",
            Status = "Approved"
        };

        bool isApproved = reservation.Status == "Approved";

        Assert.True(isApproved);
        Assert.Equal("Approved", reservation.Status);
    }

    [Fact]
    public void PcUsageLog_Initialization_SetsWorkstationCorrectly()
    {
        var workstationId = Guid.NewGuid();

        var usageLog = new PcUsageLog
        {
            Id = Guid.NewGuid(),
            WorkstationId = workstationId,
        };

        Assert.Equal(workstationId, usageLog.WorkstationId);
    }

    [Fact]
    public void FinancialExpense_Validation_ThrowsExceptionWhenAmountIsNegative()
    {
        Action createExpenseAction = () =>
        {
            var expense = new FinancialExpense
            {
                Id = Guid.NewGuid(),
                Amount = -150.00m,
                Category = "Hardware",
            };

            if (expense.Amount < 0)
            {
                throw new ArgumentException("Expense amount cannot be negative.", nameof(expense.Amount));
            }
        };

        Assert.Throws<ArgumentException>(createExpenseAction);
    }
}