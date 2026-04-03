using CloudsferQA.Data;
using CloudsferQA.Models;
using CloudsferQA.Models.ViewModels;
using CloudsferQA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudsferQA.Controllers;

public class DashboardController : Controller
{
    private const string AuthCookie = "QAAuthUserId";

    private readonly AppDbContext _db;
    private readonly StatsService _stats;

    public DashboardController(AppDbContext db, StatsService stats)
    {
        _db    = db;
        _stats = stats;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");
        ViewBag.CurrentUser = user;

        var sessions = await _db.Sessions
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();

        return View(new DashboardViewModel { Sessions = sessions });
    }

    [HttpGet]
    public async Task<IActionResult> Data(int id)
    {
        if (await GetCurrentUserAsync() == null) return Unauthorized();

        var dto = await _stats.GetStatsAsync(id);
        if (dto == null) return NotFound();
        return Json(dto);
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        if (!Request.Cookies.TryGetValue(AuthCookie, out var idStr)
            || !int.TryParse(idStr, out var id)) return null;
        var user = await _db.Users.FindAsync(id);
        return (user?.IsEmailVerified == true && user.IsActive) ? user : null;
    }
}
