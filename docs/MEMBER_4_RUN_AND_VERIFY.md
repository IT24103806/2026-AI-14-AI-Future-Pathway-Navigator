# Member 4: run order and verification

1. Start PostgreSQL. Set `ConnectionStrings__DefaultConnection` for your local database and `JwtSettings__Secret` to a private key. The sample `appsettings.json` contains development values; never use those for deployment.
2. In `backend`, run `dotnet restore`, then `dotnet ef database update --project PathwayNavigator.Api`, then `dotnet run --project PathwayNavigator.Api --launch-profile http`. Check `http://localhost:5081/swagger`.
3. In `ai_service`, create and activate a Python virtual environment, run `pip install -r requirements.txt`, then `python -m uvicorn main:app --host 127.0.0.1 --port 8000`. Check `http://localhost:8000/health`.
4. In `web/PathwayNavigator`, run `npm ci` and `npm run dev`. Visit `http://localhost:5173`. The API URL is `http://localhost:5081/api`; use HTTP consistently.
5. Sign in as Student; complete onboarding; use Career Discovery to create a saved analysis; select a recommended career in Agent 4, enter A/L stream, comma-separated grades (A,B,C), budget and skills, then Run Reality Check. Open `/student/reality-check` for result/history. A high-risk result is Pending, a low-risk result is Approved.
6. Sign in as Counsellor or Admin; open `/counsellor/dashboard`; filter Pending or All; inspect the evidence, add feedback and approve/reject/request revision. Student refreshes status. `/admin/dashboard` links to this approval queue. The default Pending filter intentionally excludes automatically approved records; choose All to see those.
7. Mobile: `cd mobile/PathwayNavigator && flutter pub get && flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5081/api`. On the Android emulator the API host is `10.0.2.2`, not localhost. The Reality Check form is part of the Career Discovery screen and uses the saved analysis id automatically. See `mobile/PathwayNavigator/README.md`.

## Verified here

The project archive contains code for all four agents. The frontend build and its test result are reported separately in the handoff. This environment has no `dotnet` or `flutter` executable and no running PostgreSQL instance; backend migration and full browser/mobile end-to-end operation must be verified on your computer. Python pytest is also unavailable here.

## Remaining rubric items

The specification asks for one *integrated* objective → plan/delegation to four distinct agents → controlled tools → persisted shared state → deterministic validation → paused authorized approval → auditable result/safe failure. Four standalone endpoints do not by themselves prove this integration; coordinate an end-to-end demonstration with the team. Member 4 should show the Agent 4 input/output contract, allow-listed catalogue tool, validations, review decision and audit evidence. Record a personal Git branch, commits, PR, unit/integration tests, Flutter workflow evidence, individual report and AI use log. Admin user and career catalogue CRUD, a meaningful mobile device feature and group performance/deployment reports require named owners and real implementation; do not claim these are already complete.
