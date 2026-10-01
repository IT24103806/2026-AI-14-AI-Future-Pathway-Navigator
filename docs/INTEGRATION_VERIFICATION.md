# Frontend ⇄ Backend ⇄ AI service integration

```
React SPA (web/PathwayNavigator, :5173)
   │  HTTPS/JSON + JWT Bearer           (the browser only ever talks to the .NET API)
   ▼
ASP.NET Core API (backend/PathwayNavigator.Api, :5081)  ── PostgreSQL
   │  HttpClient (AgentService)         (internal only)
   ▼
FastAPI AI service (ai_service, :8000)  ── Agents 1-4 (LangGraph)
```

## Endpoint map

| SPA call (`src/api/*`) | .NET endpoint | Python endpoint called by `AgentService` |
|---|---|---|
| `POST /auth/register`, `/auth/login`, `/auth/google`, `/auth/forgot-password`, `/auth/verify-reset-code`, `/auth/reset-password` | `AuthController` | – |
| `GET /profile/status`, `GET/PUT /profile` | `ProfileController` | – |
| `POST /onboarding/chat` | `OnboardingController.ChatWithAgent1` | `POST /api/v1/agent-1/chat` |
| `POST /onboarding/standard-form` | `OnboardingController.CompleteViaStandardForm` | – |
| `POST /career-discovery/analyze` | `CareerDiscoveryController.Analyze` | `POST /api/v1/agent-2/analyze` |
| `GET /career-discovery/{id}`, `PATCH …/approve`, `PATCH …/reject` | `CareerDiscoveryController` | – |
| `POST /pathway-planner/plan` | `PathwayPlannerController.Plan` | `POST /api/v1/agent-3/plan` |
| `POST /counsellor-review/analysis/{id}/evaluate` | `CounsellorReviewController.StartRealityCheck` | `POST /api/v1/agent-4/evaluate` |
| `GET /counsellor-review/me/status`, `/me/history` | `CounsellorReviewController` (Student) | – |
| `GET /counsellor-review`, `GET /{id}`, `POST /{id}/decision` | `CounsellorReviewController` (Counsellor/Admin) | – |

## Running everything locally

1. **PostgreSQL** running on `localhost:5432`.
2. **Backend** – copy `backend/PathwayNavigator.Api/appsettings.example.json` to `appsettings.json` (git-ignored) and set the
   DB password and `JwtSettings:Secret` (≥ 32 chars). Then
   `dotnet run --project backend/PathwayNavigator.Api --launch-profile http`.
   In Development the API applies EF migrations on start (`Database:AutoMigrate`, default `true`) and seeds the roles.
   Swagger: <http://localhost:5081/swagger>.
3. **AI service** – `cd ai_service && python -m venv .venv && . .venv/bin/activate && pip install -r requirements.txt`,
   copy `.env.example` to `.env` (an LLM key is optional; without one the agents use deterministic fallbacks), then
   `python -m uvicorn main:app --host 0.0.0.0 --port 8000`. Health: <http://localhost:8000/health>.
4. **Frontend** – `cd web/PathwayNavigator && npm ci && cp .env.example .env.local && npm run dev` → <http://localhost:5173>.
   `VITE_API_BASE_URL=/api` makes the dev server proxy to the API (no CORS).

## Automated checks

| Check | Command |
|---|---|
| SPA lint / unit tests / production build | `cd web/PathwayNavigator && npm run lint && npm test && npm run build` |
| AI service + **.NET DTO contract** | `cd ai_service && pytest -q` (`tests/test_backend_contract.py` reads the C# `[JsonPropertyName]` fields and checks the real FastAPI responses) |
| Backend build + tests | `dotnet test backend/PathwayNavigator.sln` |

## Known limitations / follow-ups

* Public registration lets the caller pick the `Admin`/`Counsellor` role (`RegisterPage` + `AuthService.RegisterAsync`).
  Restrict this before any real deployment.
* `Migrations/AppDbContextModelSnapshot.cs` was not regenerated after the Member 4 migrations (they were hand-written),
  so run `dotnet ef migrations add SyncSnapshot` once on a machine with the SDK and review the (should-be-empty) diff.
* `GOOGLE_MODEL` must be a currently available Gemini model; Google retires models frequently.
