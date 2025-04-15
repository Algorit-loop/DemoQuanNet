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
        if (HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(User user)
    {
        if (!ModelState.IsValid)
        {
            return View(user);
        }

        // Kiểm tra username đã tồn tại
        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
        {
            ModelState.AddModelError("Username", "Tên đăng nhập đã được sử dụng");
            return View(user);
        }

        // Kiểm tra số điện thoại đã tồn tại
        if (await _context.Users.AnyAsync(u => u.PhoneNumber == user.PhoneNumber))
        {
            ModelState.AddModelError("PhoneNumber", "Số điện thoại đã được đăng ký");
            return View(user);
        }

        user.CreatedAt = DateTime.UtcNow;
        user.Balance = 0;
        user.IsAdmin = false;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("IsAdmin", "False");

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin");
            return View();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

        if (user == null)
        {
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác");
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