namespace QuanNet.Models;

public class User
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public virtual ICollection<ComputerUsage> ComputerUsages { get; set; } = new List<ComputerUsage>();
}