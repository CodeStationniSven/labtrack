using LabTrack.Domain.Common;

namespace LabTrack.Domain.Entities;

public class Laboratory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public virtual ICollection<Workstation> Workstations { get; set; } = new List<Workstation>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public virtual ICollection<InstructorCommunication> InstructorCommunications { get; set; } = new List<InstructorCommunication>();
    public virtual ICollection<FinancialExpense> FinancialExpenses { get; set; } = new List<FinancialExpense>();
}