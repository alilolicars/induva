# INDUVA — Implementation Plan (Handoff Pack for Antigravity)

> Industry & Investment Development Platform for Egypt
> ASP.NET Core 8 MVC · Onion Architecture · EF Core + SQL Server · AI via ITI API (`deepseek.v3.2`)
> **Graduation project — do NOT over-engineer.**

---

## 0. Pack contents (read in this order)

| # | File | What it gives you |
|---|------|-------------------|
| 00 | `INDUVA_00_README.md` | Scope lock, tech decisions, agent rules, **kickoff prompt** |
| 01 | `INDUVA_01_ARCHITECTURE.md` | Onion layers, solution tree, dependency rules, domain model, config, conventions |
| 02 | `INDUVA_02_CODE_EXAMPLES.md` | Reference code for every layer (entity, repo, service, controller, view, DI, seeder, tests) |
| 03 | `INDUVA_03_AI_MODULE.md` | ITI API client, JSON runner, the 5 AI features (prompts, DTOs, services, scorer, calculator) |
| 04 | `INDUVA_04_ROADMAP_AND_RISKS.md` | Milestones with Definition of Done, testing, deployment, **risk register** |

Put all five files in `/docs` at the repo root so the agent can re-read them.

---

## 1. Product in one paragraph

INDUVA connects raw-material resources, factories, suppliers, buyers, startups and investors in one platform, and uses AI to
(a) understand what a factory can do, (b) suggest new products for its existing machines (especially during idle seasons),
(c) estimate demand, (d) match the factory/startup with the right supplier / buyer / investor, and
(e) give a first-pass investment estimate. The platform does **not** process payments.

---

## 2. Scope lock

### 2.1 In scope (MVP)

**Non-AI modules**
1. Auth with roles: `Admin`, `FactoryOwner`, `Supplier`, `Investor`, `Startup`, `Buyer`
2. Factory profiles: machines, idle-season windows, current products, raw-material needs
3. Supplier profiles (+ materials they supply)
4. Startup profiles (idea, stage, funding need, required capabilities)
5. Investor profiles (sectors, ticket size, stages)
6. Demand requests (buyer/RFQ: product, quantity, target price, deadline, export flag)
7. Resource & opportunity map (Leaflet + OpenStreetMap): factories, suppliers, governorate resources, open demand
8. Connection requests (a user asks to connect with another user; accept / decline)
9. Admin dashboard (users, lookups, AI usage, AI run log)

**AI modules (5 — see 2.2)**
| Code | Feature | Summary |
|------|---------|---------|
| F1 | Factory Analysis | Turn machines + description into a structured capability profile (tags, processes, strengths, constraints) |
| F2 | Product Suggestions | Suggest up to 5 alternative products the factory can make with current machines |
| F3 | Demand Forecast | Month-by-month demand index for a product (grounded in platform data + AI knowledge, clearly labeled as an estimate) |
| F4 | Opportunity Matching | Rank suppliers / buyers / investors / factories for a given factory or startup (rules first, AI re-ranks + explains) |
| F5 | Investment Analysis | AI proposes assumptions → **C# calculator** computes payback / ROI / break-even → user can edit assumptions and recalc |

### 2.2 Note on "4 vs 5" features
The requirement says "4 features" but lists 5 rows (Factory Analysis, Product Suggestions, Demand Forecast, Opportunity Matching,
Investment Analysis). **This plan implements all 5**, because F3 is the lightest one. If the supervisor insists on exactly 4,
drop the standalone F3 page and keep only the `demandLevel` + `seasonalityNote` fields already returned by F2.
Location Analysis is ignored, as requested (the map is non-AI).

### 2.3 Explicitly OUT of scope (do not build, do not scaffold)
- Payments, commissions, subscriptions, ads (business-model slide only)
- Chat / messaging system (connection requests only)
- Email / SMS / push notifications
- Export-order pooling logic (only an `IsExport` flag)
- AI location analysis, AI-driven transport cost optimizer
- Full localization resources (`.resx`). Only: UI in English, AI output language configurable (`en`/`ar`), optional RTL via `dir`
- CQRS, MediatR, AutoMapper, FluentValidation, microservices, message brokers, Redis, Hangfire, Docker orchestration
- Mobile app, public API for third parties

---

## 3. Technology decisions (final)

| Concern | Decision |
|---|---|
| Runtime | .NET 8, ASP.NET Core MVC (Razor views) |
| Architecture | Onion: `Domain` ← `Application` ← `Infrastructure` / `Web` |
| ORM / DB | EF Core 8 + SQL Server (LocalDB for dev) |
| Auth | ASP.NET Core Identity (cookie), roles seeded |
| Data access | `IGenericRepository<T>` + `IUnitOfWork` (no per-entity repos unless a query truly needs it) |
| Mapping | Manual extension methods (`ToDto()`), no AutoMapper |
| Validation | DataAnnotations on ViewModels + guard checks inside services |
| Errors | `Result` / `Result<T>` with `ErrorKind` — no exceptions for business failures |
| UI | Bootstrap 5, Chart.js, Leaflet — **vendored into `wwwroot/lib`** (no CDN at runtime) |
| AI transport | Typed `HttpClient` + `Microsoft.Extensions.Http.Resilience` |
| AI output | Strict JSON → `System.Text.Json` DTOs; extractor + one repair retry |
| Caching | `IMemoryCache` for lookups; DB-backed cache for AI runs (by input hash) |
| Logging | Built-in `ILogger` (no Serilog) |
| Tests | xUnit + NSubstitute; EF InMemory for service tests |
| Secrets | `dotnet user-secrets` / environment variable `SBG_API_KEY`. **Never commit keys** |

---

## 4. Rules for the coding agent (non-negotiable)

1. **Dependency direction:** `Domain` references nothing. `Application` references only `Domain`. `Infrastructure` references `Application`.
   `Web` references `Application` + `Infrastructure` (composition root only). No EF Core types in `Application` signatures.
2. **One accepted deviation:** `AccountController` may use `SignInManager<ApplicationUser>` / `UserManager<ApplicationUser>` directly. Nothing else in `Web` may touch `Infrastructure` types.
3. **Controllers are thin:** bind → call one service method → map `Result` to a view/JSON. No business logic, no `DbContext`.
4. **Never trust AI output:** parse → validate → clamp → normalize against catalogs/IDs → only then persist/render. Never `Html.Raw` AI text.
5. **All money/ratio math is deterministic C#** (`FinancialCalculator`, `RuleBasedScorer`). AI supplies assumptions/explanations only.
6. **Ownership checks live in services**, not in views or controllers (prevents IDOR).
7. **Everything async with `CancellationToken`.** Use `DateTime.UtcNow`. Strings in DB are `nvarchar` (Arabic).
8. **No new libraries / patterns** beyond section 3 without asking. If a task seems to need one, stop and ask.
9. **Small steps:** work milestone by milestone (file 04). After each milestone: `dotnet build` (0 warnings that matter), `dotnet test`, then summarize and stop.
10. When the spec is ambiguous, choose the **simplest option that satisfies the Definition of Done** and write the choice in `docs/DECISIONS.md` (one line each).

---

## 5. Kickoff prompt — paste into Antigravity

```
You are the lead engineer for INDUVA, a graduation project: an ASP.NET Core 8 MVC web app using Onion Architecture,
EF Core + SQL Server, ASP.NET Core Identity, and an AI module that calls the ITI student chat API (model deepseek.v3.2).

The full specification is in /docs:
  INDUVA_00_README.md, INDUVA_01_ARCHITECTURE.md, INDUVA_02_CODE_EXAMPLES.md,
  INDUVA_03_AI_MODULE.md, INDUVA_04_ROADMAP_AND_RISKS.md
Read all five files completely before writing any code. They are the source of truth. The code in file 02 and 03 is reference
code: follow its structure, naming and behavior, but you may fix compile errors and adapt details.

Hard rules:
- Do NOT over-engineer. No MediatR, CQRS, AutoMapper, FluentValidation, Redis, Hangfire, microservices.
- Respect the Onion dependency rules in file 01. Domain has zero dependencies; Application never references EF Core.
- Build exactly the scope in file 00 §2. Anything in §2.3 is forbidden.
- AI features: follow file 03 exactly (strict JSON, extractor + repair retry, caching, quota, Demo mode, deterministic math in C#).
- Never hard-code or log secrets. API key comes from user-secrets or env var SBG_API_KEY.

Process:
1. Start with Milestone M0 in file 04. The ITI API response shape is UNKNOWN — do the spike first and report the raw JSON shape
   to me before building the parser.
2. Work one milestone at a time. After each milestone: dotnet build, dotnet test, update docs/DECISIONS.md, then give me a short
   summary (what was built, what deviates from the spec and why, what is next) and STOP and wait for my "continue".
3. If something in the spec is ambiguous or seems wrong, choose the simplest option, log it in docs/DECISIONS.md, and tell me.

Begin with M0 now.
```

## 6. Per-milestone prompt template

```
Implement Milestone <Mx> from INDUVA_04_ROADMAP_AND_RISKS.md.
Re-read the sections of files 01–03 it references. Meet every item in its Definition of Done.
Run dotnet build and dotnet test. Summarize results and any deviations, update docs/DECISIONS.md, and stop.
```
