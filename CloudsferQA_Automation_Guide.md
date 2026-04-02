# CloudsferQA — Automation Testing Guide

**Platform:** Cloudsfer Cloud Migration & Backup SaaS  
**Stack:** ASP.NET Core 8, SQLite, Windows IIS  
**Prepared:** April 2026  
**Scope:** All 11 modules, 135+ test cases

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Automation Readiness by Module](#2-automation-readiness-by-module)
3. [Tools & Frameworks](#3-tools--frameworks)
4. [Recommended Project Structure](#4-recommended-project-structure)
5. [Module-by-Module Automation Scenarios](#5-module-by-module-automation-scenarios)
   - [Registration & Activation](#51-registration--activation)
   - [Migration](#52-migration)
   - [Backup](#53-backup)
   - [Restore](#54-restore)
   - [OPA (On-Premise Agent)](#55-opa-on-premise-agent)
   - [Administrator](#56-administrator)
   - [Task Bar](#57-task-bar)
   - [Emails & Notifications](#58-emails--notifications)
   - [CF Grid Auto Refresh](#59-cf-grid-auto-refresh)
   - [Pre-Validation](#510-pre-validation)
   - [PAYG Notifications & Banner](#511-payg-notifications--banner)
6. [Special Challenges & Solutions](#6-special-challenges--solutions)
7. [CI/CD Pipeline Setup](#7-cicd-pipeline-setup)
8. [Test Data Management](#8-test-data-management)
9. [Sprint Plan — Where to Start](#9-sprint-plan--where-to-start)
10. [NuGet & npm Package Reference](#10-nuget--npm-package-reference)

---

## 1. Executive Summary

Automating Cloudsfer's QA requires **four layers** working together:

| Layer | What It Tests | Tools |
|---|---|---|
| **Unit Tests** | Individual C# services, helpers, password hashing, stats calculations | xUnit, Moq, EF InMemory |
| **Integration Tests** | API endpoints, database operations, email delivery, auth flow | WebApplicationFactory, WireMock.Net, Mailhog |
| **E2E / UI Tests** | Full browser flows, modals, real-time updates, file uploads | Playwright (.NET) |
| **Performance Tests** | Load on dashboard, webhook floods, migration API throughput | k6 |

**Quick-win modules** (automate first, low complexity):
- Registration & Activation
- Administrator (User Management, Monthly Reports, Analytical Reports)
- Pre-Validation (CSV upload, navigation guards)
- CF Grid Auto Refresh

**Hard modules** (require special strategies):
- Migration & Backup (OAuth flows, real cloud storage)
- OPA — On-Premise Agent (MSI installation)
- Webhooks (external HTTP listener required)
- Emails & Notifications (SMTP interception required)

---

## 2. Automation Readiness by Module

| Module | UI/E2E | API | DB | Performance | Security | Difficulty |
|---|---|---|---|---|---|---|
| Registration & Activation | ✅ High | ✅ Medium | ✅ Essential | ⬜ Low | ✅ High | 🟢 Easy |
| Migration | ✅ High | ✅ Critical | ✅ High | ✅ High | ✅ Critical | 🔴 Hard |
| Backup | ✅ High | ✅ High | ✅ High | ✅ Medium | ✅ High | 🔴 Hard |
| Restore | ✅ Medium | ✅ High | ✅ High | ⬜ Low | ⬜ Low | 🟡 Medium |
| OPA (On-Premise Agent) | ⬜ Low | ✅ Medium | ⬜ Low | ⬜ Low | ⬜ Low | 🔴 Hard |
| Administrator | ✅ High | ✅ Medium | ✅ High | ✅ Medium | ⬜ Low | 🟢 Easy |
| Task Bar | ✅ High | ⬜ Low | ✅ Medium | ⬜ Low | ⬜ Low | 🟡 Medium |
| Emails & Notifications | ⬜ Low | ✅ Medium | ✅ Medium | ⬜ Low | ⬜ Low | 🟡 Medium |
| CF Grid Auto Refresh | ✅ High | ⬜ None | ⬜ None | ⬜ Low | ⬜ None | 🟢 Easy |
| Pre-Validation | ✅ High | ✅ Medium | ✅ Medium | ⬜ Low | ⬜ Low | 🟢 Easy |
| PAYG Notifications & Banner | ✅ Medium | ✅ Medium | ✅ High | ⬜ Low | ⬜ Low | 🟡 Medium |

---

## 3. Tools & Frameworks

### 3.1 E2E / Browser Automation — Playwright (Recommended)

**Why Playwright over Selenium or Cypress:**
- Native .NET SDK — no JavaScript layer needed
- Built-in CSV file upload (`Page.SetInputFilesAsync`) for Pre-Validation tests
- Request interception (`Page.RouteAsync`) to mock OAuth and cloud storage APIs
- Per-test `BrowserContext` with independent cookies — `QAAuthUserId` and `QASessionId` don't bleed between tests
- `Page.WaitForFunctionAsync()` for real-time DOM polling (CF Grid Auto Refresh, Task Bar)
- Trace viewer for debugging failed tests step-by-step
- Runs headless on Windows IIS CI with no display server

**Installation:**
```
dotnet add package Microsoft.Playwright.NUnit
dotnet build
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
```

### 3.2 Unit & Integration Tests — xUnit + WebApplicationFactory

```
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.AspNetCore.Mvc.Testing    # in-process ASP.NET Core
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Moq
```

`WebApplicationFactory<Program>` spins up the full ASP.NET Core pipeline in-process — no server needed. All controllers, middleware, and EF Core are real. Ideal for API endpoint testing.

### 3.3 HTTP Mocking — WireMock.Net

Used to mock cloud storage APIs (Google Drive, OneDrive, SharePoint), webhook receivers, and OAuth endpoints.

```
dotnet add package WireMock.Net
```

### 3.4 Email Testing — Mailhog (Local) or Mailosaur (CI)

- **Mailhog**: Free, Docker-based SMTP capture server. Exposes REST API on port 8025.
- **Mailosaur**: Cloud-based, has free tier, zero setup. Better for GitHub Actions CI.

```
docker run -p 1025:1025 -p 8025:8025 mailhog/mailhog
```

Point `appsettings.Testing.json` SMTP host to `localhost:1025`.

### 3.5 Performance Testing — k6

```
# Install k6 on Windows
winget install k6
# Run a load test
k6 run --out json=results.json migration-load.js
```

### 3.6 Test Reporting — Allure

```
dotnet add package Allure.NUnit
```

Produces rich HTML reports with screenshots, step breakdowns, and history trends. Can be published to GitHub Pages.

### 3.7 BDD (Optional) — SpecFlow

Useful for Pre-Validation and CF Grid modules where test cases are already in plain-English scenario format — they map almost directly to Gherkin steps.

```
dotnet add package SpecFlow.NUnit
dotnet add package SpecFlow.Playwright
```

### 3.8 Database Testing — SQLite In-Memory

```csharp
var connection = new SqliteConnection("DataSource=:memory:");
connection.Open();
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
using var db = new AppDbContext(options);
db.Database.EnsureCreated();
```

Use **Respawn** (`dotnet add package Respawn`) to reset the database between tests without recreating it.

---

## 4. Recommended Project Structure

```
CloudsferQA.sln
│
├── CloudsferQA/                          ← existing main application
│
├── CloudsferQA.Tests.Unit/               ← fast unit tests, no DB, no network
│   ├── CloudsferQA.Tests.Unit.csproj
│   ├── Services/
│   │   ├── StatsServiceTests.cs
│   │   └── EmailServiceTests.cs
│   └── Helpers/
│       └── PasswordHelperTests.cs
│
├── CloudsferQA.Tests.Integration/        ← API + DB + email tests
│   ├── CloudsferQA.Tests.Integration.csproj
│   ├── Auth/
│   │   ├── LoginTests.cs
│   │   └── RegistrationTests.cs
│   ├── Admin/
│   │   ├── UserManagementTests.cs
│   │   └── TestCaseManagementTests.cs
│   └── Api/
│       └── StatsApiTests.cs
│
├── CloudsferQA.Tests.E2E/                ← Playwright browser tests
│   ├── CloudsferQA.Tests.E2E.csproj
│   ├── Fixtures/
│   │   └── AuthenticatedPageTest.cs      ← base class with login helper
│   └── Modules/
│       ├── Registration/
│       │   └── RegistrationFlowTests.cs
│       ├── PreValidation/
│       │   ├── UploadCsvFlowTests.cs
│       │   ├── NavigationGuardTests.cs
│       │   └── ValidationProcessTests.cs
│       ├── CfGrid/
│       │   └── AutoRefreshTests.cs
│       ├── Admin/
│       │   ├── MonthlyReportTests.cs
│       │   ├── AnalyticalReportTests.cs
│       │   └── AdminDashboardTests.cs
│       ├── Backup/
│       │   ├── SchedulingTests.cs
│       │   ├── WebhookTests.cs
│       │   └── TokenRefreshTests.cs
│       ├── Migration/
│       │   └── MigrationExecutionTests.cs
│       ├── Banner/
│       │   └── BannerCreationTests.cs
│       └── TaskBar/
│           └── TaskProgressTests.cs
│
├── CloudsferQA.Tests.Performance/        ← k6 scripts
│   ├── migration-load.js
│   ├── webhook-flood.js
│   └── dashboard-concurrent.js
│
└── tests/
    └── fixtures/
        ├── seed.db                       ← snapshot SQLite for integration tests
        ├── users.json
        └── sessions.json
```

---

## 5. Module-by-Module Automation Scenarios

---

### 5.1 Registration & Activation

**Test Cases:** REG-001 to REG-008 | **Modules:** Account Registration, Email Verification, Onboarding Flow

#### What to Automate

| Test Case | Type | Approach |
|---|---|---|
| REG-001: Register with valid email | Integration + E2E | POST to `/Auth/Register`, assert DB row, assert redirect |
| REG-002: Block personal email domains | Integration | POST with `@gmail.com`, assert 400 / error message |
| REG-003: Reject duplicate email | Integration | Register twice with same email, assert conflict error |
| REG-004: Verify account via email link | E2E + Email | Register → Mailhog → extract link → navigate → assert verified |
| REG-005: Expired verification link | Integration | Set `VerifiedAt` to `DateTime.UtcNow.AddHours(-25)`, GET `/Auth/Verify?token=...`, assert error |
| REG-006: Resend verification email | E2E | Click resend → Mailhog → assert 2nd email received |
| REG-007: Onboarding wizard on first login | E2E | Login fresh user → assert wizard visible |
| REG-008: Skip onboarding wizard | E2E | Click skip → assert wizard dismissed, dashboard visible |

#### Key Code Patterns

```csharp
// Integration test — register new user
[Fact]
public async Task Register_ValidEmail_CreatesVerifiedUser()
{
    var response = await _client.PostAsync("/Auth/Register", new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("email", "newqa@tzunami.com"),
        new KeyValuePair<string, string>("password", "Test@1234"),
        new KeyValuePair<string, string>("confirmPassword", "Test@1234")
    }));
    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

    // Assert DB
    using var db = GetTestDb();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "newqa@tzunami.com");
    Assert.NotNull(user);
    Assert.True(user.IsEmailVerified);
    Assert.True(user.IsActive);
}
```

```csharp
// E2E — full email verification flow
[Test]
public async Task EmailVerification_FullFlow()
{
    await Page.GotoAsync("/Auth/Register?email=verify.test@tzunami.com");
    await Page.FillAsync("[name=password]", "Test@1234");
    await Page.FillAsync("[name=confirmPassword]", "Test@1234");
    await Page.ClickAsync("[type=submit]");

    // Query Mailhog
    await Task.Delay(2000);
    var mailhog = new HttpClient { BaseAddress = new Uri("http://localhost:8025") };
    var response = await mailhog.GetAsync("/api/v2/messages");
    var data = await response.Content.ReadFromJsonAsync<MailhogMessages>();
    var email = data!.Items.First(m => m.To.Contains("verify.test@tzunami.com"));

    // Extract link and navigate
    var verifyUrl = Regex.Match(email.Body, @"href=""(.*?/Auth/Verify.*?)""").Groups[1].Value;
    await Page.GotoAsync(verifyUrl);
    await Expect(Page.Locator(".welcome-message")).ToBeVisibleAsync();
}
```

#### Security Tests

- Assert `@gmail.com`, `@outlook.com`, `@yahoo.com` domains are rejected
- Assert auth cookie has `HttpOnly` and `SameSite=Lax` flags
- Assert password minimum length enforced (7 chars fails, 8 chars passes)
- Assert verification token is unique per registration

---

### 5.2 Migration

**Test Cases:** MIG-001 to MIG-018 | **Modules:** Source Connection, Destination Connection, Folder Selection, Execution, Report

#### What to Automate

| Test Case | Type | Approach |
|---|---|---|
| MIG-001/002/003/004: Connect cloud sources | Integration | Mock OAuth token exchange; inject token directly into session |
| MIG-005: Invalid credentials rejected | Integration | Send bad token to connection API; assert 401/error |
| MIG-006/007: Connect destinations | Integration | Same OAuth mock strategy as source |
| MIG-008: Same account as source/destination blocked | Integration | POST same account ID for both; assert validation error |
| MIG-009/010/011: Folder selection | E2E | WireMock stubbed folder list; select/deselect in tree UI |
| MIG-012: Start migration → Running state | Integration | POST start endpoint; poll status endpoint; assert "Running" |
| MIG-013: Pause and resume | Integration | Start → pause → assert "Paused" → resume → assert "Running" |
| MIG-014: Cancel migration | Integration | Start → cancel → assert "Cancelled" |
| MIG-015: Migration completes, 100% progress | Integration | Use WireMock to simulate fast small migration |
| MIG-016: Folder structure preserved | Integration + DB | Compare source tree to destination tree after migration |
| MIG-017: Download migration report | E2E | Click download button, assert file downloaded |
| MIG-018: Report shows failed files | Integration | Seed failed migration records; assert report contains them |

#### OAuth Strategy

**Never** try to automate Google/OneDrive/Dropbox/Box login through the browser in CI. Use pre-issued refresh tokens:

```csharp
// Exchange stored refresh token for access token (run before tests)
public static async Task<string> GetGoogleAccessToken()
{
    var tokenResponse = await new HttpClient().PostAsync(
        "https://oauth2.googleapis.com/token",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"]     = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")!,
            ["client_secret"] = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")!,
            ["refresh_token"] = Environment.GetEnvironmentVariable("GOOGLE_REFRESH_TOKEN")!,
            ["grant_type"]    = "refresh_token"
        }));
    var json = JsonSerializer.Deserialize<JsonElement>(
        await tokenResponse.Content.ReadAsStringAsync());
    return json.GetProperty("access_token").GetString()!;
}
```

Store secrets in GitHub Actions as:
- `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GOOGLE_REFRESH_TOKEN`
- `ONEDRIVE_CLIENT_ID`, `ONEDRIVE_CLIENT_SECRET`, `ONEDRIVE_REFRESH_TOKEN`
- `DROPBOX_ACCESS_TOKEN`
- `BOX_CLIENT_ID`, `BOX_CLIENT_SECRET`, `BOX_REFRESH_TOKEN`

#### Mocking Cloud Storage APIs (WireMock.Net)

```csharp
var wiremock = WireMockServer.Start(9091);

// Stub Google Drive file list
wiremock.Given(Request.Create()
    .WithPath("/drive/v3/files")
    .WithHeader("Authorization", new WildcardMatcher("Bearer *"))
    .UsingGet())
.RespondWith(Response.Create()
    .WithStatusCode(200)
    .WithHeader("Content-Type", "application/json")
    .WithBody("""
    {
        "files": [
            {"id":"f001","name":"document.docx","size":"12345"},
            {"id":"f002","name":"report.pdf","size":"98765"}
        ]
    }
    """));

// Configure test app to point to localhost:9091 instead of googleapis.com
// via appsettings.Testing.json: "GoogleDrive": { "BaseUrl": "http://localhost:9091" }
```

---

### 5.3 Backup

**Test Cases:** BAK-001 to BAK-020 | **Modules:** Configuration, Execution, History, Scheduling, SPO Metadata, Token Refresh, Webhooks

#### What to Automate

| Test Case | Type | Approach |
|---|---|---|
| BAK-001/002/003/004: Backup configuration | E2E | Fill backup form; assert DB record created with correct fields |
| BAK-005/006: Manually trigger backup, verify completion | Integration | POST to backup trigger endpoint; poll status |
| BAK-007: Incremental backup copies only changed files | Integration | Mock file list with known changes; assert only delta items processed |
| BAK-010/011: No start without Next Backup Run Time | Integration | Create backup task without schedule; assert it does not execute |
| BAK-012: Start first backup immediately (UI option) | E2E | Check for "Run Now" checkbox on creation form |
| BAK-013: Webhooks page loads with 100+ entries | Performance | Seed 150 webhook records via DB; measure page load time < 3s |
| BAK-014: Webhooks processed reliably (no stuck) | Integration | POST webhook events; assert all status = "processed" within 30s |
| BAK-015: Bulk delete webhooks | Integration | POST delete request with 10 IDs; assert all removed from DB |
| BAK-017/018: Token refresh during webhook registration | Integration | Pre-expire token in DB; trigger webhook registration; assert new token fetched |
| BAK-019/020: SPO Metadata preserved | Integration | Mock SharePoint API returning metadata; assert CreatedBy/ModifiedBy preserved |

#### Webhook Testing Setup

```csharp
[SetUp]
public void SetUpWebhookListener()
{
    _webhookServer = WireMockServer.Start(9090);
    _webhookServer
        .Given(Request.Create().WithPath("/cloudsfer-webhook").UsingPost())
        .RespondWith(Response.Create().WithStatusCode(200));
}

[Test]
public async Task WebhookEvent_IsProcessed_WithinTimeout()
{
    // Register webhook endpoint pointing to our WireMock server
    await _client.PostAsync("/Backup/RegisterWebhook",
        JsonContent.Create(new { Url = "http://localhost:9090/cloudsfer-webhook" }));

    // Simulate incoming webhook from BIM360
    await _webhookServer.Server.SendAsync(/* POST to Cloudsfer's webhook ingestion endpoint */);

    // Assert event received
    var deadline = DateTime.UtcNow.AddSeconds(30);
    while (DateTime.UtcNow < deadline)
    {
        var entry = _webhookServer.LogEntries
            .FirstOrDefault(e => e.RequestMessage.Path == "/cloudsfer-webhook");
        if (entry != null)
        {
            Assert.Contains("eventType", entry.RequestMessage.Body);
            return;
        }
        await Task.Delay(500);
    }
    Assert.Fail("Webhook not received within 30 seconds");
}
```

#### Token Refresh Testing (BAK-017/018)

```csharp
[Test]
public async Task TokenRefresh_TriggeredWhenNearExpiry()
{
    // Pre-expire the token in the database
    using var db = GetTestDb();
    var token = await db.OAuthTokens.FirstAsync();
    token.ExpiresAt = DateTime.UtcNow.AddSeconds(-1); // already expired
    await db.SaveChangesAsync();

    // Trigger webhook registration (which needs a fresh token)
    var response = await _client.PostAsync("/Backup/RegisterWebhooks",
        JsonContent.Create(new { BackupTaskId = 1 }));
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    // Assert a new token was fetched (ExpiresAt is now in the future)
    await db.Entry(token).ReloadAsync();
    Assert.True(token.ExpiresAt > DateTime.UtcNow);
}
```

---

### 5.4 Restore

**Test Cases:** RES-001 to RES-008 | **Modules:** Restore Point Selection, Execution, Verification

#### What to Automate

| Test Case | Type | Approach |
|---|---|---|
| RES-001: Browse available restore points | E2E | Navigate to restore section; assert date list visible |
| RES-002: Select restore point by date | E2E | Click specific date in restore point list |
| RES-003: Restore all files | Integration | POST restore all; poll until complete; assert file count matches |
| RES-004: Restore specific files | Integration | POST restore with file IDs; assert only those files restored |
| RES-005: Restore to alternate destination | Integration | POST with alternate folder path; assert files created there |
| RES-006: Conflict — overwrite existing | Integration | Pre-create destination file; restore; assert overwritten |
| RES-007: Conflict — keep both | Integration | Same as above, different conflict strategy setting |
| RES-008: Verify restored file content matches original | Integration | Compare hash/size of restored file with source snapshot |

#### File Verification Pattern

```csharp
[Test]
public async Task RestoreFile_ContentMatchesOriginal()
{
    // Get source file info from backup snapshot
    var snapshot = await GetBackupSnapshot(restorePointId: 1);
    var sourceFile = snapshot.Files.First(f => f.Name == "test-document.docx");

    // Trigger restore
    await _client.PostAsync("/Restore/Execute",
        JsonContent.Create(new { RestorePointId = 1, FileIds = new[] { sourceFile.Id } }));

    // Poll until complete
    await WaitForRestoreComplete(restoreJobId: 1);

    // Verify via destination storage API
    var destinationFile = await GetFileFromDestination("test-document.docx");
    Assert.Equal(sourceFile.SizeBytes, destinationFile.SizeBytes);
    Assert.Equal(sourceFile.Checksum, destinationFile.Checksum);
}
```

---

### 5.5 OPA (On-Premise Agent)

**Test Cases:** OPA-001 to OPA-009 | **Modules:** Installation, Agent Status, File Server Migration

#### What to Automate

> OPA installation is the hardest area to automate. Realistic strategy per level:

**Level 1 — Smoke test after manual install (PowerShell / Pester)**

```powershell
# opa-smoke-test.ps1 — run manually after installation
Describe "OPA Installation Verification" {
    It "Windows service is installed and running" {
        $svc = Get-Service -Name "CloudsferOPA" -ErrorAction SilentlyContinue
        $svc | Should -Not -BeNullOrEmpty
        $svc.Status | Should -Be "Running"
    }
    It "Agent port is listening" {
        (Test-NetConnection -ComputerName localhost -Port 8443).TcpTestSucceeded | Should -BeTrue
    }
    It "Health endpoint responds 200" {
        $r = Invoke-RestMethod -Uri "http://localhost:8443/health" -Method Get
        $r.status | Should -Be "healthy"
    }
    It "Invalid registration token is rejected" {
        { Invoke-RestMethod -Uri "http://localhost:8443/register" -Method Post `
            -Body (@{token="invalid-token"} | ConvertTo-Json) `
            -ContentType "application/json" } | Should -Throw
    }
}
```

**Level 2 — API tests after install (fully automatable in CI)**

```csharp
// OPA-004: Assert agent shows Online status in portal
[Test]
public async Task OpaAgent_ShowsOnlineStatus_InPortal()
{
    // Agent is pre-installed on test VM
    var response = await _client.GetAsync("/Admin/Agents");
    var html = await response.Content.ReadAsStringAsync();
    Assert.Contains("Online", html);
    Assert.Contains("CloudsferOPA-TestVM", html);
}

// OPA-005: Offline when service stopped
[Test]
public async Task OpaAgent_ShowsOffline_WhenServiceStopped()
{
    // Stop the OPA Windows service
    Process.Start("sc", "stop CloudsferOPA").WaitForExit();
    await Task.Delay(10000); // wait for heartbeat timeout

    var response = await _client.GetAsync("/Admin/Agents");
    var html = await response.Content.ReadAsStringAsync();
    Assert.Contains("Offline", html);
}
```

**Level 3 — Large file migration via OPA (OPA-009)**

Run this test manually or in a dedicated performance environment. Use a pre-prepared 2.5GB test file on the file server. This test is too slow and resource-intensive for standard CI.

---

### 5.6 Administrator

**Test Cases:** ADM-001 to ADM-009, ADR-001 to ADR-016 | **Modules:** User Management, Admin Dashboard, Monthly Reports, Analytical Reports, Activity Monitoring, System Settings

#### User Management (ADM-001 to ADM-009)

```csharp
// ADM-001: Admin can view all registered users
[Test]
public async Task AdminIndex_ShowsAllUsers()
{
    // Seed 3 users
    using var db = GetTestDb();
    await TestDataBuilder.CreateUsers(db, 3);

    var response = await _adminClient.GetAsync("/Admin");
    var html = await response.Content.ReadAsStringAsync();
    Assert.Contains("qa1@tzunami.com", html);
    Assert.Contains("qa2@tzunami.com", html);
    Assert.Contains("qa3@tzunami.com", html);
}

// ADM-004: Admin cannot deactivate own account
[Test]
public async Task AdminToggleActive_OwnAccount_ReturnsBadRequest()
{
    var response = await _adminClient.PostAsync("/Admin/ToggleActive",
        new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("userId", _adminUserId.ToString())
        }));
    // Should redirect back with error, not actually deactivate
    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains("cannot deactivate", content.ToLower());
}

// ADM-005: Admin can change user role
[Test]
public async Task SetRole_ChangesUserRole_InDatabase()
{
    var user = await TestDataBuilder.CreateVerifiedUser(db, "changerole@tzunami.com");
    await _adminClient.PostAsync("/Admin/SetRole",
        new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("userId", user.Id.ToString()),
            new KeyValuePair<string, string>("role", "SubAdmin")
        }));
    await db.Entry(user).ReloadAsync();
    Assert.Equal("SubAdmin", user.Role);
}
```

#### Monthly Report Tests (ADR-001 to ADR-009)

```csharp
// ADR-003: Year filter shows ONLY results from selected year (Bug 42975)
[Test]
public async Task YearFilter_ShowsOnlySelectedYear()
{
    await Page.GotoAsync("/Admin/Reports");
    await Page.SelectOptionAsync("#year-filter", "2024");
    await Page.ClickAsync("#btn-apply-filter");
    var rows = await Page.Locator("table tbody tr").AllAsync();
    foreach (var row in rows)
    {
        var yearCell = await row.Locator("td.year-column").TextContentAsync();
        Assert.Equal("2024", yearCell!.Trim());
    }
}

// ADR-005: No infinite loading when filter returns zero results (Bug 42974)
[Test]
public async Task EmptyFilterResult_DoesNotCauseInfiniteLoading()
{
    await Page.GotoAsync("/Admin/Reports");
    await Page.SelectOptionAsync("#year-filter", "1999"); // no data for this year
    await Page.ClickAsync("#btn-apply-filter");

    // Loading spinner should disappear within 5 seconds
    await Expect(Page.Locator(".loading-spinner")).Not.ToBeVisibleAsync(
        new() { Timeout = 5000 });
    // Empty state message should be shown
    await Expect(Page.Locator(".empty-state")).ToBeVisibleAsync();
}

// ADR-006: Download Report exports only filtered data
[Test]
public async Task DownloadReport_ExportsOnlyFilteredData()
{
    await Page.GotoAsync("/Admin/Reports");
    await Page.SelectOptionAsync("#year-filter", "2025");
    await Page.ClickAsync("#btn-apply-filter");

    // Set up download handler
    var downloadTask = Page.WaitForDownloadAsync();
    await Page.ClickAsync("#btn-download-report");
    var download = await downloadTask;

    // Read and verify Excel file
    using var wb = new XLWorkbook(await download.PathAsync());
    var ws = wb.Worksheet(1);
    // All rows in report should be year 2025
    for (int row = 2; row <= ws.LastRowUsed().RowNumber(); row++)
    {
        var year = ws.Cell(row, 1).GetValue<string>();
        Assert.StartsWith("2025", year);
    }
}
```

#### Analytical Reports (ADR-010 to ADR-013)

```csharp
// ADR-010: Bar graph renders correctly (no JS errors)
[Test]
public async Task AnalyticalReport_BarGraph_RendersWithoutErrors()
{
    var errors = new List<string>();
    Page.PageError += (_, e) => errors.Add(e.Message);

    await Page.GotoAsync("/Admin/AnalyticalReport");
    await Expect(Page.Locator(".highcharts-root")).ToBeVisibleAsync();
    Assert.Empty(errors); // no JS console errors
}

// ADR-013: Hover over bar shows tooltip
[Test]
public async Task AnalyticalReport_HoverBar_ShowsTooltip()
{
    await Page.GotoAsync("/Admin/AnalyticalReport");
    var bar = Page.Locator(".highcharts-series-0 .highcharts-bar").First;
    await bar.HoverAsync();
    await Expect(Page.Locator(".highcharts-tooltip")).ToBeVisibleAsync();
    var tooltipText = await Page.Locator(".highcharts-tooltip").TextContentAsync();
    Assert.False(string.IsNullOrWhiteSpace(tooltipText));
}
```

#### Admin Dashboard (ADR-014 to ADR-016)

```csharp
// ADR-015: Dashboard reflects real-time state (Bug 43004)
[Test]
public async Task AdminDashboard_ReflectsRealTimeState()
{
    await Page.GotoAsync("/Admin");
    var initialCount = await Page.Locator("#migration-running-count").TextContentAsync();

    // Create a new running migration in DB
    using var db = GetTestDb();
    db.MigrationPlans.Add(new() { Status = "Running", StartedAt = DateTime.UtcNow });
    await db.SaveChangesAsync();

    // Reload and verify count increased
    await Page.ReloadAsync();
    var newCount = await Page.Locator("#migration-running-count").TextContentAsync();
    Assert.Equal(int.Parse(initialCount!) + 1, int.Parse(newCount!));
}

// ADR-016: New backup tiles present (Task 42954)
[Test]
public async Task AdminDashboard_AllFiveBackupTilesPresent()
{
    await Page.GotoAsync("/Admin");
    await Expect(Page.Locator("[data-tile='backup-running']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-tile='backup-scheduled']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-tile='backup-queued']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-tile='backup-paused']")).ToBeVisibleAsync();
    await Expect(Page.Locator("[data-tile='backup-running-auto']")).ToBeVisibleAsync();
}
```

---

### 5.7 Task Bar

**Test Cases:** TASK-001 to TASK-008 | **Modules:** Task List, Actions, Progress, Notifications

```csharp
// TASK-001: All active tasks visible
[Test]
public async Task TaskBar_ShowsAllActiveTasks()
{
    // Seed 2 running tasks in DB
    await TestDataBuilder.CreateRunningTasks(db, 2);
    await Page.GotoAsync("/");
    var taskItems = await Page.Locator(".taskbar-item").AllAsync();
    Assert.Equal(2, taskItems.Count);
}

// TASK-003: Progress updates in real time
[Test]
public async Task TaskBar_ProgressUpdatesRealTime()
{
    // Mock the progress polling endpoint to return increasing progress
    int callCount = 0;
    await Page.RouteAsync("**/api/tasks/progress**", async route =>
    {
        callCount++;
        var progress = Math.Min(callCount * 25, 100);
        await route.FulfillAsync(new()
        {
            ContentType = "application/json",
            Body = $$$"""{"status":"Running","progress":{{{progress}}}}"""
        });
    });

    await Page.GotoAsync("/tasks/1");
    // Wait for progress to reach 100%
    await Page.WaitForFunctionAsync(
        "() => document.querySelector('.progress-bar').getAttribute('value') === '100'",
        new() { Timeout = 30000 });
}

// TASK-005: Pause task from task bar
[Test]
public async Task TaskBar_PauseTask_UpdatesStatus()
{
    await Page.GotoAsync("/");
    await Page.ClickAsync(".taskbar-item:first-child .btn-pause");
    await Expect(Page.Locator(".taskbar-item:first-child .task-status"))
        .ToHaveTextAsync("Paused");

    // Verify in DB
    using var db = GetTestDb();
    var task = await db.MigrationPlans.FirstAsync();
    Assert.Equal("Paused", task.Status);
}
```

---

### 5.8 Emails & Notifications

**Test Cases:** NOT-001 to NOT-007 | **Modules:** Email Delivery, Content, Settings

```csharp
// NOT-001: Migration completion email sent
[Test]
public async Task MigrationComplete_EmailSentToAccountOwner()
{
    // Trigger migration completion
    await _client.PostAsync("/Migration/Complete",
        JsonContent.Create(new { MigrationId = 1 }));

    // Wait for email delivery
    await Task.Delay(2000);
    var mailhog = new HttpClient { BaseAddress = new Uri("http://localhost:8025") };
    var messages = await mailhog.GetFromJsonAsync<MailhogResponse>("/api/v2/messages");

    var email = messages!.Items.FirstOrDefault(m =>
        m.To.Contains("owner@tzunami.com") &&
        m.Subject.Contains("Migration Complete"));
    Assert.NotNull(email);
}

// NOT-006: Email contains correct task name and stats
[Test]
public async Task NotificationEmail_ContainsCorrectTaskStats()
{
    var messages = await GetMailhogMessages("owner@tzunami.com");
    var email = messages.First();

    Assert.Contains("Migration Task Name", email.Body);
    Assert.Contains("Files Migrated:", email.Body);
    Assert.Contains("Errors:", email.Body);
}

// NOT-004: Disable notifications for a specific task
[Test]
public async Task DisableNotifications_NoEmailSent()
{
    // Disable notifications for task 1
    await _client.PostAsync("/Notifications/Disable",
        JsonContent.Create(new { TaskId = 1 }));

    // Complete the task
    await _client.PostAsync("/Migration/Complete",
        JsonContent.Create(new { MigrationId = 1 }));

    await Task.Delay(2000);
    var messages = await GetMailhogMessages("owner@tzunami.com");
    Assert.Empty(messages.Where(m => m.Subject.Contains("Migration Complete")));
}
```

---

### 5.9 CF Grid Auto Refresh

**Test Cases:** CFR-001 to CFR-007 | **Modules:** Grid Columns, Refresh Interval

```csharp
// CFR-001: Refresh interval is config-driven (not hardcoded)
// Set RefreshIntervalSeconds = 5 in appsettings.Testing.json for fast test
[Test]
public async Task GridAutoRefresh_IntervalFromConfig_NotHardcoded()
{
    await Page.GotoAsync("/migrations");
    var startTime = DateTime.UtcNow;

    // Update a migration status directly in DB
    using var db = GetTestDb();
    var plan = await db.MigrationPlans.FirstAsync();
    plan.Status = "Running";
    await db.SaveChangesAsync();

    // Grid should update within config interval + 2s buffer
    await Expect(Page.Locator($"[data-plan-id='{plan.Id}'] .status-badge"))
        .ToHaveTextAsync("Running", new() { Timeout = 7000 }); // 5s interval + 2s buffer

    var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
    Assert.True(elapsed < 8, $"Refresh took {elapsed}s — expected < 8s for 5s config interval");
}

// CFR-003: Status reflects latest state after refresh without manual reload
[Test]
public async Task GridAutoRefresh_ReflectsLatestState_WithoutManualReload()
{
    await Page.GotoAsync("/migrations");
    await Expect(Page.Locator(".status-badge").First).ToHaveTextAsync("Not Started");

    // Change status in DB
    using var db = GetTestDb();
    var plan = await db.MigrationPlans.FirstAsync();
    plan.Status = "Completed";
    await db.SaveChangesAsync();

    // Page should auto-update without reload
    await Expect(Page.Locator(".status-badge").First)
        .ToHaveTextAsync("Completed", new() { Timeout = 10000 });
}

// CFR-002: All expected columns present
[Test]
public async Task Grid_AllExpectedColumnsPresent()
{
    await Page.GotoAsync("/migrations");
    await Expect(Page.Locator("th:has-text('Source')")).ToBeVisibleAsync();
    await Expect(Page.Locator("th:has-text('Destination')")).ToBeVisibleAsync();
    await Expect(Page.Locator("th:has-text('Status')")).ToBeVisibleAsync();
    await Expect(Page.Locator("th:has-text('Progress')")).ToBeVisibleAsync();
    await Expect(Page.Locator("th:has-text('Actions')")).ToBeVisibleAsync();
}
```

**`appsettings.Testing.json` addition:**
```json
{
  "Grid": {
    "RefreshIntervalSeconds": 5
  }
}
```

---

### 5.10 Pre-Validation

**Test Cases:** PRV-001 to PRV-025 | **Modules:** Upload CSV, Validation, Navigation Guards, Step Indicator, Data Integrity, UI Fixes, Status, Page 2, Greyed-Out Projects

This module has the most high-priority bug-regression test cases. All test cases map directly to Playwright E2E tests.

```csharp
// PRV-001: Wizard opens with 3 steps
[Test]
public async Task PreValidation_WizardHasThreeSteps()
{
    await Page.GotoAsync("/PreValidation");
    var steps = await Page.Locator(".wizard-step").AllAsync();
    Assert.Equal(3, steps.Count);
    var stepTexts = await Task.WhenAll(steps.Select(s => s.TextContentAsync()));
    Assert.Contains(stepTexts, t => t!.Contains("Download"));
    Assert.Contains(stepTexts, t => t!.Contains("Upload"));
    Assert.Contains(stepTexts, t => t!.Contains("Update"));
}

// PRV-003: Next button blocked when no CSV uploaded (Bug 43052)
[Test]
public async Task NextButton_IsDisabled_WhenNoCsvUploaded()
{
    await Page.GotoAsync("/PreValidation");
    await Expect(Page.Locator("#btn-next")).ToBeDisabledAsync();
    // Should NOT navigate to step 2
    await Page.ClickAsync("#btn-next", new() { Force = true });
    await Expect(Page.Locator(".wizard-step.active")).ToHaveTextAsync("Upload", new() { IgnoreCase = true });
}

// PRV-004: Clicking outside Upload CSV modal does not close it (Bug 43052)
[Test]
public async Task UploadCsvModal_ClickOutside_DoesNotClose()
{
    await Page.GotoAsync("/PreValidation");
    await Page.ClickAsync("#btn-upload-csv");
    await Expect(Page.Locator(".upload-modal")).ToBeVisibleAsync();

    // Click outside the modal
    await Page.Mouse.ClickAsync(10, 10);
    await Expect(Page.Locator(".upload-modal")).ToBeVisibleAsync(); // still visible
}

// PRV-006: Validation runs in batches with live count updates
[Test]
public async Task Validation_LiveCountUpdates_DuringProcessing()
{
    await Page.GotoAsync("/PreValidation");
    await Page.SetInputFilesAsync("#csv-upload", "tests/fixtures/valid-projects.csv");
    await Page.ClickAsync("#btn-next");

    // Start validation
    await Page.ClickAsync("#btn-validate");

    // Counter should increment as batches complete
    var counts = new List<string>();
    for (int i = 0; i < 10; i++)
    {
        var count = await Page.Locator("#validated-count").TextContentAsync();
        counts.Add(count!);
        await Task.Delay(500);
    }
    // Should see at least 2 distinct count values (proof of live updates)
    Assert.True(counts.Distinct().Count() > 1, "Count did not update during validation");
}

// PRV-010: Step 2 does NOT show green tick when cancelled mid-way (Bug 43082)
[Test]
public async Task StepIndicator_NoGreenTick_WhenValidationCancelled()
{
    await Page.GotoAsync("/PreValidation");
    await Page.SetInputFilesAsync("#csv-upload", "tests/fixtures/valid-projects.csv");
    await Page.ClickAsync("#btn-next");
    await Page.ClickAsync("#btn-validate");

    // Cancel mid-process
    await Task.Delay(1000);
    await Page.ClickAsync("#btn-cancel-validation");

    // Step 2 should NOT have green checkmark
    var step2 = Page.Locator(".wizard-step:nth-child(2)");
    await Expect(step2.Locator(".step-check")).Not.ToBeVisibleAsync();
}

// PRV-012: Last project not duplicated (Bug 43090)
[Test]
public async Task ValidationResults_LastProject_NotDuplicated()
{
    await Page.GotoAsync("/PreValidation");
    await Page.SetInputFilesAsync("#csv-upload", "tests/fixtures/10-projects.csv");
    await Page.ClickAsync("#btn-next");
    await Page.ClickAsync("#btn-validate");
    await Page.WaitForSelectorAsync(".validation-complete");

    var rows = await Page.Locator("table#results-table tbody tr").AllAsync();
    var projectNames = await Task.WhenAll(rows.Select(r =>
        r.Locator("td:first-child").TextContentAsync()));

    // No duplicates
    Assert.Equal(projectNames.Length, projectNames.Distinct().Count());
}

// PRV-017: Finish button greyed out while Attention Required > 0
[Test]
public async Task FinishButton_IsDisabled_WhenAttentionRequiredAboveZero()
{
    await RunValidationWith_AttentionRequiredProjects();
    await Expect(Page.Locator("#btn-finish")).ToBeDisabledAsync();
}

// PRV-023: Create Backup option hidden for greyed-out projects (Task 43036)
[Test]
public async Task GreyedOutProject_CreateBackupOption_IsHidden()
{
    await Page.GotoAsync("/PreValidation/Page2");
    var greyedProject = Page.Locator(".project-row.greyed-out").First;
    await greyedProject.ClickAsync();
    await Expect(Page.Locator(".action-menu .create-backup")).Not.ToBeVisibleAsync();
}
```

---

### 5.11 PAYG Notifications & Banner

**Test Cases:** PN-001 to PN-027 | **Modules:** Cloudsfer Administrator, Banner Creation, Banner Pop-up, Database, Monitor Service, Log

```csharp
// PN-001: Banners section added under System Settings
[Test]
public async Task SystemSettings_HasBannersSection()
{
    await Page.GotoAsync("/Admin/SystemSettings");
    await Expect(Page.Locator("a:has-text('Banners')")).ToBeVisibleAsync();
}

// PN-002: Dropdown shows Banner List and Create Banner
[Test]
public async Task BannersDropdown_HasBothOptions()
{
    await Page.GotoAsync("/Admin/SystemSettings");
    await Page.ClickAsync("a:has-text('Banners')");
    await Expect(Page.Locator("a:has-text('Banner List')")).ToBeVisibleAsync();
    await Expect(Page.Locator("a:has-text('Create Banner')")).ToBeVisibleAsync();
}

// PN-005 to PN-012: Create banners with all types and scopes
[TestCase("Maintenance")]
[TestCase("Info")]
[TestCase("Warning")]
[TestCase("License")]
[TestCase("Scheme")]
public async Task CreateBanner_AllTypes_SucceedAndAppearInList(string bannerType)
{
    await Page.GotoAsync("/Admin/Banners/Create");
    await Page.SelectOptionAsync("#banner-type", bannerType);
    await Page.FillAsync("#banner-message", $"Test {bannerType} banner");
    await Page.ClickAsync("#btn-save-banner");

    await Page.GotoAsync("/Admin/Banners");
    await Expect(Page.Locator($"td:has-text('Test {bannerType} banner')")).ToBeVisibleAsync();
}

// PN-017: Created banners listed in Settings Database
[Test]
public async Task BannerCreated_ExistsInDatabase()
{
    // Create banner via API
    var response = await _adminClient.PostAsync("/Admin/Banners/Create",
        new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Info"),
            new KeyValuePair<string, string>("Message", "DB test banner"),
            new KeyValuePair<string, string>("Scope", "Broadcast")
        }));
    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

    // Assert DB record
    using var db = GetTestDb();
    var banner = await db.Banners.FirstOrDefaultAsync(b => b.Message == "DB test banner");
    Assert.NotNull(banner);
    Assert.Equal("Info", banner.Type);
    Assert.Equal("Broadcast", banner.Scope);
}

// PN-019: Status is Draft until scheduled
[Test]
public async Task Banner_StatusIsDraft_UntilScheduled()
{
    using var db = GetTestDb();
    var banner = await db.Banners.FirstAsync();
    Assert.Equal("Draft", banner.Status);
}

// PN-022: Banner Monitor added under Monitor setting
[Test]
public async Task MonitorSetting_HasBannerMonitorEntry()
{
    await Page.GotoAsync("/Admin/MonitorSettings");
    await Expect(Page.Locator("td:has-text('Banner Monitor')")).ToBeVisibleAsync();
}

// PN-025/026/027: Monitor service logs (log assertion)
[Test]
public async Task BannerMonitor_LogsExpectedMessages()
{
    // Trigger banner sync
    await _client.PostAsync("/Admin/BannerMonitor/TriggerSync", null);
    await Task.Delay(2000);

    // Read log file
    var logContent = await File.ReadAllTextAsync("app.log");
    Assert.Contains("Starting Banner status sync", logContent);
    Assert.Contains("banners loaded", logContent);
    Assert.Contains("Banner status sync completed", logContent);
}
```

---

## 6. Special Challenges & Solutions

### 6.1 OAuth Flows — Three-Layer Strategy

| Layer | When to Use | How |
|---|---|---|
| **Unit/Mock** | CI, every commit | `RichardSzalay.MockHttp` returns fake tokens |
| **Pre-issued refresh tokens** | Integration CI | Store real refresh tokens as GitHub secrets; exchange before tests |
| **Saved browser state** | Periodic E2E smoke tests | `context.StorageStateAsync()` after manual login; refresh every 30 days |

### 6.2 Real-Time Features (Grid Refresh, Task Progress)

Use `Page.WaitForFunctionAsync()` instead of fixed sleeps:

```csharp
// DON'T: await Task.Delay(5000);
// DO:
await Page.WaitForFunctionAsync(
    "() => document.querySelector('.status').textContent !== 'Not Started'",
    new() { Timeout = 15000 });
```

For config-driven refresh intervals: set `RefreshIntervalSeconds: 3` in `appsettings.Testing.json` so tests run in seconds, not minutes.

### 6.3 File Upload Tests (Pre-Validation)

Playwright handles file uploads natively:

```csharp
await Page.SetInputFilesAsync("#csv-upload", new[] { "tests/fixtures/10-projects.csv" });
```

Maintain a set of test CSV fixtures:
- `valid-10-projects.csv` — 10 valid projects
- `projects-with-attention.csv` — includes blocked/restricted projects
- `corrupted.csv` — binary content disguised as CSV (tests PRV-013)
- `empty.csv` — zero rows (edge case)

### 6.4 Email Delivery Timing

Never use fixed `Task.Delay()` for email delivery. Use a polling loop:

```csharp
async Task<MailhogMessage> WaitForEmail(string toAddress, int timeoutSeconds = 15)
{
    var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
    while (DateTime.UtcNow < deadline)
    {
        var response = await _mailhog.GetFromJsonAsync<MailhogResponse>("/api/v2/messages");
        var email = response?.Items.FirstOrDefault(m => m.To.Contains(toAddress));
        if (email != null) return email;
        await Task.Delay(500);
    }
    throw new TimeoutException($"Email to {toAddress} not received within {timeoutSeconds}s");
}
```

### 6.5 Test Database Isolation

```csharp
// Each test class gets its own SQLite file
[SetUp]
public void SetUpDatabase()
{
    _dbPath = Path.GetTempFileName() + ".db";
    File.Copy("tests/fixtures/seed.db", _dbPath, overwrite: true);
    _db = BuildDbContext(_dbPath);
}

[TearDown]
public void TearDownDatabase()
{
    _db?.Dispose();
    if (File.Exists(_dbPath)) File.Delete(_dbPath);
}
```

---

## 7. CI/CD Pipeline Setup

```yaml
# .github/workflows/qa.yml
name: CloudsferQA Automation

on:
  push:
    branches: [master]
  pull_request:
    branches: [master]
  schedule:
    - cron: '0 2 * * *'  # nightly full run

jobs:

  # ── Layer 1: Unit Tests ─────────────────────────────────────────────────
  unit-tests:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test CloudsferQA.Tests.Unit
          --no-build -c Release
          --logger "trx;LogFileName=unit.trx"
          --results-directory TestResults
      - uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Unit Test Results
          path: TestResults/unit.trx
          reporter: dotnet-trx

  # ── Layer 2: Integration Tests ──────────────────────────────────────────
  integration-tests:
    runs-on: windows-latest
    needs: unit-tests
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Start Mailhog
        run: |
          Invoke-WebRequest "https://github.com/mailhog/MailHog/releases/download/v1.0.1/MailHog_windows_amd64.exe" -OutFile mailhog.exe
          Start-Process ./mailhog.exe -WindowStyle Hidden
        shell: pwsh
      - run: dotnet test CloudsferQA.Tests.Integration
          --logger "trx;LogFileName=integration.trx"
          --results-directory TestResults
        env:
          ASPNETCORE_ENVIRONMENT: Testing
          SMTP_HOST: localhost
          SMTP_PORT: 1025
          GOOGLE_CLIENT_ID: ${{ secrets.GOOGLE_CLIENT_ID }}
          GOOGLE_REFRESH_TOKEN: ${{ secrets.GOOGLE_REFRESH_TOKEN }}

  # ── Layer 3: E2E Tests ──────────────────────────────────────────────────
  e2e-tests:
    runs-on: windows-latest
    needs: integration-tests
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Build and install Playwright
        run: |
          dotnet build CloudsferQA.Tests.E2E -c Release
          pwsh CloudsferQA.Tests.E2E/bin/Release/net8.0/playwright.ps1 install --with-deps chromium
        shell: pwsh
      - name: Start Cloudsfer app
        run: |
          $env:ASPNETCORE_ENVIRONMENT = "Testing"
          $env:Grid__RefreshIntervalSeconds = "3"
          Start-Process dotnet -ArgumentList "run --project CloudsferQA --no-build -c Release --urls http://localhost:5000" -WindowStyle Hidden
          Start-Sleep 8
        shell: pwsh
      - run: dotnet test CloudsferQA.Tests.E2E
          --logger "trx;LogFileName=e2e.trx"
          --results-directory TestResults
        env:
          PLAYWRIGHT_BASE_URL: http://localhost:5000
          HEADLESS: true
      - name: Upload traces on failure
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-traces
          path: "**/*.zip"

  # ── Layer 4: Performance Tests (nightly only) ────────────────────────────
  performance-tests:
    runs-on: windows-latest
    needs: e2e-tests
    if: github.event_name == 'schedule'
    steps:
      - uses: actions/checkout@v4
      - name: Install k6
        run: winget install k6 --silent
      - name: Run performance tests
        run: |
          k6 run --out json=migration-results.json tests/performance/migration-load.js
          k6 run --out json=webhook-results.json tests/performance/webhook-flood.js
      - uses: actions/upload-artifact@v4
        with:
          name: k6-results
          path: "*-results.json"
```

---

## 8. Test Data Management

### 8.1 Test Accounts per Cloud Provider

| Provider | Account | Credentials Location |
|---|---|---|
| Google Drive | Dedicated G Suite test account | GitHub Secret: `GOOGLE_REFRESH_TOKEN` |
| OneDrive | M365 Dev Tenant (free) | GitHub Secret: `ONEDRIVE_REFRESH_TOKEN` |
| Dropbox | Dedicated app test account | GitHub Secret: `DROPBOX_ACCESS_TOKEN` |
| Box | Box developer account | GitHub Secret: `BOX_REFRESH_TOKEN` |
| BIM360 | Autodesk Forge sandbox | GitHub Secret: `BIM360_REFRESH_TOKEN` |
| SharePoint | Same M365 Dev Tenant as OneDrive | Shared with OneDrive credentials |

**Test folder structure in each account:**
```
/CloudsferQA/
  /SmallFiles/          10 files, <1MB each
  /LargeFiles/          3 files, 100MB–500MB each
  /DeepFolderTree/      6 levels deep, 3 files per level
  /SpecialChars/        Files with spaces, unicode, long names
  /EmptyFolders/        For empty folder handling tests
```

All test files prefixed with `[QA-TEST]` for easy cleanup.

### 8.2 Database Seeding

**Snapshot strategy** (fastest — recommended for E2E tests):
```csharp
// Copy seed.db to temp path at start of each test
var testDbPath = Path.GetTempFileName() + ".db";
File.Copy("tests/fixtures/seed.db", testDbPath, overwrite: true);
```

**Builder pattern** (best for precision — use in integration tests):
```csharp
public class TestDataBuilder
{
    private readonly AppDbContext _db;

    public static async Task<User> CreateVerifiedUser(AppDbContext db,
        string email = "qa@tzunami.com", string role = "QA")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHelper.Hash("Test@1234"),
            Role = role,
            IsEmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<TestSession> CreateSession(AppDbContext db, int userId,
        string version = "v3.41.0", string env = "APP4")
    {
        var session = new TestSession
        {
            UserId = userId,
            Tester = "qa@tzunami.com",
            Version = version,
            Environment = env,
            StartedAt = DateTime.UtcNow
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }
}
```

### 8.3 Test CSV Fixtures

Create these files in `tests/fixtures/csv/`:

| File | Purpose | Row Count |
|---|---|---|
| `valid-10-projects.csv` | Standard happy path | 10 |
| `valid-100-projects.csv` | Batch processing test | 100 |
| `projects-with-blocked.csv` | Attention Required tests | 10 (3 blocked) |
| `corrupted.csv` | PRV-013 error handling | Binary content |
| `empty.csv` | Edge case empty upload | 0 |
| `special-chars.csv` | Unicode, spaces in names | 10 |

---

## 9. Sprint Plan — Where to Start

### Sprint 1 — Foundation (Week 1–2)

**Goal:** Set up test infrastructure and automate the easiest, highest-value tests.

| # | Task | Module | Test Cases | Type |
|---|---|---|---|---|
| 1 | Set up test project structure (Unit + Integration + E2E) | All | — | Setup |
| 2 | Unit tests for PasswordHelper, StatsService | — | — | Unit |
| 3 | Login and registration API tests | Registration | REG-001/002/003 | Integration |
| 4 | Admin user management API tests | Administrator | ADM-001/002/003/004/005/006 | Integration |
| 5 | Pre-Validation navigation guard E2E tests | Pre-Validation | PRV-003/004/005 | E2E |
| 6 | Set up Mailhog and email delivery test | Registration | REG-004/006 | Integration + E2E |

### Sprint 2 — Core Coverage (Week 3–4)

| # | Task | Module | Test Cases | Type |
|---|---|---|---|---|
| 7 | Admin Monthly Reports filter tests | Administrator | ADR-001/002/003/004/005/006/007/008/009 | E2E |
| 8 | Admin Analytical Reports graph tests | Administrator | ADR-010/011/012/013 | E2E |
| 9 | CF Grid Auto Refresh tests | CF Grid | CFR-001/002/003/004/005/006/007 | E2E |
| 10 | Pre-Validation full flow tests | Pre-Validation | PRV-001/002/006/007/008/009/010/011/012/013 | E2E |
| 11 | Banner creation and DB tests | PAYG Notifications | PN-001 to PN-021 | E2E + Integration |

### Sprint 3 — Advanced (Week 5–6)

| # | Task | Module | Test Cases | Type |
|---|---|---|---|---|
| 12 | Set up WireMock for cloud storage APIs | Migration | MIG-001 to MIG-008 | Integration |
| 13 | Migration execution state machine tests | Migration | MIG-012/013/014/015 | Integration |
| 14 | Backup scheduling and token refresh tests | Backup | BAK-010/011/017/018 | Integration |
| 15 | Webhook tests with WireMock listener | Backup | BAK-013/014/015 | Integration |
| 16 | Restore execution and file verification | Restore | RES-001 to RES-008 | Integration |
| 17 | Task bar real-time progress tests | Task Bar | TASK-001/003/004/005/006 | E2E |

### Sprint 4 — Performance & Security (Week 7–8)

| # | Task | Module | Test Cases | Type |
|---|---|---|---|---|
| 18 | k6 load test — Admin Dashboard | Administrator | ADR-015 | Performance |
| 19 | k6 load test — Webhooks page 100+ entries | Backup | BAK-013 | Performance |
| 20 | OPA PowerShell smoke test scripts | OPA | OPA-001/002/003/004/005 | Pester |
| 21 | Security tests — cookie flags, domain restriction | Registration | REG-002 | Security |
| 22 | Set up Allure reporting and GitHub Actions full pipeline | All | — | CI/CD |

---

## 10. NuGet & npm Package Reference

### .NET NuGet Packages

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Playwright.NUnit` | 1.42+ | E2E browser automation |
| `Microsoft.AspNetCore.Mvc.Testing` | 8.0+ | In-process API testing |
| `xunit` | 2.7+ | Test framework |
| `xunit.runner.visualstudio` | 2.5+ | VS Test Explorer integration |
| `NUnit` | 3.14+ | Alternative test framework (Playwright samples use NUnit) |
| `NUnit3TestAdapter` | 4.5+ | NUnit runner for `dotnet test` |
| `Moq` | 4.20+ | Mocking framework |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0+ | In-memory EF Core for unit tests |
| `WireMock.Net` | 1.5+ | HTTP mock server for cloud APIs, webhooks |
| `RichardSzalay.MockHttp` | 7.0+ | Lightweight HTTP mocking inside tests |
| `Respawn` | 6.1+ | Database reset between integration tests |
| `Allure.NUnit` | 2.12+ | Rich HTML test reports |
| `SpecFlow.NUnit` | 3.9+ | BDD Gherkin support (optional) |
| `SpecFlow.Playwright` | latest | Playwright steps for SpecFlow |
| `PactNet` | 4.5+ | Contract testing between API consumer/provider |

### npm / CLI Tools

| Tool | Install | Purpose |
|---|---|---|
| k6 | `winget install k6` | Performance / load testing |
| Newman | `npm install -g newman` | Run Postman collections in CI |
| Allure CLI | `npm install -g allure-commandline` | Generate Allure HTML reports |

### Docker Images

| Image | Purpose |
|---|---|
| `mailhog/mailhog` | SMTP email capture for local/CI testing |
| `wiremock/wiremock` | Alternative to WireMock.Net for standalone server |

---

## Quick Reference — Test Case to Automation Type

| Test Case | Module | Automation Type | Priority |
|---|---|---|---|
| REG-001/002/003 | Registration | Integration | P1 |
| REG-004/006 | Registration | E2E + Email | P1 |
| MIG-001 to MIG-018 | Migration | Integration + E2E | P2 |
| BAK-010/011 | Backup | Integration | P1 |
| BAK-013/014/015 | Backup (Webhooks) | Integration | P2 |
| BAK-017/018 | Backup (Token) | Integration | P2 |
| OPA-001 to OPA-009 | OPA | Pester + Integration | P3 |
| ADM-001 to ADM-009 | Admin Users | Integration | P1 |
| ADR-003/004/005 | Admin Reports | E2E | P1 |
| ADR-006/007 | Admin Reports | E2E | P1 |
| ADR-010 to ADR-013 | Analytical Reports | E2E | P2 |
| ADR-014/015/016 | Admin Dashboard | E2E + Performance | P2 |
| TASK-001 to TASK-008 | Task Bar | E2E | P2 |
| NOT-001 to NOT-007 | Email/Notifications | Integration | P2 |
| CFR-001 to CFR-007 | CF Grid | E2E | P1 |
| PRV-001 to PRV-025 | Pre-Validation | E2E | P1 |
| PN-001 to PN-027 | PAYG Banners | E2E + Integration | P2 |
| RES-001 to RES-008 | Restore | Integration | P2 |

**P1** = automate in Sprint 1–2 | **P2** = Sprint 3 | **P3** = Sprint 4 or manual

---

*CloudsferQA Automation Guide — Internal Document — Tzunami QA Team — April 2026*
