# Member 4 Gap Audit and Role Separation

## Why every dashboard originally looked the same

The login and registration pages always navigated to `/dashboard`, the generic dashboard loaded student profile data for every role, and the navbar rendered student links for every authenticated user. In addition, the React constant used `Instructor` while the API and PostgreSQL seed use `Counsellor`.

The corrected routes are:

- Student: `/student/dashboard`
- Counsellor: `/counsellor/dashboard`
- Admin: `/admin/dashboard`
- `/dashboard`: authenticated role-aware redirect only

Student-only onboarding and career-discovery routes are now protected by the `Student` role. The Counsellor sees the approval queue. The Admin receives a governance dashboard and may enter the approval centre for oversight because the API explicitly authorizes `Counsellor,Admin`.

## What Agent 4 now does

Agent 4 is a distinct feasibility and safety gate. It receives a selected pathway plus student A/L, budget and skill data; calls an allow-listed versioned requirement-catalogue tool; validates inputs; applies deterministic academic, financial and evidence rules; and produces structured output containing:

- feasibility score and risk reasons;
- degree or alternative qualification requirement;
- subject and entry requirements;
- budget-specific cost guidance;
- missing skills and a shortest gap-closing plan;
- evidence source summaries, validation results, controlled tool-call summaries and execution timings;
- a decision to complete normally or pause for Counsellor approval.

ASP.NET Core is the only public integration point. It calls Agent 4 internally, validates the structured response, persists the workflow summary in PostgreSQL, and enforces student ownership plus staff-only decisions. React supports review/approve/reject/revise. Flutter supports submission, status, feedback, gap-plan display and history.

## Member 4 evidence present

- Backend/API: six protected workflow endpoints and a business-specific approval operation.
- Database: pathway review, audit trail, status/indexes and Agent 4 evidence fields with EF migrations.
- React: protected review queue, search/filter/sort/pagination, evidence inspection and decisions.
- Flutter: secure token storage, submission, status/history and gap-plan UI.
- Agent: structured contract, allow-listed tool, validation, safe failure and approval routing.
- Tests: Python Agent 4 rules, backend service tests, React decision tests and Flutter API tests.
- Documentation/ADR/CI: Member 4 guide, ADR and GitHub Actions workflow.

## Group-level gaps that Member 4 cannot truthfully claim as complete

The repository contains Agent 1, Agent 2 and Agent 4, but no Agent 3 implementation directory. It also does not contain one persisted coordinator workflow that visibly plans and delegates across Agents 1–4. Therefore, the group should not claim that the four-agent minimum assessed workflow is complete until Student 3 adds Pathway Planner/Coordinator and the group demonstrates the end-to-end persisted workflow.

The Admin dashboard distinguishes the role and provides governance/approval oversight, but full user-account CRUD and career-catalogue CRUD are not implemented in this Member 4 component. Those should be assigned to the owner of the Admin/catalogue component rather than falsely attributed to Member 4.

Public self-registration of privileged staff roles should be replaced before production by Student-only public registration plus Admin-controlled staff role assignment. Until that administration API exists, use controlled seed data only for demonstrations.

## Required demo sequence

1. Student signs in on Flutter, selects an existing saved pathway analysis and submits a Reality Check.
2. ASP.NET Core verifies ownership and calls Agent 4 internally.
3. Agent 4 returns structured feasibility, education, entry, cost and gap evidence.
4. PostgreSQL stores the workflow and audit summary. A risky case remains `Pending`.
5. Counsellor signs in on React, opens `/counsellor/dashboard`, inspects evidence and approves, rejects or requests revision.
6. Student refreshes Flutter and sees the updated decision, feedback and progress/history.
7. Admin signs in on React and is routed to `/admin/dashboard`, not the Student dashboard.
