using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class User : AuditableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Pin { get; set; }
    public string Role { get; set; } = string.Empty; // e.g., Admin, Instructor, Student, Technician
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginUtc { get; set; }

    // Navigation properties
    public virtual ICollection<AuthSession> AuthSessions { get; set; } = new List<AuthSession>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public virtual ICollection<ProblemReport> ProblemReports { get; set; } = new List<ProblemReport>();
    public virtual ICollection<RepairTicket> ReportedRepairTickets { get; set; } = new List<RepairTicket>();
    public virtual ICollection<RepairTicket> AssignedRepairTickets { get; set; } = new List<RepairTicket>();
    public virtual ICollection<ReplacementRequest> RequestedReplacements { get; set; } = new List<ReplacementRequest>();
    public virtual ICollection<ReplacementRequest> ApprovedReplacements { get; set; } = new List<ReplacementRequest>();
    public virtual ICollection<InstructorCommunication> InstructorCommunications { get; set; } = new List<InstructorCommunication>();
    public virtual ICollection<ChainOfCustodyEvent> ReleasedTransfers { get; set; } = new List<ChainOfCustodyEvent>();
    public virtual ICollection<ChainOfCustodyEvent> ReceivedTransfers { get; set; } = new List<ChainOfCustodyEvent>();
    public virtual ICollection<DamageAttribution> ReportedDamages { get; set; } = new List<DamageAttribution>();
    public virtual ICollection<DamageAttribution> ResponsibleDamages { get; set; } = new List<DamageAttribution>();
    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
    public virtual ICollection<MaintenanceSchedule> AssignedMaintenanceSchedules { get; set; } = new List<MaintenanceSchedule>();
    public virtual ICollection<FinancialExpense> FinancialExpenses { get; set; } = new List<FinancialExpense>();
    public virtual ICollection<PcUsageLog> PcUsageLogs { get; set; } = new List<PcUsageLog>();
    public virtual ICollection<QrSession> QrSessions { get; set; } = new List<QrSession>();
}
