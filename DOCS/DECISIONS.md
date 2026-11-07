# INDUVA — Architecture & Implementation Decisions

| Date | Milestone | Decision | Why (one line) | Deviates from spec? |
|---|---|---|---|---|
| 2026-11-06 | M0 | Pinned .NET SDK to 8.0.426 via global.json | Avoids .NET 10 preview workload resolver issues in restricted sandbox environment | No |
| 2026-11-06 | M0 | Confirmed ITI API response text field is `output_text` | Live spike with model deepseek.v3.2 returned top-level `output_text` field | No (spec expected spike discovery) |
| 2026-11-06 | M0 | Added `temperature: 0.2` payload support for ITI API | Spike confirmed ITI API accepts temperature parameter | No (file 03 §4.1 instructed to add if accepted) |
| 2026-11-06 | M1 | Added `Microsoft.AspNetCore.App` FrameworkReference to `Induva.Infrastructure` | Allows ASP.NET Core Identity DI extensions (`AddIdentity`, `AddEntityFrameworkStores`) in Infrastructure | No |
| 2026-11-06 | M1 | Added `Microsoft.Extensions.Caching.Abstractions` to `Induva.Application` | Allows `ILookupService` to cache lookups via `IMemoryCache` in Application layer | No |
| 2026-11-06 | M1 | Supported `db.Database.IsRelational()` check in `DbInitializer.SeedAsync` | Allows both relational migrations (`MigrateAsync`) and in-memory test databases (`EnsureCreatedAsync`) | No |
| 2026-11-06 | M1 | Included child entity operations (machines, idle windows, products, material needs) directly in `FactoryService` | Enforces uniform ownership and IDOR checks within service methods before mutating child tables | No |
| 2026-11-06 | M2 | Finalized `ItiResponseParser` with `output_text` as primary property | Matches live ITI API format while keeping backwards compatibility for OpenAI format | No |
| 2026-11-06 | M2 | Configured HttpClient resilience with 120s attempt timeout and infinite client timeout | Prevents standard 100s HttpClient timeout from killing long deepseek inference calls | No |
| 2026-11-06 | M2 | Controlled vocabulary filtering for F1 output against `CapabilityTags.All` | Eliminates hallucinated tags and guarantees joinability across platform features | No |
| 2026-11-06 | M3 | Vendored Leaflet (v1.9.4) & Chart.js (v4.4.1) locally into `wwwroot/lib` | Ensures offline capability and compliance with demo environment constraints | No |
| 2026-11-06 | M3 | Map popups rendered via DOM `textContent` rather than `innerHTML` | Mitigates stored XSS from untrusted user text in factory, supplier, and demand request listings | No |
| 2026-11-06 | M3 | Universal `_ConnectButtonAndModal` partial with anti-self & anti-duplicate pending validation | Prevents self-messaging and redundant connection spam across all entity types | No |
| 2026-11-06 | M4 | Computed unit margin % deterministically in C# with risk threshold flags (< 5% thin, > 70% verify) | Adheres to core principle: "AI is an advisor, not a calculator" | No |
| 2026-11-06 | M4 | Server assigns forecast month labels starting from next calendar month | Mitigates AI date math errors (R32) and aligns with Egyptian seasonality | No |
| 2026-11-06 | M4 | Dynamic forecast length adjustment (pad/trim to exactly N values, cap confidence <= 0.3) | Guarantees consistent Chart.js visualization regardless of LLM array truncation | No |
| 2026-11-06 | M4 | F2 Product Suggestions preserves `Saved` items when generating new opportunities | Retains user pipeline state while replacing transient `Suggested` items | No |
| 2026-11-06 | M5 | Pure function `FinancialCalculator` executes 100% of financial math (payback, ROI, monthly discounted NPV, 3 scenarios) | AI proposes assumptions only; financial math is deterministic C#; recalculations never invoke AI | No |
| 2026-11-06 | M5 | `RuleBasedScorer` fail-soft architecture with ±15 score adjustment clamping | Rules computed first; if AI is unavailable, falls back gracefully to pure rule scores with UI badge; clamps AI variance | No |
| 2026-11-06 | M6 | Enforced zero `Html.Raw` across all Razor views and zero raw SQL strings in data access | Eliminates stored XSS and SQL injection across entire solution | No |
| 2026-11-06 | M6 | Global security response headers middleware (`nosniff`, `SAMEORIGIN`, `strict-origin-when-cross-origin`) | Hardens browser security and eliminates clickjacking risks | No |
| 2026-11-06 | M6 | Identity account lockout (5 failed attempts, 15m lockout) & admin toggle | Protects against brute-force attacks and provides administrative security override | No |
| 2026-11-06 | M6 | Admin AI telemetry excludes raw prompt and raw completion disclosure | Prevents proprietary prompt and system prompt leakage via telemetry endpoints | No |
| 2026-11-06 | M7 | Golden demo seeder (`SeedGoldenDemoDataAsync`) initialized from Roadmap §4 | Pre-loads 6 realistic Egyptian factories, 6 suppliers, 8 RFQs, 4 startups, 4 investors, and governorate resources | No |
| 2026-11-06 | M7 | Zero external CDN dependency & dual mode (Live / Demo) architecture | Guarantees demo day success even if ITI network or venue internet is offline | No |
