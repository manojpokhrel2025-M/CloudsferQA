using CloudsferQA.Data;
using CloudsferQA.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CloudsferQA.Services;

public class StatsService
{
    private readonly AppDbContext _db;

    private static readonly List<string> ModuleOrder = new()
    {
        "Migration", "Backup", "Restore", "Emails & Notifications",
        "Registration & Onboarding", "OPA (On-Premise Agent)",
        "Task Bar", "Administrator", "QA & Dev"
    };

    public StatsService(AppDbContext db) => _db = db;

    public async Task<SessionStatsDto?> GetStatsAsync(int sessionId)
    {
        var session = await _db.Sessions.FindAsync(sessionId);
        if (session == null) return null;

        var allTestCases = await _db.TestCases.ToListAsync();
        var results      = await _db.Results
            .Where(r => r.SessionId == sessionId)
            .ToListAsync();

        var resultDict = results.ToDictionary(r => r.TestCaseId);

        // ── Per-module stats in defined order ────────────────────────────
        var moduleStats = new List<ModuleStatDto>();
        foreach (var module in ModuleOrder)
        {
            var cases = allTestCases.Where(tc => tc.Module == module).ToList();
            if (cases.Count == 0) continue;

            int mPass = 0, mFail = 0, mBlocked = 0, mSkip = 0, mPending = 0;
            foreach (var tc in cases)
            {
                if (resultDict.TryGetValue(tc.Id, out var r))
                    Tally(r.Status, ref mPass, ref mFail, ref mBlocked, ref mSkip, ref mPending);
                else
                    mPending++;
            }

            moduleStats.Add(new ModuleStatDto
            {
                Module   = module,
                Total    = cases.Count,
                Pass     = mPass,
                Fail     = mFail,
                Blocked  = mBlocked,
                Skip     = mSkip,
                Pending  = mPending,
                PassRate = cases.Count > 0
                    ? Math.Round((double)mPass / cases.Count * 100, 1) : 0
            });
        }

        // ── Overall counts ────────────────────────────────────────────────
        int total = allTestCases.Count;
        int pass = 0, fail = 0, blocked = 0, skip = 0, pending = 0;

        foreach (var tc in allTestCases)
        {
            if (resultDict.TryGetValue(tc.Id, out var r))
                Tally(r.Status, ref pass, ref fail, ref blocked, ref skip, ref pending);
            else
                pending++;
        }

        // ── Priority breakdown ────────────────────────────────────────────
        var priorities = new[] { "High", "Medium", "Low" };
        var priorityStats = priorities.Select(p =>
        {
            var cases = allTestCases.Where(tc => tc.Priority == p).ToList();
            int pp = 0, pf = 0, pb = 0, ps = 0, ppend = 0;

            foreach (var tc in cases)
            {
                if (resultDict.TryGetValue(tc.Id, out var r))
                    Tally(r.Status, ref pp, ref pf, ref pb, ref ps, ref ppend);
                else
                    ppend++;
            }

            return new PriorityStatDto
            {
                Priority = p,
                Pass     = pp,
                Fail     = pf,
                Blocked  = pb,
                Skip     = ps,
                Pending  = ppend
            };
        }).ToList();

        // ── Recent activity — last 20 results with a timestamp ───────────
        var recentActivity = results
            .Where(r => r.TestedAt.HasValue)
            .OrderByDescending(r => r.TestedAt)
            .Take(20)
            .Select(r =>
            {
                var tc = allTestCases.FirstOrDefault(t => t.Id == r.TestCaseId);
                return new RecentActivityDto
                {
                    Scenario  = tc?.Scenario ?? r.TestCaseId,
                    Module    = tc?.Module   ?? string.Empty,
                    Status    = r.Status,
                    TestedAt  = r.TestedAt
                };
            }).ToList();

        // ── Failed and blocked cases ──────────────────────────────────────
        var failedAndBlocked = results
            .Where(r => r.Status == "fail" || r.Status == "blocked")
            .Select(r =>
            {
                var tc = allTestCases.FirstOrDefault(t => t.Id == r.TestCaseId);
                return new FailedCaseDto
                {
                    TestCaseId = r.TestCaseId,
                    Scenario   = tc?.Scenario ?? r.TestCaseId,
                    Module     = tc?.Module   ?? string.Empty,
                    Status     = r.Status
                };
            }).ToList();

        return new SessionStatsDto
        {
            SessionId         = session.Id,
            Tester            = session.Tester,
            Version           = session.Version,
            Environment       = session.Environment,
            StartedAt         = session.StartedAt,
            Total             = total,
            Pass              = pass,
            Fail              = fail,
            Blocked           = blocked,
            Skip              = skip,
            Pending           = pending,
            PassRate          = total > 0 ? Math.Round((double)pass / total * 100, 1) : 0,
            Modules           = moduleStats,
            PriorityBreakdown = priorityStats,
            RecentActivity    = recentActivity,
            FailedAndBlocked  = failedAndBlocked
        };
    }

    /// <summary>
    /// Increment one of the six status counters based on the status string.
    /// Uses local variable refs — safe to call with local int variables only.
    /// </summary>
    private static void Tally(
        string status,
        ref int pass, ref int fail, ref int blocked, ref int skip, ref int pending)
    {
        switch (status)
        {
            case "pass":    pass++;    break;
            case "fail":    fail++;    break;
            case "blocked": blocked++; break;
            case "skip":    skip++;    break;
            default:        pending++; break;
        }
    }
}
