# INDUVA — 04 Roadmap, Testing, Deployment and Risk Register

Order is **risk-first**: the AI integration (the biggest unknown) is proven end-to-end by M2, before the rest of the CRUD is built.
Each milestone ends with: `dotnet build` clean, `dotnet test` green, `docs/DECISIONS.md` updated, short summary, STOP.

---

## 1. Milestones

### M0 — Spike & scaffolding (½–1 day)
**Do**
- Create the solution + 4 projects + test project exactly as file 01 §2; wire project references per the dependency table; add `.gitignore` (include `appsettings.*.local.json`, `*.user`, `bin/obj`).
- Store secrets: `dotnet user-secrets init` in `Web`; `Ai:ApiKey`, `Seed:AdminPassword`. (Or env var `SBG_API_KEY`.)
- **AI spike** (throwaway console project or xUnit `[Fact(Skip=...)]`): call `POST {BaseUrl}student/chat` with a trivial prompt. Record: raw JSON shape (save to `docs/ai-response-sample.json`), latency, headers, behavior with a ~1.5k-token JSON answer, whether `<think>` or code fences appear, error body on bad key, whether `temperature`/`max_tokens` are accepted, whether it is reachable from the machine that will host the demo.
- Report the findings **to the user before building the parser**.

**Definition of Done**
- [ ] `dotnet build` works on a clean clone; layer references obey file 01.
- [ ] `docs/ai-response-sample.json` + a written summary of the response shape and latency exist.
- [ ] No secret is present in git history or any tracked file.

---

### M1 — Foundation + Factory vertical slice (2–3 days)
**Refs:** file 01 §3–5, file 02 §1–4.
**Do**
- Domain: `BaseEntity`, enums, `Roles`, `CapabilityTags`, all entities from file 01 §3 (fields only; no logic).
- Application: `Result`, `PagedResult`, repository/UoW/current-user interfaces, `FactoryService`, DTOs, mappings, `ILookupService` (governorates, raw materials; cached with `IMemoryCache`).
- Infrastructure: `ApplicationDbContext`, all `IEntityTypeConfiguration`s (cascade rules per file 01 §3.4), `GenericRepository`, `UnitOfWork`, Identity, DI, `DbInitializer` (roles, admin, 27 governorates, raw materials), first migration `Init`.
- Web: `Program.cs`, `CurrentUserService`, `_Layout` (Bootstrap, nav by role, csrf meta), `AccountController` (Register with role select limited to `Roles.SelfRegistrable`, Login, Logout, AccessDenied).
- Factory module: list (filters + paging), details, create, edit, delete; child management for **Machines, IdleWindows, Products, MaterialNeeds** (inline add/remove on the Details page via normal form posts — no SPA).

**Definition of Done**
- [ ] Register/login/logout works for every self-registrable role; `Admin` cannot be selected at registration (test it).
- [ ] A FactoryOwner can create a factory with machines, idle windows, products, material needs; another user gets 403 on edit/delete (unit test + manual).
- [ ] DB created by migration + seeder on first run; second run is idempotent.
- [ ] Unit tests: `FactoryService` ownership (create/update/delete), paging filter.

---

### M2 — AI infrastructure + F1 Factory Analysis (2–3 days) ← the "walking skeleton"
**Refs:** file 03 §2–6, §7-F1, §8, §9.
**Do**
- Application: `AiOptions`, `IAiClient`, `AiRequest/Response`, `AiProviderException`, `AiJsonCall`, `AiOutput<T>`, `JsonExtractor`, `AiJsonRunner`, `PromptCommon`, `FactoryAnalysisPrompt`, `FactoryAnalysisService`, `FactoryAnalysisResult`, DTOs.
- Infrastructure: `ItiAiClient` (+ `AiConcurrencyGate`), `ItiResponseParser` **finalized from the M0 sample**, `DemoAiClient`, resilience config (timeouts per file 02 §3.6), `AiAnalysisRunConfiguration`, `FactoryCapabilityProfileConfiguration`, migration.
- Web: `FactoryAnalysisController`, `_FactoryAnalysisCard`, `ai-actions.js`, `/Admin/AiHealth` (admin only).
- Save a real Live output as `DemoResponses/FactoryAnalysis.json`; verify Demo mode works with `Ai:Mode=Demo`.

**Definition of Done**
- [ ] Clicking "Analyze my factory" on a seeded factory returns a rendered card in Live mode (≤ ~60 s) and in Demo mode.
- [ ] Unit tests: `JsonExtractor` (fence, think-block, garbage, numbers-as-strings), `AiJsonRunner` (valid / repair-then-valid / always-invalid / 429 / quota / cache hit), `ItiResponseParser` (sample from M0 + plain-text fallback).
- [ ] An `AiAnalysisRun` row exists for every real call; second identical click is served from cache and creates no new run.
- [ ] Timeout test: with a simulated 90 s response the call still succeeds (proves HttpClient/resilience timeouts are correct).
- [ ] No API key or full prompt in logs.

---

### M3 — Remaining profiles, directories, map, connections (3–4 days)
**Refs:** file 01 §3.2, file 02 §4.5, §4.9.
**Do**
- Supplier (+ materials), Startup (with tag multi-select from `CapabilityTags.All`), InvestorProfile, DemandRequest (tags multi-select, quantity/unit, export flag, status) — each: CRUD + public directory page with filters + ownership checks. **Copy the Factory pattern; do not invent new ones.**
- `MapService` + `MapController` + Leaflet page with 4 layers (factories, suppliers, resources by governorate, open demand) and governorate filter.
- `ConnectionService`: send request (from any authenticated user to an entity owner), inbox (received/sent), accept/decline. No messaging.
- `GovernorateResource` admin screen (Admin only) to edit indicative resource data.

**Definition of Done**
- [ ] Every module has list/details/create/edit/delete with ownership tests (one test per service).
- [ ] Map shows seeded data; popups use DOM text (no HTML injection); works offline with vendored Leaflet.
- [ ] A user cannot send a connection request to themselves, nor duplicate a pending one.

---

### M4 — F2 Product Suggestions + F3 Demand Forecast (3 days)
**Refs:** file 03 §7-F2, F3.
**Do**
- Prompts, DTOs, services, controllers, partial cards, post-processing rules exactly as specified; `ProductSuggestion`, `DemandForecast` configurations + migration.
- F2 button disabled until F1 profile exists. Per-suggestion buttons: Save / Dismiss / Forecast demand / Analyze investment (the last links to M5 page; render as disabled until then).
- F3: month labels assigned server-side; Chart.js line chart with the "AI estimate" caption and data-basis badge.
- Save Live outputs as demo JSON files.

**Definition of Done**
- [ ] Post-processing unit tests: invalid items dropped, margin computed in C#, machine-subset risk flag, forecast length padding/trim, enum fallback.
- [ ] Re-running F2 keeps `Saved` suggestions and replaces `Suggested` ones.
- [ ] Golden-factory evaluation (file 03 §10, items 1–4) done for at least 3 factories; findings written in `docs/DECISIONS.md`.

---

### M5 — F5 Investment Analysis + F4 Opportunity Matching (4 days)
**Refs:** file 03 §7-F4, F5.
**Do**
- F5: `FinancialCalculator` + tests first; `InvestmentPrompt`; material-distance hint (`Geo`); assumptions endpoint (AI) → editable form → calculate endpoint (no AI) → results page with 3 scenarios + Chart.js; persist `InvestmentAnalysis`; validator + warnings; disclaimer.
- F4: `RuleBasedScorer` (all 5 flows) + tests first; `MatchingService`; AI re-rank with fail-soft fallback; `MatchResult` persistence; "Request connection" button wired to M3 `ConnectionService`.

**Definition of Done**
- [ ] All calculator/scorer tests green (including a hand-computed scenario).
- [ ] Editing an assumption and recalculating does **not** create an `AiAnalysisRun`.
- [ ] Matching with AI disabled (Demo mode with missing file, or forced failure) still returns rule-based matches with the "rules only" badge.
- [ ] Unknown candidate IDs from AI are discarded (test).

---

### M6 — Admin, hardening, UX polish (2–3 days)
**Do**
- Admin dashboard: counts (users per role, factories, suppliers, startups, open demand), AI usage (runs/day, failures, avg latency, by feature), last 50 `AiAnalysisRun` rows (no raw prompts), user list with role, lookup editing.
- Hardening: security headers (`X-Content-Type-Options`, `Referrer-Policy`), HTTPS redirect, lockout on login failures, error pages, 404/403 pages, `AsNoTracking` audit, indexes review, max-length review against `ConfigureConventions` (no truncated text/JSON columns!).
- UX: empty states, validation messages, loading states, responsive check at phone width, optional RTL (`<html dir>` when AI language = `ar`) and Arabic font (Cairo, self-hosted).
- Authorization sweep: every controller action has `[Authorize]`/`[AllowAnonymous]` explicitly; IDOR test for each service that mutates or reads private data.

**Definition of Done**
- [ ] A checklist table in `docs/SECURITY_CHECKLIST.md` with each controller action and its auth rule, all ticked.
- [ ] No `Html.Raw` anywhere (grep) and no string-built SQL (grep `FromSqlRaw`/`ExecuteSqlRaw`).
- [ ] Admin dashboard loads in < 2 s on seeded data.

---

### M7 — Demo data, demo-day safety, deployment, docs (2 days)
**Do**
- `DemoSeeder` (when `Seed:DemoData=true`, idempotent): the six golden factories, 6 suppliers, 8 demand requests, 4 startups, 4 investors (tables in §4), demo users per role (dev/demo only, passwords from user-secrets), governorate resources for ~15 governorates (marked *indicative*).
- Warm the cache: run F1 → F2 → F3 → F5 once for each golden factory in Live mode (identical inputs are then served from cache for 24 h — re-warm the morning of the demo).
- Final `DemoResponses/*.json` from best Live outputs; verify `Ai:Mode=Demo` works with a cold DB.
- `README.md` (run steps, secrets, Demo vs Live, known limitations), architecture diagram, ERD export, `docs/DEMO_SCRIPT.md`.
- Publish: `dotnet publish -c Release`; test on the target machine (network reachability of the ITI API!).

**Definition of Done**
- [ ] Fresh clone → follow README → app runs with seeded data in < 10 minutes.
- [ ] Demo script (§6) runs end-to-end in Live **and** Demo mode without errors.
- [ ] Screen-recording backup of the full demo exists.

---

## 2. Testing strategy (proportionate — it's a graduation project)

| Level | What | Tooling |
|---|---|---|
| Unit | `JsonExtractor`, `ItiResponseParser`, `FinancialCalculator`, `RuleBasedScorer`, AI post-processors, `AiJsonRunner` (fake `IAiClient`), ownership in every service | xUnit + NSubstitute |
| Service/integration | Services over EF InMemory (paging, filters, ownership, connection rules) | EF InMemory |
| Manual | Role-based walkthrough per role; golden-factory AI evaluation; phone-width check | Checklist in `docs/TEST_RUN.md` |
| Skipped on purpose | UI automation, load testing, contract tests | — |

Rule: **no AI call in automated tests.** Live AI is only exercised manually (M2, M4, M5 evaluations).

---

## 3. Deployment notes

- Config via `appsettings.json` + user-secrets / environment variables (`Ai__ApiKey` or `SBG_API_KEY`, `ConnectionStrings__Default`, `Seed__AdminPassword`).
- Migrations apply at startup (`MigrateAsync`) — acceptable here; call it out as a simplification in the report.
- If hosting behind IIS/reverse proxy, raise the request timeout above the AI latency (≥ 180 s) or the AI buttons will fail while the server is still working.
- The ITI API is plain **HTTP** and probably only reachable from ITI/university networks. The API key travels in clear text on that hop → call it **server-side only** (already the design), never from the browser, and rotate the key after the project.
- Backups: not required for the demo; mention "automated backups" as future work.

---

## 4. Demo seed data (for `DemoSeeder` and golden tests)

**Golden factories**
| # | Name | Governorate | Sector | Machines (examples) | Idle window | Needs | Current products |
|---|------|-------------|--------|--------------------|-------------|-------|------------------|
| 1 | Delta Tomato Canning Co. | Sharqia | FoodProcessing | washing line, hot-break pulper, vacuum evaporator, can seamer, retort sterilizer, glass-jar filling line | Oct–Feb, 80% free | Tomatoes, Salt | tomato paste, canned tomatoes |
| 2 | Mahalla Garment Works | Gharbia | Textiles | industrial sewing machines ×120, overlock ×30, cutting table, steam press | Jan–Feb, 30% free | Cotton | T-shirts, work uniforms |
| 3 | Tenth Plastics | Sharqia | Plastics | injection molding 150T ×4 and 250T ×2, single-screw extruder, mold storage | Dec–Feb, 25% free | Polymers (resin), Recycled plastic | food containers, crates |
| 4 | Ain Sokhna Ceramics | Suez | Ceramics | ball mill, spray dryer, hydraulic press, tunnel kiln, glazing line | Jan–Feb, 40% free | Clay, Kaolin, Feldspar, Silica sand | wall tiles |
| 5 | Shaq El Thoban Stone | Cairo | Construction | gang saw, CNC bridge saw, polishing line, waterjet cutter | none (85% utilized) | Marble, Granite | marble slabs |
| 6 | Kom Ombo Sugar Mill | Aswan | FoodProcessing | cane mills, evaporators, centrifuges, boilers | Jun–Dec, 90% free | Sugarcane | white sugar, molasses |

**Suppliers (6):** polymer distributor (Cairo), cotton ginning supplier (Beheira), tomato growers cooperative (Beheira), clay & kaolin supplier (Red Sea), stone quarry (Minya), recycled plastic flakes (Qalyubia).
**Demand requests (8):** tomato paste 40 t (Cairo) · 500 ml food containers 200,000 pcs (Giza) · school uniforms 20,000 pcs (Alexandria) · floor tiles 60×60 10,000 m² (Giza) · plastic crates 5,000 pcs (Dakahlia) · export: canned vegetables pallet (Port Said, `IsExport`) · marble slabs 300 m² (Cairo) · reusable packaging 50,000 pcs (Cairo).
**Startups (4):** reusable packaging from recycled plastic (Idea→Prototype, needs `injection-molding`, `plastic-recycling`) · agri-waste paper products (Prototype, needs `paper-converting`) · handmade leather accessories (Mvp, needs `leather-processing`) · ready-to-eat tomato sauces (Early revenue, needs `fruit-vegetable-processing`, `canning`).
**Investors (4):** two food & packaging focused (tickets 0.5–3 M EGP and 2–10 M EGP), one manufacturing generalist (5–20 M EGP), one early-stage angel (0.1–1 M EGP).
Use a mix of governorates so proximity scoring differs meaningfully. Mark all numbers/relationships as **demo data**.

---

## 5. Risk register

L = likelihood, I = impact (H/M/L).

### 5.1 AI integration
| ID | Risk | L | I | Mitigation | When |
|----|------|---|---|-----------|------|
| R1 | ITI response JSON shape unknown/changes | H | H | M0 spike; tolerant `ItiResponseParser`; raw body stored in `AiAnalysisRun`; parser unit tests on the real sample | M0, M2 |
| R2 | Latency/timeouts: `HttpClient` default 100 s, resilience-handler default 10 s attempt timeout, IIS/proxy 120 s | H | H | `Timeout = Infinite` + resilience `AttemptTimeout=120 s`, `CircuitBreaker.SamplingDuration ≥ 2×`; short outputs; AJAX with spinner; host timeout ≥ 180 s; timeout test in M2 | M2 |
| R3 | Model returns invalid JSON, fences, `<think>` blocks, extra prose, numbers as strings | H | M | `JsonExtractor`, lenient serializer options, one repair retry, typed DTOs with safe defaults, tests | M2 |
| R4 | Hallucinated numbers (prices, demand) presented as fact | H | H | Ground prompts in DB data; deterministic calculator; `confidence`, `assumptions`, `dataBasis`; editable assumptions; "AI estimate" disclaimers; server-side clamping and sanity warnings | M4–M5 |
| R5 | API rate limit / shared student key exhausted | M | H | Per-user daily quota, 24 h cache, concurrency gate (2), friendly 429 message, Demo mode | M2 |
| R6 | API unreachable on demo day (network, key expiry, ITI downtime) | M | H | `Ai:Mode=Demo`; warmed cache; screen recording; test from venue network the day before | M7 |
| R7 | Prompt injection via user-entered text (descriptions, names) | M | M | `<data>` wrapper + rule 2 in system prompt, `Clean()` sanitization & length caps, no tools/actions for the model, output encoded by Razor (no `Html.Raw`), IDs validated server-side | M2 |
| R8 | AI returns IDs/tags/machines that don't exist | H | M | Normalize against `CapabilityTags.All`; discard unknown `candidateId`; machine-name subset check; never trust enum strings | M2–M5 |
| R9 | Sensitive data leaves the system (emails, phones, user IDs) | L | H | Prompt builders take explicit field lists only (no entity serialization); code review checklist; no PII fields in prompt DTOs | M2 |
| R10 | Model ID retired/renamed (`deepseek.v3.2`) | M | M | `Ai:ModelId` in config; `/Admin/AiHealth` check | M2 |
| R11 | Different outputs on every click confuse users/evaluators | M | L | Cache by input hash; low temperature if supported; "Regenerate" is explicit | M2 |
| R12 | Plain-HTTP API: key sniffable on the network | M | M | Server-side only; never expose to browser/logs; rotate after project | M0 |
| R13 | Repair retry doubles cost/latency on bad outputs | M | L | Only one retry; track failure rate in admin dashboard; shorten prompts/schemas if > 20% failures | M6 |

### 5.2 Data
| ID | Risk | L | I | Mitigation | When |
|----|------|---|---|-----------|------|
| R14 | Cold start: empty platform makes matching/forecast pointless | H | H | `DemoSeeder` with realistic Egyptian data; F3 states data basis; F4 returns "no candidates yet" gracefully | M7 |
| R15 | Garbage-in: vague descriptions → vague AI output | H | M | Structured forms; required machine list; F1 returns `missingInformation`; preconditions block empty input | M1–M2 |
| R16 | Governorate resource data is approximate/inaccurate | H | M | Label as *indicative*; admin-editable; store a `Notes`/source field; present as "for exploration, verify before investing" | M3 |
| R17 | Free-text vs. tags mismatch breaks matching | M | H | Controlled `CapabilityTags` shared by factories, startups, demand requests | M1 |

### 5.3 Architecture & engineering
| ID | Risk | L | I | Mitigation | When |
|----|------|---|---|-----------|------|
| R18 | Over-engineering (CQRS, MediatR, generic everything) | M | H | Rules in file 00 §4; forbidden list; reviewer rejects new patterns | All |
| R19 | Onion violations (EF in Application, controllers using DbContext) | M | M | Project reference table; grep checks (`Microsoft.EntityFrameworkCore` must not appear in Application/Domain); one accepted Identity exception | All |
| R20 | `Expression<Func<T,object>>` includes can't express deep graphs | M | L | Use first-level includes; add a specific query method on a dedicated interface only when needed | M1+ |
| R21 | SQL Server "multiple cascade paths" migration error | H | M | Cascade only parent→owned children; `Restrict` on lookups; no FK to Identity | M1 |
| R22 | `ConfigureConventions` default `MaxLength(500)` silently truncates JSON/long text | M | H | Explicit `nvarchar(max)` for JSON/Description/RawResponse; test inserting 5 000 chars | M1–M2 |
| R23 | N+1 queries / cartesian explosion | M | M | `AsNoTracking` lists, includes only where used, `AsSplitQuery` when > 1 include, paging everywhere | M1+ |
| R24 | Double-submit / parallel AI clicks | M | M | Disable button; cache; per-user quota; concurrency gate | M2 |
| R25 | IDOR (user edits someone else's data by changing an ID) | H | H | Ownership checks inside services + one test per service; no ID trust from forms (e.g., `OwnerUserId` never bound from a ViewModel) | M1+ |
| R26 | Role escalation through registration | M | H | Whitelist `Roles.SelfRegistrable`; admin seeded only; test | M1 |
| R27 | Over-posting/mass assignment through MVC model binding | M | M | ViewModels (never entities) as action parameters; explicit mapping to DTOs | M1+ |
| R28 | CSRF on AJAX POSTs | M | M | Global `AutoValidateAntiforgeryToken` + header token in `ai-actions.js` | M2 |
| R29 | Stored XSS via names/descriptions/AI text | M | H | Razor encoding; map popups via DOM `textContent`; zero `Html.Raw` | M3, M6 |
| R30 | Arabic/RTL/encoding issues | M | L | `nvarchar` everywhere, UTF-8 files, `dir` toggle, Cairo font self-hosted | M6 |
| R31 | CDN/library unavailable on demo network (Leaflet tiles, Chart.js) | M | M | Vendor JS/CSS locally; OSM tiles need internet → keep a static fallback screenshot, or cache tile area; mention in README | M3, M7 |
| R32 | Time zones / date math | L | L | Store UTC; seasonal data is month numbers; server assigns forecast month labels | M4 |

### 5.4 Product, legal, project
| ID | Risk | L | I | Mitigation | When |
|----|------|---|---|-----------|------|
| R33 | Investment output read as financial advice; startup–investor matching raises regulatory questions | M | H | Disclaimer on every F5/F4-investor result; platform does not handle money or promise returns; commissions/success fees are *future work* in the report | M5 |
| R34 | Scope creep (spec says "4 features", lists 5; business model lists commissions/subscriptions/ads) | H | M | Scope lock in file 00 §2; F3 can be folded into F2; business-model items are slide-only | All |
| R35 | AI coding agent drifts from the spec / produces plausible but wrong code | M | H | Milestone-by-milestone with DoD, build/test gates, `DECISIONS.md`, review diff per milestone; kickoff prompt forbids unrequested libraries | All |
| R36 | Evaluators judge "AI" as a black box | M | M | Show the pipeline: grounded inputs → JSON → validation → deterministic math; admin AI-run log; explicit confidence/assumptions | M6–M7 |
| R37 | Not enough time to finish all modules | M | H | Cut order if needed: (1) F3 standalone page, (2) Connections inbox → simple "contact request" list, (3) Admin charts, (4) Map layers beyond factories/demand. **Never cut:** F1, F2, F4, F5, auth, ownership checks, Demo mode | All |

---

## 6. Demo script (≈ 7 minutes, works in Live and Demo mode)

1. **Map:** open the resource map → show factories, resources, open demand layers; filter Sharqia.
2. **Factory owner** (Delta Tomato Canning): open profile → machines & idle window (Oct–Feb, 80% free) → **Analyze my factory** (F1: capability tags, constraints, missing info).
3. **Product suggestions** (F2): 3–5 alternatives (e.g., ketchup/sauces, jams, canned beans) with unit economics and risks → click **Forecast demand** on one (F3: chart with Ramadan/summer peaks).
4. **Investment analysis** (F5) for one suggestion: AI assumptions → change price/volume live → recalculation with 3 scenarios and payback chart (no AI call).
5. **Opportunity matching** (F4): top suppliers (tomato growers co-op), buyers (open demand), investors, with reasons and next steps → **Request connection**.
6. **Startup view:** reusable-packaging startup → matching to Tenth Plastics (idle injection-molding capacity) and an investor.
7. **Admin:** dashboard + AI run log (latency, failures, cache hits).

---

## 7. `docs/DECISIONS.md` format

```
| Date | Milestone | Decision | Why (one line) | Deviates from spec? |
```

## 8. Global Definition of Done (project)
- [ ] Builds and tests pass on a clean clone; README run steps verified.
- [ ] No secrets in repo; `Ai:Mode=Demo` works with no network.
- [ ] Every AI result shows disclaimer + confidence; no `Html.Raw`; every action has explicit authorization.
- [ ] Risk register reviewed: each High/High item has its mitigation implemented and tested.
