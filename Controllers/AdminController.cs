using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanNet.Data;
using QuanNet.Models;

namespace QuanNet.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin()
    {
        var isAdmin = HttpContext.Session.GetString("IsAdmin");
        return isAdmin?.ToLower() == "true";
    }

    private ActionResult? CheckAdminAccess()
    {
        if (!IsAdmin())
            return RedirectToAction("Login", "Account");
        return null;
    }

    // Redirect admin to computers management instead of dashboard
    public IActionResult Index()
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;
            
        return RedirectToAction("Computers");
    }

    // Computer Management
    public async Task<IActionResult> Computers()
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;

        var computers = await _context.Computers
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(computers);
    }

    [HttpPost]
    public async Task<IActionResult> AddComputer(Computer computer)
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;

        if (string.IsNullOrWhiteSpace(computer.Name))
        {
            TempData["Error"] = "Tên máy không được để trống";
            return RedirectToAction(nameof(Computers));
        }

        if (computer.HourlyRate <= 0)
        {
            TempData["Error"] = "Giá/giờ phải lớn hơn 0";
            return RedirectToAction(nameof(Computers));
        }

        // Check if computer name already exists
        if (await _context.Computers.AnyAsync(c => c.Name == computer.Name))
        {
            TempData["Error"] = "Tên máy đã tồn tại";
            return RedirectToAction(nameof(Computers));
        }

        computer.Status = ComputerStatus.Available;
        _context.Computers.Add(computer);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã thêm máy {computer.Name}";
        return RedirectToAction(nameof(Computers));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateComputer(Computer computer)
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;

        if (string.IsNullOrWhiteSpace(computer.Name))
        {
            TempData["Error"] = "Tên máy không được để trống";
            return RedirectToAction(nameof(Computers));
        }

        if (computer.HourlyRate <= 0)
        {
            TempData["Error"] = "Giá/giờ phải lớn hơn 0";
            return RedirectToAction(nameof(Computers));
        }

        // Check if computer name already exists for different computer
        if (await _context.Computers.AnyAsync(c => c.Name == computer.Name && c.ComputerId != computer.ComputerId))
        {
            TempData["Error"] = "Tên máy đã tồn tại";
            return RedirectToAction(nameof(Computers));
        }

        var existingComputer = await _context.Computers.FindAsync(computer.ComputerId);
        if (existingComputer == null)
        {
            TempData["Error"] = "Không tìm thấy máy";
            return RedirectToAction(nameof(Computers));
        }

        // Don't allow changing status of computer in use unless it's being marked as out of order
        if (existingComputer.Status == ComputerStatus.InUse && 
            computer.Status != ComputerStatus.InUse && 
            computer.Status != ComputerStatus.OutOfOrder)
        {
            TempData["Error"] = "Không thể thay đổi trạng thái máy đang sử dụng";
            return RedirectToAction(nameof(Computers));
        }

        existingComputer.Name = computer.Name;
        existingComputer.HourlyRate = computer.HourlyRate;
        existingComputer.Status = computer.Status;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật máy {computer.Name}";
        return RedirectToAction(nameof(Computers));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteComputer(int id)
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;

        var computer = await _context.Computers
            .Include(c => c.ComputerUsages)
            .FirstOrDefaultAsync(c => c.ComputerId == id);

        if (computer == null)
        {
            TempData["Error"] = "Không tìm thấy máy";
            return RedirectToAction(nameof(Computers));
        }

        if (computer.Status == ComputerStatus.InUse)
        {
            TempData["Error"] = "Không thể xóa máy đang sử dụng";
            return RedirectToAction(nameof(Computers));
        }

        if (computer.ComputerUsages.Any())
        {
            TempData["Error"] = "Không thể xóa máy đã có lịch sử sử dụng";
            return RedirectToAction(nameof(Computers));
        }

        _context.Computers.Remove(computer);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa máy {computer.Name}";
        return RedirectToAction(nameof(Computers));
    }

    [HttpPost]
    public async Task<IActionResult> EndSession(int computerId)
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;

        var computer = await _context.Computers
            .Include(c => c.ComputerUsages.Where(cu => cu.EndTime == null))
            .FirstOrDefaultAsync(c => c.ComputerId == computerId);

        if (computer == null)
        {
            TempData["Error"] = "Không tìm thấy máy";
            return RedirectToAction(nameof(Computers));
        }

        var activeSession = computer.ComputerUsages.FirstOrDefault();
        if (activeSession == null)
        {
            TempData["Error"] = "Không tìm thấy phiên sử dụng";
            return RedirectToAction(nameof(Computers));
        }

        // Calculate amount
        activeSession.EndTime = DateTime.UtcNow;
        var totalSeconds = (decimal)(activeSession.EndTime.Value - activeSession.StartTime).TotalSeconds;
        var hourlyRate = computer.HourlyRate;
        var secondRate = hourlyRate / 3600m;
        activeSession.Amount = Math.Ceiling(totalSeconds * secondRate);

        // Update user balance
        var user = await _context.Users.FindAsync(activeSession.UserId);
        if (user != null)
        {
            if (user.Balance >= activeSession.Amount)
            {
                user.Balance -= activeSession.Amount.Value;
            }
            else
            {
                // If user doesn't have enough balance, charge what they have
                activeSession.Amount = user.Balance;
                user.Balance = 0;
            }
        }

        // Update computer status
        computer.Status = ComputerStatus.Available;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã kết thúc phiên sử dụng. Số tiền: {activeSession.Amount:N0} VNĐ";
        return RedirectToAction(nameof(Computers));
    }

    // User Management
    public async Task<IActionResult> Users()
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;

        var users = await _context.Users
            .Where(u => !u.IsAdmin)
            .OrderBy(u => u.Name)
            .ToListAsync();
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUser(int userId, string username, string name, string phoneNumber, string? password, decimal balance)
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;

        if (string.IsNullOrWhiteSpace(username))
        {
            TempData["Error"] = "Tài khoản không được để trống";
            return RedirectToAction(nameof(Users));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Tên không được để trống";
            return RedirectToAction(nameof(Users));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            TempData["Error"] = "Số điện thoại không được để trống";
            return RedirectToAction(nameof(Users));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.IsAdmin)
        {
            TempData["Error"] = "Không tìm thấy người dùng";
            return RedirectToAction(nameof(Users));
        }

        // Check if username exists for different user
        if (await _context.Users.AnyAsync(u => u.Username == username && u.UserId != userId))
        {
            TempData["Error"] = "Tài khoản đã tồn tại";
            return RedirectToAction(nameof(Users));
        }

        // Check if phone number exists for different user
        if (await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber && u.UserId != userId))
        {
            TempData["Error"] = "Số điện thoại đã tồn tại";
            return RedirectToAction(nameof(Users));
        }

        user.Username = username;
        user.Name = name;
        user.PhoneNumber = phoneNumber;
        user.Balance = balance;
        
        // Only update password if a new one is provided
        if (!string.IsNullOrWhiteSpace(password))
        {
            user.Password = password;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật thông tin người dùng {name}";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;

        var user = await _context.Users
            .Include(u => u.ComputerUsages)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || user.IsAdmin)
        {
            TempData["Error"] = "Không tìm thấy người dùng";
            return RedirectToAction(nameof(Users));
        }

        // Check if user has any active sessions
        if (user.ComputerUsages.Any(cu => cu.EndTime == null))
        {
            TempData["Error"] = "Không thể xóa người dùng đang sử dụng máy";
            return RedirectToAction(nameof(Users));
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa người dùng {user.Name}";
        return RedirectToAction(nameof(Users));
    }

    // Comprehensive Usage History
    public async Task<IActionResult> History()
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;

        var history = await _context.ComputerUsages
            .Include(cu => cu.User)
            .Include(cu => cu.Computer)
            .OrderByDescending(cu => cu.StartTime)
            .Select(cu => new ComputerUsageViewModel
            {
                UserName = cu.User.Name,
                PhoneNumber = cu.User.PhoneNumber,
                ComputerName = cu.Computer.Name,
                StartTime = cu.StartTime,
                EndTime = cu.EndTime,
                Amount = cu.Amount ?? 0
            })
            .ToListAsync();

        return View(history);
    }

    // Revenue statistics
    public async Task<IActionResult> Revenue()
    {
        var checkResult = CheckAdminAccess();
        if (checkResult != null) return checkResult;

        var computers = await _context.Computers
            .Include(c => c.ComputerUsages.Where(cu => cu.EndTime != null))
            .ToListAsync();

        var statistics = computers.Select(c => {
            var completedSessions = c.ComputerUsages.Where(cu => cu.EndTime != null);
            return new ComputerRevenueViewModel
            {
                ComputerName = c.Name,
                TotalRevenue = completedSessions.Sum(cu => cu.Amount ?? 0),
                TotalSessions = completedSessions.Count(),
                TotalUsageTime = TimeSpan.FromSeconds(completedSessions
                    .Sum(cu => (cu.EndTime!.Value - cu.StartTime).TotalSeconds))
            };
        })
        .OrderByDescending(s => s.TotalRevenue)
        .ToList();

        return View(statistics);
    }
}