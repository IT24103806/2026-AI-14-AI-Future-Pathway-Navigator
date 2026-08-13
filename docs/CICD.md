# Continuous Integration & Continuous Deployment (CI/CD) Architecture

## Overview

To maintain high code quality and prevent integration regressions, the **AI Future Pathway Navigator** utilizes GitHub Actions for its Continuous Integration (CI) pipeline. This automated pipeline acts as the first line of defense, ensuring that all commits introduced to our primary codebase meet our strict compilation, architectural, and testing standards.

## Pipeline Triggers

Our CI pipeline enforces branch protection and code stability by automatically executing under the following conditions:

* **On Push:** Any direct commit pushed to the `main` branch.


* **On Pull Request (PR):** Any PR targeting the `main` branch.



*Note: PRs cannot be merged into `main` unless all CI jobs pass successfully.*

## CI Workflow Stages

The pipeline is structured into three parallel, highly decoupled jobs to optimize execution time and isolate domain-specific failures.

### 1. Backend (ASP.NET Core) Pipeline - *Mandatory System Gate*

This is the core validation for our authoritative RESTful API layer.

* **Restore:** Fetches and resolves all NuGet packages and dependencies.


* **Build:** Compiles the C# ASP.NET Core solution in `Release` mode to catch compilation and syntax errors.


* **Test:** Executes the suite of automated backend tests (unit, integration, and service-layer tests) to validate business rules and API integrity.



### 2. Frontend (React) Pipeline - *Web Architecture Validation*

Ensures the administrative and monitoring web interfaces remain functional.

* **Install:** Utilizes `npm ci` for deterministic, clean dependency resolution.
* **Lint:** Runs static code analysis to enforce JavaScript/TypeScript standards and React hooks rules.


* **Build:** Compiles the production-ready React bundle, verifying that no unresolved imports or build-time errors exist.



### 3. Mobile (Flutter) Pipeline - *Cross-Platform Validation*

Validates the Dart and Flutter widget ecosystem for user-facing applications.

* **Dependencies:** Runs `flutter pub get` to lock in standard packages.


* **Analyze:** Executes `flutter analyze` to ensure strict type safety and adherence to Dart best practices.


* **Test:** Runs automated widget and unit tests for the mobile workflow.



## Developer Guidelines & Best Practices

* **Never bypass the pipeline:** Do not attempt to force-push to `main` if the pipeline is failing.
* **Local Verification:** Before opening a PR, developers must run their respective test suites locally (e.g., `dotnet test`, `npm run build`, `flutter analyze`) to minimize CI bottlenecking.
* **Fail Fast:** If your designated component's CI job fails, investigate the GitHub Actions execution logs immediately. Provide the fix in the same feature branch and push to trigger a re-run.