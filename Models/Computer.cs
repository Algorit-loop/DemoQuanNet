namespace QuanNet.Models;

public enum ComputerStatus
{
    Available,
    InUse,
    Maintenance,
    OutOfOrder
}

public class Computer
{
    public int ComputerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ComputerStatus Status { get; set; }
    public decimal HourlyRate { get; set; }
    public virtual ICollection<ComputerUsage> ComputerUsages { get; set; } = new List<ComputerUsage>();
}