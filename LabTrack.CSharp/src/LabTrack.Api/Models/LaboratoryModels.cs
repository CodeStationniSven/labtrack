using System.ComponentModel.DataAnnotations;

namespace LabTrack.Api.Models;

public class CreateLaboratoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = "Main Building";
    public int Capacity { get; set; } = 40;
}

public class LaboratoryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int WorkstationCount { get; set; }
}
