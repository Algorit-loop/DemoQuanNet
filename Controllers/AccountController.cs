using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanNet.Data;
using QuanNet.Models;

namespace QuanNet.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(User user)
    {
        if (ModelState.IsValid)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.Balance = 0;
            user.IsAdmin = false;
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            HttpContext.Session.SetInt32("UserId", user.UserId);
            return RedirectToAction("Index", "Home");
        }
        return View(user);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string phoneNumber, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.Password == password);

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            return View();
        }

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

        if (user.IsAdmin)
        {
            return RedirectToAction("Index", "Admin");
        }
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}