# HealthRater

Calculates a person's overall health rating from 39 parameters (score 1–10 each,
summed into a **Total Health Rating out of 390**), plus four supporting health
states: **Energy, Strength & Stamina**, **Mental & Emotional**, **Immunity**, and
**Longevity**.

> ⚠️ No official HealthRater scoring formulas were supplied for this build. Every
> threshold below is a **provisional, configurable heuristic** — clearly isolated in
> `ScoringConfig` (C#) / `scoring/config.py` (Python) so it can be replaced without
> touching UI, API or engine code. See [Scoring Methodology](#scoring-methodology).

## Architecture

```
healthrater/
├── backend/                     .NET 8 solution
│   ├── HealthRater.Core/        scoring engine (shared logic, no ASP.NET dependency)
│   │   ├── Models/               AssessmentInput, output DTOs, enums
│   │   ├── Scoring/
│   │   │   ├── ScoringConfig.cs      every threshold, centralized
│   │   │   ├── DerivedMetricsCalculator.cs   BMI / WHtR / WHR
│   │   │   ├── HealthRatingEngine.cs         orchestrates all 39 scorers + total
│   │   │   ├── FourStateCalculator.cs        parameter → state mapping
│   │   │   └── Scorers/          BloodPressureScorer, CooperScorer, HeartRateScorer,
│   │   │                         HydrationScorer, FunctionalPowerScorer,
│   │   │                         BodyCompositionScorer, LifestyleScorers, ...
│   │   └── Validation/           AssessmentValidator (DataAnnotations + cross-field rules)
│   ├── HealthRater.Api/         ASP.NET Core Web API — POST /api/health-rating/calculate
│   └── HealthRater.Tests/       dependency-free console test runner (see note below)
│
├── frontend/                    React + Vite + TypeScript
│   └── src/
│       ├── pages/                HomePage, AssessmentPage, ResultsPage
│       ├── components/           Layout, FieldInput, Tooltip, ProgressBar, ScoreCard
│       ├── data/sections.ts      the 8 assessment sections & field metadata (single source of truth)
│       ├── context/               AssessmentContext (in-progress answers + last result)
│       └── api/healthRatingApi.ts fetch client for the .NET API
│
├── python/                      Python mirror application (same scoring philosophy)
│   ├── healthrater/
│   │   ├── models.py, validation.py, sample_profile.py, cli.py
│   │   └── scoring/               config.py, derived.py, scorers.py, engine.py, four_states.py
│   └── tests/                    pytest suite
│
└── demo.html                    self-contained single-file demo (vanilla JS port of the
                                  scoring engine) — lets you try the assessment without
                                  running either server; published as an artifact in-chat
```

## Running it

### Backend (.NET API)
```bash
cd backend/HealthRater.Api
dotnet run --urls http://localhost:5080
```
`POST http://localhost:5080/api/health-rating/calculate` with an `AssessmentInput` JSON body
(see `python/healthrater/sample_profile.py` or the Postman-style example below).

### Frontend (React + Vite)
```bash
cd frontend
npm install    # already installed in this sandbox
npm run dev    # http://localhost:5173
```
The frontend calls `http://localhost:5080` by default (see `VITE_API_BASE_URL` in
`src/api/healthRatingApi.ts` to override). CORS is already configured on the API for
`localhost:5173`.

### Python application
```bash
cd python
python3 -m healthrater.cli                 # runs the built-in healthy sample profile
python3 -m healthrater.cli --worst-case    # runs the built-in worst-case profile
python3 -m healthrater.cli --json path.json
```

### Tests
```bash
# .NET (see note below on why this isn't xUnit)
cd backend && dotnet run --project HealthRater.Tests

# Python
cd python && python3 -m pytest -q
```

## Scoring Methodology

Every one of the 39 parameters produces an integer **1–10**. The **Total Health
Rating is the literal sum** of those 39 scores (min 39, max 390) — never a weighted
average, and the headline number is never rescaled to 0–100 (a percentage is shown
alongside it).

**Raw vs. derived:** BMI, WHtR and WHR are *not* collected from the user — they're
computed from height/weight/waist/hip (`DerivedMetricsCalculator` / `derived.py`).

**Provisional decision — raw anthropometric fields:** Sex, Height, Weight, Waist and
Hip are each still one of the 39 numbered parameters (per the parameter list), but
their health signal is *already fully captured* by the derived/composition scores
(BMI, WHtR, WHR, Body Fat). Rather than double-penalize the same physical trait
twice, this build scores those five raw fields at a flat baseline of 10 and lets
BMI/WHtR/WHR/Body Fat carry the actual signal. This is called out explicitly because
it's a judgment call, not a supplied rule — change it in `ScoringConfig` /
`HealthRatingEngine` if you'd rather have them score independently.

**Business rules implemented as given:**
- Blood pressure: 110/70 ≈ 10, ~165/95 ≈ 1 (linear interpolation per limb, averaged)
- Hydration: ~33 ml/kg bodyweight/day is treated as the ideal; score falls off with distance from that ratio in either direction
- Caffeine: 0 servings/day → 10, more → lower
- Alcohol/tobacco/drugs: daily → 1, never → 10 (discrete frequency mapping)
- Physical training: 4–6 sessions/week lands near 10; beyond ~7/week is capped (mild overtraining penalty) rather than continuing to increase
- Cooper: ≥3,000 m in 12 minutes → 10
- Functional power: push-ups + pull-ups + bodyweight squats, benchmarked against a provisional sex/age norm table

**Everything else** (BMI center/penalty, body-fat ideal ranges, WHtR/WHR cutoffs,
resting HR ideal, HRR scale, junk food/overeating/veg-fiber penalties, NEAT steps
scale) is a reasonable, commonly-cited wellness heuristic invented for this build —
centralized in `ScoringConfig.cs` / `config.py`, one field per rule, so it's trivial
to retune without touching scoring/UI logic elsewhere.

### Four States (configurable grouping, not a clinical model)

`FourStateCalculator.cs` / `four_states.py` maps parameters to states exactly per
the spec (parameters can and do contribute to multiple states — e.g. sleep quality
counts toward both Energy and Mental & Emotional). Each state's raw score (sum of
its member parameters) is also normalized to 0–100 for display. **This grouping is
architectural, not a clinically validated predictive model.**

## API

```
POST /api/health-rating/calculate
Content-Type: application/json

{
  "sex": "Male", "age": 28, "heightCm": 180, "weightKg": 78,
  "waistCm": 82, "hipCm": 98, "bodyFatPercent": 16,
  "restingHeartRateBpm": 58, "heartRateRecoveryBpm": 28,
  "systolicBpMmHg": 115, "diastolicBpMmHg": 74,
  "energyLevel": 8, "energyStability": 7, "averageSleepQuality": 8, "circadianHealth": 7,
  "averageMood": 8, "moodStability": 7, "socialLife": 8, "jobSatisfaction": 7, "homeFamilySatisfaction": 9,
  "dailyWaterIntakeLiters": 2.6, "digestionAndEvacuation": 8, "immuneHealth": 8,
  "caffeineServingsPerDay": 1, "junkFoodServingsPerWeek": 2, "overeatingEpisodesPerWeek": 1,
  "alcoholTobaccoDrugsFrequency": "Rarely", "vegetablesFiberServingsPerDay": 4,
  "dailyStepsNeat": 9000, "trainingSessionsPerWeek": 5,
  "pushUps": 40, "pullUps": 12, "bodyweightSquats": 50, "cooperDistanceMeters": 2800,
  "skinHealth": 8, "jawSkullHealth": 9, "dentalHealth": 8, "spinalHealth": 7, "hairHealth": 8
}
```

Returns:
```json
{
  "totalHealthRating": 339,
  "maxHealthRating": 390,
  "percentage": 86.92,
  "parameterScores": { "...": "39 keys, 1-10 each" },
  "fourStates": {
    "energyStrengthStamina": { "rawScore": 83, "maxRawScore": 100, "normalizedScore": 83 },
    "mentalEmotional": { "rawScore": 54, "maxRawScore": 70, "normalizedScore": 77.14 },
    "immunity": { "rawScore": 93, "maxRawScore": 110, "normalizedScore": 84.55 },
    "longevity": { "rawScore": 121, "maxRawScore": 130, "normalizedScore": 93.08 }
  },
  "derivedMetrics": { "bmi": 24.07, "whtr": 0.456, "whr": 0.837 }
}
```
Invalid input (e.g. age out of 18–100, systolic ≤ diastolic) returns `400` with an
`errors` array/object.

## Authentication (Log in / Sign up)

Optional accounts — the assessment stays open to everyone; signing in only adds the
user to the top bar. Frontend pages: `/login` and `/signup` (`src/pages/AuthPage.tsx`).

| Endpoint | Body | Result |
|---|---|---|
| `POST /api/auth/register` | `{ name, email, password }` | `201` + user, signs in · `400` validation · `409` email taken |
| `POST /api/auth/login` | `{ email, password }` | `200` + user, signs in · `401` "Invalid email or password." |
| `POST /api/auth/logout` | — | `204`, clears the session cookie |
| `GET /api/auth/me` | — | `200` + user, or `204` when nobody is signed in |

- **Passwords**: PBKDF2-HMAC-SHA512, 210,000 iterations, random 16-byte salt per user
  (`HealthRater.Core/Auth/PasswordHasher.cs`). Plaintext is never stored or logged.
  Rules: 8–128 characters, at least one letter and one number.
- **Session**: `healthrater.auth` cookie — `HttpOnly`, `SameSite=Lax`, 7-day sliding
  expiry. The frontend calls the API with `credentials: "include"`; CORS allows
  credentials for `localhost:5173` only.
- **Brute-force protection**: register/login are rate-limited to 10 requests per
  minute per IP (`429` afterwards). Failed logins return the same message and take
  the same time whether or not the email exists.
- **Storage**: `backend/HealthRater.Api/App_Data/users.json` (override with
  `Auth:UserStorePath`). The folder is git-ignored because it holds password hashes.
  It's a single-instance file store behind `IUserStore` — replace it with a
  database-backed implementation for production.
- In production, serve the API over HTTPS so the cookie is only sent encrypted.

## Validation

Enforced in `AssessmentValidator` (C#) / `validation.py` (Python):
age 18–100, all 1–10 fields, body fat 0–100%, positive anthropometric values, valid
blood pressure (systolic > diastolic), valid sex, valid (non-negative) Cooper
distance — plus range checks on every other numeric field. Errors are returned as a
flat list of human-readable messages; the .NET API also auto-returns 400 for
`[Range]` violations via ASP.NET's built-in model validation.

## Important sandbox limitation: no NuGet access

This container's network allowlist includes `archive.ubuntu.com`/`security.ubuntu.com`
(which is how the .NET 8 SDK itself got installed via `apt`), npm's registry, and
PyPI — but **not** `api.nuget.org`. That means:
- **No Swashbuckle/Swagger** on the API (removed from the project; the endpoint still
  works, there's just no `/swagger` UI — test it with curl/Postman/the frontend).
- **No xUnit** in `HealthRater.Tests`. It's a small dependency-free console app instead
  (`Framework/TestRunner.cs` — a `~50-line` assertion+runner pair with an xUnit-like
  `Assert.*` API). All 38 tests pass; swap in real xUnit once NuGet is reachable — the
  assertions were written to make that a low-effort migration, not a rewrite.

## What's been executed (not just written)

- `dotnet build` on the full solution — **0 errors, 0 warnings**.
- `dotnet run --project HealthRater.Tests` — **38/38 passed** (BMI, WHtR, WHR, unit
  conversions, blood pressure, resting HR/HRR, Cooper, functional power, substance
  use, physical training, total health rating bounds/sum, four states, validation).
- `python3 -m pytest -q` — **40/40 passed** (mirrors the same categories).
- Backend API run live on `localhost:5080`; POSTed a real sample profile and a couple
  of invalid ones — got back correct 339/390 (86.92%) results and correct 400
  validation errors.
- **Cross-language parity verified**: the same sample profile through both the .NET
  API and `python -m healthrater.cli` produces identical results (339/390, 86.92%,
  same four-state scores, same BMI/WHtR/WHR).
- `npm run build` on the frontend — **built clean**, 0 TypeScript errors.
- Frontend dev server (`localhost:5173`) and backend API (`localhost:5080`) both
  verified running and reachable simultaneously with CORS configured between them.

## Remaining limitations

- No persistence/database — the API is stateless by design per the spec (no storage
  requirement was given); results aren't saved server-side.
- No automated end-to-end/UI tests (e.g. Playwright) for the React app — only manual
  verification (build + live server checks) plus the backend/Python unit suites.
- The functional-power and age-scoring norm tables are intentionally simple
  piecewise tables, not full percentile curves — easy to swap for a richer table in
  `ScoringConfig`/`config.py` without touching any other file.
- Accounts are stored in a local JSON file (single API instance); there's no
  password reset, email verification or per-user result history yet.
- `demo.html` is a convenience preview only — it duplicates the scoring formulas in
  vanilla JS so it can run standalone in the chat artifact viewer with no backend.
  The source of truth for scoring is `HealthRater.Core` (C#) and `healthrater/scoring`
  (Python); keep those two in sync if you change a threshold, and port to `demo.html`
  only if you want the preview to stay current too.
