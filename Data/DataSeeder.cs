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
        UpsertReportTestCases(db);
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
            // ── Browse Environments ─────────────────────────────────────────
            // Production (app.cloudsfer.com)
            new() { Id="BRW-001", Module="Browse Environments", Submodule="Production",
                Scenario="Production environment URL loads without error",
                Steps="1. Open browser\n2. Navigate to https://app.cloudsfer.com",
                ExpectedResult="Page loads successfully with HTTP 200; no error page or timeout",
                Priority="High" },

            new() { Id="BRW-002", Module="Browse Environments", Submodule="Production",
                Scenario="Login redirects correctly on Production",
                Steps="1. Navigate to https://app.cloudsfer.com\n2. Attempt to access a protected route without logging in",
                ExpectedResult="User is redirected to the login page; correct domain retained in redirect URL",
                Priority="High" },

            new() { Id="BRW-003", Module="Browse Environments", Submodule="Production",
                Scenario="Correct URL/domain confirmed on Production",
                Steps="1. Navigate to https://app.cloudsfer.com\n2. Inspect browser address bar and any API calls",
                ExpectedResult="All requests use app.cloudsfer.com; no cross-environment domain leakage",
                Priority="High" },

            new() { Id="BRW-004", Module="Browse Environments", Submodule="Production",
                Scenario="Core UI elements render correctly on Production",
                Steps="1. Navigate to https://app.cloudsfer.com\n2. Log in\n3. Inspect header, sidebar, dashboard",
                ExpectedResult="Header, navigation sidebar, and dashboard tiles all render without layout breaks or missing assets",
                Priority="High" },

            // APP4 (Testing)
            new() { Id="BRW-005", Module="Browse Environments", Submodule="APP4 - Testing",
                Scenario="APP4 testing environment URL loads without error",
                Steps="1. Open browser\n2. Navigate to https://app4.cloudsfer.com",
                ExpectedResult="Page loads successfully with HTTP 200; no error page or timeout",
                Priority="High" },

            new() { Id="BRW-006", Module="Browse Environments", Submodule="APP4 - Testing",
                Scenario="Login redirects correctly on APP4",
                Steps="1. Navigate to https://app4.cloudsfer.com\n2. Attempt to access a protected route without logging in",
                ExpectedResult="User is redirected to the login page; correct APP4 domain retained in redirect URL",
                Priority="High" },

            new() { Id="BRW-007", Module="Browse Environments", Submodule="APP4 - Testing",
                Scenario="Correct URL/domain confirmed on APP4",
                Steps="1. Navigate to https://app4.cloudsfer.com\n2. Inspect browser address bar and any API calls",
                ExpectedResult="All requests use app4.cloudsfer.com; no cross-environment domain leakage",
                Priority="High" },

            new() { Id="BRW-008", Module="Browse Environments", Submodule="APP4 - Testing",
                Scenario="Core UI elements render correctly on APP4",
                Steps="1. Navigate to https://app4.cloudsfer.com\n2. Log in\n3. Inspect header, sidebar, dashboard",
                ExpectedResult="Header, navigation sidebar, and dashboard tiles all render without layout breaks or missing assets",
                Priority="High" },

            // APP3 (Pre-Production)
            new() { Id="BRW-009", Module="Browse Environments", Submodule="APP3 - Pre-Production",
                Scenario="APP3 pre-production environment URL loads without error",
                Steps="1. Open browser\n2. Navigate to https://app3.cloudsfer.com",
                ExpectedResult="Page loads successfully with HTTP 200; no error page or timeout",
                Priority="High" },

            new() { Id="BRW-010", Module="Browse Environments", Submodule="APP3 - Pre-Production",
                Scenario="Login redirects correctly on APP3",
                Steps="1. Navigate to https://app3.cloudsfer.com\n2. Attempt to access a protected route without logging in",
                ExpectedResult="User is redirected to the login page; correct APP3 domain retained in redirect URL",
                Priority="High" },

            new() { Id="BRW-011", Module="Browse Environments", Submodule="APP3 - Pre-Production",
                Scenario="Correct URL/domain confirmed on APP3",
                Steps="1. Navigate to https://app3.cloudsfer.com\n2. Inspect browser address bar and any API calls",
                ExpectedResult="All requests use app3.cloudsfer.com; no cross-environment domain leakage",
                Priority="High" },

            new() { Id="BRW-012", Module="Browse Environments", Submodule="APP3 - Pre-Production",
                Scenario="Core UI elements render correctly on APP3",
                Steps="1. Navigate to https://app3.cloudsfer.com\n2. Log in\n3. Inspect header, sidebar, dashboard",
                ExpectedResult="Header, navigation sidebar, and dashboard tiles all render without layout breaks or missing assets",
                Priority="Medium" },

            // Masstest (Development)
            new() { Id="BRW-013", Module="Browse Environments", Submodule="Masstest - Development",
                Scenario="Masstest development environment URL loads without error",
                Steps="1. Open browser\n2. Navigate to https://masstest.cloudsfer.com",
                ExpectedResult="Page loads successfully with HTTP 200; no error page or timeout",
                Priority="Medium" },

            new() { Id="BRW-014", Module="Browse Environments", Submodule="Masstest - Development",
                Scenario="Login redirects correctly on Masstest",
                Steps="1. Navigate to https://masstest.cloudsfer.com\n2. Attempt to access a protected route without logging in",
                ExpectedResult="User is redirected to the login page; correct Masstest domain retained in redirect URL",
                Priority="Medium" },

            new() { Id="BRW-015", Module="Browse Environments", Submodule="Masstest - Development",
                Scenario="Correct URL/domain confirmed on Masstest",
                Steps="1. Navigate to https://masstest.cloudsfer.com\n2. Inspect browser address bar and any API calls",
                ExpectedResult="All requests use masstest.cloudsfer.com; no cross-environment domain leakage",
                Priority="Medium" },

            new() { Id="BRW-016", Module="Browse Environments", Submodule="Masstest - Development",
                Scenario="Core UI elements render correctly on Masstest",
                Steps="1. Navigate to https://masstest.cloudsfer.com\n2. Log in\n3. Inspect header, sidebar, dashboard",
                ExpectedResult="Header, navigation sidebar, and dashboard tiles all render without layout breaks or missing assets",
                Priority="Low" },

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

            // ── Backup ──────────────────────────────────────────────────────
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
        Console.WriteLine($"[DataSeeder] Added {toAdd.Count} new test cases from v3.39.0.14/v3.39.0.15 + BRW.");
    }

    // ── Report module test cases ────────────────────────────────────────────
    private static void UpsertReportTestCases(AppDbContext db)
    {
        var cases = new List<TestCase>
        {
            // ════════════════════════════════════════════════════════════════
            // ADMIN REPORT  ›  Migration Backup Report
            // ════════════════════════════════════════════════════════════════
            new() { Id="RAR-001", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Backup report generated for BIM 360 → Amazon S3 after successful backup",
                Steps="1. Complete a BIM 360 → Amazon S3 backup\n2. Navigate to Admin > Reports > Backup Reports",
                ExpectedResult="Report entry visible with source=BIM 360, target=Amazon S3, status=Success",
                Priority="High" },

            new() { Id="RAR-002", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Backup report generated for BIM 360 → File System after successful backup",
                Steps="1. Complete a BIM 360 → File System backup\n2. Navigate to Admin > Reports > Backup Reports",
                ExpectedResult="Report entry visible with source=BIM 360, target=File System, status=Success",
                Priority="High" },

            new() { Id="RAR-003", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Backup report generated for BIM 360 → S3 Compatible after successful backup",
                Steps="1. Complete a BIM 360 → S3 Compatible backup\n2. Navigate to Admin > Reports > Backup Reports",
                ExpectedResult="Report entry visible with source=BIM 360, target=S3 Compatible, status=Success",
                Priority="High" },

            new() { Id="RAR-004", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Backup report generated for BIM 360 → Azure Blob Storage after successful backup",
                Steps="1. Complete a BIM 360 → Azure Blob Storage backup\n2. Navigate to Admin > Reports > Backup Reports",
                ExpectedResult="Report entry visible with source=BIM 360, target=Azure Blob Storage, status=Success",
                Priority="High" },

            new() { Id="RAR-005", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Backup report generated for Egnyte → Amazon S3 after successful backup",
                Steps="1. Complete an Egnyte → Amazon S3 backup\n2. Navigate to Admin > Reports > Backup Reports",
                ExpectedResult="Report entry visible with source=Egnyte, target=Amazon S3, status=Success",
                Priority="High" },

            new() { Id="RAR-006", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Backup report generated for Egnyte → File System after successful backup",
                Steps="1. Complete an Egnyte → File System backup\n2. Navigate to Admin > Reports > Backup Reports",
                ExpectedResult="Report entry visible with source=Egnyte, target=File System, status=Success",
                Priority="High" },

            new() { Id="RAR-007", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Backup report generated for Egnyte → S3 Compatible after successful backup",
                Steps="1. Complete an Egnyte → S3 Compatible backup\n2. Navigate to Admin > Reports > Backup Reports",
                ExpectedResult="Report entry visible with source=Egnyte, target=S3 Compatible, status=Success",
                Priority="High" },

            new() { Id="RAR-008", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Backup report generated for Egnyte → Azure Blob Storage after successful backup",
                Steps="1. Complete an Egnyte → Azure Blob Storage backup\n2. Navigate to Admin > Reports > Backup Reports",
                ExpectedResult="Report entry visible with source=Egnyte, target=Azure Blob Storage, status=Success",
                Priority="High" },

            new() { Id="RAR-009", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report shows correct total file count for completed backup",
                Steps="1. Complete a backup with a known file count\n2. Open admin backup report for that job",
                ExpectedResult="Total files count in report matches the actual number of files backed up",
                Priority="High" },

            new() { Id="RAR-010", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report shows correct backup size",
                Steps="1. Complete a backup\n2. Check size shown in admin report vs actual target size",
                ExpectedResult="Backup size in report matches actual data transferred to target",
                Priority="High" },

            new() { Id="RAR-011", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report shows accurate backup start and end timestamps",
                Steps="1. Note the start time before running a backup\n2. Complete backup\n3. Open report",
                ExpectedResult="Start and end times in report match the actual execution window",
                Priority="High" },

            new() { Id="RAR-012", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report status shows 'Success' for fully completed backup",
                Steps="1. Run a backup that completes without errors\n2. Open admin report",
                ExpectedResult="Status column shows 'Success' for the backup entry",
                Priority="High" },

            new() { Id="RAR-013", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report status shows 'Completed with Errors' when some files fail",
                Steps="1. Run a backup where some files are inaccessible\n2. Open admin report",
                ExpectedResult="Status shows 'Completed with Errors'; failed file count is non-zero",
                Priority="High" },

            new() { Id="RAR-014", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report status shows 'Failed' for a backup that did not complete",
                Steps="1. Trigger or simulate a fully failed backup\n2. Open admin report",
                ExpectedResult="Status shows 'Failed'; error reason is visible",
                Priority="High" },

            new() { Id="RAR-015", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report can be filtered by date range",
                Steps="1. Select a specific From–To date range in the filter\n2. Apply",
                ExpectedResult="Only backup reports within the selected date range are displayed",
                Priority="High" },

            new() { Id="RAR-016", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report can be filtered by source (BIM 360 / Egnyte)",
                Steps="1. Select source filter = BIM 360\n2. Apply\n3. Repeat for Egnyte",
                ExpectedResult="Only reports matching the selected source are shown",
                Priority="High" },

            new() { Id="RAR-017", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report can be filtered by target (Amazon S3 / File System / S3 Compatible / Azure Blob Storage)",
                Steps="1. Select each target in the filter dropdown one at a time\n2. Apply",
                ExpectedResult="Each filter shows only reports for the selected target storage type",
                Priority="High" },

            new() { Id="RAR-018", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report can be filtered by status (Success / Completed with Errors / Failed)",
                Steps="1. Apply status filter = Failed\n2. Verify results\n3. Repeat for other statuses",
                ExpectedResult="Only reports matching the selected status are shown",
                Priority="Medium" },

            new() { Id="RAR-019", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report pagination works correctly when many backup records exist",
                Steps="1. Ensure 50+ backup records exist\n2. Open admin backup report\n3. Navigate pages",
                ExpectedResult="Pagination controls functional; records distributed correctly across pages",
                Priority="Medium" },

            new() { Id="RAR-020", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Failed backup entries show error details in report",
                Steps="1. Open a failed backup report entry\n2. Click on the entry or expand details",
                ExpectedResult="Error message or reason is visible for the failed backup",
                Priority="High" },

            new() { Id="RAR-021", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report shows skipped/excluded files count separately",
                Steps="1. Run a backup where some files are skipped\n2. Check report details",
                ExpectedResult="Skipped files count shown distinctly from failed count",
                Priority="Medium" },

            new() { Id="RAR-022", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Admin can view backup reports for all accounts across all users",
                Steps="1. Log in as Admin\n2. Open backup reports\n3. Check reports from multiple different accounts",
                ExpectedResult="Reports for all user accounts are visible to the admin",
                Priority="High" },

            new() { Id="RAR-023", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report updates and reflects new backup after it completes",
                Steps="1. Note current report list\n2. Run a new backup\n3. Refresh report page",
                ExpectedResult="New backup entry appears in the report after completion",
                Priority="High" },

            new() { Id="RAR-024", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report list is sorted by date descending by default",
                Steps="1. Open admin backup report with multiple entries",
                ExpectedResult="Most recent backup appears first; entries in descending date order",
                Priority="Medium" },

            new() { Id="RAR-025", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Admin can search or filter reports by account name",
                Steps="1. Enter an account name in the search/filter field\n2. Apply",
                ExpectedResult="Only reports belonging to the searched account are shown",
                Priority="Medium" },

            new() { Id="RAR-026", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report shows correct number of backed-up files for BIM 360 → Amazon S3 incremental backup",
                Steps="1. Run an incremental BIM 360 → Amazon S3 backup after a full backup\n2. Open report",
                ExpectedResult="Report shows only the delta (new/changed files), not the full count again",
                Priority="High" },

            new() { Id="RAR-027", Module="Report", Submodule="Admin Report", Group="Migration Backup Report",
                Scenario="Report shows correct number of backed-up files for Egnyte → Azure Blob Storage incremental backup",
                Steps="1. Run an incremental Egnyte → Azure Blob Storage backup\n2. Open report",
                ExpectedResult="Delta file count is shown; matches actual changed files",
                Priority="High" },

            // ════════════════════════════════════════════════════════════════
            // ADMIN REPORT  ›  Downloaded Report Details
            // ════════════════════════════════════════════════════════════════
            new() { Id="RAR-028", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Download Report button is accessible on the admin backup report page",
                Steps="1. Navigate to Admin > Reports > Backup Reports\n2. Look for Download/Export button",
                ExpectedResult="Download Report button is visible and enabled",
                Priority="High" },

            new() { Id="RAR-029", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report file is in CSV format",
                Steps="1. Click Download Report on admin backup reports page\n2. Check file extension",
                ExpectedResult="File downloads with .csv extension and is valid CSV",
                Priority="High" },

            new() { Id="RAR-030", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report contains all expected column headers",
                Steps="1. Download admin backup report\n2. Open in Excel/text editor\n3. Check headers",
                ExpectedResult="Headers include: Account, Source, Target, Status, Total Files, Success, Failed, Skipped, Size, Start Time, End Time",
                Priority="High" },

            new() { Id="RAR-031", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report data matches the on-screen table exactly",
                Steps="1. Note values shown on screen\n2. Download report\n3. Compare each row",
                ExpectedResult="Every row and value in the downloaded file matches what was displayed on screen",
                Priority="High" },

            new() { Id="RAR-032", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report includes all filtered results, not just the current page",
                Steps="1. Filter report to 100+ results (multi-page)\n2. Download\n3. Count rows in file",
                ExpectedResult="Downloaded file contains all filtered rows, not only the visible page",
                Priority="High" },

            new() { Id="RAR-033", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report for BIM 360 → Amazon S3 contains correct source and target values",
                Steps="1. Filter by BIM 360 source + Amazon S3 target\n2. Download\n3. Verify source/target columns",
                ExpectedResult="All rows show Source=BIM 360 and Target=Amazon S3",
                Priority="High" },

            new() { Id="RAR-034", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report for BIM 360 → File System contains correct source and target values",
                Steps="1. Filter by BIM 360 + File System\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=BIM 360 and Target=File System",
                Priority="High" },

            new() { Id="RAR-035", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report for BIM 360 → S3 Compatible contains correct source and target values",
                Steps="1. Filter by BIM 360 + S3 Compatible\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=BIM 360 and Target=S3 Compatible",
                Priority="High" },

            new() { Id="RAR-036", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report for BIM 360 → Azure Blob Storage contains correct source and target values",
                Steps="1. Filter by BIM 360 + Azure Blob Storage\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=BIM 360 and Target=Azure Blob Storage",
                Priority="High" },

            new() { Id="RAR-037", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report for Egnyte → Amazon S3 contains correct source and target values",
                Steps="1. Filter by Egnyte + Amazon S3\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=Egnyte and Target=Amazon S3",
                Priority="High" },

            new() { Id="RAR-038", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report for Egnyte → File System contains correct source and target values",
                Steps="1. Filter by Egnyte + File System\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=Egnyte and Target=File System",
                Priority="High" },

            new() { Id="RAR-039", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report for Egnyte → S3 Compatible contains correct source and target values",
                Steps="1. Filter by Egnyte + S3 Compatible\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=Egnyte and Target=S3 Compatible",
                Priority="High" },

            new() { Id="RAR-040", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report for Egnyte → Azure Blob Storage contains correct source and target values",
                Steps="1. Filter by Egnyte + Azure Blob Storage\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=Egnyte and Target=Azure Blob Storage",
                Priority="High" },

            new() { Id="RAR-041", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report includes error details for failed backup jobs",
                Steps="1. Filter to show failed backups\n2. Download report\n3. Check error column",
                ExpectedResult="Error or failure reason is present in the downloaded file for each failed entry",
                Priority="High" },

            new() { Id="RAR-042", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report file name follows expected naming convention",
                Steps="1. Download admin backup report\n2. Observe file name",
                ExpectedResult="File name includes report type and date (e.g. AdminBackupReport_2025-01-15.csv)",
                Priority="Medium" },

            new() { Id="RAR-043", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report opens without errors or encoding issues in Excel",
                Steps="1. Download report\n2. Open in Microsoft Excel",
                ExpectedResult="File opens cleanly; no garbled characters; columns auto-detected correctly",
                Priority="High" },

            new() { Id="RAR-044", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloading filtered report exports only the filtered data",
                Steps="1. Apply date range + status filter\n2. Download\n3. Verify rows",
                ExpectedResult="Downloaded file contains ONLY rows matching active filters; no extra records",
                Priority="High" },

            new() { Id="RAR-045", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Empty filtered report can be downloaded without error",
                Steps="1. Apply a filter that returns zero results\n2. Click Download",
                ExpectedResult="CSV downloads successfully with headers only; no server error or crash",
                Priority="Medium" },

            new() { Id="RAR-046", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report dates are in a consistent, unambiguous format",
                Steps="1. Download a backup report\n2. Check all date/time columns",
                ExpectedResult="All dates use ISO or dd/MM/yyyy HH:mm format consistently across all rows",
                Priority="Medium" },

            new() { Id="RAR-047", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Large report (1000+ rows) downloads completely without timeout",
                Steps="1. Ensure 1000+ backup records exist\n2. Download without applying filters",
                ExpectedResult="File downloads fully; row count in file matches total records",
                Priority="High" },

            new() { Id="RAR-048", Module="Report", Submodule="Admin Report", Group="Downloaded Report Details",
                Scenario="Downloaded report Size column shows human-readable values (KB/MB/GB)",
                Steps="1. Download backup report\n2. Check the Size column in the file",
                ExpectedResult="Size values are in human-readable units (e.g. 1.2 GB, 540 MB), not raw bytes",
                Priority="Low" },

            // ════════════════════════════════════════════════════════════════
            // CLIENT REPORT  ›  Migration Backup Report
            // ════════════════════════════════════════════════════════════════
            new() { Id="RCR-001", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can view backup report for BIM 360 → Amazon S3",
                Steps="1. Log in as a client account that has BIM 360 → Amazon S3 backups\n2. Navigate to Reports > Backup Reports",
                ExpectedResult="Backup report entries with source=BIM 360, target=Amazon S3 are visible",
                Priority="High" },

            new() { Id="RCR-002", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can view backup report for BIM 360 → File System",
                Steps="1. Log in as client with BIM 360 → File System backups\n2. Open Backup Reports",
                ExpectedResult="Backup report entries with source=BIM 360, target=File System are visible",
                Priority="High" },

            new() { Id="RCR-003", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can view backup report for BIM 360 → S3 Compatible",
                Steps="1. Log in as client with BIM 360 → S3 Compatible backups\n2. Open Backup Reports",
                ExpectedResult="Backup report entries with source=BIM 360, target=S3 Compatible are visible",
                Priority="High" },

            new() { Id="RCR-004", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can view backup report for BIM 360 → Azure Blob Storage",
                Steps="1. Log in as client with BIM 360 → Azure Blob Storage backups\n2. Open Backup Reports",
                ExpectedResult="Backup report entries with source=BIM 360, target=Azure Blob Storage are visible",
                Priority="High" },

            new() { Id="RCR-005", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can view backup report for Egnyte → Amazon S3",
                Steps="1. Log in as client with Egnyte → Amazon S3 backups\n2. Open Backup Reports",
                ExpectedResult="Backup report entries with source=Egnyte, target=Amazon S3 are visible",
                Priority="High" },

            new() { Id="RCR-006", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can view backup report for Egnyte → File System",
                Steps="1. Log in as client with Egnyte → File System backups\n2. Open Backup Reports",
                ExpectedResult="Backup report entries with source=Egnyte, target=File System are visible",
                Priority="High" },

            new() { Id="RCR-007", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can view backup report for Egnyte → S3 Compatible",
                Steps="1. Log in as client with Egnyte → S3 Compatible backups\n2. Open Backup Reports",
                ExpectedResult="Backup report entries with source=Egnyte, target=S3 Compatible are visible",
                Priority="High" },

            new() { Id="RCR-008", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can view backup report for Egnyte → Azure Blob Storage",
                Steps="1. Log in as client with Egnyte → Azure Blob Storage backups\n2. Open Backup Reports",
                ExpectedResult="Backup report entries with source=Egnyte, target=Azure Blob Storage are visible",
                Priority="High" },

            new() { Id="RCR-009", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can only see their own account's backup reports",
                Steps="1. Log in as Client A\n2. Open backup reports\n3. Verify no records from Client B",
                ExpectedResult="Only Client A's own backups are listed; data isolation enforced",
                Priority="High" },

            new() { Id="RCR-010", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report shows correct total file count per backup job",
                Steps="1. Run a backup with a known file count\n2. Open client backup report",
                ExpectedResult="Total files count matches actual files backed up",
                Priority="High" },

            new() { Id="RCR-011", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report shows correct backup size",
                Steps="1. Complete a backup\n2. Check size column in client report",
                ExpectedResult="Size value matches actual data transferred to target storage",
                Priority="High" },

            new() { Id="RCR-012", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report shows accurate backup start and end timestamps",
                Steps="1. Note start time before running backup\n2. Complete backup\n3. Check client report",
                ExpectedResult="Timestamps in report match actual execution window",
                Priority="High" },

            new() { Id="RCR-013", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report status shows 'Success' for fully completed backup",
                Steps="1. Run a backup that completes without errors\n2. Check client report",
                ExpectedResult="Status column shows 'Success'",
                Priority="High" },

            new() { Id="RCR-014", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report status shows 'Completed with Errors' when some files fail",
                Steps="1. Run a backup where some files are inaccessible\n2. Check client report",
                ExpectedResult="Status shows 'Completed with Errors'; failed file count is non-zero",
                Priority="High" },

            new() { Id="RCR-015", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report status shows 'Failed' for a fully failed backup",
                Steps="1. Trigger or simulate a fully failed backup\n2. Open client report",
                ExpectedResult="Status shows 'Failed'; error reason is visible",
                Priority="High" },

            new() { Id="RCR-016", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can filter backup report by date range",
                Steps="1. Select a From–To date range\n2. Apply filter",
                ExpectedResult="Only backup reports within the selected dates are displayed",
                Priority="High" },

            new() { Id="RCR-017", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can filter backup report by source (BIM 360 / Egnyte)",
                Steps="1. Select source filter = BIM 360\n2. Apply\n3. Repeat for Egnyte",
                ExpectedResult="Only reports matching the selected source are shown",
                Priority="High" },

            new() { Id="RCR-018", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can filter backup report by target storage type",
                Steps="1. Select each target filter (Amazon S3, File System, S3 Compatible, Azure Blob Storage)\n2. Apply each",
                ExpectedResult="Each filter shows only reports for the selected target type",
                Priority="High" },

            new() { Id="RCR-019", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client can filter backup report by status",
                Steps="1. Apply status filter = Failed\n2. Verify results\n3. Repeat for other statuses",
                ExpectedResult="Only records with the selected status are shown",
                Priority="Medium" },

            new() { Id="RCR-020", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report pagination works correctly",
                Steps="1. Ensure 50+ backup records\n2. Open client report\n3. Navigate through pages",
                ExpectedResult="Pagination works; all records accessible across pages",
                Priority="Medium" },

            new() { Id="RCR-021", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report shows error details for failed backup",
                Steps="1. Open a failed backup entry in client report\n2. Expand or click details",
                ExpectedResult="Error message or reason is displayed for the failed job",
                Priority="High" },

            new() { Id="RCR-022", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report shows skipped files count separately from failed count",
                Steps="1. Run a backup with skipped files\n2. Open client report",
                ExpectedResult="Skipped count is shown as a distinct column/value from failed count",
                Priority="Medium" },

            new() { Id="RCR-023", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report does not expose reports from other accounts",
                Steps="1. Log in as Client A\n2. Attempt to access Client B's report by URL manipulation",
                ExpectedResult="Access denied; Client A cannot view Client B's report data",
                Priority="High" },

            new() { Id="RCR-024", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report sorted by date descending by default",
                Steps="1. Open client backup report with multiple entries",
                ExpectedResult="Most recent backup entry appears first",
                Priority="Medium" },

            new() { Id="RCR-025", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report reflects new backup after it completes",
                Steps="1. Note current report list\n2. Run new backup\n3. Refresh report page",
                ExpectedResult="New backup entry appears after completion",
                Priority="High" },

            new() { Id="RCR-026", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report shows correct delta file count for BIM 360 → Amazon S3 incremental backup",
                Steps="1. Run a full BIM 360 → Amazon S3 backup\n2. Add new files\n3. Run incremental\n4. Check report",
                ExpectedResult="Report shows only the incremental (changed/new) file count, not the full total",
                Priority="High" },

            new() { Id="RCR-027", Module="Report", Submodule="Client Report", Group="Migration Backup Report",
                Scenario="Client report shows correct delta file count for Egnyte → Azure Blob Storage incremental backup",
                Steps="1. Run full Egnyte → Azure Blob backup\n2. Add files\n3. Run incremental\n4. Check report",
                ExpectedResult="Report reflects only changed/new files in the incremental run",
                Priority="High" },

            // ════════════════════════════════════════════════════════════════
            // CLIENT REPORT  ›  Downloaded Report Details
            // ════════════════════════════════════════════════════════════════
            new() { Id="RCR-028", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Download Report button is accessible on the client backup report page",
                Steps="1. Log in as client\n2. Navigate to Reports > Backup Reports\n3. Look for Download button",
                ExpectedResult="Download Report button is visible and enabled",
                Priority="High" },

            new() { Id="RCR-029", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report file is in CSV format",
                Steps="1. Click Download Report on client backup reports page\n2. Check file extension",
                ExpectedResult="File downloads as .csv and is valid CSV content",
                Priority="High" },

            new() { Id="RCR-030", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report contains all expected column headers",
                Steps="1. Download client backup report\n2. Open file\n3. Check headers",
                ExpectedResult="Headers include: Source, Target, Status, Total Files, Success, Failed, Skipped, Size, Start Time, End Time",
                Priority="High" },

            new() { Id="RCR-031", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report data matches the on-screen table exactly",
                Steps="1. Note on-screen values\n2. Download report\n3. Compare each row",
                ExpectedResult="Every row in the file matches the on-screen display",
                Priority="High" },

            new() { Id="RCR-032", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report includes all filtered results across all pages",
                Steps="1. Filter to a large result set (multi-page)\n2. Download\n3. Count rows in file",
                ExpectedResult="File contains all matching records, not just the visible page",
                Priority="High" },

            new() { Id="RCR-033", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report for BIM 360 → Amazon S3 contains correct source/target",
                Steps="1. Filter by BIM 360 + Amazon S3\n2. Download\n3. Verify Source and Target columns",
                ExpectedResult="All rows show Source=BIM 360 and Target=Amazon S3",
                Priority="High" },

            new() { Id="RCR-034", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report for BIM 360 → File System contains correct source/target",
                Steps="1. Filter by BIM 360 + File System\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=BIM 360 and Target=File System",
                Priority="High" },

            new() { Id="RCR-035", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report for BIM 360 → S3 Compatible contains correct source/target",
                Steps="1. Filter by BIM 360 + S3 Compatible\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=BIM 360 and Target=S3 Compatible",
                Priority="High" },

            new() { Id="RCR-036", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report for BIM 360 → Azure Blob Storage contains correct source/target",
                Steps="1. Filter by BIM 360 + Azure Blob Storage\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=BIM 360 and Target=Azure Blob Storage",
                Priority="High" },

            new() { Id="RCR-037", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report for Egnyte → Amazon S3 contains correct source/target",
                Steps="1. Filter by Egnyte + Amazon S3\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=Egnyte and Target=Amazon S3",
                Priority="High" },

            new() { Id="RCR-038", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report for Egnyte → File System contains correct source/target",
                Steps="1. Filter by Egnyte + File System\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=Egnyte and Target=File System",
                Priority="High" },

            new() { Id="RCR-039", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report for Egnyte → S3 Compatible contains correct source/target",
                Steps="1. Filter by Egnyte + S3 Compatible\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=Egnyte and Target=S3 Compatible",
                Priority="High" },

            new() { Id="RCR-040", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report for Egnyte → Azure Blob Storage contains correct source/target",
                Steps="1. Filter by Egnyte + Azure Blob Storage\n2. Download\n3. Verify columns",
                ExpectedResult="All rows show Source=Egnyte and Target=Azure Blob Storage",
                Priority="High" },

            new() { Id="RCR-041", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report includes error details for failed backup jobs",
                Steps="1. Filter to show failed backups\n2. Download report\n3. Check error column",
                ExpectedResult="Error/failure reason is present in the file for each failed entry",
                Priority="High" },

            new() { Id="RCR-042", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report does not contain data from other accounts",
                Steps="1. Download client report as Client A\n2. Check all rows",
                ExpectedResult="Zero rows belong to any account other than Client A",
                Priority="High" },

            new() { Id="RCR-043", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report file name includes account or date identifier",
                Steps="1. Download client backup report\n2. Observe the file name",
                ExpectedResult="File name includes date or account reference (e.g. BackupReport_2025-01-15.csv)",
                Priority="Medium" },

            new() { Id="RCR-044", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report opens without errors or encoding issues in Excel",
                Steps="1. Download report\n2. Open in Microsoft Excel",
                ExpectedResult="File opens cleanly; no garbled text; columns detected correctly",
                Priority="High" },

            new() { Id="RCR-045", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Downloading filtered client report exports only the filtered data",
                Steps="1. Apply status + date filters\n2. Download\n3. Verify rows in file",
                ExpectedResult="Downloaded file contains ONLY rows matching active filters",
                Priority="High" },

            new() { Id="RCR-046", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Empty filtered client report downloads without error",
                Steps="1. Apply a filter returning zero results\n2. Click Download",
                ExpectedResult="CSV downloads with headers only; no server error",
                Priority="Medium" },

            new() { Id="RCR-047", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Client downloaded report dates are in a consistent format",
                Steps="1. Download client backup report\n2. Inspect all date columns",
                ExpectedResult="All dates use a consistent format (ISO or dd/MM/yyyy HH:mm) throughout the file",
                Priority="Medium" },

            new() { Id="RCR-048", Module="Report", Submodule="Client Report", Group="Downloaded Report Details",
                Scenario="Large client report (500+ rows) downloads completely without timeout",
                Steps="1. Ensure 500+ backup records exist for the account\n2. Download without filters",
                ExpectedResult="File downloads fully; row count in file matches total record count",
                Priority="High" },
        };

        var existingIds = db.TestCases.Select(t => t.Id).ToHashSet();
        var toAdd = cases.Where(tc => !existingIds.Contains(tc.Id)).ToList();
        if (toAdd.Count == 0) return;

        db.TestCases.AddRange(toAdd);
        db.SaveChanges();
        Console.WriteLine($"[DataSeeder] Added {toAdd.Count} Report module test cases.");
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
