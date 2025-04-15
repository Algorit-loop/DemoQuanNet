namespace QuanNet.Models;

public class ComputerUsage
{
    public int ComputerUsageId { get; set; }
    public int UserId { get; set; }
    public int ComputerId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Amount { get; set; }
    
    public virtual User User { get; set; } = null!;
    public virtual Computer Computer { get; set; } = null!;
}