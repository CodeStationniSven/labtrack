using System.Data;
using LabTrack.Application.Interfaces.Repositories;

namespace LabTrack.Application.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IUserRepository Users { get; }
    ILaboratoryRepository Laboratories { get; }
    IAssetRepository Assets { get; }
    INotificationRepository Notifications { get; }
    IReservationRepository Reservations { get; }
    IProblemReportRepository ProblemReports { get; }
    IRepairTicketRepository RepairTickets { get; }
    IMaintenanceRepository Maintenances { get; }
    IAuditRepository Audits { get; }
    ICommunicationRepository Communications { get; }
    IFinancialRepository Financials { get; }
    INetworkRepository Networks { get; }
    IQrRepository Qrs { get; }
    IReplacementRepository Replacements { get; }
    IPcUsageRepository PcUsages { get; }
    IAuthSessionRepository AuthSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
}
