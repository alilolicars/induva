# INDUVA — Industry & Investment Development Platform for Egypt

[![.NET 8 LTS](https://img.shields.io/badge/.NET-8.0.426%20LTS-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core 8](https://img.shields.io/badge/EF%20Core-8.0.11-512BD4)](https://learn.microsoft.com/ef/core/)
[![Tests](https://img.shields.io/badge/Tests-67%20Passed%20(100%25)-success)](file:///C:/PROJECT/tests/Induva.Tests)
[![Security Hardened](https://img.shields.io/badge/Security-Zero%20Html.Raw%20%7C%20Zero%20Raw%20SQL-brightgreen)](file:///C:/PROJECT/DOCS/SECURITY_CHECKLIST.md)

**INDUVA** is a national-scale industrial development and investment matching platform engineered for the Egyptian manufacturing ecosystem. Built as a graduation project, the platform addresses industrial fragmentation by mapping Egypt's 27 governorates, regional raw materials, factory equipment, and seasonal idle capacity, connecting manufacturers with suppliers, startups, buyers, and capital providers through grounded, deterministic AI workflows.

---

## 🏛️ Architecture & Clean Design

INDUVA follows a strict **Onion Architecture** (Clean Architecture) pattern. Dependencies strictly point inward toward the pure Domain model.

```
                           +-----------------------------------+
                           |        Induva.Web (MVC 8)         |
                           +-----------------------------------+
                                             |
                                             v
                           +-----------------------------------+
                           |        Induva.Application         |
                           |   (Services, DTOs, Prompts, Math) |
                           +-----------------------------------+
                                     /               \
                                    v                 v
            +------------------------------+   +------------------------------+
            |      Induva.Domain (Pure)    |   |    Induva.Infrastructure     |
            | (Entities, Enums, Constants) |   | (EF Core 8, Identity, ITI AI)|
            +------------------------------+   +------------------------------+
```

### Architectural Constraints Enforced:
1. **Pure Domain:** `Induva.Domain` has **zero external project dependencies** and contains no framework-specific or database types.
2. **Framework-Decoupled Application:** `Induva.Application` references only `Induva.Domain` and Microsoft abstraction packages (`Microsoft.Extensions.*`). It never references EF Core or ASP.NET Core Identity types.
3. **Deterministic Financial Math:** Financial metrics (payback period, ROI, monthly discounted NPV, ramp-up schedules) are computed **100% in deterministic C#** (`FinancialCalculator.cs`). AI is utilized strictly as an advisor for qualitative assumptions, never as a calculator.
4. **Resilient AI Pipeline:** AI integration communicates with the **ITI Student Chat API** (DeepSeek v3.2 model) using a dedicated concurrency gate, per-user daily quota limits, input-hash caching, and fail-soft fallbacks to pure rule scoring if the provider is unavailable.
5. **Zero External Runtime CDN Dependencies:** Interactive mapping (Leaflet 1.9.4) and charting (Chart.js 4.4.1) are vendored locally in `wwwroot/lib`, guaranteeing uninterrupted demo-day presentation without active Internet access.

---

## 🚀 Key Modules & AI Capabilities

| Feature Code | Name | Architecture & Mechanism | Key Output / UX |
| :--- | :--- | :--- | :--- |
| **GIS Map** | **National Industrial GIS Map** | Leaflet 1.9.4 with safe DOM text injection; filters 27 governorates | Visualizes factories, idle capacity, agricultural/mineral resources, and RFQs across Egypt |
| **F1** | **Factory Capability Profiling** | Semantic analysis over machinery rosters, idle windows, and throughput | Standardized `CapabilityTags`, manufacturing processes, strengths, constraints, underused assets |
| **F2** | **Opportunity & Product Suggestions** | Evaluates idle equipment and regional raw materials to identify target products | Product opportunities, unit cost vs price, C#-calculated margin sanity checks |
| **F3** | **Market Demand Forecasting** | Egyptian seasonal indexing (Ramadan surges, harvest cycles) | 12-month demand index curves rendered with Chart.js, peak drivers, and confidence scores |
| **F4** | **Multi-Flow Opportunity Matching** | Hybrid engine: Deterministic rule scorer (0–100) + clamped AI re-ranker (±15 pts) | Ranks top suppliers, buyers, and investors with explicit reasons, next steps, and fail-soft fallback |
| **F5** | **Investment & Payback Analysis** | AI proposes Capex/Opex assumptions; pure C# engine calculates financial metrics | 3 scenarios (Conservative, Base, Optimistic), cumulative cash flow chart, payback months, 3-yr ROI |
| **B2B** | **Connections Inbox** | Asynchronous connection requests between verified platform entities | Contact initiation, duplicate prevention, status tracking (Pending, Accepted, Declined) |
| **Admin** | **Supervisory Operations & Telemetry** | Aggregate KPIs, user lockout management, AI run audit logs, live health ping | Monitor invocation counts, provider latencies, and failure logs without leaking raw prompt text |

---

## 📋 Prerequisites & Environment

- **Operating System:** Windows 10/11, Linux, or macOS
- **.NET SDK:** .NET 8.0 LTS (pinned to `8.0.426` via [global.json](file:///C:/PROJECT/global.json))
- **Database:** Microsoft SQL Server (default connection uses LocalDB `(localdb)\MSSQLLocalDB`)
- **IDE:** Visual Studio 2022 (v17.8+), VS Code with C# Dev Kit, or JetBrains Rider

---

## ⚡ Quick Start & Setup

### 1. Clone & Build
```bash
git clone <repo-url>
cd INDUVA
dotnet restore
dotnet build
```

### 2. Configure User Secrets or Environment Variables
For live AI inference using the ITI Student Chat API, configure your API key using the .NET Secret Manager:

```bash
cd src/Induva.Web
dotnet user-secrets set "Ai:ApiKey" "<YOUR_ITI_STUDENT_API_KEY>"
dotnet user-secrets set "Seed:AdminPassword" "Admin@123456"
dotnet user-secrets set "Seed:DemoPassword" "Demo@123456"
```

*Alternatively, export the environment variable:*
```bash
set SBG_API_KEY=<YOUR_ITI_STUDENT_API_KEY>
```

### 3. Database Initialization & Seeding
Migrations apply automatically on startup. The database seeds 27 Egyptian Governorates, 23 raw materials, and the complete Golden Demo Dataset (6 factories, 6 suppliers, 8 RFQs, 4 startups, 4 investors).

To start the application:
```bash
dotnet run --project src/Induva.Web
```
Navigate to: **`https://localhost:7000`** (or `http://localhost:5000`).

---

## 🎭 Dual Operation Modes: Live vs. Demo

To ensure absolute safety during demo presentations and evaluations, INDUVA features a toggleable execution mode in `appsettings.json`:

```json
{
  "Ai": {
    "Mode": "Demo",         // Set to "Live" for real ITI DeepSeek v3.2 API, or "Demo" for pre-warmed offline responses
    "BaseUrl": "http://apiaccess.iti.net.eg",
    "ModelId": "deepseek.v3.2",
    "DailyLimitPerUser": 20,
    "CacheHours": 24
  }
}
```

- **`Live` Mode:** Connects to `http://apiaccess.iti.net.eg/api/v1/student/chat` using resilient HTTP handlers (120s attempt timeout, exponential backoff, concurrency gate).
- **`Demo` Mode:** Loads high-fidelity pre-warmed responses directly from [DemoResponses](file:///C:/PROJECT/src/Induva.Infrastructure/AI/DemoResponses) with simulated network latency (1,200 ms). Works **100% offline** with zero external network connectivity.

---

## 👥 Golden Demo Accounts & Credentials

All demo accounts are pre-seeded with the default password: **`Demo@123456`** (or admin: **`Admin@123456`**).

| Role | Email | Entity Name | Governorate | Key Focus |
| :--- | :--- | :--- | :--- | :--- |
| **Admin** | `admin@induva.com` | Platform Administration | Cairo | AI Health, User Lockout, Execution Logs |
| **Factory** | `delta.tomato@induva.com` | Delta Tomato Canning Co. | Sharqia | Food processing, canning, 80% winter idle window |
| **Factory** | `tenth.plastics@induva.com` | Tenth Plastics Ltd. | Sharqia | 150T/250T Injection molding, profile extrusion |
| **Factory** | `mahalla.textiles@induva.com` | Mahalla Garment Works | Gharbia | Multi-needle industrial sewing, cotton apparel |
| **Factory** | `sokhna.ceramics@induva.com` | Ain Sokhna Ceramics | Suez | Tunnel kilns, hydraulic tile presses |
| **Factory** | `shaq.stone@induva.com` | Shaq El Thoban Stone | Cairo | Gang saws, CNC bridge saws, marble slabs |
| **Factory** | `komombo.sugar@induva.com` | Kom Ombo Sugar Mill | Aswan | Sugarcane milling, 90% off-season idle window |
| **Supplier** | `supplier.tomato@induva.com` | Beheira Tomato Growers Co-op | Beheira | Contract agricultural produce supply |
| **Supplier** | `supplier.polymers@induva.com` | Cairo Polymer Distribution | Cairo | Prime resins & recycled plastic pellets |
| **Startup** | `startup.packaging@induva.com` | EcoPack Circular Solutions | Sharqia | Circular delivery boxes from recycled plastic |
| **Startup** | `startup.sauces@induva.com` | Qaha Gourmet Sauces | Qalyubia | Artisanal culinary sauces in glass jars |
| **Investor** | `investor.nile@induva.com` | Nile Valley Agribusiness | Delta | Agro-processing & food packaging venture capital |
| **Investor** | `investor.cairo@induva.com` | Cairo Industrial Growth Fund | Cairo | Private equity for plastics & manufacturing |

---

## 🛡️ Security & Hardening Assurances

INDUVA enforces defense-in-depth principles across every layer:

- **Zero `Html.Raw`:** Verified 0 occurrences across all `.cshtml` Razor views. All dynamic text is safely encoded by the Razor engine, preventing Stored XSS.
- **Zero Raw SQL:** Verified 0 occurrences of `FromSqlRaw`, `ExecuteSqlRaw`, or `ExecuteSqlInterpolated`. All persistence operations utilize parameterized LINQ queries through EF Core 8.
- **Global CSRF Protection:** `AutoValidateAntiforgeryTokenAttribute` is configured globally across all POST/PUT/DELETE controller endpoints.
- **Brute Force Lockout:** Identity accounts lock for 15 minutes after 5 consecutive failed access attempts; administrators can unlock accounts instantly via the admin portal.
- **Open Redirect Immunity:** All user-supplied redirect parameters validate against `Url.IsLocalUrl(...)`.
- **AI Prompt Secrecy:** Admin execution telemetry surfaces latency, run status, and feature metadata only — proprietary system prompts and provider completions are never exposed to clients.

---

## 🧪 Automated Test Suite

The solution includes an extensive automated test suite covering financial mathematics, matching algorithms, AI resilience pipelines, authorization checks, and administrative telemetry:

```bash
dotnet test
```

### Test Coverage Highlights:
- **`FinancialCalculatorTests`:** Validates payback periods, 3-year ROI, monthly discounted NPV, ramp-up volume calculations, and 3-scenario variances against known financial baselines.
- **`RuleBasedScorerTests`:** Validates Haversine distance scoring, sector matching, raw material overlap, and idle window capacity alignment across all 5 matching flows.
- **`MatchingServiceTests`:** Validates candidate filtering, fail-soft fallback when AI throws, and ±15 point adjustment clamping.
- **`InvestmentServiceTests`:** Validates zero-AI deterministic recalculations and scenario persistence.
- **`AdminDashboardServiceTests`:** Validates KPI aggregations, user role breakdowns, and AI latency calculations.
- **`AiJsonRunnerTests`:** Validates per-user quotas, cache hits, JSON repair retries, and network timeout handling.
- **`FactoryServiceTests`, `StartupServiceTests`, `SupplierServiceTests`, `InvestorServiceTests`:** Validates multi-tenant ownership enforcement and IDOR prevention.

---

## 📚 Project Documentation Directory

- 📖 **[Demo Presentation Script (7-min Walkthrough)](file:///C:/PROJECT/DOCS/DEMO_SCRIPT.md):** Complete step-by-step presentation guide for demo day.
- 🔒 **[Security & Hardening Checklist](file:///C:/PROJECT/DOCS/SECURITY_CHECKLIST.md):** Audit of all controller actions, policies, and vulnerability mitigations.
- 📝 **[Architecture & Decision Log](file:///C:/PROJECT/DOCS/DECISIONS.md):** Chronological log of engineering decisions across milestones M0 through M7.
- 📐 **[System Architecture Specification](file:///C:/PROJECT/DOCS/INDUVA_01_ARCHITECTURE.md):** Detailed layer boundaries, entity diagrams, and data flows.
- 🤖 **[AI Integration Specifications](file:///C:/PROJECT/DOCS/INDUVA_03_AI_MODULE.md):** ITI API protocols, prompt engineering guidelines, and schemas.
- 🗺️ **[Roadmap & Risk Register](file:///C:/PROJECT/DOCS/INDUVA_04_ROADMAP_AND_RISKS.md):** Milestone roadmap, risk matrix, and golden dataset definitions.

---

## 📄 License & Academic Attribution
Developed as a Graduation Project under the Industry & Investment Development Initiative for Egypt.
Engineered using ASP.NET Core 8 MVC, Entity Framework Core 8, and the ITI Student AI Infrastructure.

