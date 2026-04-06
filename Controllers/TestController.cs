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

        var testCases = await _db.TestCases.Where(t => !t.IsDeleted).ToListAsync();
        var results   = await _db.Results
            .Where(r => r.SessionId == sessionId)
            .ToListAsync();

        var moduleOrder    = await GetModuleOrderAsync(testCases.Select(t => t.Module).Distinct().ToList());
        var subOrderDict   = await _db.ModuleOrders
            .Where(m => m.ModuleName.Contains("__"))
            .ToDictionaryAsync(m => m.ModuleName, m => m.SortOrder);

        var orderedCases = moduleOrder
            .SelectMany(m => {
                var mCases = testCases.Where(tc => tc.Module == m);
                var subs   = mCases.Select(tc => tc.Submodule).Distinct()
                    .OrderBy(s => subOrderDict.TryGetValue(m + "__" + s, out var so) ? so : int.MaxValue)
                    .ThenBy(s => s)
                    .ToList();
                return subs.SelectMany(s => mCases.Where(tc => tc.Submodule == s).OrderBy(tc => tc.SortOrder).ThenBy(tc => tc.Id));
            })
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
    public async Task<IActionResult> Export(int sessionId, [FromQuery] List<string>? modules)
    {
        if (await GetCurrentUserAsync() == null) return RedirectToAction("Login", "Auth");

        var session = await _db.Sessions.FindAsync(sessionId);
        if (session == null) return NotFound();

        var allTestCases = await _db.TestCases.Where(t => !t.IsDeleted).ToListAsync();
        var results      = await _db.Results.Where(r => r.SessionId == sessionId).ToListAsync();
        var resultDict   = results.ToDictionary(r => r.TestCaseId);
        var fullOrder    = await GetModuleOrderAsync(allTestCases.Select(t => t.Module).Distinct().ToList());

        // Filter to selected modules (if none specified, export all)
        var exportModules = (modules != null && modules.Count > 0)
            ? fullOrder.Where(m => modules.Contains(m)).ToList()
            : fullOrder;

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
            ("Exported At:", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC"),
            ("Modules:",     string.Join(", ", exportModules))
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
            var c = sw.Cell(9, i + 1);
            c.Value = hdr[i];
            c.Style.Font.Bold = true;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            c.Style.Font.FontColor = XLColor.White;
        }

        int row = 10;
        foreach (var module in exportModules)
        {
            var mc = allTestCases.Where(tc => tc.Module == module).ToList();
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

        // ── Per-module sheets — grouped by submodule, alternating row colors ──
        string[] cols = { "TC ID", "Submodule", "Scenario", "Steps", "Expected Result", "Priority", "Status", "Notes", "Tested At" };
        var colorA = XLColor.FromHtml("#F5F5F5"); // light grey
        var colorB = XLColor.White;

        foreach (var module in exportModules)
        {
            var mc = allTestCases.Where(tc => tc.Module == module).OrderBy(tc => tc.Submodule).ThenBy(tc => tc.Id).ToList();
            if (mc.Count == 0) continue;

            var sheetName = module.Length > 31 ? module[..31] : module;
            var ws = wb.Worksheets.Add(sheetName);

            // Header row
            for (int i = 0; i < cols.Length; i++)
            {
                var c = ws.Cell(1, i + 1);
                c.Value = cols[i];
                c.Style.Font.Bold = true;
                c.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
                c.Style.Font.FontColor = XLColor.White;
            }

            int wr = 2;
            int submoduleIndex = 0;
            string? currentSubmodule = null;

            var submoduleGroups = mc.GroupBy(tc => tc.Submodule).OrderBy(g => g.Key).ToList();

            foreach (var subGroup in submoduleGroups)
            {
                // Alternating color per submodule group
                var rowColor = submoduleIndex % 2 == 0 ? colorA : colorB;
                submoduleIndex++;

                // Submodule label row
                var labelCell = ws.Cell(wr, 1);
                ws.Range(wr, 1, wr, cols.Length).Merge();
                labelCell.Value = $"  {subGroup.Key}";
                labelCell.Style.Font.Bold = true;
                labelCell.Style.Font.FontColor = XLColor.FromHtml("#1565C0");
                labelCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");
                labelCell.Style.Alignment.Indent = 1;
                wr++;

                foreach (var tc in subGroup)
                {
                    resultDict.TryGetValue(tc.Id, out var r);
                    ws.Cell(wr, 1).Value = tc.Id;
                    ws.Cell(wr, 2).Value = tc.Submodule;
                    ws.Cell(wr, 3).Value = tc.Scenario;
                    ws.Cell(wr, 4).Value = tc.Steps;
                    ws.Cell(wr, 5).Value = tc.ExpectedResult;
                    ws.Cell(wr, 6).Value = tc.Priority;
                    ws.Cell(wr, 7).Value = r?.Status ?? "pending";
                    ws.Cell(wr, 8).Value = r?.Notes  ?? string.Empty;
                    ws.Cell(wr, 9).Value = r?.TestedAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;

                    // Row background alternates per submodule group
                    for (int col = 1; col <= cols.Length; col++)
                        ws.Cell(wr, col).Style.Fill.BackgroundColor = rowColor;

                    // Override status cell color
                    ws.Cell(wr, 7).Style.Fill.BackgroundColor = (r?.Status ?? "pending") switch
                    {
                        "pass"    => XLColor.FromHtml("#C8E6C9"),
                        "fail"    => XLColor.FromHtml("#FFCDD2"),
                        "blocked" => XLColor.FromHtml("#FFE0B2"),
                        "skip"    => XLColor.FromHtml("#ECEFF1"),
                        _         => XLColor.FromHtml("#FFF9C4")
                    };
                    wr++;
                }

                // Empty separator row between submodule groups
                wr++;
            }

            ws.Column(3).Width = 45;
            ws.Column(4).Width = 40;
            ws.Column(5).Width = 40;
            ws.Columns(1, 2).AdjustToContents();
            ws.Columns(6, 9).AdjustToContents();
        }

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        var suffix = exportModules.Count == fullOrder.Count ? "AllModules" : $"{exportModules.Count}Modules";
        var name = $"CloudsferQA_{session.Tester.Split('@')[0]}_{session.Version.Replace(" ", "_")}_{suffix}_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(int sessionId, [FromQuery] List<string>? modules)
    {
        if (await GetCurrentUserAsync() == null) return RedirectToAction("Login", "Auth");

        var session = await _db.Sessions.FindAsync(sessionId);
        if (session == null) return NotFound();

        var allTestCases = await _db.TestCases.Where(t => !t.IsDeleted).ToListAsync();
        var results      = await _db.Results.Where(r => r.SessionId == sessionId).ToListAsync();
        var resultDict   = results.ToDictionary(r => r.TestCaseId);
        var fullOrder    = await GetModuleOrderAsync(allTestCases.Select(t => t.Module).Distinct().ToList());

        var exportModules = (modules != null && modules.Count > 0)
            ? fullOrder.Where(m => modules.Contains(m)).ToList()
            : fullOrder;

        static string CsvCell(string? v) =>
            v == null ? "" : v.Contains(',') || v.Contains('"') || v.Contains('\n')
                ? $"\"{v.Replace("\"", "\"\"")}\"" : v;

        var sb = new System.Text.StringBuilder();

        // File header info
        sb.AppendLine($"CloudsferQA Test Session Export");
        sb.AppendLine($"Tester,{CsvCell(session.Tester)}");
        sb.AppendLine($"Version,{CsvCell(session.Version)}");
        sb.AppendLine($"Environment,{CsvCell(session.Environment)}");
        sb.AppendLine($"Started At,{session.StartedAt:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine($"Exported At,{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine($"Modules,{CsvCell(string.Join(" | ", exportModules))}");
        sb.AppendLine();

        // Column headers
        sb.AppendLine("TC ID,Module,Submodule,Scenario,Steps,Expected Result,Priority,Status,Notes,Tested At");

        foreach (var module in exportModules)
        {
            var mc = allTestCases.Where(tc => tc.Module == module)
                                 .OrderBy(tc => tc.Submodule).ThenBy(tc => tc.Id).ToList();
            if (mc.Count == 0) continue;

            foreach (var tc in mc)
            {
                resultDict.TryGetValue(tc.Id, out var r);
                sb.AppendLine(string.Join(",",
                    CsvCell(tc.Id),
                    CsvCell(tc.Module),
                    CsvCell(tc.Submodule),
                    CsvCell(tc.Scenario),
                    CsvCell(tc.Steps),
                    CsvCell(tc.ExpectedResult),
                    CsvCell(tc.Priority),
                    CsvCell(r?.Status ?? "pending"),
                    CsvCell(r?.Notes ?? ""),
                    CsvCell(r?.TestedAt?.ToString("yyyy-MM-dd HH:mm") ?? "")
                ));
            }
        }

        var suffix = exportModules.Count == fullOrder.Count ? "AllModules" : $"{exportModules.Count}Modules";
        var name = $"CloudsferQA_{session.Tester.Split('@')[0]}_{session.Version.Replace(" ", "_")}_{suffix}_{DateTime.UtcNow:yyyyMMdd}.csv";
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", name);
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
