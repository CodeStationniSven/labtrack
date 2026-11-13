using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LabTrack.Tests.Integration.Repair;

public class RepairIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly LabTrackDbContext _dbContext;

    public RepairIntegrationTests()
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
    public async Task GetRepairTicketById_WhenExists_ReturnsTicketWithDetails()
    {
        var ticketId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var reportedByUserId = Guid.NewGuid();

        var ticket = new RepairTicket
        {
            Id = ticketId,
            AssetId = assetId,
            ReportedByUserId = reportedByUserId,
            Title = "GPU Artifacting during workload",
            Description = "Display outputs visual artifacts when running CUDA compute tasks.",
            Priority = "High",
            Status = "Open",
        };

        await _dbContext.RepairTickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        var fetchedTicket = await _dbContext.RepairTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        Assert.NotNull(fetchedTicket);
        Assert.Equal(ticketId, fetchedTicket.Id);
        Assert.Equal("GPU Artifacting during workload", fetchedTicket.Title);
        Assert.Equal("High", fetchedTicket.Priority);
        Assert.Equal("Open", fetchedTicket.Status);
    }

    [Fact]
    public async Task CreateRepairTicket_WithValidData_PersistsTicketInDatabase()
    {
        var assetId = Guid.NewGuid();
        var reportedByUserId = Guid.NewGuid();

        var newTicket = new RepairTicket
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            ReportedByUserId = reportedByUserId,
            Title = "Power Supply Failure",
            Description = "PC fails to turn on; power LED blinks repeatedly.",
            Priority = "Critical",
            Status = "Open",
        };

        await _dbContext.RepairTickets.AddAsync(newTicket);
        var affectedRows = await _dbContext.SaveChangesAsync();

        var persistedTicket = await _dbContext.RepairTickets
            .FirstOrDefaultAsync(t => t.Id == newTicket.Id);

        Assert.Equal(1, affectedRows);
        Assert.NotNull(persistedTicket);
        Assert.Equal("Critical", persistedTicket.Priority);
        Assert.Equal(assetId, persistedTicket.AssetId);
    }

    [Fact]
    public async Task AssignTechnician_UpdatesTicketStatus()
    {
        var ticketId = Guid.NewGuid();

        var ticket = new RepairTicket
        {
            Id = ticketId,
            AssetId = Guid.NewGuid(),
            ReportedByUserId = Guid.NewGuid(),
            Title = "Network Card Unresponsive",
            Priority = "Medium",
            Status = "Open",
        };

        await _dbContext.RepairTickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        ticket.Status = "InProgress";
        _dbContext.RepairTickets.Update(ticket);
        await _dbContext.SaveChangesAsync();

        var updatedTicket = await _dbContext.RepairTickets.FindAsync(ticketId);

        Assert.NotNull(updatedTicket);
        Assert.Equal("InProgress", updatedTicket.Status);
    }

    [Fact]
    public async Task GetOpenRepairTickets_FiltersByStatusCorrectly()
    {
        var assetId = Guid.NewGuid();

        var tickets = new List<RepairTicket>
        {
            new() { Id = Guid.NewGuid(), AssetId = assetId, ReportedByUserId = Guid.NewGuid(), Title = "Unresolved Issue 1", Priority = "High", Status = "Open" },
            new() { Id = Guid.NewGuid(), AssetId = assetId, ReportedByUserId = Guid.NewGuid(), Title = "In Progress Issue 2", Priority = "Low", Status = "InProgress" },
            new() { Id = Guid.NewGuid(), AssetId = assetId, ReportedByUserId = Guid.NewGuid(), Title = "Resolved Issue 3", Priority = "Medium", Status = "Resolved" },
        };

        await _dbContext.RepairTickets.AddRangeAsync(tickets);
        await _dbContext.SaveChangesAsync();

        var activeTickets = await _dbContext.RepairTickets
            .Where(t => t.Status == "Open" || t.Status == "InProgress")
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(2, activeTickets.Count);
        Assert.DoesNotContain(activeTickets, t => t.Status == "Resolved");
    }

    [Fact]
    public async Task DeleteRepairTicket_WhenExists_RemovesFromDatabase()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new RepairTicket
        {
            Id = ticketId,
            AssetId = Guid.NewGuid(),
            ReportedByUserId = Guid.NewGuid(),
            Title = "Duplicate Ticket",
            Priority = "Low",
            Status = "Cancelled",
        };

        await _dbContext.RepairTickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        _dbContext.RepairTickets.Remove(ticket);
        await _dbContext.SaveChangesAsync();

        var deletedTicket = await _dbContext.RepairTickets.FindAsync(ticketId);

        Assert.Null(deletedTicket);
    }
}