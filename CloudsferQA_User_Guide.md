# CloudsferQA — User Guide

**Version:** 1.0  
**Platform:** http://cloudsferteam-001-site1.site4future.com/  
**Internal Use Only — Tzunami Team**

---

## Table of Contents

1. [Overview](#1-overview)
2. [Roles & Permissions](#2-roles--permissions)
3. [Getting Started — Login & Registration](#3-getting-started--login--registration)
4. [Starting a Test Session](#4-starting-a-test-session)
5. [Running Tests](#5-running-tests)
6. [Exporting Results](#6-exporting-results)
7. [Dashboard](#7-dashboard)
8. [Admin Panel — User Management](#8-admin-panel--user-management)
9. [Admin Panel — Test Case Management](#9-admin-panel--test-case-management)
10. [Bin (Deleted Items)](#10-bin-deleted-items)
11. [Frequently Asked Questions](#11-frequently-asked-questions)

---

## 1. Overview

**CloudsferQA** is an internal QA testing platform for the Tzunami team. It allows QA testers to run structured test sessions against defined test cases, record results, and export professional reports — all from a browser.

**Key capabilities:**
- Run test sessions with structured Pass / Fail / Blocked / Skip / Pending results
- Manage test cases organized by Module → Submodule
- Export results as Excel or CSV
- View real-time dashboards with charts and statistics
- Manage users and roles (Admin only)

---

## 2. Roles & Permissions

| Feature | QA | Dev | SubAdmin | Admin |
|---|---|---|---|---|
| Start & run test sessions | ✅ | ✅ | ✅ | ✅ |
| Export results (Excel / CSV) | ✅ | ✅ | ✅ | ✅ |
| View Dashboard | ✅ | ✅ | ✅ | ✅ |
| Add / Edit / Delete test cases | ❌ | ❌ | ✅ | ✅ |
| Add / Rename / Delete modules | ❌ | ❌ | ✅ | ✅ |
| Reorder modules (drag & drop) | ❌ | ❌ | ✅ | ✅ |
| Restore / Permanently delete (Bin) | ❌ | ❌ | ✅ | ✅ |
| Add / Manage users | ❌ | ❌ | ❌ | ✅ |
| Change user roles | ❌ | ❌ | ❌ | ✅ |
| Deactivate / Reactivate users | ❌ | ❌ | ❌ | ✅ |

> **Note:** Only `@tzunami.com` email addresses are accepted.

---

## 3. Getting Started — Login & Registration

### 3.1 First-Time Login

If you have been added to the system by an Admin:

1. Go to the platform URL
2. Enter your `@tzunami.com` email
3. You will be redirected to the **Register** page
4. Set your password (minimum 8 characters)
5. Confirm your password and click **Create Account**
6. You are now logged in

### 3.2 Returning Login

1. Go to the platform URL
2. Enter your `@tzunami.com` email and password
3. Click **Login**

### 3.3 Possible Login Errors

| Error | Cause | Solution |
|---|---|---|
| "Email not found" | Not yet added to the system | Contact your Admin to add you |
| "Email hasn't been verified yet" | Account not verified | Contact your Admin to verify you |
| "Account has been deactivated" | Admin has deactivated your account | Contact your Admin |
| "Incorrect password" | Wrong password | Re-enter or reset via Admin |

### 3.4 Logout

Click **Logout** in the top-right of the test interface at any time.

---

## 4. Starting a Test Session

After logging in, you will land on the **Start Session** page.

### 4.1 New Session

1. A **version code** is auto-generated for you (e.g., `john2401`). You can type a custom version instead.
2. Select the **Environment** from the dropdown:
   - Mass Test
   - APP4
   - APP3
   - APP
3. Click **Start Session**

### 4.2 Resume an Existing Session

If you have previous sessions:

1. Click **↩ Existing Version** toggle
2. Select your previous session from the list
3. Click **Continue this Session**

> Sessions that are marked **Completed** are read-only. Start a new session for a new test run.

### 4.3 Complete a Session

When testing is done:

1. Click the **✓ Complete** button in the top-right header
2. Confirm the dialog
3. The session is marked as completed and you return to the Start page

---

## 5. Running Tests

### 5.1 Interface Overview

The test interface has three areas:

| Area | Description |
|---|---|
| **Header** | Session info, progress bar, Pass/Fail/Blocked counts, action buttons |
| **Left Sidebar** | Module tree — click to jump to any module or submodule |
| **Main Content** | Test case cards — one card per test case |

### 5.2 Recording a Result

Each test case card shows:
- **TC ID** (e.g., `RA-001`) — unique identifier
- **Priority** badge — High (red) / Medium (orange) / Low (green)
- **Scenario** — what is being tested
- **Status buttons** — Pass / Fail / Blocked / Skip

To record a result:
1. Read the scenario
2. Click **Details** to expand Steps and Expected Result (optional)
3. Run the test in the application
4. Click the appropriate status button:
   - **Pass** — test passed as expected
   - **Fail** — test did not pass
   - **Blocked** — cannot test (dependency issue, environment problem, etc.)
   - **Skip** — intentionally skipping this test case
5. Optionally add a **Note** in the notes field (e.g., bug reference, comment)
6. Result saves automatically — no save button needed

### 5.3 Status Meanings

| Status | Meaning |
|---|---|
| ⬜ Pending | Not yet tested |
| ✅ Pass | Test passed as expected |
| ❌ Fail | Test failed |
| 🔶 Blocked | Cannot be tested at this time |
| ⏭ Skip | Intentionally skipped |

### 5.4 Filtering Test Cases

Use the filter bar above the test cases to show only:
- **All** — show everything
- **Pending** — only untested cases
- **Pass / Fail / Blocked / Skip** — only cases with that status

### 5.5 Sidebar Navigation

- Click a **Module** in the sidebar to scroll to it
- Click a **Submodule** to jump directly to it
- Each submodule shows a counter `(done / total)` — e.g., `(3 / 7)`

### 5.6 Bulk Actions

Each submodule section has two bulk buttons:
- **Mark All Pass** — sets all test cases in that submodule to Pass
- **Skip All** — sets all test cases in that submodule to Skip

---

## 6. Exporting Results

### 6.1 Opening the Export Modal

Click **↓ Export** in the top-right header of the test interface.

A modal will appear showing all modules with checkboxes.

### 6.2 Selecting Modules

- **All Modules** checkbox — selects or deselects all at once
- Check individual modules you want to include
- Each module shows the test case count next to it

### 6.3 Export Formats

| Button | Format | Best For |
|---|---|---|
| **↓ Export Excel** | `.xlsx` | Professional formatted report with colors |
| **↓ Export CSV** | `.csv` | Raw data, open in any spreadsheet tool |

### 6.4 Excel Report Structure

The Excel file contains:

**Summary Sheet:**
- Session metadata (Tester, Version, Environment, dates)
- Module summary table: Total, Pass, Fail, Blocked, Skip, Pending, Pass Rate %

**Per-Module Sheets (one sheet per module):**
- Columns: TC ID | Submodule | Scenario | Steps | Expected Result | Priority | Status | Notes | Tested At
- Test cases grouped by submodule with a blue label header per submodule
- Alternating grey/white row colors per submodule group
- Status cells color-coded:
  - 🟢 Pass — green
  - 🔴 Fail — red
  - 🟠 Blocked — orange
  - ⚫ Skip — grey
  - 🟡 Pending — yellow

**Filename format:**  
`CloudsferQA_[tester]_[version]_[N]Modules_[YYYYMMDD].xlsx`

### 6.5 CSV Report Structure

- File header rows (Tester, Version, Environment, etc.)
- Flat table: TC ID, Module, Submodule, Scenario, Steps, Expected Result, Priority, Status, Notes, Tested At
- UTF-8 encoded, opens cleanly in Excel

**Filename format:**  
`CloudsferQA_[tester]_[version]_[N]Modules_[YYYYMMDD].csv`

---

## 7. Dashboard

The Dashboard shows aggregated results across all test sessions.

**Access:** Click **Dashboard** from the test interface or the start page.

### 7.1 Session Selector

At the top, select any session from the dropdown to view its results.

### 7.2 KPI Cards

Six summary cards at the top:
- **Total** — total test cases
- **Pass** — passed count and percentage
- **Fail** — failed count and percentage
- **Blocked** — blocked count and percentage
- **Skip** — skipped count and percentage
- **Pending** — not yet tested count and percentage

### 7.3 Charts

| Chart | What it shows |
|---|---|
| **Result Distribution (Donut)** | Proportion of each status overall |
| **Pass Rate by Module (Bar)** | Pass % for each module |
| **Results by Module (Stacked Bar)** | Absolute counts per status per module |
| **Priority Breakdown** | Results broken down by High / Medium / Low priority |

### 7.4 Module Breakdown Table

A detailed table showing per-module statistics with a visual progress bar.

### 7.5 Failed & Blocked Cases

A list of all failed and blocked test cases with their scenarios — useful for bug reporting.

### 7.6 Recent Activity

Shows the last 20 tested cases with timestamps — tracks tester activity in real time.

---

## 8. Admin Panel — User Management

> **Access:** Admin role only. Click **Admin Panel** from the Start page.

### 8.1 Adding a User

1. Go to **Admin Panel** (`/Admin`)
2. In the **Add User to Directory** form:
   - Enter the user's `@tzunami.com` email
   - Select their role (QA / Dev / SubAdmin / Admin)
   - Click **Add User**
3. The user is now added — they can register and set their password on first login

### 8.2 Managing Users

The users table shows all registered users with:
- Email, Role, Verification status, Active status, Session count

**Available actions per user:**
| Action | What it does |
|---|---|
| **View Detail** | Opens user's test session history with stats |
| **Verify** | Manually verifies user's email (bypasses email) |
| **Change Role** | Changes role to QA / Dev / SubAdmin / Admin |
| **Deactivate / Activate** | Enables or disables login access |

### 8.3 User Detail Page

Shows all test sessions for a specific user:
- Session info (Version, Environment, Date)
- Per-session stats: Total, Pass, Fail, Blocked, Skip, Tested, Pass Rate %
- Completion status badge
- Direct link to export that session

---

## 9. Admin Panel — Test Case Management

> **Access:** Admin and SubAdmin roles. Click **Test Cases** from the Admin panel.

### 9.1 Page Overview

- Total test case count and module count shown in header
- All modules listed as collapsible accordion panels
- Modules can be drag-and-drop reordered

### 9.2 Adding a New Module

1. Click **+ Add New Module** button
2. Enter the **Module Name**
3. At least one **Submodule** row is shown automatically
   - Enter the **Submodule Name**
   - Enter the **First Scenario**
   - Select **Priority** (High / Medium / Low)
4. Click **+ Add Scenario** to add more scenarios to a submodule
5. Click **+ Add Submodule** (dashed button) to add another submodule
6. Click **Save Module** (pinned in the blue header) to save everything

> The new module will appear at the **bottom** of the module list. Drag it to reorder.

### 9.3 Adding a Submodule to Existing Module

1. Find the module in the accordion
2. Click **+ Submodule** in the module header
3. Fill in Submodule name, Scenario, Priority
4. Click **Add Test Case**

### 9.4 Adding a Test Case to Existing Submodule

1. Expand the module → find the submodule
2. Click **+ Test Case** next to the submodule
3. Fill in the form and click **Add Test Case**

**Test Case ID** is auto-generated using:
- First letters of the module name as prefix (e.g., `Registration & Activation` → `RA`)
- Sequential number: `RA-001`, `RA-002`, etc.

### 9.5 Renaming a Module

1. Click **Rename** on the module header
2. Type the new name
3. Click **Save**

> All test cases in the module are updated to the new name automatically.

### 9.6 Renaming a Submodule

1. Expand the module
2. Click **Rename** next to the submodule
3. Type the new name and click **Save**

### 9.7 Editing a Test Case

1. Expand the module and submodule
2. Click **Edit** next to the test case
3. Update any field and click **Save Changes**

> The TC ID cannot be changed.

### 9.8 Deleting a Module / Submodule / Test Case

Click the **Delete** button next to any module, submodule, or test case.

> Deleted items are **not permanently removed** — they go to the **Bin** and can be restored.

### 9.9 Reordering Modules

Drag the **⠿** handle on the left of any module header to reorder. The new order saves automatically and is reflected in the test interface and exports.

---

## 10. Bin (Deleted Items)

The **Bin** section appears at the bottom of the Test Cases page.

### 10.1 What goes in the Bin

Any module, submodule, or test case that is deleted goes to the Bin instead of being permanently removed.

### 10.2 Bin Actions

| Action | What it does |
|---|---|
| **Restore** | Brings the module and all its test cases back to the active list |
| **Delete Forever** | Permanently deletes the module and all its test cases — **cannot be undone** |
| **Empty Bin** | Permanently deletes everything in the Bin at once — **cannot be undone** |

When the Bin is empty, it shows:
> *"Bin is empty — deleted modules will appear here"*

---

## 11. Frequently Asked Questions

**Q: I can't log in — it says my email is not found.**  
A: You need to be added by an Admin first. Contact your Admin to add your `@tzunami.com` email to the system.

---

**Q: I registered but can't log in — it says email not verified.**  
A: Contact your Admin — they can verify your account manually from the Admin Panel.

---

**Q: I can't find my test results from last week.**  
A: Go to the **Start Session** page → click **↩ Existing Version** → your previous sessions will be listed there.

---

**Q: My test session expired / I was logged out mid-session.**  
A: Sessions are stored for 8 hours. Simply log back in, go to Start Session → Existing Version, and resume your session. All previously recorded results are saved.

---

**Q: The module I need is not showing in the test interface.**  
A: The module may have no test cases assigned, or it may have been deleted. Contact your Admin or SubAdmin to check the Test Case Management page.

---

**Q: I accidentally deleted a module. Can I recover it?**  
A: Yes. Go to Admin Panel → Test Cases → scroll to the **Bin** section at the bottom → click **Restore** next to the module.

---

**Q: Can I run multiple test sessions simultaneously?**  
A: Each user can have one active session at a time. To switch sessions, complete the current one and start a new one, or resume an existing one from the Existing Version list.

---

**Q: Why does my exported Excel show all test cases even ones I didn't test?**  
A: This is by design — the export includes all test cases so you have a complete test report. Untested cases appear with status `pending`.

---

**Q: What does the Pass Rate in the export mean?**  
A: Pass Rate = (Pass count ÷ Total cases) × 100. It only counts explicitly passed tests — Pending, Blocked, and Skipped are not counted as Pass.

---

**Q: The live site doesn't show my latest modules — why?**  
A: The database (`cloudsferqa.db`) needs to be uploaded to the server after adding new test cases locally. Ask your Admin to upload the latest database file via the hosting File Manager.

---

*CloudsferQA — Internal QA Platform | Tzunami Team*
