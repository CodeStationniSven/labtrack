using System.Data;
using LabTrack.Application.Interfaces;
using LabTrack.Application.Interfaces.Repositories;
using LabTrack.Domain.Entities;
using LabTrack.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LabTrack.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly LabTrackDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    private IAiChatRepository? _aiChats;
    private IAssetRepository? _assets;
    private IAuditRepository? _audits;
    private ICommunicationRepository? _communications;
    private IFinancialRepository? _financials;
    private ILaboratoryRepository? _laboratories;
    private IMaintenanceRepository? _maintenances;
    private INetworkRepository? _networks;
    private INotificationRepository? _notifications;
    private IPcUsageRepository? _pcUsages;
    private IAuthSessionRepository? _authSessions;
    private IProblemReportRepository? _problemReports;
    private IQrRepository? _qrs;
    private IRepairTicketRepository? _repairTickets;
    private IReplacementRepository? _replacements;
    private IReservationRepository? _reservations;
    private IUserRepository? _users;

    public UnitOfWork(LabTrackDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IAiChatRepository AiChats => _aiChats ??= new AiChatRepository(_context);
    public IAssetRepository Assets => _assets ??= new AssetRepository(_context);
    public IAuditRepository Audits => _audits ??= new AuditRepository(_context);
    public ICommunicationRepository Communications => _communications ??= new CommunicationRepository(_context);
    public IFinancialRepository Financials => _financials ??= new FinancialRepository(_context);
    public ILaboratoryRepository Laboratories => _laboratories ??= new LaboratoryRepository(_context);
    public IMaintenanceRepository Maintenances => _maintenances ??= new MaintenanceRepository(_context);
    public INetworkRepository Networks => _networks ??= new NetworkRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);
    public IPcUsageRepository PcUsages => _pcUsages ??= new PcUsageRepository(_context);
    public IAuthSessionRepository AuthSessions => _authSessions ??= new AuthSessionRepository(_context);
    public IProblemReportRepository ProblemReports => _problemReports ??= new ProblemReportRepository(_context);
    public IQrRepository Qrs => _qrs ??= new QrRepository(_context);
    public IRepairTicketRepository RepairTickets => _repairTickets ??= new RepairTicketRepository(_context);
    public IReplacementRepository Replacements => _replacements ??= new ReplacementRepository(_context);
    public IReservationRepository Reservations => _reservations ??= new ReservationRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return _currentTransaction;
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return _currentTransaction;
    }

    public async Task BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            if (_currentTransaction != null)
                await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction != null)
                await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _currentTransaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_currentTransaction != null)
            await _currentTransaction.DisposeAsync();
        await _context.DisposeAsync();
    }
}


