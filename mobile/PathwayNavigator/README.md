# Pathway Navigator – Android app (Flutter)

Native Android client for the **AI Future Pathway Navigator**. It is a functional clone of the
React web app (`web/PathwayNavigator`) and talks to the **same ASP.NET Core API** – it never calls the
Python AI service directly.

| Web page | Mobile screen | Roles |
|---|---|---|
| Login / Register / Forgot password | `features/auth` | everyone (register = Student only) |
| Onboarding (AI chat + standard form) | `features/onboarding` | Student |
| Student dashboard | `features/dashboard` | Student |
| Career Discovery (Agents 2 + 3, approve/reject, roadmap) | `features/career_discovery` | Student |
| Reality Check (Agent 4 form + result + history) | `features/reality_check` | Student |
| Counsellor Approval Centre (queue, evidence, decision) | `features/reality_check` | Counsellor, Admin |
| Admin governance overview | `features/dashboard` | Admin |
| – (mobile only) Privacy & AI consent, Settings | `features/privacy`, `features/dashboard` | everyone |

Architecture, conventions and the ethics checklist are in
[`docs/MOBILE_ARCHITECTURE.md`](../../docs/MOBILE_ARCHITECTURE.md).

## Prerequisites

* Flutter (stable channel, Dart ≥ 3.12 as pinned in `pubspec.yaml`) and Android SDK / JDK 17
* The backend running (see `docs/MEMBER_4_RUN_AND_VERIFY.md`): API on `http://localhost:5081`

## First run

```bash
cd mobile/PathwayNavigator
flutter pub get            # also (re)creates pubspec.lock – commit it
dart fix --apply           # optional: apply any lint auto-fixes
flutter analyze
flutter test
```

### Pointing the app at the API

The API URL is a **build-time define**, never hard-coded:

```bash
# Android emulator -> API on your computer (this is the default)
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5081/api

# Physical phone on the same Wi-Fi (debug builds only; the API must listen on 0.0.0.0
# and the Development CORS policy is irrelevant to a native app)
flutter run --dart-define=API_BASE_URL=http://192.168.1.23:5081/api

# Release: HTTPS is mandatory – the app refuses to start with an http:// URL
flutter build apk --release \
  --dart-define=API_BASE_URL=https://api.your-domain.example/api \
  --dart-define=PRIVACY_CONTACT=privacy@your-domain.example
```

| Define | Purpose |
|---|---|
| `API_BASE_URL` | ASP.NET API base **including** `/api`. Default `http://10.0.2.2:5081/api`. |
| `PRIVACY_CONTACT` | Shown in the privacy notice (where users send access / deletion requests). |

Plain-HTTP traffic is allowed **only** by `android/app/src/debug/AndroidManifest.xml`.

### Release signing

Create `android/key.properties` (git-ignored – never commit it or the keystore):

```properties
storeFile=/absolute/path/to/upload-keystore.jks
storePassword=...
keyAlias=upload
keyPassword=...
```

Without that file `flutter build apk --release` falls back to the debug key and prints a warning;
such an APK must not be distributed.

## Project layout

```
lib/
  main.dart                     entry point (reads AppConfig, builds dependencies)
  app/                          composition root, router + guards, theme
  core/                         config, errors, HTTP client, storage, validators, shared widgets
  features/<feature>/
    data/                       models (fromJson) + repository interface + Remote… implementation
    presentation/               ChangeNotifier controllers + screens/widgets
test/                           unit, controller and widget tests (all use fakes – no network)
```

Dependency rule: `presentation → data → core`. Screens never import `package:http`; repositories
never import `package:flutter/material.dart`.

## Common tasks

* **New endpoint / field** – add it to the feature's model + repository, add a parsing test in
  `test/features/`. The web contract lives in `web/PathwayNavigator/src/api/*.js` and the .NET DTOs
  in `backend/PathwayNavigator.Api/DTOs`.
* **New screen** – add the route in `core/constants/routes.dart` + `app/router.dart`, and extend the
  matrix in `test/app/router_test.dart` if it needs a role guard.
* **New role-dependent behaviour** – put the rule in `resolveRedirect` (pure function) so it is tested.
* **Privacy notice changed materially** – edit `privacy_notice.dart` **and** bump
  `ConsentStore.currentVersion` so every user is asked again.

## Ethics & privacy at a glance

Consent gate before any network use · AI disclosure wherever AI output is shown · human-in-the-loop
for high-impact pathways (counsellor decision) · no analytics/tracking SDKs · token in Android
Keystore · cloud backup disabled · HTTPS enforced in release · no personal data in logs · no raw
server/exception text shown to users. Details: `docs/MOBILE_ARCHITECTURE.md`.
