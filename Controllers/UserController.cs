using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanNet.Data;
using QuanNet.Models;

namespace QuanNet.Controllers;

public class UserController : Controller
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin()
    {
        return HttpContext.Session.GetString("IsAdmin") == "True";
    }

    private ActionResult? CheckAccess()
    {
        if (IsAdmin())
        {
            return RedirectToAction("Computers", "Admin");
        }

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        return null;
    }

    private async Task<User?> GetCurrentUser()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return null;

        return await _context.Users.FindAsync(userId.Value);
    }

    // List available computers
    public async Task<IActionResult> AvailableComputers()
    {
        var checkResult = CheckAccess();
        if (checkResult != null) return checkResult;

        var user = await GetCurrentUser();
        if (user == null) return RedirectToAction("Login", "Account");

        var computers = await _context.Computers
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(computers);
    }

    // Start using a computer
    [HttpPost]
    public async Task<IActionResult> StartSession(int computerId)
    {
        var checkResult = CheckAccess();
        if (checkResult != null) return checkResult;

        var user = await GetCurrentUser();
        if (user == null) return RedirectToAction("Login", "Account");

        // Check if user has any active sessions
        var activeSession = await _context.ComputerUsages
            .FirstOrDefaultAsync(cu => cu.UserId == user.UserId && cu.EndTime == null);
        if (activeSession != null)
        {
            TempData["Error"] = "Bạn đang có phiên sử dụng khác";
            return RedirectToAction("CurrentSession");
        }

        var computer = await _context.Computers.FindAsync(computerId);
        if (computer == null || computer.Status != ComputerStatus.Available)
        {
            TempData["Error"] = "Máy không khả dụng";
            return RedirectToAction("AvailableComputers");
        }

        // Create new session
        var usage = new ComputerUsage
        {
            UserId = user.UserId,
            ComputerId = computerId,
            StartTime = DateTime.UtcNow
        };

        computer.Status = ComputerStatus.InUse;

        _context.ComputerUsages.Add(usage);
        await _context.SaveChangesAsync();

        return RedirectToAction("CurrentSession");
    }

    // End current session
    [HttpPost]
    public async Task<IActionResult> EndSession(bool isAutoEnd = false)
    {
        var checkResult = CheckAccess();
        if (checkResult != null) return checkResult;

        var user = await GetCurrentUser();
        if (user == null) return RedirectToAction("Login", "Account");

        var activeSession = await _context.ComputerUsages
            .Include(cu => cu.Computer)
            .FirstOrDefaultAsync(cu => cu.UserId == user.UserId && cu.EndTime == null);

        if (activeSession == null)
        {
            TempData["Error"] = "Không tìm thấy phiên sử dụng";
            return RedirectToAction("Index", "Home");
        }

        activeSession.EndTime = DateTime.UtcNow;
        var totalSeconds = (decimal)(activeSession.EndTime.Value - activeSession.StartTime).TotalSeconds;
        var hourlyRate = activeSession.Computer.HourlyRate;
        var secondRate = hourlyRate / 3600m; // Convert hourly rate to per-second rate
        activeSession.Amount = Math.Ceiling(totalSeconds * secondRate);

        // Handle auto-end case where balance is exceeded
        if (isAutoEnd || activeSession.Amount > user.Balance)
        {
            activeSession.Amount = user.Balance;
            user.Balance = 0;
        }
        else
        {
            // Normal end session
            user.Balance -= activeSession.Amount.Value;
        }

        // Update computer status
        var computer = await _context.Computers.FindAsync(activeSession.ComputerId);
        if (computer != null)
        {
            computer.Status = ComputerStatus.Available;
        }

        await _context.SaveChangesAsync();

        if (isAutoEnd)
        {
            TempData["Warning"] = $"Phiên sử dụng đã kết thúc tự động do hết số dư. Số tiền: {activeSession.Amount.Value:N0} VNĐ";
        }
        else
        {
            TempData["Success"] = $"Đã kết thúc phiên sử dụng. Số tiền: {activeSession.Amount.Value:N0} VNĐ";
        }
        
        return RedirectToAction("UsageHistory");
    }

    // View current session
    public async Task<IActionResult> CurrentSession()
    {
        var checkResult = CheckAccess();
        if (checkResult != null) return checkResult;

        var user = await GetCurrentUser();
        if (user == null) return RedirectToAction("Login", "Account");

        var activeSession = await _context.ComputerUsages
            .Include(cu => cu.Computer)
            .FirstOrDefaultAsync(cu => cu.UserId == user.UserId && cu.EndTime == null);

        ViewBag.CurrentBalance = user.Balance;
        return View(activeSession);
    }

    // View usage history
    public async Task<IActionResult> UsageHistory()
    {
        var checkResult = CheckAccess();
        if (checkResult != null) return checkResult;

        var user = await GetCurrentUser();
        if (user == null) return RedirectToAction("Login", "Account");

        var history = await _context.ComputerUsages
            .Include(cu => cu.Computer)
            .Where(cu => cu.UserId == user.UserId)
            .OrderByDescending(cu => cu.StartTime)
            .ToListAsync();

        return View(history);
    }

    // Add balance
    [HttpPost]
    public async Task<IActionResult> AddBalance(decimal amount)
    {
        var checkResult = CheckAccess();
        if (checkResult != null) return checkResult;

        if (amount <= 0)
        {
            TempData["Error"] = "Số tiền nạp phải lớn hơn 0";
            return RedirectToAction("Index", "Home");
        }

        var user = await GetCurrentUser();
        if (user == null) return RedirectToAction("Login", "Account");

        user.Balance += amount;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Nạp thành công {amount:N0} VNĐ";
        return RedirectToAction("Index", "Home");
    }
}