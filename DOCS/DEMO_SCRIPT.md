# INDUVA — Demo Presentation Script (≈ 7 Minutes)

This script provides a step-by-step walkthrough for evaluating and presenting the INDUVA platform to graduation committees, jury members, and industrial partners.

The flow operates identically in **Live AI Mode** (using the ITI Student Chat API with DeepSeek v3.2) and **Demo Mode** (`Ai:Mode=Demo`), ensuring zero presentation failure risk on demo day.

---

## 0. Demo Credentials Quick Reference

| Role | Account Email | Password | Primary Entity / Profile |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@induva.com` | `Demo@123456` | Platform Operations & AI Telemetry |
| **Factory Owner** | `delta.tomato@induva.com` | `Demo@123456` | Delta Tomato Canning Co. (Sharqia) |
| **Factory Owner** | `tenth.plastics@induva.com` | `Demo@123456` | Tenth Plastics Ltd. (10th of Ramadan) |
| **Startup** | `startup.packaging@induva.com` | `Demo@123456` | EcoPack Circular Solutions |
| **Supplier** | `supplier.tomato@induva.com` | `Demo@123456` | Beheira Tomato Growers Co-op |
| **Investor** | `investor.nile@induva.com` | `Demo@123456` | Nile Valley Agribusiness Ventures |

---

## Act 1: The National Industrial Map (Minute 0:00 – 1:00)

**Goal:** Demonstrate Egypt's regional resource distribution and idle industrial capacity without requiring sign-in.

1. **Navigate to:** `https://localhost:7000/Map` (or top navigation **Map** link).
2. **Key Talking Points:**
   - *"Egypt's industrial landscape suffers from informational silos: raw materials exist in one governorate while factories with idle machines operate in another without knowledge of local supply chains."*
   - Show the interactive Leaflet GIS map with vendor-bundled assets (zero CDN dependencies).
   - Toggle layers:
     - **Factories** (e.g., Delta Tomato Canning in Sharqia, Mahalla Garments in Gharbia, Sokhna Ceramics in Suez).
     - **Governorate Resources** (e.g., Tomatoes in Beheira/Sharqia, Kaolin & Silica in Suez/Red Sea, Marble in Minya/Cairo).
     - **Open Demands (RFQs)**.
   - Filter by **Sharqia** governorate to showcase industrial density in 10th of Ramadan and Belbeis.
   - Click a factory pin to view machine capacity and idle periods directly in the safe DOM popup.

---

## Act 2: Factory Diagnostics & AI Capability Profiling — F1 (Minute 1:00 – 2:15)

**Goal:** Demonstrate F1 Factory Analysis converting unstructured equipment lists into normalized industrial capabilities.

1. **Sign In:** Click **Login** &rarr; use `delta.tomato@induva.com` / `Demo@123456`.
2. **Navigate to:** **Factories** &rarr; open **Delta Tomato Canning Co.**.
3. **Inspect Factory Assets:**
   - Point out the 6 registered machines (washing line, hot-break pulper, vacuum evaporator, can seamer, retort sterilizer, glass-jar line).
   - Highlight the **Idle Capacity Window**: **October &ndash; February, 80% available capacity** (off-peak post tomato harvest).
4. **Trigger AI Analysis (F1):**
   - Click **⚡ Run AI Capability Analysis**.
   - Note the responsive UI state (button disabled with loading spinner).
   - Review generated results:
     - **Capability Tags:** `canning`, `fruit-vegetable-processing`, `food-packaging`, `beverage-bottling` (strictly validated against controlled vocabulary).
     - **Manufacturing Processes:** Step-by-step breakdown from cleaning to thermal sterilization.
     - **Key Strengths & Operational Constraints:** High boiler steam requirements and seasonal downtime.
     - **Underutilized Assets:** Highlight recommendation to monetize the glass-jar and retort lines during winter off-peak.

---

## Act 3: Opportunity Discovery & Demand Forecasting — F2 & F3 (Minute 2:15 – 3:30)

**Goal:** Demonstrate F2 Product Suggestions and F3 Market Demand Forecasting grounded in Egyptian seasonality.

1. **Navigate to:** **Product Suggestions** tab or page for Delta Tomato Canning Co.
2. **Trigger Product Suggestions (F2):**
   - Click **💡 Generate Product Suggestions**.
   - Show suggested product alternatives designed to fill the winter idle window:
     - *Ready-to-eat tomato ketchup & pasta sauces*
     - *Artisanal fruit jams & purees*
     - *Canned legumes (Fava beans / Foul Mudammas)*
   - Highlight **Deterministic Unit Economics**: unit cost, selling price, and C#-calculated gross margin % with automated margin sanity flags (< 5% thin, > 70% verify).
3. **Forecast Demand (F3):**
   - Click **📈 Forecast Demand** on the selected product candidate.
   - Review the Chart.js seasonal trend curve:
     - Explain the seasonal peaks (e.g., Ramadan consumption surge in Q1/Q2).
     - Highlight the **Data Basis badge** (`Historical`, `Trend`, `Mixed`) and AI confidence score.
     - Emphasize: *"AI forecasts trends and seasonal patterns, but the month indexing and timeline alignment are locked by deterministic application code."*

---

## Act 4: Deterministic Investment & Payback Calculator — F5 (Minute 3:30 – 4:45)

**Goal:** Prove that financial calculations are 100% deterministic C# math and immune to LLM hallucination.

1. **Navigate to:** **Investments** &rarr; **New Investment Analysis**.
2. **Propose Assumptions (AI Assistance):**
   - Select **Delta Tomato Canning Co.** and target product.
   - Click **🤖 Propose Financial Assumptions**.
   - AI proposes realistic Egyptian capital expenditure items (multi-cavity molds, changeover tooling, worker training) and unit cost estimates.
3. **Deterministic Recalculation (Zero AI):**
   - Adjust the **Unit Selling Price** from 65 EGP to 72 EGP.
   - Adjust the **Monthly Production Volume** from 15,000 to 20,000 units.
   - Click **Calculate Analysis**.
   - **Point out to the committee:**
     - The calculation returns **instantly (0 ms AI latency)** because it executes **pure C# math** without contacting the LLM.
     - Review the 3 scenarios side-by-side:
       - **Conservative:** -15% volume, +10% costs.
       - **Base Case:** Target projections.
       - **Optimistic:** +15% volume, -10% costs.
     - Inspect the Payback Period, 3-Year Cumulative Cash Flow, and discounted NPV.
     - Point out the regulatory and financial risk disclaimer: *"INDUVA provides indicative economic modeling, not financial advisory."*

---

## Act 5: Multi-Flow Opportunity Matching & Networking — F4 (Minute 4:45 – 5:45)

**Goal:** Show fail-soft multi-criteria matching connecting factories, suppliers, buyers, and investors.

1. **Navigate to:** **Opportunity Matching** &rarr; select **Delta Tomato Canning Co.**.
2. **Run Matching (F4):**
   - Select **Suppliers** flow &rarr; click **Find Matches**.
   - **Explain the Dual-Engine Architecture:**
     - **Deterministic Scorer:** Evaluates geographic distance (Haversine formula), sector compatibility, material overlap, and capacity fit to generate a base score (0&ndash;100).
     - **AI Re-Ranker:** Inspects feedstock purity, regional logistics corridors, and reliability to propose a bounded adjustment (&plusmn;15 points).
     - If AI service fails, the platform soft-fails to pure rule scores with a "Rules Only" badge.
   - Inspect top match: **Beheira Tomato Growers Co-op** with tailored next steps.
3. **Send Connection Request:**
   - Click **🤝 Connect** &rarr; modal opens.
   - Type note: *"Interested in contract purchasing 20 tons of industrial processing tomatoes for winter canning cycle."*
   - Click **Send Request**.
   - Show that self-connection and duplicate connection requests are blocked.

---

## Act 6: Startup & Circular Economy Integration (Minute 5:45 – 6:30)

**Goal:** Showcase how industrial startups leverage existing factory equipment without massive upfront capital expenditure.

1. **Switch User:** Sign out &rarr; sign in as `startup.packaging@induva.com` / `Demo@123456`.
2. **View Startup Profile:** **EcoPack Circular Solutions** (Prototype stage, seeking 1.2M EGP for reusable containers made from recycled polymers).
3. **Run Startup &rarr; Factory Matching:**
   - Match against factories with injection molding capability.
   - Top result: **Tenth Plastics Ltd.** in Sharqia with 25% idle capacity during winter.
   - Highlight circular economy synergy: Tenth Plastics provides tooling capacity, while Delta Recycled Flakes provides raw regrind flakes.

---

## Act 7: Administration, Health Checks & Telemetry (Minute 6:30 – 7:15)

**Goal:** Show the supervisory monitoring, operational security, and provider health telemetry.

1. **Switch User:** Sign out &rarr; sign in as `admin@induva.com` / `Demo@123456`.
2. **Navigate to:** **Admin** (`/Admin`).
3. **Showcase Administrative Controls:**
   - **Platform KPI Overview:** Total users, factories, suppliers, startups, investors, and open RFQs.
   - **User Security & Lockout Table:**
     - Point out user status badges.
     - Click **Lock / Unlock** to demonstrate instant account access revocation.
   - **AI Execution Telemetry:**
     - Invocations count, success rate, and average provider latency (ms).
     - 7-day invocation histogram.
     - **Audit Log (Last 50 Runs):** Displays feature, target entity, duration, cache status, and execution result.
     - **Privacy & Security Proof:** Point out that raw system prompts and raw completions are never exposed to clients or logged in administrative tables.
   - **Live AI Provider Ping:**
     - Click **⚡ Ping AI Provider**.
     - Shows live latency check and provider status (`Healthy` / `200 OK`).

---

## Summary Wrap-Up for Evaluators

- **Graduation Project Pillars Delivered:**
  1. **National Industrial Scope:** Tailored specifically for Egyptian governorates, raw materials, and public industrial tenders.
  2. **Architectural Rigor:** Clean Onion Architecture, Zero `Html.Raw` (stored XSS immune), Zero Raw SQL (SQLi immune), and strict Identity lockout.
  3. **AI as an Advisor, Not a Calculator:** All financial metrics, margins, and distance rankings are computed deterministically in C#; AI handles semantic structuring and market reasoning.
  4. **Demo-Day Resilience:** Fully operational with live ITI DeepSeek v3.2 model, or 100% offline via pre-warmed canned responses.

