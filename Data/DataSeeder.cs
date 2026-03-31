using System.Text.Json;
using System.Text.Json.Serialization;
using CloudsferQA.Helpers;
using CloudsferQA.Models;

namespace CloudsferQA.Data;

public static class DataSeeder
{
    public static void Seed(AppDbContext db, string webRootPath, IConfiguration config)
    {
        SeedTestCases(db, webRootPath);
        UpsertNewTestCases(db);
        SeedAdminUser(db, config);
        SeedDemoSession(db);
    }

    // ── Test cases from JSON ────────────────────────────────────────────────
    private static void SeedTestCases(AppDbContext db, string webRootPath)
    {
        if (db.TestCases.Any()) return;

        var jsonPath = Path.Combine(webRootPath, "data", "testcases.json");
        if (!File.Exists(jsonPath)) return;

        var json    = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var items   = JsonSerializer.Deserialize<List<TestCaseJson>>(json, options);
        if (items == null || items.Count == 0) return;

        db.TestCases.AddRange(items.Select(i => new TestCase
        {
            Id             = i.Id,
            Module         = i.Module,
            Submodule      = i.Submodule,
            Scenario       = i.Scenario,
            Steps          = i.Steps          ?? string.Empty,
            ExpectedResult = i.ExpectedResult ?? string.Empty,
            Priority       = i.Priority
        }));
        db.SaveChanges();
    }

    // ── Upsert new test cases from release notes (v3.39.0.14 / v3.39.0.15) ──
    private static void UpsertNewTestCases(AppDbContext db)
    {
        var newCases = new List<TestCase>
        {
            // ── Pre-Validation ──────────────────────────────────────────────
            new() { Id="PRV-001", Module="Pre-Validation", Submodule="Upload CSV Flow",
                Scenario="Wizard opens with 3 steps: Download, Upload, Update",
                Steps="1. Log in\n2. Open BIM360 Backup source\n3. Enable Auto Backup",
                ExpectedResult="Three steps visible: Download · Upload (Upload CSV + Validation) · Update",
                Priority="High" },

            new() { Id="PRV-002", Module="Pre-Validation", Submodule="Upload CSV Flow",
                Scenario="Step 1 Download CSV includes all active Admin + Member projects; archived excluded",
                Steps="1. Click Download in Step 1\n2. Inspect CSV content",
                ExpectedResult="All active Admin + Member projects listed; archived projects absent",
                Priority="High" },

            new() { Id="PRV-003", Module="Pre-Validation", Submodule="Navigation Guards",
                Scenario="Next button blocked when no CSV uploaded (Bug 43052)",
                Steps="1. Open Upload CSV page\n2. Do NOT upload a file\n3. Click Next",
                ExpectedResult="Next button disabled or shows warning; user cannot advance without CSV",
                Priority="High" },

            new() { Id="PRV-004", Module="Pre-Validation", Submodule="Navigation Guards",
                Scenario="Clicking outside Upload CSV modal does not close it (Bug 43052)",
                Steps="1. Open Upload CSV modal\n2. Click anywhere outside the modal boundary",
                ExpectedResult="Modal stays open; outside click is ignored",
                Priority="High" },

            new() { Id="PRV-005", Module="Pre-Validation", Submodule="Navigation Guards",
                Scenario="Upload CSV and Remove buttons absent on Page 2 (Bug 43058)",
                Steps="1. Complete validation on Page 1\n2. Click Next\n3. Inspect Page 2 buttons",
                ExpectedResult="Page 2 shows only Validate · Back · Close · Finish – no Upload CSV or Remove buttons",
                Priority="High" },

            new() { Id="PRV-006", Module="Pre-Validation", Submodule="Validation Process",
                Scenario="Validation runs in batches of ~20 with live count updates",
                Steps="1. Upload CSV with many projects\n2. Observe validation process",
                ExpectedResult="~20 projects per batch; Total / Validated / Attention Required counts update live; Analyzing... shown per project",
                Priority="High" },

            new() { Id="PRV-007", Module="Pre-Validation", Submodule="Validation Process",
                Scenario="Color coding: Green = Validated, Red = Attention Required, Grey = Total; Total = V + AR",
                Steps="1. Upload CSV with mixed Admin/Member projects\n2. Run validation",
                ExpectedResult="Color coding correct; Total count equals Validated plus Attention Required",
                Priority="High" },

            new() { Id="PRV-008", Module="Pre-Validation", Submodule="Validation Process",
                Scenario="Close button stops validation mid-process",
                Steps="1. Start validation\n2. Click Close while validation is running",
                ExpectedResult="Validation halts immediately; modal closes",
                Priority="Medium" },

            new() { Id="PRV-009", Module="Pre-Validation", Submodule="Validation Process",
                Scenario="Re-uploading a CSV resets and starts fresh validation",
                Steps="1. Complete a validation\n2. Upload a new CSV file",
                ExpectedResult="Previous data cleared; validation restarts for new file",
                Priority="Medium" },

            new() { Id="PRV-010", Module="Pre-Validation", Submodule="Step Indicator",
                Scenario="Step 2 does NOT show green tick when validation cancelled mid-way (Bug 43082)",
                Steps="1. Upload CSV, start validation\n2. Close modal before completion\n3. Reopen wizard, observe Step 2 indicator",
                ExpectedResult="Step 2 shows incomplete/in-progress state; NOT green tick",
                Priority="High" },

            new() { Id="PRV-011", Module="Pre-Validation", Submodule="Step Indicator",
                Scenario="Validation page opens in default Total Projects view on reopen (Bug 43106)",
                Steps="1. Complete a validation session\n2. Close\n3. Reopen Step 2",
                ExpectedResult="Page opens at Total Projects view; previous filter/state is NOT restored",
                Priority="High" },

            new() { Id="PRV-012", Module="Pre-Validation", Submodule="Data Integrity",
                Scenario="Last project not duplicated in validation result table (Bug 43090)",
                Steps="1. Upload CSV with multiple projects\n2. Complete validation\n3. Review result table",
                ExpectedResult="Every project appears exactly once; no duplicate at end of list",
                Priority="High" },

            new() { Id="PRV-013", Module="Pre-Validation", Submodule="Data Integrity",
                Scenario="Clear error message shown for corrupted CSV upload (Bug 43100)",
                Steps="1. Prepare a corrupt/malformed CSV file\n2. Upload in Step 2",
                ExpectedResult="Descriptive error shown; validation does not silently fail or crash",
                Priority="High" },

            new() { Id="PRV-014", Module="Pre-Validation", Submodule="Data Integrity",
                Scenario="Upload button disabled during background processing after cancel (Bug 43105)",
                Steps="1. Start uploading a file\n2. Cancel mid-upload\n3. Immediately try Upload again",
                ExpectedResult="Upload button stays disabled until background chunk processing fully stops",
                Priority="High" },

            new() { Id="PRV-015", Module="Pre-Validation", Submodule="Page 2 Attention Required",
                Scenario="Blocked projects listed with correct attention message on Page 2",
                Steps="1. Upload CSV with Member-only projects\n2. Complete validation\n3. Click Next",
                ExpectedResult="Message: 'Listed projects require attention – connected account must have project administrator access.'",
                Priority="High" },

            new() { Id="PRV-016", Module="Pre-Validation", Submodule="Page 2 Attention Required",
                Scenario="'Please see the guide' is clickable with hover tooltip showing URL (Task 43187)",
                Steps="1. Open Page 2\n2. Hover over 'Please see the guide'\n3. Click the link",
                ExpectedResult="Tooltip shows target URL on hover; click opens correct HubSpot guide in new tab",
                Priority="Medium" },

            new() { Id="PRV-017", Module="Pre-Validation", Submodule="Page 2 Attention Required",
                Scenario="Finish button greyed out while Attention Required > 0",
                Steps="1. Complete validation with blocked projects remaining\n2. Check Finish button on Page 2",
                ExpectedResult="Finish button disabled; cannot finish while any projects need attention",
                Priority="High" },

            new() { Id="PRV-018", Module="Pre-Validation", Submodule="Page 2 Attention Required",
                Scenario="Finish button activates when Attention Required = 0",
                Steps="1. Resolve all blocked project permissions in BIM360\n2. Click Validate on Page 2",
                ExpectedResult="After Attention Required drops to 0, Finish button becomes active",
                Priority="High" },

            new() { Id="PRV-019", Module="Pre-Validation", Submodule="UI Fixes",
                Scenario="'Manual Access - Attention Required!' text is correct with no typos (Task 43085)",
                Steps="1. Navigate to Pre-Validation manual access section",
                ExpectedResult="Text displays correctly without any typos",
                Priority="Medium" },

            new() { Id="PRV-020", Module="Pre-Validation", Submodule="UI Fixes",
                Scenario="Support@cloudsfer.com is a clickable mailto link (Task 43085)",
                Steps="1. Locate support email in Pre-Validation page\n2. Click it",
                ExpectedResult="Default mail client opens with support@cloudsfer.com pre-filled",
                Priority="Medium" },

            new() { Id="PRV-021", Module="Pre-Validation", Submodule="UI Fixes",
                Scenario="Finish button is light green not dark green (Task 43085)",
                Steps="1. Open Upload CSV Page 2\n2. Observe Finish button color",
                ExpectedResult="Finish button uses light green color",
                Priority="Low" },

            new() { Id="PRV-022", Module="Pre-Validation", Submodule="Status Indication",
                Scenario="'Updated' status has same visual indicator style as 'Approved' (Task 43169)",
                Steps="1. Update projects via Step 3 Update CSV\n2. Compare Updated vs Approved indicator",
                ExpectedResult="Updated projects show same checkmark/indicator style as Approved; no missing indicator",
                Priority="Medium" },

            new() { Id="PRV-023", Module="Pre-Validation", Submodule="Greyed-Out Projects",
                Scenario="'Create Backup' option hidden for greyed-out projects (Task 43036)",
                Steps="1. Find a project greyed out due to no admin permission\n2. Check options menu",
                ExpectedResult="Create Backup option is hidden for greyed-out projects",
                Priority="High" },

            new() { Id="PRV-024", Module="Pre-Validation", Submodule="Greyed-Out Projects",
                Scenario="Greyed-out project cannot be expanded in tree view (Bug 43009)",
                Steps="1. Find a greyed-out project\n2. Try to expand it",
                ExpectedResult="Expansion blocked; tree does not open for greyed-out projects",
                Priority="High" },

            new() { Id="PRV-025", Module="Pre-Validation", Submodule="Greyed-Out Projects",
                Scenario="Unselected blocked projects excluded from migration (Bug 42986)",
                Steps="1. Have blocked projects unselected in UI\n2. Start migration\n3. Check executions and actions",
                ExpectedResult="Blocked projects absent from execution; no actions created for them",
                Priority="High" },

            // ── CF Grid Auto Refresh ────────────────────────────────────────
            new() { Id="CFR-001", Module="CF Grid Auto Refresh", Submodule="Refresh Interval",
                Scenario="Refresh interval is driven by Config file value, not hardcoded (Task 42700)",
                Steps="1. Set custom interval in Config file\n2. Load CF homepage\n3. Observe grid refresh timing",
                ExpectedResult="DataGrid refreshes at the configured interval",
                Priority="High" },

            new() { Id="CFR-002", Module="CF Grid Auto Refresh", Submodule="Grid Columns",
                Scenario="All expected columns present in CF homepage grid",
                Steps="1. Load CF homepage\n2. Observe all visible columns",
                ExpectedResult="Columns: Title · Source Path · Target Path · From-To icons · Creation Date · Status · Quick Action · Backup Action · Actions",
                Priority="High" },

            new() { Id="CFR-003", Module="CF Grid Auto Refresh", Submodule="Refresh Interval",
                Scenario="Migration status reflects latest state after each refresh cycle without manual reload",
                Steps="1. Start a migration\n2. Wait for configured refresh interval\n3. Check status on homepage",
                ExpectedResult="Status updates (e.g. Running → Completed) without manual page reload",
                Priority="High" },

            new() { Id="CFR-004", Module="CF Grid Auto Refresh", Submodule="Grid Columns",
                Scenario="Source Path, Target Path, and connector icons display correctly per plan",
                Steps="1. Create plans with various sources/targets\n2. Check grid",
                ExpectedResult="Correct paths and icons shown for each plan",
                Priority="Medium" },

            new() { Id="CFR-005", Module="CF Grid Auto Refresh", Submodule="Grid Columns",
                Scenario="Status column covers all migration/backup states",
                Steps="1. Check plans across all possible states",
                ExpectedResult="Not Started · Scheduled · Running · Completed · Completed with Error · Paused all display correctly",
                Priority="High" },

            new() { Id="CFR-006", Module="CF Grid Auto Refresh", Submodule="Grid Columns",
                Scenario="Correct Quick Actions shown per migration state",
                Steps="1. Check Quick Actions column for each migration state",
                ExpectedResult="Start / Resume / Delta / Retry shown appropriately per state",
                Priority="Medium" },

            new() { Id="CFR-007", Module="CF Grid Auto Refresh", Submodule="Refresh Interval",
                Scenario="Auto Refresh behavior on Backup home page",
                Steps="1. Go to Backup home page\n2. Observe refresh behavior",
                ExpectedResult="If implemented: refreshes at config interval. If not yet: document as Not Yet Implemented",
                Priority="Low" },

            // ── Administrator – Report Summaries & Dashboard ────────────────
            new() { Id="ADR-001", Module="Administrator", Submodule="Monthly Report Summaries",
                Scenario="Initial load of Reports page shows empty state until filter applied",
                Steps="1. Navigate to Admin > Reports",
                ExpectedResult="No data / empty state on first landing",
                Priority="Medium" },

            new() { Id="ADR-002", Module="Administrator", Submodule="Monthly Report Summaries",
                Scenario="Filter Reset clears all filters and restores full list",
                Steps="1. Apply multiple filters\n2. Click Filter Reset",
                ExpectedResult="All filters cleared; full summary data shown",
                Priority="Medium" },

            new() { Id="ADR-003", Module="Administrator", Submodule="Monthly Report Summaries",
                Scenario="Year filter shows ONLY results from selected year (Bug 42975)",
                Steps="1. Select Year = 2025\n2. Apply filter\n3. Review all results",
                ExpectedResult="Every result belongs to 2025 only; zero results from any other year",
                Priority="High" },

            new() { Id="ADR-004", Module="Administrator", Submodule="Monthly Report Summaries",
                Scenario="Year + Month filter returns correct year, not one year earlier (Bug 42976)",
                Steps="1. Select Year = 2025, Month = January\n2. Apply\n3. Check result dates",
                ExpectedResult="Results are January 2025 – NOT January 2024",
                Priority="High" },

            new() { Id="ADR-005", Module="Administrator", Submodule="Monthly Report Summaries",
                Scenario="No infinite loading when filter returns zero results (Bug 42974)",
                Steps="1. Select a filter combination with no activity (e.g. future month)\n2. Apply",
                ExpectedResult="Empty state shown immediately; page does NOT load indefinitely",
                Priority="High" },

            new() { Id="ADR-006", Module="Administrator", Submodule="Monthly Report Summaries",
                Scenario="Download Report exports only currently filtered data, not all records (Task 43007)",
                Steps="1. Apply filters to narrow results\n2. Click Download Report\n3. Open downloaded file",
                ExpectedResult="File contains ONLY rows matching the active filter, not the entire database",
                Priority="High" },

            new() { Id="ADR-007", Module="Administrator", Submodule="Monthly Report Summaries",
                Scenario="Monthly Report table shows only columns selected in 'Fields' (Task 43008)",
                Steps="1. In Select Fields, choose specific fields\n2. Apply",
                ExpectedResult="Table shows only selected fields; unselected fields are hidden",
                Priority="High" },

            new() { Id="ADR-008", Module="Administrator", Submodule="Monthly Report Summaries",
                Scenario="Future months/years not available in date dropdowns (Task 43034)",
                Steps="1. Open Year and Month dropdowns in Report Summary",
                ExpectedResult="Dates beyond current month/year are hidden or disabled",
                Priority="Medium" },

            new() { Id="ADR-009", Module="Administrator", Submodule="Monthly Report Summaries",
                Scenario="Multiple months can be selected simultaneously in Report Summary (Task 42673)",
                Steps="1. Open month filter\n2. Select multiple months\n3. Apply",
                ExpectedResult="Results include data for all selected months combined",
                Priority="Medium" },

            new() { Id="ADR-010", Module="Administrator", Submodule="Analytical Report",
                Scenario="Bar graph renders correctly using highcharts (Task 42669)",
                Steps="1. Navigate to Analytical Reports section",
                ExpectedResult="Graph renders; axes and labels visible; no JS console errors",
                Priority="High" },

            new() { Id="ADR-011", Module="Administrator", Submodule="Analytical Report",
                Scenario="Selecting a single field updates bar graph correctly",
                Steps="1. Select a single field in Analytical Report\n2. Observe graph",
                ExpectedResult="Graph reflects single field data accurately",
                Priority="Medium" },

            new() { Id="ADR-012", Module="Administrator", Submodule="Analytical Report",
                Scenario="Multiple fields shown in distinct colors with legend",
                Steps="1. Select 2 or more fields",
                ExpectedResult="Each field uses a distinct color; legend is visible and accurate",
                Priority="Medium" },

            new() { Id="ADR-013", Module="Administrator", Submodule="Analytical Report",
                Scenario="Hovering over a bar shows its data value in a tooltip",
                Steps="1. Hover over any bar in the graph",
                ExpectedResult="Tooltip displays the numeric value for that bar",
                Priority="Low" },

            new() { Id="ADR-014", Module="Administrator", Submodule="Admin Dashboard",
                Scenario="Scheduled/Queued Migration + Backup counts correct on APP4 Dashboard (Bug 42979)",
                Steps="1. Create a known number of scheduled/queued items\n2. Check Dashboard counts",
                ExpectedResult="Dashboard count exactly matches actual items in DB; no discrepancy",
                Priority="High" },

            new() { Id="ADR-015", Module="Administrator", Submodule="Admin Dashboard",
                Scenario="Admin Dashboard reflects real-time state (Bug 43004)",
                Steps="1. Create or complete a migration\n2. Refresh Dashboard",
                ExpectedResult="Counts update to reflect current actual state",
                Priority="High" },

            new() { Id="ADR-016", Module="Administrator", Submodule="Admin Dashboard",
                Scenario="New backup tiles present: Running / Scheduled / Queued / Paused / Running Auto Backups (Task 42954)",
                Steps="1. Go to Admin Dashboard\n2. Inspect Backup section",
                ExpectedResult="All five backup tiles visible with accurate counts",
                Priority="High" },

            // ── Backup – new from release notes ────────────────────────────
            new() { Id="BAK-010", Module="Backup", Submodule="Backup Scheduling",
                Scenario="Backup does not start without Next Backup Run Time on Standard Plan weekly cron (Bug 43184)",
                Steps="1. Subscribe to Standard Plan\n2. Create Backup with weekly cron\n3. Skip Next Backup Run Time selection\n4. Save",
                ExpectedResult="Backup does not start; validation prompts user to select a run time",
                Priority="High" },

            new() { Id="BAK-011", Module="Backup", Submodule="Backup Scheduling",
                Scenario="Auto Backup does not start without Next Backup Run Time on Standard Plan weekly cron (Bug 43184)",
                Steps="1. Standard Plan\n2. Create Auto Backup with weekly cron\n3. Skip run time selection\n4. Save",
                ExpectedResult="Auto Backup does not start; error/validation shown requiring time selection",
                Priority="High" },

            new() { Id="BAK-012", Module="Backup", Submodule="Backup Execution",
                Scenario="UI option to start first backup execution immediately (Task 42719)",
                Steps="1. Create a new Backup plan\n2. Find and click Run Now / Start Now option",
                ExpectedResult="Backup starts immediately; visible in Running Backups; does not wait for scheduled time",
                Priority="High" },

            new() { Id="BAK-013", Module="Backup", Submodule="Webhooks",
                Scenario="Admin Webhooks page loads without issue with 100+ entries (Task 42894)",
                Steps="1. Open Admin > Webhooks when there are 100+ entries",
                ExpectedResult="Page loads successfully; no timeout or freeze",
                Priority="High" },

            new() { Id="BAK-014", Module="Backup", Submodule="Webhooks",
                Scenario="Webhooks processed reliably with no stuck messages (Bug 42895)",
                Steps="1. Trigger many webhook events\n2. Monitor Admin UI over time",
                ExpectedResult="All events processed; nothing accumulates in Not Processed state",
                Priority="High" },

            new() { Id="BAK-015", Module="Backup", Submodule="Webhooks",
                Scenario="Multiple webhooks deleted in a single request from Admin (Task 42707)",
                Steps="1. Select 2 or more webhooks in Admin\n2. Delete all selected",
                ExpectedResult="All selected webhooks deleted in one operation; UI refreshes correctly",
                Priority="Medium" },

            new() { Id="BAK-016", Module="Backup", Submodule="Webhooks",
                Scenario="Webhook message handlers moved to MaaS.Backend.Webhooks (Task 43087)",
                Steps="1. Trigger webhook events\n2. Monitor processing across services",
                ExpectedResult="Events handled by MaaS.Backend.Webhooks; no duplicates or missed processing",
                Priority="High" },

            new() { Id="BAK-017", Module="Backup", Submodule="Token Refresh",
                Scenario="New token used when registering webhooks and getting top BIM folder (Bug 42927)",
                Steps="1. Allow token to near expiry\n2. Register webhooks for a BIM project\n3. Monitor auth logs",
                ExpectedResult="Refreshed token used; no 401 errors; webhook registration and top folder retrieval succeed",
                Priority="High" },

            new() { Id="BAK-018", Module="Backup", Submodule="Token Refresh",
                Scenario="Token refreshed automatically during hook registration flow (Task 42909)",
                Steps="1. Simulate token expiry mid-registration",
                ExpectedResult="Token refreshed silently; registration completes without user re-authentication",
                Priority="High" },

            new() { Id="BAK-019", Module="Backup", Submodule="SPO Metadata",
                Scenario="Created By and Modified By preserved when migrating SPO-Graph → SPO (v3.39.0.15)",
                Steps="1. Migrate SPO-Graph source → SPO target\n2. Check target file metadata after completion",
                ExpectedResult="Target files retain original Created By and Modified By; not overwritten by service account",
                Priority="High" },

            new() { Id="BAK-020", Module="Backup", Submodule="SPO Metadata",
                Scenario="Metadata preservation applies only to Graph source, not classic SPO source (v3.39.0.15)",
                Steps="1. Migrate classic SPO → SPO\n2. Compare metadata behavior with Graph source migration",
                ExpectedResult="Classic SPO source behavior unchanged; feature limited to Graph source only",
                Priority="Medium" },
        };

        var existingIds = db.TestCases.Select(t => t.Id).ToHashSet();
        var toAdd = newCases.Where(tc => !existingIds.Contains(tc.Id)).ToList();
        if (toAdd.Count == 0) return;

        db.TestCases.AddRange(toAdd);
        db.SaveChanges();
        Console.WriteLine($"[DataSeeder] Added {toAdd.Count} new test cases from v3.39.0.14/v3.39.0.15.");
    }

    // ── Admin user ──────────────────────────────────────────────────────────
    private static void SeedAdminUser(AppDbContext db, IConfiguration config)
    {
        if (db.Users.Any(u => u.Role == "Admin")) return;

        var email    = config["Admin:Email"]    ?? "admin@tzunami.com";
        var password = config["Admin:Password"] ?? "Admin@2024!";

        db.Users.Add(new User
        {
            Email             = email.ToLowerInvariant(),
            PasswordHash      = PasswordHelper.Hash(password),
            Role              = "Admin",
            IsEmailVerified   = true,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow,
            VerifiedAt        = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    // ── Demo session so dashboard has data on first run ────────────────────
    private static void SeedDemoSession(AppDbContext db)
    {
        if (db.Sessions.Any()) return;

        var admin = db.Users.FirstOrDefault(u => u.Role == "Admin");
        if (admin == null) return;

        var allCases = db.TestCases.ToList();
        if (allCases.Count == 0) return;

        var session = new TestSession
        {
            UserId      = admin.Id,
            Tester      = admin.Email,
            Version     = "v3.41.0",
            Environment = "APP4",
            StartedAt   = DateTime.UtcNow.AddHours(-2)
        };
        db.Sessions.Add(session);
        db.SaveChanges();

        var rng = new Random(42);
        var statuses = new[] { "pass", "pass", "pass", "fail", "blocked", "skip" };
        var now = DateTime.UtcNow;

        var results = allCases.Take(50).Select((tc, i) => new TestResult
        {
            SessionId  = session.Id,
            TestCaseId = tc.Id,
            Status     = statuses[rng.Next(statuses.Length)],
            Notes      = "",
            TestedAt   = now.AddMinutes(-rng.Next(1, 120))
        }).ToList();

        db.Results.AddRange(results);
        db.SaveChanges();
    }

    // ── DTO for JSON deserialization ────────────────────────────────────────
    private sealed class TestCaseJson
    {
        public string  Id        { get; set; } = string.Empty;
        public string  Module    { get; set; } = string.Empty;
        public string  Submodule { get; set; } = string.Empty;
        public string  Scenario  { get; set; } = string.Empty;
        public string? Steps     { get; set; }

        [JsonPropertyName("expected_result")]
        public string? ExpectedResult { get; set; }

        public string Priority { get; set; } = string.Empty;
    }
}
