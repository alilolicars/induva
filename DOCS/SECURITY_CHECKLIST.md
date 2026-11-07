# INDUVA — Security & Hardening Checklist

This document verifies the defense-in-depth security posture of INDUVA as specified in Milestone M6.

---

## 1. Controller Endpoint Authorization Audit

| Controller | Action / Endpoint | HTTP Method | Policy / Access Rule | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **AccountController** | `Login` | GET, POST | `[AllowAnonymous]` | Protected by lockout policy and `Url.IsLocalUrl` redirect check |
| | `Register` | GET, POST | `[AllowAnonymous]` | Protected by `Roles.SelfRegistrable` whitelist (blocks unauthorized Admin registration) |
| | `Logout` | POST | `[Authorize]` | Requires active session |
| | `AccessDenied` | GET | `[AllowAnonymous]` | Friendly 403 screen |
| **AdminController** | `Index` | GET | `[Authorize(Roles = "Admin")]` | Telemetry, stats, user accounts |
| | `ToggleLockout` | POST | `[Authorize(Roles = "Admin")]` | Manual account lockout toggle |
| | `AiHealth` | GET | `[Authorize(Roles = "Admin")]` | Live AI provider ping |
| **ConnectionsController** | `Index` | GET | `[Authorize]` | User connection inbox (sent & received) |
| | `Send` | POST | `[Authorize]` | Sender identity tied to authenticated claims; verified `Url.IsLocalUrl` redirect |
| | `Respond` | POST | `[Authorize]` | Target recipient verified against authenticated user |
| **DemandForecastsController**| `Index` | GET | `[Authorize]` | Aggregate market forecasts |
| | `Run` | POST | `[Authorize]` | On-demand forecast trigger |
| **DemandRequestsController** | `Index` | GET | `[AllowAnonymous]` | Public RFQ marketplace listing |
| | `Details` | GET | `[AllowAnonymous]` | Public RFQ specification |
| | `Create` | GET, POST | `[Authorize]` | Creator set to authenticated user |
| | `Edit` | GET, POST | `[Authorize]` | Verified ownership check (`RequesterUserId == currentUserId`) |
| | `Delete` | POST | `[Authorize]` | Verified ownership or Admin role check |
| **FactoriesController** | `Index` | GET | `[AllowAnonymous]` | Public factory directory (excludes unlisted) |
| | `Details` | GET | `[AllowAnonymous]` | Factory profile and public equipment |
| | `Create` | GET, POST | `[Authorize]` | Creator set to authenticated user |
| | `Edit` | GET, POST | `[Authorize]` | Verified ownership or Admin role check |
| | `Delete` | POST | `[Authorize]` | Verified ownership or Admin role check |
| | `AddMachine` | POST | `[Authorize]` | Verified factory ownership |
| | `AddIdleWindow`| POST | `[Authorize]` | Verified factory ownership |
| | `AddProduct` | POST | `[Authorize]` | Verified factory ownership |
| **FactoryAnalysisController**| `Run` | POST | `[Authorize]` | Quota enforced; ownership verified |
| **GovernorateResourcesController** | `Index` | GET | `[Authorize(Roles = "Admin")]` | Regional resource list |
| | `Create` | GET, POST | `[Authorize(Roles = "Admin")]` | Admin resource creation |
| | `Edit` | GET, POST | `[Authorize(Roles = "Admin")]` | Admin resource modification |
| | `Delete` | POST | `[Authorize(Roles = "Admin")]` | Admin resource removal |
| **HomeController** | `Index` | GET | `[AllowAnonymous]` | Platform landing page |
| | `Error` | GET | `[AllowAnonymous]` | Friendly 404/403/500 error view with status codes |
| **InvestmentsController** | `Index` | GET | `[Authorize]` | Analysis form & past scenarios |
| | `ProposeAssumptions` | POST | `[Authorize]` | AI assistance for realistic financial assumptions |
| | `Calculate` | POST | `[Authorize]` | Deterministic C# engine calculation (zero AI) |
| | `Details` | GET | `[Authorize]` | Investment scenario review |
| **InvestorsController** | `Index` | GET | `[AllowAnonymous]` | Public investor directory |
| | `Details` | GET | `[AllowAnonymous]` | Investor profile |
| | `MyProfile` | GET, POST | `[Authorize(Roles = "Investor,Admin")]`| Self-management of ticket sizes & criteria |
| **MapController** | `Index` | GET | `[AllowAnonymous]` | Interactive Egypt industrial GIS map |
| | `Data` | GET | `[AllowAnonymous]` | GeoJSON / marker points |
| **MatchingController** | `Index` | GET | `[Authorize]` | Multi-flow opportunity matching engine |
| | `Run` | POST | `[Authorize]` | Rule-based scoring + AI re-ranking pipeline |
| **ProductSuggestionsController**| `Index` | GET | `[Authorize]` | AI product suggestions review |
| | `Run` | POST | `[Authorize]` | AI suggestion generator |
| **StartupsController** | `Index` | GET | `[AllowAnonymous]` | Public startup directory |
| | `Details` | GET | `[AllowAnonymous]` | Startup profile & stage |
| | `Create` | GET, POST | `[Authorize]` | Creator set to authenticated user |
| | `Edit` | GET, POST | `[Authorize]` | Ownership or Admin role check |
| | `Delete` | POST | `[Authorize]` | Ownership or Admin role check |
| **SuppliersController** | `Index` | GET | `[AllowAnonymous]` | Public supplier directory |
| | `Details` | GET | `[AllowAnonymous]` | Materials & specifications |
| | `Create` | GET, POST | `[Authorize]` | Creator set to authenticated user |
| | `Edit` | GET, POST | `[Authorize]` | Ownership or Admin role check |
| | `Delete` | POST | `[Authorize]` | Ownership or Admin role check |
| | `AddMaterial`| POST | `[Authorize]` | Ownership check |

---

## 2. Core Vulnerability Defenses

### A. Cross-Site Scripting (XSS)
- **Zero `Html.Raw`**: Verified 0 usages of `Html.Raw` across all `.cshtml` Razor views in the entire repository.
- **Strict DOM Insertion in JS**: Interactive map popups (`map.js`) and AI card renderers construct DOM elements using `document.createElement`, `textContent`, and safe DOM assignment rather than raw string concatenation into `innerHTML`.
- **Security Response Headers**: `X-Content-Type-Options: nosniff` header is emitted on every HTTP response.

### B. SQL Injection (SQLi)
- **Zero Raw SQL Execution**: Verified 0 occurrences of `FromSqlRaw`, `ExecuteSqlRaw`, or `ExecuteSqlInterpolated` across all C# code.
- **Strict EF Core Querying**: All database operations use strongly-typed LINQ with EF Core 8 parameterized queries.

### C. Cross-Site Request Forgery (CSRF)
- **Global Antiforgery Validation**: `AutoValidateAntiforgeryTokenAttribute` is registered in `builder.Services.AddControllersWithViews(...)`.
- **Header & Form Support**: Antiforgery configured with header name `RequestVerificationToken` for AJAX requests and standard hidden form tokens for HTML form posts.

### D. Open Redirect Prevention
- All user-supplied redirect targets (e.g., `returnUrl` in `AccountController.Login` and `Referer` in `ConnectionsController.RedirectBack`) are validated using `Url.IsLocalUrl(...)`.

### E. Brute Force & Identity Lockout
- Configured in ASP.NET Core Identity:
  - `Lockout.MaxFailedAccessAttempts = 5`
  - `Lockout.DefaultLockoutTimeSpan = 15 minutes`
  - `Lockout.AllowedForNewUsers = true`
- Login requests execute with `lockoutOnFailure: true`.
- Admins can manually lock or unlock accounts via `AdminController.ToggleLockout`.

### F. Privilege Escalation
- Self-registration endpoint explicitly limits role assignment to `Roles.SelfRegistrable` (`Factory`, `Supplier`, `Startup`, `Investor`).
- The `Admin` role cannot be self-selected or injected via HTTP POST payloads.

### G. AI Prompt & Secret Exposure
- User secrets (`Ai:ApiKey`) and environment variables (`SBG_API_KEY`) are kept out of source code and configuration files.
- Admin execution telemetry surfaces run metadata, latency, status, and error messages — raw system prompts and provider completions are never exposed to the client.
- Daily per-user AI quotas (`DailyLimitPerUser`) protect against provider abuse and denial-of-wallet.

### H. Clickjacking & Frame Options
- `X-Frame-Options: SAMEORIGIN` is configured globally via response middleware.
- `Referrer-Policy: strict-origin-when-cross-origin` protects user navigation privacy.

