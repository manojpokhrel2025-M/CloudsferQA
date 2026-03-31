using CloudsferQA.Data;
using CloudsferQA.Helpers;
using CloudsferQA.Models;
using CloudsferQA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CloudsferQA.Controllers;

public class AuthController : Controller
{
    private const string AllowedDomain = "@tzunami.com";
    private const string AuthCookie    = "QAAuthUserId";

    private readonly AppDbContext  _db;
    private readonly EmailService  _email;
    private readonly ILogger<AuthController> _log;

    public AuthController(AppDbContext db, EmailService email, ILogger<AuthController> log)
    {
        _db    = db;
        _email = email;
        _log   = log;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LOGIN
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (GetCurrentUserId().HasValue)
            return RedirectToAction("Start", "Session");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl)
    {
        email = Normalize(email);
        ViewBag.Email = email;

        if (!email.EndsWith(AllowedDomain, StringComparison.OrdinalIgnoreCase))
        {
            ViewBag.Error = $"Only {AllowedDomain} email addresses are allowed.";
            return View();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return RedirectToAction("Register", new { email });

        if (!user.IsEmailVerified)
        {
            ViewBag.Error = "Your email hasn't been verified yet. Please check your inbox and click the verification link.";
            return View();
        }

        if (!user.IsActive)
        {
            ViewBag.Error = "Your account has been deactivated. Please contact an administrator.";
            return View();
        }

        if (!PasswordHelper.Verify(password, user.PasswordHash))
        {
            ViewBag.Error = "Incorrect password.";
            return View();
        }

        SetAuthCookie(user.Id);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Start", "Session");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // REGISTER
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet]
    public IActionResult Register(string? email)
    {
        ViewBag.Email = email ?? string.Empty;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        string email, string password, string confirmPassword)
    {
        email = Normalize(email);
        ViewBag.Email = email;

        // ── Check admin directory ────────────────────────────────────────
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (existing == null)
        {
            ViewBag.Error = "Your email is not in the Users directory. Please contact your administrator.";
            return View();
        }

        if (!string.IsNullOrEmpty(existing.PasswordHash))
        {
            ViewBag.Error = "This email is already registered. Please log in instead.";
            return View();
        }

        // ── Password validation ──────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            ViewBag.Error = "Password must be at least 8 characters.";
            return View();
        }

        if (password != confirmPassword)
        {
            ViewBag.Error = "Passwords do not match.";
            return View();
        }

        // ── Complete registration — auto-verify, no email needed ─────────
        existing.PasswordHash    = PasswordHelper.Hash(password);
        existing.IsEmailVerified = true;
        existing.VerifiedAt      = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        SetAuthCookie(existing.Id);
        TempData["Welcome"] = existing.Email;

        return RedirectToAction("Start", "Session");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // VERIFY
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Verify(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);

        if (user == null)
        {
            ViewBag.Error = "This verification link is invalid or has already been used.";
            return View("VerifyPending");
        }

        user.IsEmailVerified   = true;
        user.VerificationToken = null;
        user.VerifiedAt        = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        SetAuthCookie(user.Id);
        TempData["Welcome"] = user.Email;

        return RedirectToAction("Start", "Session");
    }

    [HttpGet]
    public IActionResult VerifyPending()
    {
        ViewBag.Email     = TempData["VerifyEmail"]?.ToString();
        ViewBag.VerifyUrl = TempData["VerifyUrl"]?.ToString(); // dev mode only
        return View();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LOGOUT
    // ═══════════════════════════════════════════════════════════════════════

    [HttpPost]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookie);
        Response.Cookies.Delete("QASessionId");
        return RedirectToAction("Login");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private void SetAuthCookie(int userId) =>
        Response.Cookies.Append(AuthCookie, userId.ToString(), new CookieOptions
        {
            Expires  = DateTimeOffset.UtcNow.AddDays(1),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax
        });

    private int? GetCurrentUserId()
    {
        if (Request.Cookies.TryGetValue(AuthCookie, out var v) && int.TryParse(v, out var id))
            return id;
        return null;
    }

    private static string Normalize(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
}
