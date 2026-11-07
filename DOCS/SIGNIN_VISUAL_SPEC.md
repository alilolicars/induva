# INDUVA — Sign In Page Visual Specification & Implementation Guide

## 1. Overview & Context

This document defines the high-fidelity UI visual spec and front-end implementation standard for the **INDUVA Sign In** page (`/Account/Login`), designed for Egypt's B2B industrial ecosystem.

- **Screen Viewport:** 1440 × 900 px (single viewport without outer scrollbars on desktop).
- **Page Theme:** Clean Light Industrial Theme with a balanced 50/50 split layout.
- **Visual Personality:** Engineered, credible, industrial-grade, calm, and uncluttered. Warm paper canvas, crisp hairline borders, deep teal primary, copper AI accents, and an ink-paneled value showcase.
- **Reference Mockup:**
  - Standalone Prototype: [`signin_mockup.html`](file:///C:/Users/alilo/.gemini/antigravity/brain/003777c3-332b-4f74-b5d0-0e8ec2d4330c/signin_mockup.html)
  - Razor View Implementation: [`src/Induva.Web/Views/Account/Login.cshtml`](file:///C:/PROJECT/src/Induva.Web/Views/Account/Login.cshtml)
  - Global Shell: [`src/Induva.Web/Views/Shared/_Layout.cshtml`](file:///C:/PROJECT/src/Induva.Web/Views/Shared/_Layout.cshtml)

---

## 2. Design Tokens & Styling Rules

| Token Name | Hex Value | Usage in Sign In Page |
| :--- | :--- | :--- |
| **Warm Paper (Page BG)** | `#F6F4EF` | Right side canvas background |
| **Ink (Deep Navy)** | `#0E1B24` | Left panel background, primary headers & input text |
| **Surface (Card & Navbar)** | `#FFFFFF` | Navbar background, 440px floating card, inputs |
| **Hairline Border** | `#E3DFD6` | Navbar border, floating card border, card divider |
| **Input Border** | `#D5D0C5` | Form input outlines and checkbox borders |
| **Primary Deep Teal** | `#0B5563` | "Sign in" primary CTA button, "Create an account" link, "Forgot password?" link |
| **Teal Hover** | `#094651` | Hover state for primary button and active links |
| **Teal Accent / Eyebrow** | `#64A7B3` | "WELCOME BACK" eyebrow text, vector contour lines |
| **Muted Text** | `#5B6670` | Card subtitle, helper text, switcher prefix |
| **Subtle / Placeholder** | `#8A949C` | Input placeholder text, icons, security footnote |
| **Accent AI (Copper)** | `#C96A2B` | Navbar "AI Tools" sparkle icon |

### Typography
- **Primary Font Family:** `IBM Plex Sans` (Latin) + `IBM Plex Sans Arabic` fallback.
- **Monospace Font Family:** `IBM Plex Mono` (for numeric metrics and code).
- **H1 Headline:** 38px / line-height 1.18, 700 weight, `#FFFFFF`.
- **H2 Card Title:** 28px / line-height 1.2, 700 weight, `#0E1B24`.
- **Body & Inputs:** 14px – 15px, 400 & 500 weight.
- **Eyebrow:** 13px, 700 weight, uppercase, letter-spacing `0.14em`.

---

## 3. Structural Layout Breakdown

### A. Global Navigation Bar (`height: 64px`)
- **Left:** INDUVA Brand Logo with the golden industrial cogwheel emblem (`شعار إندوفا الصناعي بالعجلة الذهبية.webp`).
- **Center:** Navigation items (`Explore ▾`, `Map`, `AI Tools ▾` with copper sparkle icon `#C96A2B`).
- **Right:** Logged-out state actions — `"Sign in"` text link + `"Get started"` deep teal button (`#0B5563`).

### B. Left Panel — 50% Width (`#0E1B24`)
- **Decorative Elements:** Faint organic topographic contour curves (`#4EA5B3`, 22–28% opacity) anchored in top-left and bottom-right.
- **Eyebrow:** `WELCOME BACK` (`#64A7B3`, uppercase, letter-spacing `0.14em`).
- **Main Heading:** `Your factory network is waiting.` (`#FFFFFF`, 38px, bold).
- **3 Value Propositions:**
  1. `Pick up your AI analyses where you left off`
  2. `Review new matches and connection requests`
  3. `Keep your profile and demand requests up to date`
  - Each item is paired with a clean white vector circle-check icon (`20 × 20 px`).
- **Lower Illustration:**
  - Asset: `login_side_image.webp` (located at `src/Induva.Web/wwwroot/images/auth/login_side_image.webp`).
  - Represents the central isometric factory island connected by dotted copper transmission paths to three satellite functional islands (raw materials warehouse, logistics transport truck, and investment analytics).

### C. Right Panel — 50% Width (`#F6F4EF`)
- **Card Container:**
  - Width: `440px`, padding: `40px 38px`, border-radius: `16px`.
  - Border: `1px solid #E3DFD6`.
  - Background: `#FFFFFF`.
  - Box Shadow: `0 4px 24px rgba(14, 27, 36, 0.04), 0 1px 3px rgba(14, 27, 36, 0.02)`.
- **Card Contents:**
  - **Heading:** `"Sign in"` in `#0E1B24`.
  - **Subtitle:** `"Enter your details to access your INDUVA account."` in `#5B6670`.
  - **Email Field:** Envelope icon on the left, placeholder `"name@company.com"`.
  - **Password Field:** Lock icon on the left, eye toggle button on the right with interactive show/hide password script, and `"Forgot password?"` link above.
  - **Remember Me Checkbox:** `"Keep me signed in"`.
  - **Primary CTA:** Full-width `"Sign in"` button (`#0B5563`, hover `#094651`).
  - **Hairline Divider & Switcher:** `"New to INDUVA? Create an account"`.
  - **Privacy Caption:** Shield icon + `"Your data stays on INDUVA. We never share contact details without your approval."`.

### D. Footer
- Explicitly omitted on the Sign In page via `ViewData["HideFooter"] = true` in accordance with the 1440 × 900 single-viewport specification.

---

## 4. Non-Scrollable Viewport Architecture

To ensure the Login page is strictly **non-scrollable** across all desktop resolutions:
1. **HTML & Body Tokens:** `html.auth-page-html, body.auth-page-body` apply `height: 100vh !important; max-height: 100vh !important; overflow: hidden !important; margin: 0; padding: 0;`.
2. **Container Constraints:** `.auth-viewport-container` enforces `height: calc(100vh - 64px) !important; max-height: calc(100vh - 64px) !important; overflow: hidden !important;`.
3. **Adaptive Proportions (`clamp`):**
   - Panel padding and typography scale gracefully with `clamp(...)` to guarantee that screens with heights from 650px to 1080px fit all elements (emblem, headline, bullet points, illustration, and form card) without clipping or scrollbars.
4. **GSAP Entrance Animation:**
   - Both the floating sign-in card (`.signin-glass-card`) and the network isometric artwork (`.login-side-artwork`) feature smooth GSAP entrance transitions upon page load.

---

## 5. Website GSAP Scroll Engine (`scroll-animations.js`)

GreenSock Animation Platform (GSAP 3.12.5) with `ScrollTrigger` and `ScrollToPlugin` powers all scroll-driven interactions:
- **Global Scroll Progress Indicator:** Top hairline progress bar animating in Deep Teal (`#0B5563`).
- **Sticky Navbar Elevation:** ScrollTrigger watches scroll offset and seamlessly transitions shadow and border styling.
- **Smooth Anchor Navigation:** Auto-intercepts anchor links and scrolls smoothly with header offset (`offsetY: 72`).
- **Landing Page Scroll Orchestration:**
  - Hero staggered entrance and parallax scrub on `hero-factory.webp`.
  - Facts Strip card reveal & synchronized numeric count-up from 0 to targets (27, 23, 5, 6).
  - How It Works cards staggered entrance on scroll.
  - Platform Features 6-card grid staggered entrance.
  - Persona cards staggered scale-up.
  - Tomato canner example showcase reveal.
  - Final CTA card reveal.
- **Generic Support:** Any element across the website decorated with `.gsap-reveal` automatically participates in scroll-driven reveals.

---

## 6. OTP-Based Password Recovery Flow

1. **Unregistered Email Validation:**
   - When an email address is submitted that does not exist in the database, the system does not claim an email was sent. Instead, an explicit error is displayed immediately: *"No account found with this email address. Please check your spelling or register."*
2. **6-Digit OTP Generation & Security:**
   - Registered users receive a 6-digit numeric OTP code (e.g. `492815`) valid for 15 minutes.
   - Brute-force protection: Cached with a maximum limit of 5 failed attempts per OTP.
3. **Verify & Reset Interface (`/Account/ResetPassword`):**
   - Centered monospace 6-digit input box with letter-spacing for easy verification.
   - New password and password confirmation fields with show/hide password toggle.
   - Built-in *"Resend Code"* action and *"Use a different email address"* link.

