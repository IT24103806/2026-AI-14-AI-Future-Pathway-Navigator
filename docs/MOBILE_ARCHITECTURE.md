# Mobile (Flutter / Android) – architecture, conventions & ethics

Scope: `mobile/PathwayNavigator`. Companion to `docs/INTEGRATION_VERIFICATION.md` (endpoint map).

> **Verification status.** This app was written in an environment with **no Flutter/Dart SDK and no
> access to pub.dev**, so it has **not** been compiled, analysed, run or tested. The only automated
> check performed was a tree-sitter *syntax* parse of every `.dart` file plus a scripted check that
> relative imports and referenced class names resolve. Expect to fix a few analyzer findings on the
> first `flutter analyze` (`dart fix --apply` handles most). CI (`test-flutter-mobile`) is the gate.

## 1. Goals

1. Feature parity with the web client for Student, Counsellor and Admin roles.
2. One backend contract. The app only calls the ASP.NET API (`/api/...`); the Python agents stay
   internal.
3. Code that is cheap to change: small feature modules, one place for each concern, tests that run
   without network or device.
4. Ethical by construction (section 5), not by afterthought.

## 2. Layering

```
presentation (screens, ChangeNotifier controllers)
      │  depends on interfaces only
      ▼
data  (repository interface ▸ RemoteXRepository ▸ models.fromJson)
      │
      ▼
core  (ApiClient, AppException, KeyValueStore, validators, JWT helpers, widgets)
```

* **Composition root:** `app/dependencies.dart` is the *only* place that picks concrete classes.
  Tests construct `AppDependencies` with fakes (`test/support/fakes.dart`).
* **State management:** `provider` + `ChangeNotifier`. App-wide: `AuthController`,
  `ConsentController`. Screen-scoped controllers are created by a `ChangeNotifierProvider` in the
  screen's wrapper widget and die with the route.
* **Navigation:** `go_router`. All access rules live in the pure function `resolveRedirect`
  (`app/router.dart`): *wait for state → consent → authentication → role*. It is unit-tested as a table.
  Route constants: `core/constants/routes.dart`.
* **Errors:** every failure leaves the data layer as `AppException` (kind + user-safe message).
  UI shows `describeError(e)`; unknown exceptions show a generic fallback – never `toString()` of an
  arbitrary object, a stack trace or a server body.
* **Parsing is defensive:** `json_helpers.dart` never throws on missing/mistyped fields and accepts
  the backend's quirks (list columns stored as JSON strings, snake_case audit evidence).
* **Config:** `--dart-define` only (`AppConfig`). Release builds refuse non-HTTPS.

## 3. Backend contract notes (do not "fix" in one place only)

| Topic | Rule |
|---|---|
| Auth | `POST /auth/login` → `{userId,email,role,token,expiresAt}`. Password ≥ 8 chars. Mobile registers **Student only**. |
| Agent 1 history | Send only turns **before** the new message; the agent appends it itself (otherwise it is duplicated). |
| Pathway DTOs | `snake_case` (`pathway_name`, `match_score`…). Review DTOs are `camelCase`. |
| Review list columns | `*Json` fields are JSON **strings**; tool-call/trace entries are snake_case inside. |
| `GET /counsellor-review/me/status` | `404` = "no review yet" (empty state, not an error). |
| Timeouts | LLM-backed calls can take a minute → default client timeout 120 s. |
| 401 on an authenticated call | token expired/revoked → `AuthController.expireSession()` → login with notice. |

## 4. Parity & intentional differences from the web app

| Difference | Reason |
|---|---|
| No Google sign-in | Needs a Google Cloud OAuth client + SHA-1 registration per signing key; add when credentials exist (the repository seam is `AuthRepository`). |
| No "Learning Modules" tile | The web tile is a non-functional placeholder; not cloned. |
| No JWT snippet on the dashboard | Showing token material is a security anti-pattern. |
| Self-registration limited to Student | The web form lets anyone pick Admin/Counsellor (open privilege escalation – reported in `INTEGRATION_VERIFICATION.md`). Staff accounts must be provisioned server-side. |
| Admin tools not implemented | Backend has no user/catalogue management endpoints; the screen is the same read-only governance overview as the web. |
| Consent screen + Settings | Mobile-only: privacy notice, consent withdrawal, licences. |

## 5. Software-engineering ethics checklist

(Aligned with ACM/IEEE-CS Software Engineering Code of Ethics: public interest, honesty, product
quality, privacy.)

| Principle | How it is implemented | Where |
|---|---|---|
| **Informed consent / transparency** | First-run notice explains what is collected, why, how AI is used and who to contact; nothing is shown beyond it until accepted; decline exits. Consent is **versioned** – bump `ConsentStore.currentVersion` on material change. Withdrawable in Settings. | `features/privacy`, router consent gate |
| **Honest AI disclosure** | `AiDisclosureBanner` on AI-generated screens; chat banner says you are talking to an AI and warns not to share secrets; market data is labelled *live* vs *simulated*. | `common_widgets.dart` |
| **Human in the loop** | High-impact pathways stay *Pending* until a counsellor decides; counsellor must write feedback (≥ 5 chars, enforced client **and** server); UI states AI is advice only. | `reality_check` |
| **Data minimisation** | Only fields the backend needs; no analytics / ads / tracking SDKs; no device identifiers. | `pubspec.yaml` (review deps in PRs) |
| **Security of personal data** | JWT in Android Keystore (`flutter_secure_storage`); `allowBackup=false`; HTTPS enforced in release; cleartext only in the debug manifest; expired tokens never sent; unknown roles rejected. | `core/storage`, `AndroidManifest*.xml`, `AppConfig` |
| **No sensitive logging** | `appLog` is debug-only and documents "no PII/tokens"; no `print`. | `core/utils/app_log.dart` |
| **Truthful errors** | Server messages are surfaced, internals are not; failures never silently look like success. | `ApiClient`, `describeError` |
| **Accessibility / inclusion** | 48 dp targets, Semantics labels/headers, icon + text for status (never colour alone), light & dark themes, text scales with system font size. | theme, widgets |
| **Server is the authority** | Client guards and validators are UX only; the API still enforces roles and validation. | `resolveRedirect` doc |
| **Licensing / supply chain** | Few, well-known dependencies; `showLicensePage` exposes licences; keep `pubspec.lock` committed. | Settings |

Known gaps to close before a public launch: a real privacy-policy URL and data-deletion process,
rate limiting / lockout on the API, certificate pinning (optional), Play Data-safety form, and an
accessibility audit with TalkBack.

## 6. Testing strategy

* **Unit:** parsing, validators, JWT, config, `ApiClient` (via `package:http/testing.dart`).
* **Policy:** `resolveRedirect` table (consent, roles, prefix look-alikes).
* **Controllers:** fake repositories; assertions on state transitions and on what is sent
  (e.g. Agent 1 history excludes the new message; short feedback never reaches the API).
* **Widget:** app flow from consent → login → role home, using `AppDependencies` with fakes.
* Add a test with every new endpoint/field/guard. No test may touch the network or the Keystore.

## 7. How to extend

1. **Feature:** `features/<name>/{data,presentation}`; interface + `Remote…` impl in `data`; register in
   `AppDependencies` and `app.dart` providers; route in `routes.dart` + `router.dart`.
2. **Dependency bump:** change `pubspec.yaml`, run `flutter pub upgrade`, analyze, test, commit lock.
3. **Backend change:** update the model's `fromJson` and its test; keep `docs/INTEGRATION_VERIFICATION.md` in sync.
4. **Release:** bump `version:` in `pubspec.yaml`, build with HTTPS `API_BASE_URL` and a real keystore.

## 8. Decisions (mini-ADR)

* **Provider/ChangeNotifier over Bloc/Riverpod** – smallest concept count for a student team; the
  repository interfaces mean the choice can change without touching `data/`.
* **go_router with a pure redirect function** – policy is testable without widgets.
* **`--dart-define` config, no `.env` assets** – nothing secret can be bundled by accident.
* **Hand-written `fromJson`** – no code generation step, so a clean checkout builds with `flutter pub get`.
