using ClosedXML.Excel;
using CloudsferQA.Data;
using CloudsferQA.Models;
using CloudsferQA.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudsferQA.Controllers;

public class TestController : Controller
{
    private const string AuthCookie    = "QAAuthUserId";
    private const string SessionCookie = "QASessionId";

    private readonly AppDbContext _db;

    public TestController(AppDbContext db) => _db = db;

    /// <summary>Returns module names sorted by admin-defined order, with any unordered modules appended alphabetically.</summary>
    private async Task<List<string>> GetModuleOrderAsync(List<string> allModules)
    {
        var saved = await _db.ModuleOrders.OrderBy(m => m.SortOrder).Select(m => m.ModuleName).ToListAsync();
        var unordered = allModules.Where(m => !saved.Contains(m)).OrderBy(m => m).ToList();
        return saved.Where(m => allModules.Contains(m)).Concat(unordered).ToList();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INDEX
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Auth");
        ViewBag.CurrentUser = user;

        if (!Request.Cookies.TryGetValue(SessionCookie, out var sessionIdStr)
            || !int.TryParse(sessionIdStr, out var sessionId))
            return RedirectToAction("Start", "Session");

        var session = await _db.Sessions.FindAsync(sessionId);
        if (session == null) return RedirectToAction("Start", "Session");

        var testCases = await _db.TestCases.ToListAsync();
        var results   = await _db.Results
            .Where(r => r.SessionId == sessionId)
            .ToListAsync();

        var moduleOrder  = await GetModuleOrderAsync(testCases.Select(t => t.Module).Distinct().ToList());
        var orderedCases = moduleOrder
            .SelectMany(m => testCases.Where(tc => tc.Module == m).OrderBy(tc => tc.Submodule).ThenBy(tc => tc.Id))
            .ToList();

        return View(new TestViewModel
        {
            Session     = session,
            TestCases   = orderedCases,
            Results     = results,
            ModuleOrder = moduleOrder
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SAVE RESULT
    // ═══════════════════════════════════════════════════════════════════════

    [HttpPost]
    public async Task<IActionResult> SaveResult([FromBody] SaveResultRequest request)
    {
        if (await GetCurrentUserAsync() == null) return Unauthorized();

        if (!Request.Cookies.TryGetValue(SessionCookie, out var sessionIdStr)
            || !int.TryParse(sessionIdStr, out var sessionId))
            return Unauthorized();

        var existing = await _db.Results
            .FirstOrDefaultAsync(r => r.SessionId == sessionId
                                   && r.TestCaseId == request.TestCaseId);

        DateTime? testedAt = request.Status != "pending" ? DateTime.UtcNow : null;

        if (existing == null)
        {
            _db.Results.Add(new TestResult
            {
                SessionId  = sessionId,
                TestCaseId = request.TestCaseId,
                Status     = request.Status,
                Notes      = request.Notes ?? string.Empty,
                TestedAt   = testedAt
            });
        }
        else
        {
            existing.Status   = request.Status;
            existing.Notes    = request.Notes ?? string.Empty;
            existing.TestedAt = testedAt;
        }

        await _db.SaveChangesAsync();
        return Json(new { success = true, testedAt = testedAt?.ToString("o") });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EXPORT
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Export(int sessionId)
    {
        if (await GetCurrentUserAsync() == null) return RedirectToAction("Login", "Auth");

        var session = await _db.Sessions.FindAsync(sessionId);
        if (session == null) return NotFound();

        var testCases   = await _db.TestCases.ToListAsync();
        var results     = await _db.Results.Where(r => r.SessionId == sessionId).ToListAsync();
        var resultDict  = results.ToDictionary(r => r.TestCaseId);
        var moduleOrder = await GetModuleOrderAsync(testCases.Select(t => t.Module).Distinct().ToList());

        using var wb = new XLWorkbook();

        // ── Summary sheet ─────────────────────────────────────────────────
        var sw = wb.Worksheets.Add("Summary");

        sw.Cell(1, 1).Value = "CloudsferQA — Test Session Export";
        sw.Cell(1, 1).Style.Font.Bold = true;
        sw.Cell(1, 1).Style.Font.FontSize = 14;
        sw.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1565C0");

        var meta = new[]
        {
            ("Tester:",      session.Tester),
            ("Version:",     session.Version),
            ("Environment:", session.Environment),
            ("Started At:",  session.StartedAt.ToString("yyyy-MM-dd HH:mm") + " UTC"),
            ("Exported At:", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC")
        };
        for (int i = 0; i < meta.Length; i++)
        {
            sw.Cell(i + 2, 1).Value = meta[i].Item1;
            sw.Cell(i + 2, 1).Style.Font.Bold = true;
            sw.Cell(i + 2, 2).Value = meta[i].Item2;
        }

        string[] hdr = { "Module", "Total", "Pass", "Fail", "Blocked", "Skip", "Pending", "Pass Rate" };
        for (int i = 0; i < hdr.Length; i++)
        {
            var c = sw.Cell(8, i + 1);
            c.Value = hdr[i];
            c.Style.Font.Bold = true;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            c.Style.Font.FontColor = XLColor.White;
        }

        int row = 9;
        foreach (var module in moduleOrder)
        {
            var mc = testCases.Where(tc => tc.Module == module).ToList();
            if (mc.Count == 0) continue;

            int mp = 0, mf = 0, mb = 0, ms = 0, mpend = 0;
            foreach (var tc in mc)
            {
                if (resultDict.TryGetValue(tc.Id, out var r))
                    switch (r.Status) { case "pass": mp++; break; case "fail": mf++; break; case "blocked": mb++; break; case "skip": ms++; break; default: mpend++; break; }
                else mpend++;
            }
            double pr = mc.Count > 0 ? Math.Round((double)mp / mc.Count * 100, 1) : 0;

            sw.Cell(row, 1).Value = module; sw.Cell(row, 2).Value = mc.Count;
            sw.Cell(row, 3).Value = mp;     sw.Cell(row, 4).Value = mf;
            sw.Cell(row, 5).Value = mb;     sw.Cell(row, 6).Value = ms;
            sw.Cell(row, 7).Value = mpend;  sw.Cell(row, 8).Value = $"{pr}%";
            row++;
        }
        sw.Columns().AdjustToContents();

        // ── Per-module sheets ─────────────────────────────────────────────
        string[] cols = { "TC ID", "Submodule", "Scenario", "Steps", "Expected Result", "Priority", "Status", "Notes", "Tested At" };

        foreach (var module in moduleOrder)
        {
            var mc = testCases.Where(tc => tc.Module == module).ToList();
            if (mc.Count == 0) continue;

            var ws = wb.Worksheets.Add(module.Length > 31 ? module[..31] : module);

            for (int i = 0; i < cols.Length; i++)
            {
                var c = ws.Cell(1, i + 1);
                c.Value = cols[i];
                c.Style.Font.Bold = true;
                c.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
                c.Style.Font.FontColor = XLColor.White;
            }

            int wr = 2;
            foreach (var tc in mc)
            {
                resultDict.TryGetValue(tc.Id, out var r);
                ws.Cell(wr, 1).Value = tc.Id;          ws.Cell(wr, 2).Value = tc.Submodule;
                ws.Cell(wr, 3).Value = tc.Scenario;    ws.Cell(wr, 4).Value = tc.Steps;
                ws.Cell(wr, 5).Value = tc.ExpectedResult; ws.Cell(wr, 6).Value = tc.Priority;
                ws.Cell(wr, 7).Value = r?.Status ?? "pending";
                ws.Cell(wr, 8).Value = r?.Notes  ?? string.Empty;
                ws.Cell(wr, 9).Value = r?.TestedAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;

                ws.Cell(wr, 7).Style.Fill.BackgroundColor = (r?.Status ?? "pending") switch
                {
                    "pass"    => XLColor.FromHtml("#E8F5E9"),
                    "fail"    => XLColor.FromHtml("#FFEBEE"),
                    "blocked" => XLColor.FromHtml("#FFF3E0"),
                    "skip"    => XLColor.FromHtml("#F5F5F5"),
                    _         => XLColor.White
                };
                wr++;
            }

            ws.Column(3).Width = 40;
            ws.Column(4).Width = 40;
            ws.Column(5).Width = 40;
            ws.Columns(1, 2).AdjustToContents();
            ws.Columns(6, 9).AdjustToContents();
        }

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        var name = $"CloudsferQA_{session.Tester.Split('@')[0]}_{session.Version.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPER
    // ═══════════════════════════════════════════════════════════════════════

    private async Task<User?> GetCurrentUserAsync()
    {
        if (!Request.Cookies.TryGetValue(AuthCookie, out var idStr)
            || !int.TryParse(idStr, out var id)) return null;
        var user = await _db.Users.FindAsync(id);
        return (user?.IsEmailVerified == true && user.IsActive) ? user : null;
    }
}

public sealed class SaveResultRequest
{
    public string  TestCaseId { get; set; } = string.Empty;
    public string  Status     { get; set; } = string.Empty;
    public string? Notes      { get; set; }
}
