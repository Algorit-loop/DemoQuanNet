using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanNet.Models;
using QuanNet.Data;

namespace QuanNet.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var isAdmin = HttpContext.Session.GetString("IsAdmin") == "True";

        if (isAdmin)
        {
            return RedirectToAction("Computers", "Admin");
        }

        if (userId.HasValue)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                ViewBag.UserName = user.Name;
                ViewBag.Balance = user.Balance;

                // Get active session if exists
                var activeSession = await _context.ComputerUsages
                    .Include(cu => cu.Computer)
                    .FirstOrDefaultAsync(cu => cu.UserId == userId && cu.EndTime == null);
                ViewBag.ActiveSession = activeSession;
            }
        }

        // Get available computers count
        ViewBag.AvailableComputersCount = await _context.Computers
            .CountAsync(c => c.Status == ComputerStatus.Available);

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
