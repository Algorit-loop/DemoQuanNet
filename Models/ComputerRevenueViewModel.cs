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