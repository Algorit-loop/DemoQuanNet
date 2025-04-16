using System;

namespace QuanNet.Models;

public class ComputerRevenueViewModel
{
    public string ComputerName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TotalSessions { get; set; }
    public TimeSpan TotalUsageTime { get; set; }
    public decimal AverageRevenuePerSession => TotalSessions > 0 ? Math.Round(TotalRevenue / TotalSessions, 2) : 0;
    public TimeSpan AverageTimePerSession => TotalSessions > 0 ? TimeSpan.FromTicks(TotalUsageTime.Ticks / TotalSessions) : TimeSpan.Zero;
}

public class RevenueFilterViewModel
{
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);
    public DateTime EndDate { get; set; } = DateTime.Today;
    public List<ComputerRevenueViewModel> Statistics { get; set; } = new();
}