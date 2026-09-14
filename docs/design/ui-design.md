# UI Design — Leave Management System (LMS)

**Generated:** 2026-09-14 | **Skill:** `/design-ui` | **Status:** Draft — Awaiting HITL Review

---

## Design Preferences

| | |
|---|---|
| **Design system source** | `ai-context/design-system.md` (authoritative, from /constitution interview) |
| **Color palette** | Primary: `#1565C0` (Corporate Blue) · Dark: `#003c8f` · Light: `#4491f8` · Pale: `#e3f2fd` · Error: `#d32f2f` · Warning: `#ed6c02` · Success: `#2e7d32` |
| **Typography** | Roboto (Google Fonts) · Base 14px · Headings via weight 700 |
| **Iconography** | `@mui/icons-material` (MUI v5 icon set) — SVG inlined in mockups |
| **Theming** | Both light + dark with a toggle button in every topbar and on the login page |
| **Accessibility baseline** | WCAG 2.1 AA (from `ai-context/design-system.md`) — semantic HTML, visible focus states, sufficient contrast, logical heading/landmark structure enforced in all mockups |
| **Reference anchor** | None given |
| **Selected layout pattern** | Sidebar-first: 240 px fixed sidebar (Corporate Blue background) + 64 px topbar + fluid main content area |

---

## Navigation & IA Map

```
Root
├── Login (SCR-001) — public, no sidebar
│
└── Authenticated Shell (sidebar + topbar)
    │
    ├── Employee Role
    │   ├── My Dashboard (SCR-002) — default landing
    │   ├── Apply for Leave (SCR-007)
    │   ├── My Leave History (SCR-008) — structural only
    │   ├── Team Calendar (SCR-009) — structural only
    │   └── Comp-Off (SCR-010) — structural only
    │
    ├── Manager Role (extends Employee nav)
    │   ├── Manager Dashboard (SCR-003) — default landing for managers
    │   ├── Approval Queue (SCR-012)
    │   ├── Apply for Leave (SCR-007) — shared
    │   ├── Team Calendar (SCR-009) — shared
    │   └── Leave History (SCR-008) — shared
    │
    └── HR Admin / Super Admin Role
        ├── HR Admin Dashboard (SCR-004) — default landing
        ├── Employees (SCR-015 list → SCR-016 detail) — structural only
        ├── Departments (SCR-017) — structural only
        ├── Leave Policies / Types (SCR-018) — structural only
        ├── Leave Balances (SCR-019) — structural only
        ├── Holidays (SCR-020) — structural only
        ├── Reports & Analytics (SCR-022)
        └── Audit Log (SCR-023)
            [+ Super Admin only]
            ├── User Management / Roles (SCR-021) — structural only
            └── Hangfire Dashboard (SCR-025) — structural only
```

---

## Screen Inventory

| Screen ID | Name | Feature(s) | States | Mockup |
|---|---|---|---|---|
| SCR-001 | Login Page | F-01 | Default, Error (invalid creds), Locked, Azure SSO | [Link](./mockups/SCR-001-login.html) |
| SCR-002 | Employee Dashboard | F-05, F-06, F-09 | Populated, Loading, Empty | [Link](./mockups/SCR-002-employee-dashboard.html) |
| SCR-003 | Manager Dashboard | F-06, F-07, F-08, F-09 | Populated, Loading, Overdue alerts | [Link](./mockups/SCR-003-manager-dashboard.html) |
| SCR-004 | HR Admin Dashboard | F-02, F-03, F-05, F-06, F-11 | Populated, Loading | [Link](./mockups/SCR-004-hr-admin-dashboard.html) |
| SCR-005 | Super Admin Dashboard | F-01, F-02, F-13, F-15 | Populated, Loading | Structural only |
| SCR-006 | Forgot Password / Reset | F-01 | Request sent, Error | Structural only |
| SCR-007 | Apply for Leave | F-06, F-05, F-10 | Empty form, Populated, Success, Insufficient balance error | [Link](./mockups/SCR-007-apply-for-leave.html) |
| SCR-008 | My Leave History | F-06, F-07 | Populated, Empty, Filters active | Structural only |
| SCR-009 | Team Calendar | F-06, F-07, F-10 | Monthly view, Weekly view, Empty | Structural only |
| SCR-010 | Comp-Off — Request & History | F-07 | Request form, Credit list, Empty | Structural only |
| SCR-011 | Leave Request Detail | F-06, F-08 | Pending, Approved, Rejected, Cancelled, Revoked | Structural only |
| SCR-012 | Approval Queue | F-08, F-06, F-07 | Pending list + detail panel, Empty queue, Overdue indicator | [Link](./mockups/SCR-012-approval-queue.html) |
| SCR-013 | Comp-Off Approval Queue | F-07, F-08 | Pending list, Empty | Structural only |
| SCR-014 | Notifications Inbox | F-09 | Unread, Read, Empty | Structural only |
| SCR-015 | Employee List | F-02 | Populated, Loading, Search/filter active, Empty | Structural only |
| SCR-016 | Employee Create / Edit | F-02 | Create form, Edit form, Deactivate confirm | Structural only |
| SCR-017 | Department Management | F-03 | List, Create/Edit modal, Delete confirm, Manager reassign | Structural only |
| SCR-018 | Leave Type & Policy Management | F-04 | List, Create/Edit modal, Delete confirm | Structural only |
| SCR-019 | Leave Balance Management | F-05 | Employee search, Balance list, Adjustment modal | Structural only |
| SCR-020 | Public Holiday Management | F-10 | List, Create/Edit modal, Delete confirm | Structural only |
| SCR-021 | User Management / Roles | F-01, F-02 | User list, Role assignment modal, Lock/Unlock | Structural only |
| SCR-022 | Reports & Analytics | F-11, F-12 | Overview tab, Balance tab, Comp-Off tab, By-Department tab, Export | [Link](./mockups/SCR-022-reports.html) |
| SCR-023 | Audit Log | F-13 | Filtered, Unfiltered, Empty result, JSON diff expanded | [Link](./mockups/SCR-023-audit-log.html) |
| SCR-024 | Profile & Settings | F-01, F-02 | Personal info, Change password, GDPR data export | Structural only |
| SCR-025 | Hangfire Dashboard | F-15 | Job list, Failed jobs, Job detail | Structural only |

---

## Component Inventory

| Component ID | Name | Used On | Purpose |
|---|---|---|---|
| CMP-001 | App Shell | All authenticated screens | Sidebar + topbar wrapper, theme context |
| CMP-002 | Sidebar Navigation | All authenticated screens | 240px fixed nav with role-filtered items, active state, badge indicators |
| CMP-003 | Topbar | All authenticated screens | Page title, theme toggle, user avatar/menu |
| CMP-004 | Theme Toggle Button | SCR-001, all authenticated via CMP-003 | Switches light/dark; persists to `data-theme` on root |
| CMP-005 | Leave Balance Card | SCR-002, SCR-003, SCR-004, SCR-007 | Balance display with progress bar, days used, pending count |
| CMP-006 | Stat Card | SCR-003, SCR-004 | Single metric with label and trend indicator |
| CMP-007 | Leave Request Card | SCR-012, SCR-013 | Approve/Reject/Details mini-card with overdue indicator |
| CMP-008 | Data Table | SCR-002, SCR-004, SCR-012, SCR-022, SCR-023, SCR-015 | Sortable/filterable table with pagination |
| CMP-009 | Pagination | All screens with data tables | 50-row pages with ellipsis, ARIA labels |
| CMP-010 | Confirm Modal | SCR-002, SCR-003, SCR-004, SCR-012 | Generic approve/reject/cancel confirmation dialog |
| CMP-011 | Sandwich Rule Preview | SCR-007, SCR-012 | Real-time calculation showing which days are counted as leave days |
| CMP-012 | Filter Bar | SCR-012, SCR-022, SCR-023 | Multi-field filter row with active filter chips |
| CMP-013 | Badge | All screens | Status pill (Pending/Approved/Rejected/Cancelled/Expired/Revoked) + type labels |
| CMP-014 | Alert / Notification Banner | SCR-007, SCR-003, SCR-023 | Contextual info/warning/error banners |
| CMP-015 | Empty State | All screens | Centered icon + message for zero-data states |
| CMP-016 | Avatar | All screens via topbar | 2-letter initials circle, role-coloured |
| CMP-017 | Approval Detail Panel | SCR-012, SCR-013 | Sticky right-panel showing full request details + approve/reject form |
| CMP-018 | Chart Area | SCR-004, SCR-022 | Bar/line/donut chart wrappers with legend + accessible `role="img"` |
| CMP-019 | Mini Bar Chart | SCR-003, SCR-004 | Compact 6-month team-leave trend bars |
| CMP-020 | Attachment Drop Zone | SCR-007 | Drag-and-drop file upload area for leave attachments (PDF/JPG/PNG ≤5MB) |

---

## Screen Detail

### SCR-001 — Login Page

**Owning Feature(s):** F-01 | **User Stories:** US-01.1, US-01.2, US-01.4, US-01.5
**Purpose:** Authenticates users via Azure AD SSO (primary) or local email+password (fallback).
**States:** Default · Invalid credentials (error banner, remaining attempts) · Account locked notice · Azure SSO flow
**Primary Actions:** Sign in with Azure AD (MSAL redirect) · Submit local credentials · Forgot password
**Data Displayed:** Email input, password input, error messages, lock notice
**Mockup:** [Link](./mockups/SCR-001-login.html)

---

### SCR-002 — Employee Dashboard

**Owning Feature(s):** F-05, F-06, F-09 | **User Stories:** US-05.1, US-05.2, US-06.1, US-09.1
**Purpose:** Gives employees an at-a-glance view of leave balances, upcoming leaves, notifications, and recent request history.
**States:** Populated · Loading · Empty (no leaves this year)
**Primary Actions:** Apply for Leave → SCR-007 · Request Comp-Off → SCR-010 · View History → SCR-008 · Cancel a pending request (modal)
**Data Displayed:** Balance cards (all types) · Upcoming leave timeline · Notifications list · Recent requests table
**Mockup:** [Link](./mockups/SCR-002-employee-dashboard.html)

---

### SCR-003 — Manager Dashboard

**Owning Feature(s):** F-06, F-07, F-08, F-09 | **User Stories:** US-08.2, US-08.3
**Purpose:** Gives managers an overview of team leave, pending approvals (with overdue indicators), team members on leave today, and a leave trend chart.
**States:** Populated · Overdue approvals highlighted · Team overlap warning banner · Empty pending queue
**Primary Actions:** Approve / Reject inline from dashboard · Navigate to full Approval Queue → SCR-012 · View Team Calendar → SCR-009
**Data Displayed:** Stat cards (pending/overdue/on-leave/approved) · Pending approval cards with overdue badge · On-leave-today list · 6-month trend bar chart
**Mockup:** [Link](./mockups/SCR-003-manager-dashboard.html)

---

### SCR-004 — HR Admin Dashboard

**Owning Feature(s):** F-02, F-03, F-05, F-06, F-11 | **User Stories:** US-11.x (dashboard FRs)
**Purpose:** System-wide leave management hub for HR Admins: workforce stats, department leave table, type distribution donut, system event log, and quick admin actions.
**States:** Populated · Loading · Alert for overdue approvals / account unlocks
**Primary Actions:** Add Employee · Adjust Balance · Manage Holidays · Export CSV Report · View Audit Log · Manage Roles
**Data Displayed:** 6 stat cards · Department leave table with mini utilisation bars · Leave type donut chart · System event log (Hangfire outcomes, account events)
**Mockup:** [Link](./mockups/SCR-004-hr-admin-dashboard.html)

---

### SCR-005 — Super Admin Dashboard

**Owning Feature(s):** F-01, F-02, F-13, F-15 | **User Stories:** US-13.x, US-15.x
**Purpose:** Extends HR Admin dashboard with Hangfire job status panel, audit log viewer shortcut, and user role management.
**States:** Populated · Hangfire job failure alert
**Primary Actions:** Open Hangfire Dashboard · User Management → SCR-021 · Audit Log → SCR-023
**Data Displayed:** Inherits HR Admin dashboard + Hangfire job status panel
**Mockup:** Structural inventory only — not mocked in this pass

---

### SCR-006 — Forgot Password / Reset

**Owning Feature(s):** F-01 | **User Stories:** US-01.4, US-01.5
**Purpose:** Allows local-account users to request a password reset link via email.
**States:** Request form · Confirmation (email sent) · Error (email not found)
**Primary Actions:** Submit email · Return to login
**Data Displayed:** Email input, success/error message
**Mockup:** Structural inventory only

---

### SCR-007 — Apply for Leave

**Owning Feature(s):** F-06, F-05, F-10 | **User Stories:** US-06.1, US-05.2
**Purpose:** Full leave application form with leave-type card selection, date picker, real-time sandwich rule preview, attachment upload, balance sidebar, and approver info.
**States:** Empty form · Date selected with sandwich preview · Insufficient balance error · Submission success modal
**Primary Actions:** Select leave type · Choose dates · Toggle half-day · Upload attachment · Submit (modal confirmation)
**Data Displayed:** Leave type cards with remaining balances · Date inputs · Sandwich rule day-by-day breakdown · Balance sidebar · Approver name · Team overlap status
**Mockup:** [Link](./mockups/SCR-007-apply-for-leave.html)

---

### SCR-008 — My Leave History

**Owning Feature(s):** F-06, F-07 | **User Stories:** US-06.6, US-07.3
**Purpose:** Full paginated history of all an employee's leave and comp-off requests with cancel/view-detail actions.
**States:** Populated · Filtered · Empty
**Primary Actions:** Filter by type/status/date · Cancel pending request (modal) · View detail → SCR-011
**Data Displayed:** Table: type, dates, days, reason, status, actions · Pagination
**Mockup:** Structural inventory only

---

### SCR-009 — Team Calendar

**Owning Feature(s):** F-06, F-07, F-10 | **User Stories:** US-06.3
**Purpose:** FullCalendar monthly/weekly view showing all team members' leaves and public holidays.
**States:** Monthly view · Weekly view · Empty month
**Primary Actions:** Switch month/week · Click event to view request detail
**Data Displayed:** Coloured event bars per leave type · Public holiday markers · Team member names
**Mockup:** Structural inventory only

---

### SCR-010 — Comp-Off Management

**Owning Feature(s):** F-07 | **User Stories:** US-07.1, US-07.2, US-07.3
**Purpose:** Allows employees to claim comp-off credits for overtime/holiday work and view their credit history with expiry dates.
**States:** Credit list populated · Expired credits greyed out · Claim form open
**Primary Actions:** Claim Comp-Off credit (date + reason form) · View claim history
**Data Displayed:** Active credits with expiry dates · Expired credits · Approved comp-off leaves taken
**Mockup:** Structural inventory only

---

### SCR-011 — Leave Request Detail

**Owning Feature(s):** F-06, F-08 | **User Stories:** US-06.1, US-06.4, US-06.5, US-06.6
**Purpose:** Full detail view for a single leave request — timeline of status changes, attachment preview, sandwich breakdown, and cancel/revoke action.
**States:** Pending · Approved · Rejected · Cancelled · Revoked
**Primary Actions:** Cancel (employee, if pending) · Revoke (manager/HR) · Download attachment
**Data Displayed:** All request fields · Sandwich rule breakdown · Approval timeline · Attachment link
**Mockup:** Structural inventory only

---

### SCR-012 — Approval Queue

**Owning Feature(s):** F-08, F-06, F-07 | **User Stories:** US-08.2, US-08.3
**Purpose:** Manager's primary workspace for reviewing, approving, and rejecting leave requests; features tab filtering, overdue highlights, team overlap warnings, and a detail panel with approve/reject form.
**States:** Pending list populated · Overdue items highlighted · Empty queue · Detail panel selected
**Primary Actions:** Approve · Reject (with reason) · View detail in right panel · Filter by type/date · Tab switch (Pending/Approved/Rejected/All)
**Data Displayed:** Request cards with employee name, type, dates, days, pending duration · Detail panel: all fields + sandwich calc + attachment + approval form
**Mockup:** [Link](./mockups/SCR-012-approval-queue.html)

---

### SCR-013 — Comp-Off Approval Queue

**Owning Feature(s):** F-07, F-08 | **User Stories:** US-07.2
**Purpose:** Separate queue for comp-off credit approval requests, showing overtime date, reason, and credit expiry if approved.
**States:** Pending list · Empty
**Primary Actions:** Approve · Reject (with reason)
**Data Displayed:** Employee name, overtime date, reason, credit expiry
**Mockup:** Structural inventory only

---

### SCR-014 — Notifications Inbox

**Owning Feature(s):** F-09 | **User Stories:** US-09.1, US-09.2
**Purpose:** In-app notification centre listing all leave-related system notifications for the user.
**States:** Unread items · All read · Empty
**Primary Actions:** Mark as read · Mark all read · Click notification to navigate to relevant request
**Data Displayed:** Notification type icon, message, timestamp, read/unread state
**Mockup:** Structural inventory only

---

### SCR-015 — Employee List

**Owning Feature(s):** F-02 | **User Stories:** US-02.1, US-02.3, US-02.4
**Purpose:** Paginated, searchable table of all employees with create/edit/deactivate actions.
**States:** Populated · Search active · Filter active · Empty
**Primary Actions:** + Add Employee → SCR-016 · Edit → SCR-016 · Deactivate (confirm modal) · Search · Filter by department/status
**Data Displayed:** Name, email, department, role, manager, status, join date · Actions column
**Mockup:** Structural inventory only

---

### SCR-016 — Employee Create / Edit

**Owning Feature(s):** F-02 | **User Stories:** US-02.1, US-02.2
**Purpose:** Form to create or edit an employee record including personal info, department assignment, role, manager, and photo upload.
**States:** Create (blank) · Edit (pre-populated) · Validation errors
**Primary Actions:** Save · Cancel · Deactivate (edit mode) · Upload photo
**Data Displayed:** Name, email, phone, department (dropdown), manager (dropdown), role, join date, photo
**Mockup:** Structural inventory only

---

### SCR-017 — Department Management

**Owning Feature(s):** F-03 | **User Stories:** US-03.1
**Purpose:** List and manage departments; create/edit/deactivate; shows employee count and manager per department.
**States:** List populated · Create/Edit modal open · Delete confirm (with employee re-assignment)
**Primary Actions:** + New Department · Edit · Deactivate (require reassignment if employees present)
**Data Displayed:** Dept name, description, manager, employee count, status
**Mockup:** Structural inventory only

---

### SCR-018 — Leave Type & Policy Management

**Owning Feature(s):** F-04 | **User Stories:** US-04.1
**Purpose:** Configure the 5 default leave types and any custom types: days allowed, carry-forward, half-day eligibility, medical certificate threshold, gender restrictions.
**States:** List · Create/Edit modal · Delete confirm (with balance impact warning)
**Primary Actions:** + New Leave Type · Edit · Delete (if no balances exist)
**Data Displayed:** Type name, annual allowance, carry-forward, half-day allowed, gender restriction, certificate threshold
**Mockup:** Structural inventory only

---

### SCR-019 — Leave Balance Management

**Owning Feature(s):** F-05 | **User Stories:** US-05.1, US-05.3
**Purpose:** HR Admin view to search an employee's balances and manually adjust any balance with a mandatory reason logged to audit trail.
**States:** Search empty · Employee selected, balances shown · Adjustment modal open
**Primary Actions:** Search employee · Adjust balance (+ / - with reason) · Reset to policy default
**Data Displayed:** All leave types with current balance, used, pending, adjusted; adjustment history log
**Mockup:** Structural inventory only

---

### SCR-020 — Public Holiday Management

**Owning Feature(s):** F-10 | **User Stories:** US-10.2
**Purpose:** CRUD for the annual public holiday calendar used by the sandwich rule engine.
**States:** List (year view) · Create/Edit modal · Delete confirm
**Primary Actions:** + Add Holiday · Edit · Delete
**Data Displayed:** Holiday name, date, type (national/regional/optional), year; sorted by date
**Mockup:** Structural inventory only

---

### SCR-021 — User Management / Roles

**Owning Feature(s):** F-01, F-02 | **User Stories:** US-02.4
**Purpose:** Super Admin screen to change user roles, lock/unlock accounts, reset passwords.
**States:** User list · Role change confirm · Lock/Unlock confirm
**Primary Actions:** Change role (modal) · Lock account · Unlock account · Reset password (send email)
**Data Displayed:** User name, email, role, account status, last login, failed attempts
**Mockup:** Structural inventory only

---

### SCR-022 — Reports & Analytics

**Owning Feature(s):** F-11, F-12 | **User Stories:** (FR-79 to FR-85)
**Purpose:** Tabbed reporting hub: leave overview (charts + table), balance report, comp-off report, by-department report. All tabs support CSV export.
**States:** Overview tab (default) · Balance / Comp-Off / Dept tabs · Loading · Empty result
**Primary Actions:** Switch tabs · Apply date/department/type filters · Generate · Export CSV
**Data Displayed:** Summary stat cards · Stacked bar chart (monthly by type) · Horizontal utilisation chart · Data table with pagination
**Mockup:** [Link](./mockups/SCR-022-reports.html)

---

### SCR-023 — Audit Log

**Owning Feature(s):** F-13 | **User Stories:** US-13.1, US-13.2, US-13.3
**Purpose:** Immutable, append-only audit log viewer with multi-field search (user, action type, record type, date range), active filter chips, paginated 50-row table, and expandable JSON before/after diffs.
**States:** Unfiltered (all entries) · Filtered with active chips · Empty result · JSON diff expanded
**Primary Actions:** Apply filters · Clear filters · Expand/collapse JSON diff · Navigate pages
**Data Displayed:** Timestamp, actor, action badge, record type, record ID, IP address, JSON old/new diff
**Mockup:** [Link](./mockups/SCR-023-audit-log.html)

---

### SCR-024 — Profile & Settings

**Owning Feature(s):** F-01, F-02 | **User Stories:** US-01.3 (GDPR data export)
**Purpose:** User profile page: personal info (read-only unless HR), change password (local accounts only), GDPR Art.15 data export request, notification preferences.
**States:** Viewing · Editing (HR) · Password change form · Export requested
**Primary Actions:** Change password · Request personal data export · Update notification preferences
**Data Displayed:** Name, email, department, role, phone, join date; GDPR export status
**Mockup:** Structural inventory only

---

### SCR-025 — Hangfire Dashboard

**Owning Feature(s):** F-15 | **User Stories:** US-15.1, US-15.2, US-15.3
**Purpose:** Super Admin view of the embedded Hangfire job dashboard — job list, schedules, last run, failures.
**States:** Jobs running · Failed jobs (alert) · Job history
**Primary Actions:** Trigger job manually · Retry failed job · View job detail log
**Data Displayed:** Job name, schedule (cron), last run time, status, next fire time, failure reason
**Mockup:** Structural inventory only

---
