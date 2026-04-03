using ClosedXML.Excel;
using CloudsferQA.Data;
using CloudsferQA.Models;
using CloudsferQA.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudsferQA.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    // ═══════════════════════════════════════════════════════════════════════
    // USER LIST
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // SubAdmin can't manage users — send them to test cases
        var subAdmin = await RequireAdminOrSubAdminAsync();
        if (subAdmin != null && subAdmin.Role == "SubAdmin")
            return RedirectToAction("TestCases");

        var admin = await RequireAdminAsync();
        if (admin == null) return RedirectToAction("Login", "Auth");

        var users = await _db.Users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.CreatedAt)
            .ToListAsync();

        // Count sessions per user
        var sessionCounts = await _db.Sessions
            .Where(s => s.UserId != null)
            .GroupBy(s => s.UserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        // Result counts per user (aggregate across all their sessions)
        var userStats = await _db.Sessions
            .Where(s => s.UserId != null)
            .Join(_db.Results, s => s.Id, r => r.SessionId,
                  (s, r) => new { s.UserId, r.Status })
            .GroupBy(x => x.UserId!.Value)
            .Select(g => new
            {
                UserId  = g.Key,
                Pass    = g.Count(x => x.Status == "pass"),
                Fail    = g.Count(x => x.Status == "fail"),
                Blocked = g.Count(x => x.Status == "blocked"),
                Total   = g.Count()
            })
            .ToDictionaryAsync(x => x.UserId);

        ViewBag.CurrentUser = admin;
        ViewBag.UserStats   = userStats;

        return View(new AdminUsersViewModel
        {
            Users         = users,
            SessionCounts = sessionCounts,
            CurrentAdmin  = admin
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // USER DETAIL
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> UserDetail(int id)
    {
        var admin = await RequireAdminAsync();
        if (admin == null) return RedirectToAction("Login", "Auth");
        ViewBag.CurrentUser = admin;

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        var sessions = await _db.Sessions
            .Where(s => s.UserId == id)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();

        var sessionIds = sessions.Select(s => s.Id).ToList();
        var allResults = await _db.Results
            .Where(r => sessionIds.Contains(r.SessionId))
            .ToListAsync();

        var totalCases = await _db.TestCases.CountAsync(t => !t.IsDeleted);

        var summaries = sessions.Select(s =>
        {
            var res = allResults.Where(r => r.SessionId == s.Id).ToList();
            return new SessionSummary
            {
                Session = s,
                Total   = totalCases,
                Pass    = res.Count(r => r.Status == "pass"),
                Fail    = res.Count(r => r.Status == "fail"),
                Blocked = res.Count(r => r.Status == "blocked"),
                Skip    = res.Count(r => r.Status == "skip"),
                Tested  = res.Count(r => r.Status != "pending")
            };
        }).ToList();

        return View(new AdminUserDetailViewModel
        {
            User         = user,
            Sessions     = summaries,
            CurrentAdmin = admin
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // USER MANAGEMENT ACTIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Change a user's role (QA / Dev / Admin).</summary>
    [HttpPost]
    public async Task<IActionResult> SetRole(int userId, string role)
    {
        if (await RequireAdminAsync() == null) return Unauthorized();

        if (role != "QA" && role != "Dev" && role != "Admin" && role != "SubAdmin")
            return BadRequest("Invalid role.");

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.Role = role;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"{user.Email} role changed to {role}.";
        return RedirectToAction("UserDetail", new { id = userId });
    }

    /// <summary>Toggle active / deactivated state for a user.</summary>
    [HttpPost]
    public async Task<IActionResult> ToggleActive(int userId)
    {
        var admin = await RequireAdminAsync();
        if (admin == null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (user.Id == admin.Id)
        {
            TempData["Error"] = "You cannot deactivate your own account.";
            return RedirectToAction("Index");
        }

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = user.IsActive
            ? $"{user.Email} has been re-activated."
            : $"{user.Email} has been deactivated.";

        return RedirectToAction("Index");
    }

    /// <summary>Admin-side manual email verification (useful when SMTP is not configured).</summary>
    [HttpPost]
    public async Task<IActionResult> VerifyUser(int userId)
    {
        if (await RequireAdminAsync() == null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.IsEmailVerified   = true;
        user.VerificationToken = null;
        user.VerifiedAt        = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"{user.Email} has been manually verified.";
        return RedirectToAction("Index");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPER
    // ═══════════════════════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════════════════════
    // ADD USER TO DIRECTORY
    // ═══════════════════════════════════════════════════════════════════════

    [HttpPost]
    public async Task<IActionResult> AddUser(string email, string role)
    {
        if (await RequireAdminAsync() == null) return Unauthorized();

        email = email.Trim().ToLowerInvariant();

        if (role != "QA" && role != "Dev" && role != "Admin" && role != "SubAdmin")
        {
            TempData["Error"] = "Invalid role.";
            return RedirectToAction("Index");
        }

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            TempData["Error"] = $"{email} is already in the directory.";
            return RedirectToAction("Index");
        }

        // Pre-create user with no password — they will complete registration themselves
        _db.Users.Add(new User
        {
            Email             = email,
            PasswordHash      = string.Empty,   // empty = not yet registered
            Role              = role,
            IsEmailVerified   = false,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        await LogAsync("AddUser", $"User {email} added to directory with role {role}", email: email, category: "User");

        TempData["Success"] = $"{email} added to the directory as {role}. They can now register.";
        return RedirectToAction("Index");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASE MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> TestCases()
    {
        var admin = await RequireAdminOrSubAdminAsync();
        if (admin == null) return RedirectToAction("Login", "Auth");
        ViewBag.CurrentUser = admin;

        // Load saved module order
        var orders = await _db.ModuleOrders.ToDictionaryAsync(m => m.ModuleName, m => m.SortOrder);
        ViewBag.ModuleOrder = orders;

        var testCases = await _db.TestCases.OrderBy(t => t.Submodule).ThenBy(t => t.Id).ToListAsync();
        return View(testCases);
    }

    [HttpPost]
    public async Task<IActionResult> SaveModuleOrder([FromBody] List<string> moduleNames)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();

        var existing = await _db.ModuleOrders.ToListAsync();
        _db.ModuleOrders.RemoveRange(existing);

        for (int i = 0; i < moduleNames.Count; i++)
            _db.ModuleOrders.Add(new CloudsferQA.Models.ModuleOrder { ModuleName = moduleNames[i], SortOrder = i });

        await _db.SaveChangesAsync();
        return Ok();
    }

    // ── Module ──────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> RenameModule(string oldName, string newName)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();
        newName = newName.Trim();
        if (string.IsNullOrWhiteSpace(newName)) return BadRequest();

        var cases = await _db.TestCases.Where(t => t.Module == oldName && !t.IsDeleted).ToListAsync();
        cases.ForEach(t => t.Module = newName);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Module renamed to \"{newName}\".";
        return RedirectToAction("TestCases");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteModule(string moduleName)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();

        var cases = await _db.TestCases.Where(t => t.Module == moduleName && !t.IsDeleted).ToListAsync();
        cases.ForEach(t => { t.IsDeleted = true; t.DeletedAt = DateTime.UtcNow; });
        await _db.SaveChangesAsync();
        await LogAsync("DeleteModule", $"Module \"{moduleName}\" moved to Bin ({cases.Count} test cases)", category: "Module");
        TempData["Success"] = $"Module \"{moduleName}\" moved to Bin.";
        return RedirectToAction("TestCases");
    }

    // ── Submodule ────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> RenameSubmodule(string moduleName, string oldName, string newName)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();
        newName = newName.Trim();
        if (string.IsNullOrWhiteSpace(newName)) return BadRequest();

        var cases = await _db.TestCases.Where(t => t.Module == moduleName && t.Submodule == oldName && !t.IsDeleted).ToListAsync();
        cases.ForEach(t => t.Submodule = newName);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Submodule renamed to \"{newName}\".";
        return RedirectToAction("TestCases");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSubmodule(string moduleName, string submoduleName)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();

        var cases = await _db.TestCases.Where(t => t.Module == moduleName && t.Submodule == submoduleName && !t.IsDeleted).ToListAsync();
        cases.ForEach(t => { t.IsDeleted = true; t.DeletedAt = DateTime.UtcNow; });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Submodule \"{submoduleName}\" moved to Bin.";
        return RedirectToAction("TestCases");
    }

    // ── Test Case ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> AddTestCase(string moduleName, string submoduleName)
    {
        var user = await RequireAdminOrSubAdminAsync();
        if (user == null) return RedirectToAction("Login", "Auth");
        ViewBag.CurrentUser = user;
        ViewBag.Modules     = (await _db.TestCases.Where(t => !t.IsDeleted).Select(t => t.Module).Distinct().OrderBy(m => m).ToListAsync());
        ViewBag.Module      = moduleName;
        ViewBag.Submodule   = submoduleName;
        return View(new TestCase { Module = moduleName, Submodule = submoduleName });
    }

    [HttpPost]
    public async Task<IActionResult> AddTestCase(TestCase tc)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();

        // Auto-generate ID: prefix from module + next number
        var prefix = string.Concat(tc.Module.Split(' ', '&', '(', ')')
            .Where(w => w.Length > 0)
            .Take(2)
            .Select(w => w[0]))
            .ToUpper();
        if (prefix.Length < 2) prefix = tc.Module.Replace(" ", "").ToUpper()[..Math.Min(3, tc.Module.Replace(" ", "").Length)];

        var existing = await _db.TestCases.Where(t => t.Id.StartsWith(prefix)).Select(t => t.Id).ToListAsync(); // include deleted for ID uniqueness
        int next = existing
            .Select(id => { int.TryParse(id.Replace(prefix + "-", ""), out var n); return n; })
            .DefaultIfEmpty(0).Max() + 1;

        tc.Id = $"{prefix}-{next:D3}";
        tc.Steps          ??= string.Empty;
        tc.ExpectedResult ??= string.Empty;

        _db.TestCases.Add(tc);

        // If this is a brand-new module, append it at the end of the order list
        bool moduleExists = await _db.ModuleOrders.AnyAsync(m => m.ModuleName == tc.Module);
        if (!moduleExists)
        {
            int maxOrder = await _db.ModuleOrders.AnyAsync()
                ? await _db.ModuleOrders.MaxAsync(m => m.SortOrder)
                : -1;
            _db.ModuleOrders.Add(new CloudsferQA.Models.ModuleOrder { ModuleName = tc.Module, SortOrder = maxOrder + 1 });
        }

        await _db.SaveChangesAsync();
        await LogAsync("AddTestCase", $"Test case {tc.Id} added to [{tc.Module}] > [{tc.Submodule}]: {tc.Scenario}", category: "TestCase");
        TempData["Success"] = $"Test case {tc.Id} added.";
        return RedirectToAction("TestCases");
    }

    [HttpGet]
    public async Task<IActionResult> EditTestCase(string id)
    {
        var user = await RequireAdminOrSubAdminAsync();
        if (user == null) return RedirectToAction("Login", "Auth");
        ViewBag.CurrentUser = user;
        ViewBag.Modules     = await _db.TestCases.Select(t => t.Module).Distinct().OrderBy(m => m).ToListAsync();

        var tc = await _db.TestCases.FindAsync(id);
        if (tc == null) return NotFound();
        return View(tc);
    }

    [HttpPost]
    public async Task<IActionResult> EditTestCase(TestCase updated)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();

        var tc = await _db.TestCases.FindAsync(updated.Id);
        if (tc == null) return NotFound();

        tc.Module         = updated.Module;
        tc.Submodule      = updated.Submodule;
        tc.Group          = updated.Group          ?? string.Empty;
        tc.Scenario       = updated.Scenario;
        tc.Steps          = updated.Steps          ?? string.Empty;
        tc.ExpectedResult = updated.ExpectedResult ?? string.Empty;
        tc.Priority       = updated.Priority;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Test case {tc.Id} updated.";
        return RedirectToAction("TestCases");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTestCase(string id)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();

        var tc = await _db.TestCases.FindAsync(id);
        if (tc == null) return NotFound();

        tc.IsDeleted = true;
        tc.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Test case {id} moved to Bin.";
        return RedirectToAction("TestCases");
    }

    // ── Bin ──────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> RestoreModule(string moduleName)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();

        var cases = await _db.TestCases.Where(t => t.Module == moduleName && t.IsDeleted).ToListAsync();
        cases.ForEach(t => { t.IsDeleted = false; t.DeletedAt = null; });
        await _db.SaveChangesAsync();
        await LogAsync("RestoreModule", $"Module \"{moduleName}\" restored from Bin ({cases.Count} test cases)", category: "Module");
        TempData["Success"] = $"Module \"{moduleName}\" restored.";
        return RedirectToAction("TestCases");
    }

    [HttpPost]
    public async Task<IActionResult> PermanentDeleteModule(string moduleName)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();

        var cases = await _db.TestCases.Where(t => t.Module == moduleName && t.IsDeleted).ToListAsync();
        _db.TestCases.RemoveRange(cases);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Module \"{moduleName}\" permanently deleted.";
        return RedirectToAction("TestCases");
    }

    [HttpPost]
    public async Task<IActionResult> EmptyBin()
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();

        var cases = await _db.TestCases.Where(t => t.IsDeleted).ToListAsync();
        _db.TestCases.RemoveRange(cases);
        await _db.SaveChangesAsync();
        await LogAsync("EmptyBin", $"Bin emptied permanently ({cases.Count} test cases deleted)", category: "Module");
        TempData["Success"] = "Bin emptied permanently.";
        return RedirectToAction("TestCases");
    }

    // ── IMPORT ───────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult DownloadImportTemplate()
    {
        var csv = "Module,Submodule,Scenario,Steps,ExpectedResult,Priority\r\n" +
                  "Registration & Activation,Account Registration,Verify user can register with valid email,\"1. Go to register page\n2. Fill form\n3. Click Submit\",User receives a verification email,High\r\n" +
                  "Registration & Activation,Email Verification,Verify clicking verification link activates account,,Account is activated and user can login,Medium";
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "CloudsferQA_Import_Template.csv");
    }

    [HttpPost]
    public async Task<IActionResult> ImportTestCases(IFormFile file)
    {
        if (await RequireAdminOrSubAdminAsync() == null) return Unauthorized();

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to import.";
            return RedirectToAction("TestCases");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".csv" && ext != ".xlsx")
        {
            TempData["Error"] = "Only CSV (.csv) and Excel (.xlsx) files are supported.";
            return RedirectToAction("TestCases");
        }

        var rows = new List<TestCase>();

        try
        {
            if (ext == ".csv")
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var headerLine = await reader.ReadLineAsync();
                if (headerLine == null) { TempData["Error"] = "File is empty."; return RedirectToAction("TestCases"); }

                var headers = headerLine.Split(',').Select(h => h.Trim().Trim('"').ToLowerInvariant()).ToList();
                int modCol = headers.IndexOf("module");
                int subCol = headers.IndexOf("submodule");
                int grpCol = headers.IndexOf("group");
                int scnCol = headers.IndexOf("scenario");
                int stpCol = headers.IndexOf("steps");
                int expCol = headers.IndexOf("expectedresult");
                int priCol = headers.IndexOf("priority");

                if (modCol < 0 || subCol < 0 || scnCol < 0)
                {
                    TempData["Error"] = "File must have columns: Module, Submodule, Scenario (Steps, ExpectedResult, Priority are optional).";
                    return RedirectToAction("TestCases");
                }

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var cols = SplitCsvLine(line);
                    var tc = new TestCase
                    {
                        Module        = GetCol(cols, modCol),
                        Submodule     = GetCol(cols, subCol),
                        Group         = GetCol(cols, grpCol),
                        Scenario      = GetCol(cols, scnCol),
                        Steps         = GetCol(cols, stpCol),
                        ExpectedResult= GetCol(cols, expCol),
                        Priority      = GetCol(cols, priCol) is { Length: > 0 } p ? p : "Medium"
                    };
                    if (!string.IsNullOrWhiteSpace(tc.Module) && !string.IsNullOrWhiteSpace(tc.Scenario))
                        rows.Add(tc);
                }
            }
            else // .xlsx
            {
                using var stream = file.OpenReadStream();
                using var wb = new XLWorkbook(stream);
                var ws = wb.Worksheet(1);
                var lastCol = ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 10;

                var headers = Enumerable.Range(1, lastCol)
                    .Select(c => ws.Cell(1, c).GetString().Trim().ToLowerInvariant())
                    .ToList();

                int modCol = headers.IndexOf("module") + 1;
                int subCol = headers.IndexOf("submodule") + 1;
                int grpCol = headers.IndexOf("group") + 1;
                int scnCol = headers.IndexOf("scenario") + 1;
                int stpCol = headers.IndexOf("steps") + 1;
                int expCol = headers.IndexOf("expectedresult") + 1;
                int priCol = headers.IndexOf("priority") + 1;

                if (modCol == 0 || subCol == 0 || scnCol == 0)
                {
                    TempData["Error"] = "Excel must have columns: Module, Submodule, Scenario (Steps, ExpectedResult, Priority are optional).";
                    return RedirectToAction("TestCases");
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                for (int r = 2; r <= lastRow; r++)
                {
                    var module   = ws.Cell(r, modCol).GetString().Trim();
                    var scenario = ws.Cell(r, scnCol).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(module) && string.IsNullOrWhiteSpace(scenario)) continue;

                    var priVal = priCol > 0 ? ws.Cell(r, priCol).GetString().Trim() : "";
                    rows.Add(new TestCase
                    {
                        Module        = module,
                        Submodule     = ws.Cell(r, subCol).GetString().Trim(),
                        Group         = grpCol > 0 ? ws.Cell(r, grpCol).GetString().Trim() : "",
                        Scenario      = scenario,
                        Steps         = stpCol > 0 ? ws.Cell(r, stpCol).GetString().Trim() : "",
                        ExpectedResult= expCol > 0 ? ws.Cell(r, expCol).GetString().Trim() : "",
                        Priority      = priVal.Length > 0 ? priVal : "Medium"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to parse file: {ex.Message}";
            return RedirectToAction("TestCases");
        }

        if (rows.Count == 0)
        {
            TempData["Error"] = "No valid rows found. Make sure the file has Module, Submodule, and Scenario columns with data.";
            return RedirectToAction("TestCases");
        }

        int added = 0;
        foreach (var tc in rows)
        {
            // Same ID generation as AddTestCase
            var prefix = string.Concat(tc.Module.Split(' ', '&', '(', ')')
                .Where(w => w.Length > 0)
                .Take(2)
                .Select(w => w[0]))
                .ToUpper();
            if (prefix.Length < 2)
                prefix = tc.Module.Replace(" ", "").ToUpper()[..Math.Min(3, tc.Module.Replace(" ", "").Length)];

            var existing = await _db.TestCases.Where(t => t.Id.StartsWith(prefix)).Select(t => t.Id).ToListAsync();
            int next = existing
                .Select(id => { int.TryParse(id.Replace(prefix + "-", ""), out var n); return n; })
                .DefaultIfEmpty(0).Max() + 1;

            tc.Id             = $"{prefix}-{next:D3}";
            tc.Steps          ??= string.Empty;
            tc.ExpectedResult ??= string.Empty;

            _db.TestCases.Add(tc);

            bool moduleExists = await _db.ModuleOrders.AnyAsync(m => m.ModuleName == tc.Module);
            if (!moduleExists)
            {
                int maxOrder = await _db.ModuleOrders.AnyAsync()
                    ? await _db.ModuleOrders.MaxAsync(m => m.SortOrder)
                    : -1;
                _db.ModuleOrders.Add(new CloudsferQA.Models.ModuleOrder { ModuleName = tc.Module, SortOrder = maxOrder + 1 });
            }

            await _db.SaveChangesAsync();
            added++;
        }

        var importedModules = rows.Select(r => r.Module).Distinct();
        await LogAsync("Import", $"Imported {added} test case(s) from \"{file.FileName}\" into modules: {string.Join(", ", importedModules)}", category: "TestCase");
        TempData["Success"] = $"{added} test case(s) imported successfully from \"{file.FileName}\".";
        return RedirectToAction("TestCases");
    }

    private static string GetCol(List<string> cols, int idx) =>
        idx >= 0 && idx < cols.Count ? cols[idx].Trim().Trim('"') : "";

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuote = false;
        var current = new System.Text.StringBuilder();
        foreach (char c in line)
        {
            if (c == '"') { inQuote = !inQuote; }
            else if (c == ',' && !inQuote) { result.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }
        result.Add(current.ToString());
        return result;
    }

    // ── RESET ALL SESSIONS ───────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> ResetAllSessions()
    {
        if (await RequireAdminAsync() == null) return Unauthorized();

        var results  = await _db.Results.ToListAsync();
        var sessions = await _db.Sessions.ToListAsync();

        _db.Results.RemoveRange(results);
        _db.Sessions.RemoveRange(sessions);
        await _db.SaveChangesAsync();

        await LogAsync("ResetAllSessions", $"All sessions and results wiped ({sessions.Count} sessions, {results.Count} results deleted)", category: "User");
        TempData["Success"] = $"All sessions cleared — {sessions.Count} session(s) and {results.Count} result(s) deleted. Everyone starts fresh.";
        return RedirectToAction("Index");
    }

    // ── ACTIVITY LOG ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ActivityLog(string? category, int page = 1)
    {
        var admin = await RequireAdminAsync();
        if (admin == null) return RedirectToAction("Login", "Auth");
        ViewBag.CurrentUser = admin;

        var query = _db.ActivityLogs.AsQueryable();
        if (!string.IsNullOrEmpty(category))
            query = query.Where(l => l.Category == category);

        int total = await query.CountAsync();
        int pageSize = 50;
        var logs = await query
            .OrderByDescending(l => l.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Category = category;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Total = total;
        return View(logs);
    }

    private async Task LogAsync(string action, string details, string? email = null, string category = "")
    {
        // Get current user email from cookie
        string performedBy = email ?? "System";
        if (email == null && Request.Cookies.TryGetValue("QAAuthUserId", out var idStr) && int.TryParse(idStr, out var id))
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null) performedBy = user.Email;
        }

        _db.ActivityLogs.Add(new CloudsferQA.Models.ActivityLog
        {
            Action      = action,
            Details     = details,
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow,
            Category    = category
        });
        await _db.SaveChangesAsync();
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────

    /// <summary>Admin only — for user management actions.</summary>
    private async Task<User?> RequireAdminAsync()
    {
        if (!Request.Cookies.TryGetValue("QAAuthUserId", out var idStr)
            || !int.TryParse(idStr, out var id)) return null;

        var user = await _db.Users.FindAsync(id);
        return (user?.IsEmailVerified == true && user.IsActive && user.Role == "Admin")
            ? user : null;
    }

    /// <summary>Admin or SubAdmin — for test case management actions.</summary>
    private async Task<User?> RequireAdminOrSubAdminAsync()
    {
        if (!Request.Cookies.TryGetValue("QAAuthUserId", out var idStr)
            || !int.TryParse(idStr, out var id)) return null;

        var user = await _db.Users.FindAsync(id);
        return (user?.IsEmailVerified == true && user.IsActive &&
                (user.Role == "Admin" || user.Role == "SubAdmin"))
            ? user : null;
    }
}
