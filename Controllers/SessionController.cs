using CloudsferQA.Data;
using CloudsferQA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudsferQA.Controllers;

public class SessionController : Controller
{
    private const string AuthCookie    = "QAAuthUserId";
    private const string SessionCookie = "QASessionId";

    private readonly AppDbContext _db;

    public SessionController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Start()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");

        ViewBag.CurrentUser = user;

        if (TempData.ContainsKey("Welcome"))
            ViewBag.Welcome = $"Welcome, {TempData["Welcome"]}!";

        // Auto-generate version: emailname + last digit of year + MM + DD
        var emailName  = user.Email.Split('@')[0];
        var now        = DateTime.Now;
        var autoVersion = $"{emailName}{now.Year % 10}{now.Month:D2}{now.Day:D2}";
        ViewBag.AutoVersion = autoVersion;

        // Previous sessions for this user (newest first) for the "existing" picker
        var previousSessions = await _db.Sessions
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
        ViewBag.PreviousSessions = previousSessions;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Start(string version, string environment, int? resumeSessionId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");

        int sessionId;

        if (resumeSessionId.HasValue)
        {
            // Resume an existing session — verify it belongs to this user
            var existing = await _db.Sessions
                .FirstOrDefaultAsync(s => s.Id == resumeSessionId.Value && s.UserId == user.Id);

            if (existing == null) return NotFound();
            sessionId = existing.Id;
        }
        else
        {
            var session = new TestSession
            {
                UserId      = user.Id,
                Tester      = user.Email,
                Version     = version,
                Environment = environment,
                StartedAt   = DateTime.UtcNow
            };

            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();
            sessionId = session.Id;
        }

        Response.Cookies.Append(SessionCookie, sessionId.ToString(), new CookieOptions
        {
            Expires  = DateTimeOffset.UtcNow.AddHours(8),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax
        });

        return RedirectToAction("Index", "Test");
    }

    [HttpPost]
    public async Task<IActionResult> Complete()
    {
        if (await GetCurrentUserAsync() == null) return Unauthorized();

        if (!Request.Cookies.TryGetValue(SessionCookie, out var sessionIdStr)
            || !int.TryParse(sessionIdStr, out var sessionId))
            return RedirectToAction("Start");

        var session = await _db.Sessions.FindAsync(sessionId);
        if (session != null && session.CompletedAt == null)
        {
            session.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Clear the session cookie so a fresh session is required
        Response.Cookies.Delete(SessionCookie);
        TempData["Completed"] = session?.Version;
        return RedirectToAction("Start");
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        if (!Request.Cookies.TryGetValue(AuthCookie, out var idStr)
            || !int.TryParse(idStr, out var id)) return null;

        var user = await _db.Users.FindAsync(id);
        return (user?.IsEmailVerified == true && user.IsActive) ? user : null;
    }
}
