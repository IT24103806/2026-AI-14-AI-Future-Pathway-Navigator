# Member 4 (Student 4) - Roadmap Reality Check and Approval

**Student ID:** IT24510004  
**Primary component:** Component D - Roadmap Reality Check, Counsellor Approval, and Student Progress Status  
**Distinct AI responsibility:** Agent 4 validates whether a proposed pathway is achievable, identifies prerequisite gaps and risks, and enforces a human approval gate for high-impact results.

## Business workflow

1. An authenticated Student chooses a career from a saved `PathwayAnalysis`.
2. Flutter/React sends the request only to ASP.NET Core.
3. ASP.NET verifies that the signed-in student owns the analysis.
4. ASP.NET calls the internal Python Agent 4 endpoint with a 60-second timeout.
5. Agent 4 validates inputs, calls only the allow-listed prerequisite lookup, applies deterministic rules, validates its structured output, and records timings.
6. ASP.NET validates and persists the result, execution evidence, status and first audit event in one transaction.
7. A high-impact/risky result pauses as `Pending`. Only Counsellor/Admin roles can approve, reject, or request revision.
8. The Flutter student app reads `/me/status` and `/me/history`; it never accepts a student ID from the client.

## Member 4 ownership map

| Layer | Main files | Evidence |
|---|---|---|
| ASP.NET API | `CounsellorReviewController.cs`, `RealityCheckDto.cs` | REST routes, DTO validation, JWT roles, ownership checks, correct status codes |
| Service layer | `CounsellorReviewService.cs`, `AgentService.cs` | async operations, internal AI call, structured validation, transaction, safe failures |
| PostgreSQL/EF Core | `PathwayReview.cs`, `PathwayReviewAudit.cs`, migration `20260923070000...` | foreign keys, indexes, workflow uniqueness, audit history, timestamps |
| React | `CounsellorDashboardPage.jsx`, `counsellorReviewApi.js` | protected route, search/filter/sort/pagination, loading/empty/error states, decision validation, AI evidence |
| Flutter | `features/reality_check/` (repository, student + counsellor screens), `features/career_discovery/` (Reality Check form) - see `docs/MOBILE_ARCHITECTURE.md` | secure token storage, API status/history, responsive states, pull-to-refresh, counsellor decision screen |
| Agentic AI | `agent_4_reality_check/*` | defined contract, distinct validation role, allow-listed tool, deterministic rules, structured trace, approval routing |
| Tests | Agent 4 pytest, counsellor xUnit, React Vitest, Flutter test | golden case, risk case, prompt injection, ownership, repeated decision, validation and UI/API behaviour |

## API contract

All public endpoints require a JWT and are served by ASP.NET Core.

| Method and route | Role | Purpose |
|---|---|---|
| `POST /api/counsellor-review/analysis/{analysisId}/evaluate` | Student | Run Agent 4 for an analysis owned by the current user |
| `GET /api/counsellor-review?status=&search=&sort=&page=&pageSize=` | Counsellor/Admin | Review queue with filtering, sorting and pagination |
| `GET /api/counsellor-review/{id}` | Owner or staff | Read one review with authorization |
| `POST /api/counsellor-review/{id}/decision` | Counsellor/Admin | Approve, reject or request revision; only while Pending |
| `GET /api/counsellor-review/me/status` | Student | Current student's latest status |
| `GET /api/counsellor-review/me/history` | Student | Current student's status history |

Example evaluate body:

```json
{
  "targetCareer": "AI Engineer",
  "alStream": "Physical Science",
  "alResults": "A,B,C",
  "budgetLevel": "Medium",
  "currentSkills": ["Python", "Statistics"]
}
```

## Security and failure controls

- React and Flutter never call Python or PostgreSQL directly.
- Student identity is taken from the signed JWT, not a request parameter.
- `[Authorize(Roles=...)]` protects decisions and queue access.
- Data annotations and Pydantic validate both API boundaries.
- Instruction-like input is rejected before agent execution.
- Agent 4 can call only `career_prerequisite_lookup`; this tool has no network or personal-data permission.
- Invalid/unknown careers and risky results are routed to a human rather than silently published.
- Completed reviews cannot be decided twice.
- The agent returns a recorded safe failure; ASP.NET maps unavailable/invalid AI responses to `502`.
- Secrets belong in environment/app settings and are never committed.

## Local setup and verification

1. Configure PostgreSQL and `DefaultConnection`, JWT settings and `AiServiceSettings:BaseUrl`.
2. Run `dotnet ef database update --project backend/PathwayNavigator.Api`.
3. Start AI service: `cd ai_service && pip install -r requirements.txt && uvicorn main:app --reload`.
4. Start API: `dotnet run --project backend/PathwayNavigator.Api`.
5. Start React: `cd web/PathwayNavigator && npm ci && npm run dev`.
6. Start Flutter with the API URL: `flutter run --dart-define=API_BASE_URL=https://YOUR_API_HOST/api` (the value must include `/api`; plain HTTP is accepted only in debug builds).

Tests:

```text
dotnet test backend/PathwayNavigator.sln
cd ai_service && pytest -q
cd web/PathwayNavigator && npm test && npm run build
cd mobile/PathwayNavigator && flutter analyze && flutter test
```

## Demo checklist

- Login as Student and show protected status/history on Flutter.
- Run a risky pathway through ASP.NET and show the PostgreSQL `PathwayReviews` and `PathwayReviewAudits` rows.
- Login as Counsellor on React; search/filter the queue and inspect tool/validation/timing evidence.
- Approve or request revision, then refresh Flutter and show the updated result.
- Attempt a second decision and an unauthorized student's record to demonstrate enforcement.
- Run all four Member 4 test groups and show the GitHub Actions result.

## Git evidence to retain on GitHub

- Use the existing `feature/member-4-reality-check-approval-IT24510004` branch.
- Commit Member 4 changes in logical groups: agent, backend/database, React, Flutter, tests, documentation.
- Open a pull request linked to a Member 4 issue; request a teammate review and keep the review conversation.
- Do not commit `.venv`, `venv`, `node_modules`, `.dart_tool`, `build`, `dist`, `bin`, `obj`, `.env`, secrets or generated caches.
- Capture the passing CI run, PR, issue and project-board card for the individual report.

## Viva quick answers

- **Why is Agent 4 distinct?** It is the validation/safety specialist. Its input/output contract, allow-listed tool, deterministic feasibility rules and approval routing differ from discovery and planning agents.
- **Why is approval required?** A career pathway can materially affect education, time and cost. High-risk results remain Pending until an authorized counsellor acts.
- **How is IDOR prevented?** Student endpoints derive the user ID from the verified JWT and query by both review ID and owner ID.
- **Why store JSON summaries?** Tool calls and traces are variable-length structured evidence. Only summaries are stored; hidden reasoning and secrets are not.
- **Why use an audit table?** It preserves who changed a high-impact decision, when it changed and the before/after states.
- **What happens when Python fails?** The call times out or returns null, the API returns 502, and no misleading successful review is stored.
- **What would you modify in the viva?** Add a deterministic prerequisite, change a status filter, add a DTO rule, or trace the Flutter request through API, service and database.
