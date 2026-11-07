# INDUVA — 01 Architecture

## 1. Onion layers

```
                ┌──────────────────────────────────────────────┐
                │                    Web (MVC)                 │  controllers, views, viewmodels,
                │  composition root, CurrentUserService        │  Program.cs, wwwroot
                └───────────────┬──────────────────────────────┘
                                │ references
                ┌───────────────▼───────────────┐   ┌──────────────────────────────┐
                │        Infrastructure         │   │   (also referenced by Web    │
                │ EF Core, Identity, repos, UoW │   │    only for DI registration) │
                │ ItiAiClient, DemoAiClient     │   └──────────────────────────────┘
                │ DbInitializer (seeding)       │
                └───────────────┬───────────────┘
                                │ implements interfaces of
                ┌───────────────▼───────────────┐
                │          Application          │  services, DTOs, interfaces, Result,
                │ business logic, AI orchestra- │  prompts, FinancialCalculator,
                │ tion, scoring, calculators    │  RuleBasedScorer, JsonExtractor
                └───────────────┬───────────────┘
                                │ references
                ┌───────────────▼───────────────┐
                │            Domain             │  entities, enums, constants
                │      (no dependencies)        │
                └───────────────────────────────┘
```

### Dependency rules (enforce in review)
| Project | May reference | Must NOT |
|---|---|---|
| `Induva.Domain` | nothing | any package except none |
| `Induva.Application` | `Domain`; packages `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions` | EF Core, ASP.NET, Identity, HttpClient implementations |
| `Induva.Infrastructure` | `Application` (+ transitively Domain) | `Web` |
| `Induva.Web` | `Application`, `Infrastructure` | Using `ApplicationDbContext` or repos in controllers |
| `Induva.Tests` | all | — |

Where things live that people often misplace:
- `ICurrentUserService` **interface** → Application; **implementation** → Web (needs `HttpContext`).
- `IAiClient` interface → Application; `ItiAiClient`/`DemoAiClient` → Infrastructure.
- `AiJsonRunner` (call + parse + repair + audit + cache + quota) → **Application** (it is orchestration logic, depends only on interfaces).
- `ApplicationUser : IdentityUser` → Infrastructure. Domain entities store `OwnerUserId` (string) with no FK to Identity.

---

## 2. Solution structure

```
Induva/
├─ docs/                           ← the 5 plan files + DECISIONS.md
├─ src/
│  ├─ Induva.Domain/
│  │  ├─ Common/BaseEntity.cs
│  │  ├─ Constants/Roles.cs, CapabilityTags.cs
│  │  ├─ Enums/*.cs
│  │  └─ Entities/*.cs
│  ├─ Induva.Application/
│  │  ├─ Abstractions/             IGenericRepository, IUnitOfWork, ICurrentUserService, IAiClient, IAiJsonRunner
│  │  ├─ Common/                   Result, ErrorKind, PagedResult, Geo
│  │  ├─ Dtos/                     per module
│  │  ├─ Mappings/                 manual ToDto extensions
│  │  ├─ Services/                 FactoryService, SupplierService, StartupService, InvestorService,
│  │  │                            DemandRequestService, ConnectionService, MapService, LookupService, AdminService
│  │  ├─ AI/
│  │  │  ├─ AiOptions.cs, AiJsonCall.cs, AiJsonRunner.cs, JsonExtractor.cs
│  │  │  ├─ Prompts/               FactoryAnalysisPrompt, ProductSuggestionPrompt, DemandForecastPrompt, MatchingPrompt, InvestmentPrompt
│  │  │  ├─ Results/               typed AI output DTOs
│  │  │  ├─ Features/              FactoryAnalysisService, ProductSuggestionService, DemandForecastService,
│  │  │  │                         MatchingService, InvestmentAnalysisService
│  │  │  ├─ Scoring/RuleBasedScorer.cs
│  │  │  └─ Finance/FinancialCalculator.cs
│  │  └─ DependencyInjection.cs    AddApplication()
│  ├─ Induva.Infrastructure/
│  │  ├─ Persistence/              ApplicationDbContext, Configurations/*, Repositories/*, Migrations/
│  │  ├─ Identity/ApplicationUser.cs
│  │  ├─ AI/                       ItiAiClient, ItiResponseParser, DemoAiClient, DemoResponses/*.json
│  │  ├─ Seeding/DbInitializer.cs
│  │  └─ DependencyInjection.cs    AddInfrastructure(config)
│  └─ Induva.Web/
│     ├─ Controllers/              Home, Account, Factories, Suppliers, Startups, Investors, DemandRequests,
│     │                            Map, Connections, Admin, FactoryAnalysis, ProductSuggestions, DemandForecasts,
│     │                            Matching, InvestmentAnalysis
│     ├─ ViewModels/
│     ├─ Services/CurrentUserService.cs
│     ├─ Extensions/ResultExtensions.cs
│     ├─ Views/ (Shared/_Layout, _AiCard partials)
│     ├─ wwwroot/ (css, js/ai-actions.js, js/map.js, lib/)
│     ├─ appsettings.json
│     └─ Program.cs
└─ tests/Induva.Tests/
```

### Packages per project
- **Application:** `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`
- **Infrastructure:** `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (8.x), `Microsoft.EntityFrameworkCore.SqlServer` (8.x), `Microsoft.Extensions.Http.Resilience` (8.x)
- **Web:** `Microsoft.EntityFrameworkCore.Design`
- **Tests:** `xunit`, `NSubstitute`, `Microsoft.EntityFrameworkCore.InMemory`

Migration command:
`dotnet ef migrations add Init -p src/Induva.Infrastructure -s src/Induva.Web -o Persistence/Migrations`

---

## 3. Domain model

All entities inherit `BaseEntity { int Id; DateTime CreatedAtUtc; DateTime? UpdatedAtUtc }` (Identity tables excluded).
`Csv` = comma-separated string (kept simple on purpose; normalized lowercase).

### 3.1 Reference data
| Entity | Key fields |
|---|---|
| `Governorate` | NameEn, NameAr, Latitude, Longitude |
| `RawMaterial` | NameEn, NameAr, Category (Mineral / Agricultural / Chemical / Other) |
| `GovernorateResource` | GovernorateId, RawMaterialId, EstimatedAnnualTons?, Notes — *indicative data, admin-editable* |

### 3.2 Participants
| Entity | Key fields |
|---|---|
| `Factory` | OwnerUserId, Name, Sector, GovernorateId, Latitude, Longitude, Description, UtilizationPercent (0–100), IsPublic; nav: Machines, IdleWindows, Products, MaterialNeeds, CapabilityProfile |
| `Machine` | FactoryId, Name, MachineType, Quantity, CapacityPerHour?, CapacityUnit?, Description |
| `IdleWindow` | FactoryId, StartMonth (1–12), EndMonth (1–12), AvailableCapacityPercent, Note |
| `FactoryProduct` | FactoryId, Name, Category? |
| `FactoryMaterialNeed` | FactoryId, RawMaterialId, MonthlyTons? |
| `Supplier` | OwnerUserId, CompanyName, GovernorateId, Latitude, Longitude, Description, IsPublic; nav: Materials |
| `SupplierMaterial` | SupplierId, RawMaterialId, MinOrderTons?, PriceNote? |
| `Startup` | OwnerUserId, Name, Sector, Stage, Description, ProductIdea, FundingNeededEgp?, RequiredTagsCsv, GovernorateId? |
| `InvestorProfile` | OwnerUserId, DisplayName, SectorsCsv, MinTicketEgp?, MaxTicketEgp?, PreferredStagesCsv, Bio |
| `DemandRequest` | RequesterUserId, Title, ProductName, Sector, RequiredTagsCsv, Quantity, Unit, TargetPriceEgp?, DeadlineUtc?, GovernorateId, IsExport, Status (Open/Closed), Description |
| `ConnectionRequest` | FromUserId, ToUserId, RelatedType (MatchEntityType), RelatedId, Message, Status (Pending/Accepted/Declined), RespondedAtUtc? |

### 3.3 AI-related
| Entity | Key fields |
|---|---|
| `AiAnalysisRun` | Feature, UserId, EntityType, EntityId, PromptVersion, Model, InputHash, RawResponse (trimmed to 8k), ParsedJson, Status (Pending/Succeeded/Failed), ErrorMessage, LatencyMs — **audit + cache table** |
| `FactoryCapabilityProfile` | FactoryId (unique), Summary, TagsCsv, ResultJson, AiRunId, GeneratedAtUtc |
| `ProductSuggestion` | FactoryId, AiRunId, Name, Category, Rationale, RequiredMachinesCsv, RequiredMaterialsCsv, EstUnitCostEgp, EstUnitPriceEgp, EstMonthlyVolume, DemandLevel, SeasonalityNote, Confidence (0–1), RisksJson, Status (Suggested/Saved/Dismissed) |
| `DemandForecast` | RequestedByUserId, ProductName, Category?, GovernorateId?, HorizonMonths, Trend, Confidence, ResultJson, AiRunId |
| `MatchResult` | RequestedByUserId, SourceType, SourceId, TargetType, TargetId, RuleScore, AiAdjustment, FinalScore, ReasonsJson, NextStep, AiRunId? |
| `InvestmentAnalysis` | RequestedByUserId, Title, FactoryId?, StartupId?, ProductSuggestionId?, GovernorateId, AssumptionsJson, MetricsJson, PaybackMonths?, ThreeYearRoi?, AiRunId? |

### 3.4 Relationships (ASCII)
```
Governorate 1─* Factory 1─* Machine
                     1─* IdleWindow
                     1─* FactoryProduct
                     1─* FactoryMaterialNeed *─1 RawMaterial
                     1─1 FactoryCapabilityProfile
                     1─* ProductSuggestion
Governorate 1─* Supplier 1─* SupplierMaterial *─1 RawMaterial
Governorate 1─* GovernorateResource *─1 RawMaterial
Governorate 1─* DemandRequest
AiAnalysisRun is referenced by AiRunId on every AI result table (no FK needed — plain int, avoids cascade paths)
```
> **SQL Server gotcha:** multiple cascade paths cause migration errors. Rule: cascade only parent→owned children
> (Factory→Machine, etc.). All FKs to `Governorate`, `RawMaterial` use `DeleteBehavior.Restrict`.

### 3.5 Key enums
`Sector` { FoodProcessing, Textiles, Plastics, Chemicals, Metals, Ceramics, Wood, Construction, Packaging, Electronics, Agriculture, Other }
`StartupStage` { Idea, Prototype, Mvp, EarlyRevenue, Growth }
`AiFeature` { FactoryAnalysis, ProductSuggestion, DemandForecast, Matching, Investment }
`AiRunStatus` { Pending, Succeeded, Failed }
`DemandLevel` { Low, Medium, High }
`SuggestionStatus` { Suggested, Saved, Dismissed }
`MatchEntityType` { Factory, Supplier, Startup, Investor, DemandRequest }
`ConnectionStatus` { Pending, Accepted, Declined }

### 3.6 Controlled vocabulary: Capability tags (important)
Matching is only reliable if factories, demand requests and startups speak the same vocabulary.
`CapabilityTags.All` (Domain constant list). AI output (F1) is **filtered to this list**; forms for DemandRequest/Startup use a multi-select of the same list.

```
injection-molding, extrusion, blow-molding, thermoforming, cnc-machining, metal-stamping, welding, casting,
sheet-metal-fabrication, assembly, textile-spinning, weaving, knitting, dyeing-finishing, garment-sewing,
leather-processing, food-packaging, fruit-vegetable-processing, canning, dairy-processing, milling,
bakery-production, beverage-bottling, oil-pressing, woodworking, furniture-making, stone-cutting,
ceramic-firing, glass-forming, paper-converting, printing, chemical-blending, cosmetics-filling,
plastic-recycling, 3d-printing, electronics-assembly
```

---

## 4. Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\MSSQLLocalDB;Database=Induva;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Ai": {
    "Mode": "Live",
    "BaseUrl": "http://apiaccess.iti.net.eg/api/v1/",
    "ModelId": "deepseek.v3.2",
    "OutputLanguage": "en",
    "CacheHours": 24,
    "DailyLimitPerUser": 30,
    "MaxConcurrentCalls": 2,
    "RequestTimeoutSeconds": 120
  },
  "Finance": {
    "DiscountRate": 0.20,
    "Years": 3
  },
  "Seed": {
    "AdminEmail": "admin@induva.local",
    "DemoData": true
  }
}
```
- `Ai:ApiKey` is **not** in the file. Resolve with `config["Ai:ApiKey"] ?? config["SBG_API_KEY"]` (user-secrets or env var).
- `Seed:AdminPassword` comes from user-secrets only.
- `Ai:Mode` = `Live` | `Demo` (Demo returns canned JSON from `DemoResponses/` — for demo day and offline dev).
- `Finance:DiscountRate` is a placeholder; label it as an assumption in the UI.

---

## 5. Cross-cutting conventions

| Topic | Convention |
|---|---|
| **Results** | Services return `Result` / `Result<T>`; `ErrorKind` ∈ {Validation, NotFound, Forbidden, RateLimited, AiUnavailable}. Web maps: Validation→400, NotFound→404, Forbidden→403, RateLimited→429, AiUnavailable→503 |
| **Auth** | Roles via Identity. Registration allows only `Roles.SelfRegistrable`. `Admin` is seeded. Role check on controller (`[Authorize(Roles=...)]`) + ownership check in service |
| **Ownership** | `OwnerUserId == user.UserId \|\| user.IsInRole(Admin)` — inside the service, every mutating or private-read method |
| **Validation** | ViewModel DataAnnotations (UI) + service guards (`Result.Fail(Validation, ...)`). Max lengths mirrored in EF configurations |
| **Paging** | `PagedResult<T>`; default page size 12; always `AsNoTracking` for lists |
| **Dates** | UTC in DB; month numbers (1–12) for seasonality |
| **Money** | `decimal` (18,2), EGP only |
| **Mapping** | `static class XMappings { public static XDto ToDto(this X e) }` in `Application/Mappings` |
| **Logging** | `ILogger<T>`; log AI failures with run id; never log API keys or full prompts at Information level |
| **Antiforgery** | Global `AutoValidateAntiforgeryTokenAttribute`; AJAX sends header `RequestVerificationToken` |
| **Error page** | `/Home/Error` in production; friendly AI error messages (never raw provider errors) |
| **Static libs** | Vendor Bootstrap, Chart.js, Leaflet into `wwwroot/lib` (LibMan) |

---

## 6. Request flow example (AI feature)

```
Browser (button)  ──POST /factories/12/analysis (fetch, antiforgery header)──►
FactoryAnalysisController.Run(12)
   └► IFactoryAnalysisService.AnalyzeAsync(12)
        ├─ load Factory (+Machines, IdleWindows, Products) via IUnitOfWork
        ├─ ownership + completeness checks  → Result.Fail(...) on problem
        ├─ build prompt (FactoryAnalysisPrompt)
        ├─ IAiJsonRunner.RunAsync<FactoryAnalysisResult>()
        │     ├─ quota check (AiAnalysisRun count today)
        │     ├─ cache lookup by InputHash (24h)
        │     ├─ IAiClient.ChatAsync()  ── ItiAiClient (HTTP, resilience) or DemoAiClient
        │     ├─ JsonExtractor → typed DTO  (+ one repair retry)
        │     └─ persist AiAnalysisRun (audit)
        ├─ normalize (tags ∩ CapabilityTags.All, clamp numbers)
        ├─ upsert FactoryCapabilityProfile, SaveChanges
        └─ return Result<FactoryAnalysisDto>
◄── partial view (_FactoryAnalysisCard) or JSON {error} with proper status
```
