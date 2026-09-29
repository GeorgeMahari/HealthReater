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
│   ├── HealthRater.Data/        EF Core persistence (users, sessions, assessment history)
│   │   ├── Entities/             User, RefreshToken, HealthAssessment, AssessmentParameterScore
│   │   ├── HealthRaterDbContext.cs   model + SQLite / SQL Server context subclasses
│   │   ├── Migrations/Sqlite/    migrations for the SQLite provider (development default)
│   │   ├── Migrations/SqlServer/ migrations for the SQL Server provider
│   │   ├── Snapshots/            ParameterCatalog (39 params) + AssessmentSnapshotBuilder
│   │   └── Services/             UserService, SessionService, AssessmentService
│   ├── HealthRater.Api/         ASP.NET Core Web API — calculate, auth, assessments
│   └── HealthRater.Tests/       dependency-free console test runner (see note below)
│
├── frontend/                    React + Vite + TypeScript
│   └── src/
│       ├── pages/                HomePage, AssessmentPage, ResultsPage, AuthPage,
│       │                         ProfilePage (/profile), HistoricalResultPage (/history/:id)
│       ├── components/profile/   calendar, score-history charts, history list, comparison, dialogs
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

**Quick start (Windows):** from the project root run

```powershell
powershell -ExecutionPolicy Bypass -File .\start-dev.ps1
```

It opens the backend (http://localhost:5080) and the frontend (http://localhost:5173)
in their own windows and skips whichever is already running. If the app shows
"Could not reach the HealthRater API. Is the backend running?", the backend window
isn't open — run the script again (or start the backend as below).

### Backend (.NET API)
```bash
cd backend/HealthRater.Api
dotnet run          # http://localhost:5080 (see Properties/launchSettings.json)
```
In Development the SQLite database (`App_Data/healthrater.db`) is created and migrated
automatically on startup — no setup needed.

**Swagger UI:** http://localhost:5080/swagger (Development only). Every endpoint is listed
with its request/response schemas; locked ones need a session — run
`POST /api/auth/register` or `/api/auth/login` from Swagger first, and the browser keeps
the session cookie for the other calls. The raw OpenAPI document is at
`/swagger/v1/swagger.json`. See [Database](#database) for SQL Server
and production.

Assessments require a signed-in account with a complete profile — see
[Profile context](#profile-context-sex--date-of-birth) and the [API](#api) section.

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

### Profile context (sex & date of birth)

Sex, age, height and weight are **parameters #1–#4 of the 39** (not extra parameters, so
the maximum stays 39 × 10 = 390). They are not asked in the questionnaire: they come from
the signed-in user's profile, and the first assessment step ("Basic Information") only
shows them for the user to confirm or edit. Sex and age are also the **context** for the
parameters whose references differ by sex and age; height and weight feed BMI, WHtR and
hydration. On the results page these four rows show the recorded values (e.g. "Male",
"21 years", "180 cm", "78 kg") rather than their score, which still counts in the total.

- The profile stores `Sex` (Male/Female), `DateOfBirth`, `HeightCm` and `WeightKg`. Age is
  **calculated** from the date of birth (UTC date) whenever it's needed; it is never stored
  on the profile.
- A user must have all four before an assessment can start or be submitted — enforced in
  the frontend (`/complete-profile`, asked once) and in the API (`409 profile_incomplete`).
  Allowed: age 18–100, height 50–250 cm, weight 20–400 kg.
- On submission the API loads the profile, calculates the age, scores with it, and stores
  `SexAtAssessment`, `AgeAtAssessment`, `DateOfBirthAtAssessment`, `Height` and `Weight` on
  the assessment. Any `sex`/`age`/`heightCm`/`weightKg` in the request is ignored.
- Changing the profile later only affects **future** assessments; saved ones keep their
  snapshot and are never re-scored.

### Sex- and age-specific scoring references (configurable)

No official HealthRater tables per sex/age were supplied, so these references are
**provisional placeholders**, kept outside the code in
`backend/HealthRater.Core/Scoring/References/scoring-references.json`:

| Parameter | Varies by | Current placeholder |
|---|---|---|
| `bodyFat` | sex (age groups supported) | ideal 15 % (male) / 23 % (female), −0.4 per point |
| `whr` | sex (age groups supported) | full score up to 0.90 (male) / 0.80 (female) |
| `functionalPower` | sex × age group (18–29 … 60–100) | "excellent" total reps 150…70 (male), 110…50 (female) |

Each entry is `{ parameterKey, sex (or null for any), minAge, maxAge, method, …, status }`
with methods `IdealCenter`, `UpperThreshold`, `RatioToNorm` or `Bands` (an explicit
min/max → score table). A sex-specific entry wins over a sex-neutral one. The file is
validated at startup: every parameter it lists must resolve to exactly one entry for
Male and Female at every age 18–100, otherwise the API refuses to start and says what is
missing or overlapping. To plug in the official tables, edit the file (or point
`Scoring:ReferencesFile` at another one) and set `"status": "Official"` — no code change.
The shipped values reproduce the previous formulas exactly, so existing scores are unchanged.

Everything else that is sex/age related stays as before: the `age` parameter's own score
(10 up to 30, gently lower per decade after) and the Four States grouping. Longevity uses
the same 13 parameter scores as before (including age, body fat and WHR), so sex and age
reach it only through those scores — there is **no mortality or life-expectancy model**.

### Four States (configurable grouping, not a clinical model)

`FourStateCalculator.cs` / `four_states.py` maps parameters to states exactly per
the spec (parameters can and do contribute to multiple states — e.g. sleep quality
counts toward both Energy and Mental & Emotional). Each state's raw score (sum of
its member parameters) is also normalized to 0–100 for display. **This grouping is
architectural, not a clinically validated predictive model.**

## API

Both scoring endpoints require a signed-in user with a complete profile and take only the
questionnaire answers (everything except sex, age, height and weight).
`POST /api/assessments` scores **and saves**;
`POST /api/health-rating/calculate` returns the same result without saving (preview).
Sex, age, height and weight are read from the profile — if sent, they are ignored.

```
POST /api/assessments            (or /api/health-rating/calculate)
Content-Type: application/json
Cookie: healthrater.auth=…

{
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
Invalid input (e.g. systolic ≤ diastolic) returns `400` with an `errors` array/object;
an incomplete profile returns `409` with `"code": "profile_incomplete"`; no session `401`.
The example result above is for a profile of Male, 28, 180 cm, 78 kg.

## Authentication (Log in / Sign up)

An account is required to take an assessment ("Start your assessment" sends guests to
log in or sign up, then to `/complete-profile` once, then back to the assessment).
Frontend pages: `/login`, `/signup` (`src/pages/AuthPage.tsx`), `/complete-profile`.

| Endpoint | Body | Result |
|---|---|---|
| `POST /api/auth/register` | `{ firstName, lastName, email, password }` | `201` + user, signs in · `400` validation · `409` email taken |
| `POST /api/auth/login` | `{ email, password }` | `200` + user, signs in · `401` "Invalid email or password." |
| `POST /api/auth/logout` | — | `204`, clears the session cookie |
| `GET /api/auth/me` | — | `200` + user, or `204` when nobody is signed in |

- **Passwords**: PBKDF2-HMAC-SHA512, 210,000 iterations, random 16-byte salt per user
  (`HealthRater.Core/Auth/PasswordHasher.cs`). Plaintext is never stored or logged.
  Rules: 8–128 characters, at least one letter and one number.
- **Session**: `healthrater.auth` cookie — `HttpOnly`, `SameSite=Lax`, 7-day sliding
  expiry. The cookie carries a random session token whose SHA-256 hash is a
  `RefreshTokens` row (30-day absolute lifetime). Every request re-validates it, so
  logging out revokes the session server-side — a copied cookie stops working
  immediately. The frontend calls the API with `credentials: "include"`; CORS allows
  credentials for `localhost:5173` only.
- **Brute-force protection**: register/login are rate-limited to 10 requests per
  minute per IP (`429` afterwards). Failed logins return the same message and take
  the same time whether or not the email exists.
- **Storage**: the `Users` table (see [Database](#database)).
- In production, serve the API over HTTPS so the cookie is only sent encrypted.

## Assessment history

Every completed assessment is saved automatically to the signed-in user's history
(`POST /api/assessments`), each as a new record.
All endpoints require a session and only ever touch the signed-in user's data — the
owner comes from the session, never from the request. Someone else's id returns `404`.

| Endpoint | Result |
|---|---|
| `POST /api/assessments` | Body: `AssessmentInput`. Validates, scores, stores. `201` + full snapshot |
| `GET /api/assessments` | Completed assessments, newest first — summary only (no parameters) |
| `GET /api/assessments/{id}` | Full snapshot: totals, four states, derived metrics, body & cardiovascular snapshot, all 39 parameters (raw value, unit, score) and the original input |
| `GET /api/assessments/calendar?year=2026&month=9&timeZone=Europe/Bucharest` | Lightweight entries for one month; `date` is the local day in the given IANA time zone (default UTC) |
| `DELETE /api/assessments/{id}` | `204`, or `404` if it isn't yours |

**Historical stability.** Each assessment is an immutable snapshot: inputs, derived
metrics, every parameter score and the `ScoringVersion` in force are stored at
creation. Reading an assessment never re-runs the engine, so changing
`ScoringConfig` later doesn't alter past results. New scans are always new rows.

**Time.** Timestamps are stored in UTC and returned with a `Z` suffix; the frontend
formats them in the viewer's own time zone.

## Profile & health history (frontend)

`/profile` (signed-in only; guests are sent to log in and brought back afterwards):

- **Profile card** — avatar, name, email, "Member since", edit profile.
- **Calendar** — completed assessments per day in the viewer's time zone. Green means
  *an assessment was completed that day*, not a health judgement. Several scans on
  one day show as dots; clicking a day lists each scan with its total and four states.
- **Health History** — score-history chart (Total Health Rating, plus one small chart
  per health state), the full list of assessments, and a two-assessment comparison
  showing recorded differences without labelling them good or bad.
- **Account Settings** — edit profile, profile photo, change password, log out,
  delete account (requires typing DELETE and the password).
- `/history/:id` reuses the Results design to show a saved assessment exactly as it was
  stored ("Assessment from 25 September 2026"); nothing is recalculated.

All data comes from the API below; the frontend never sends a user id.

### Profile API (`[Authorize]`, always the signed-in user)

| Endpoint | Result |
|---|---|
| `PUT /api/profile` | `{ firstName, lastName, email, sex?, dateOfBirth?, heightCm?, weightKg? }` — validated, email normalized; `409` if taken. When any of the profile data is sent, all four must be valid (sex "Male"/"Female", date "yyyy-MM-dd" giving age 18–100, height 50–250 cm, weight 20–400 kg) |
| `PUT /api/profile/password` | `{ currentPassword, newPassword }` — requires the current password; signs out other devices |
| `POST /api/profile/avatar` | multipart field `file`; JPG/PNG/WEBP detected **from the bytes**, ≤ 2 MB, 32–4096 px |
| `GET /api/profile/avatar` | the user's own image (`nosniff`, private cache); `404` if none |
| `DELETE /api/profile/avatar` | removes the photo |
| `DELETE /api/profile` | `{ password }` — permanently deletes the account and all its assessments |

`GET /api/auth/me` also returns `createdAt`, `avatarUrl` (an API-relative, versioned URL),
`sex`, `dateOfBirth`, `age` (calculated today), `heightCm`, `weightKg` and `profileCompleted`.
Avatars are stored in the `UserAvatars` table (not on disk), so no filesystem path is
ever exposed and they are deleted with the account. The browser centre-crops and
re-encodes the chosen image to 320×320 before upload, which also strips photo
metadata such as GPS location; the server validates it again independently.

## Database

EF Core 8 with two supported providers. Each has its own `DbContext` subclass and
migrations folder (EF migrations are provider-specific); application code only uses
`HealthRaterDbContext`.

| Setting | Values | Default |
|---|---|---|
| `Database:Provider` | `Sqlite` · `SqlServer` | `Sqlite` |
| `ConnectionStrings:Sqlite` | SQLite connection string (relative paths resolve from `HealthRater.Api/`) | `Data Source=App_Data/healthrater.db` |
| `ConnectionStrings:SqlServer` | SQL Server connection string — **never commit one with a password** | — |
| `Database:MigrateOnStartup` | apply pending migrations at startup (Development only) | `true` |
| `Database:SeedDevelopmentData` | create a demo account with 3 fictional scans (Development only) | `false` |

Any setting can come from an environment variable (`__` instead of `:`), e.g.
`Database__Provider=SqlServer` and `ConnectionStrings__SqlServer=...`, or from
`dotnet user-secrets` in `HealthRater.Api`.

**Tables:** `Users` (unique email) → `UserAvatars` (one per user), `RefreshTokens` (unique token hash) and
`HealthAssessments` (indexed by user, completion date, and user+status+date) →
`AssessmentParameterScores` (unique per assessment + key, check constraint
`Score BETWEEN 1 AND 10`). Deleting a user or an assessment cascades to its children.

### Development (SQLite — zero setup)
Just run the API. The database file lives in `backend/HealthRater.Api/App_Data/`
(git-ignored). Stop the API and delete that folder to start from an empty database.

### SQL Server (local or production)
```bash
cd backend
# Windows authentication against LocalDB — no password involved:
export Database__Provider=SqlServer
export ConnectionStrings__SqlServer="Server=(localdb)\MSSQLLocalDB;Database=HealthRater;Trusted_Connection=True;TrustServerCertificate=True"
dotnet ef database update --context SqlServerHealthRaterDbContext --project HealthRater.Data --startup-project HealthRater.Api
dotnet run --project HealthRater.Api
```
In production, set the same two variables in the hosting environment (with any password
coming from the platform's secret store), apply migrations explicitly with
`dotnet ef database update` or a migration bundle (`dotnet ef migrations bundle`);
automatic migration only ever runs in Development.

### Adding a migration after changing the model
Add one for **each** provider (requires `dotnet tool install --global dotnet-ef`):
```bash
cd backend
dotnet ef migrations add <Name> --context SqliteHealthRaterDbContext --project HealthRater.Data --startup-project HealthRater.Api --output-dir Migrations/Sqlite
Database__Provider=SqlServer ConnectionStrings__SqlServer="Server=(localdb)\MSSQLLocalDB;Database=HealthRater;Trusted_Connection=True" dotnet ef migrations add <Name> --context SqlServerHealthRaterDbContext --project HealthRater.Data --startup-project HealthRater.Api --output-dir Migrations/SqlServer
```

### Demo data (development only)
Set `Database:SeedDevelopmentData` to `true` in `appsettings.Development.json` and start
the API: it creates **demo@healthrater.test** / `DemoPassword1` ("Demo Test Data") with
three fictional assessments from the last four weeks. It never runs outside the
Development environment.

## Validation

Enforced in `AssessmentValidator` (C#) / `validation.py` (Python):
age 18–100, all 1–10 fields, body fat 0–100%, positive anthropometric values, valid
blood pressure (systolic > diastolic), valid sex, valid (non-negative) Cooper
distance — plus range checks on every other numeric field. Errors are returned as a
flat list of human-readable messages; the .NET API also auto-returns 400 for
`[Range]` violations via ASP.NET's built-in model validation.

## Test runner and NuGet

The project was started in a sandbox without NuGet access, so `HealthRater.Tests` is a
small dependency-free console runner (`Framework/TestRunner.cs`, xUnit-like `Assert.*`).
NuGet is now enabled in `backend/NuGet.Config` (EF Core and Swashbuckle come from it), so
moving the tests to xUnit is possible whenever wanted. The suite has
76 tests, including persistence, profile and profile-context tests that apply the real
SQLite migrations to a private database per test.

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

- No automated end-to-end/UI tests (e.g. Playwright) for the React app — only manual
  verification (build + live server checks) plus the backend/Python unit suites.
- The functional-power and age-scoring norm tables are intentionally simple
  piecewise tables, not full percentile curves — easy to swap for a richer table in
  `ScoringConfig`/`config.py` without touching any other file.
- No password reset or email verification yet; changing the email doesn't require
  re-verification.
- `demo.html` is a convenience preview only — it duplicates the scoring formulas in
  vanilla JS so it can run standalone in the chat artifact viewer with no backend.
  The source of truth for scoring is `HealthRater.Core` (C#) and `healthrater/scoring`
  (Python); keep those two in sync if you change a threshold, and port to `demo.html`
  only if you want the preview to stay current too.
