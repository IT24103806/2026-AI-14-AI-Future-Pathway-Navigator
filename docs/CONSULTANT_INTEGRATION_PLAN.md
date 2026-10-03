# Consultant Role Integration — Comprehensive Plan

**Status:** Proposed (awaiting team sign-off)
**Scope:** ASP.NET Core API · PostgreSQL · React SPA · Flutter app · Python AI service
**Companion docs:** `docs/INTEGRATION_VERIFICATION.md`, `docs/MOBILE_ARCHITECTURE.md`, `docs/ADR-004-MEMBER-4-APPROVAL-WORKFLOW.md`

---

## 1. Executive summary

The platform today is a four-agent pipeline that a student walks alone:

```
Agent 1 (Onboarding) → Agent 2 (Career Discovery) → Agent 3 (Pathway Planner) → Agent 4 (Reality Check → Counsellor approval)
```

The only human in the loop is the **Counsellor**, and only at the very end, only for a binary
decision (Approve / Reject / NeedsRevision) on a high-impact Reality Check. There is no channel for a
student to raise a *question* — "which pathway should I actually pick?", "what does an AI Engineer
actually do?", "the Reality Check said I'm missing statistics, where do I learn it?", "can someone
explain this cost figure?" — and there is no way for any human to answer back inside the product.

This plan adds a fifth actor — the **Consultant** — as a first-class, context-anchored support layer,
plus the notification spine that lets a student see the answer and *continue their journey without
losing their place*.

### 1.1 The five decisions this plan makes

| # | Decision | Chosen option | Why |
|---|---|---|---|
| **D1** | Is Consultant a new role or a rename of Counsellor? | **New role `Consultant`**, seeded as a 4th role; counselling remains a *governance* function, consulting is a *support* function | Different duties, different audit story, least privilege. A consultant must not be able to approve a pathway; a counsellor is not necessarily a subject-matter expert |
| **D2** | Where in the journey does the Consultant attach? | **Career Discovery (Agent 2/3 output) and Reality Check (Agent 4) are the two primary anchors**, with the Pathway Planner roadmap as a third anchor and the student dashboard as a general fallback | These are the two moments of real *uncertainty + consequence*. Onboarding is too early (nothing to ask yet) and is Agent 1's exclusive surface |
| **D3** | Live chat or async ticket thread? | **Async thread (ticket) with SLA timers**; live chat deliberately deferred | Matches the deployment (949 MiB VPS, no websocket infra), survives disconnects, is auditable and testable — the same reasoning that produced ADR-004 |
| **D4** | How does the student learn about a reply? | **DB-backed notification inbox + unread badge + short polling (30–60 s) + optional email via the existing `IEmailService`**; `INotificationPublisher` abstraction leaves room for SSE/SignalR/FCM later | Zero new infrastructure, works identically on web and mobile, and is upgradeable without a schema change |
| **D5** | Does AI get involved? | **Yes — Agent 5 "Consultant Copilot": triage, case brief, draft reply, FAQ match. It never auto-sends.** | Keeps the project's core AI story (distinct agent responsibilities, human-in-the-loop, allow-listed tools, deterministic fallback) and is the natural "distinct AI responsibility" for a 5th team member |

### 1.2 What the student experiences (the loop we are building)

```
Student is on Career Discovery / Reality Check
        │
        ├─ taps "🙋 Ask a consultant"  (context is captured automatically)
        │        → request lands in the Consultant Desk with a case brief
        │
        ├─ notification badge lights up when the consultant claims it
        │
        └─ consultant replies (guide / answer / clarification / revision suggestion)
                 │
                 ▼
        🔔 notification → deep link back to the exact panel they were on
                 │
                 └─ the answer is rendered *inside* that component
                    (roadmap milestone, Reality Check panel, pathway card)
                    and the journey's "next step" button is re-enabled
```

Everything below is the detailed design for that loop.

---

## 2. Current-state audit (what exists, what is missing)

### 2.1 Assets we build on

| Layer | Existing capability | Reused for |
|---|---|---|
| Auth | JWT with role claim, `[Authorize(Roles=…)]`, BCrypt, Google sign-in (`AuthController`, `AuthService`) | Consultant login is **already solved** — it is a new role value, not a new auth system |
| Roles | `Roles` table + `Role` model + seeded Student/Counsellor/Admin (`AppDbContext.OnModelCreating`, migration `20260924153916_SeedDefaultRoles`) | Add `Consultant` as `10000000-0000-0000-0000-000000000004` |
| Staff UI pattern | `ProtectedRoute allowedRoles`, `RoleDashboardRoute`, `dashboardPathForRole()`, `CounsellorDashboardPage` | Copy this pattern for `/consultant/dashboard` |
| Service pattern | `ICounsellorReviewService` / `CounsellorReviewService` (ownership checks, execution strategy, audit rows, `Map()` DTO projection) | Identical shape for `IConsultationService` |
| Audit pattern | `PathwayReviewAudit` (ActorUserId, Action, FromStatus, ToStatus, Details) | `ConsultationAudit` + reuse for escalations |
| AI gateway | `AgentService` + `IAgentService` HTTP client to FastAPI, 90 s timeout, 502 mapping | Add Agent 5 methods |
| Agent pattern | `ai_service/agent_4_reality_check/` (router, schemas with inject-style validators, deterministic rules, tool-call + execution trace) | Copy the skeleton for `agent_5_consultant_copilot/` |
| Email | `IEmailService` (SMTP) | Optional "you have a reply" mail |
| Mobile | feature-first Flutter (`data/` + `presentation/`), `go_router` with `resolveRedirect`, `AppRoutes`, `Roles`, `AppDependencies` composition root | Mirror everything for `features/support/` |
| CI | 4 jobs (backend, web, python, flutter) in `.github/workflows/ci.yml` | New tests slot into the existing jobs, no pipeline redesign |

### 2.2 Gaps this plan must close

| Gap | Evidence in repo | Consequence today |
|---|---|---|
| No way for a student to ask a human anything | No "ask/help/message" endpoint anywhere in `Controllers/` | The only human interaction is a one-shot approve/reject |
| No notification model | No `Notification` entity, no badge, no email on decision | A student must manually re-open Reality Check and poll `/me/status` to learn their fate |
| No consultant role | `Roles` seed has exactly 3 rows | — |
| Students can self-register as **Admin or Counsellor** | `RegisterPage.jsx` renders a role `<select>` with Student/Counsellor/Admin; `AuthService.RegisterAsync` trusts `RoleName` | **Privilege escalation by design.** Must be fixed *before* a new staff role is added, or Consultant is compromised on day one |
| Startup role seed is all-or-nothing | `Program.cs`: `if (!dbContext.Roles.Any(r => r.Name == "Student"))` then inserts all three | Adding a 4th role would never be seeded on an existing DB |
| Student-facing write-back is absent | `PathwayPlan` has no consultant/guidance field; `PathwayReview` has no advice field | A consultant's answer would be trapped in a separate inbox |

---

## 3. Design decision record (options considered)

### D1 — New `Consultant` role vs. extending `Counsellor`

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| A. Rename/merge: Consultant = Counsellor | Zero schema change, one dashboard | Blurs two very different duties; a consultant could approve/reject pathways; contradicts the ADR-004 least-privilege story; viva question "why can a subject tutor approve a pathway?" has no answer | ✗ |
| B. New role, no AI, pure CRUD | Fastest to ship (~2 days) | No distinct AI responsibility for a 5th member; weaker rubric story | ✗ |
| C. **New role `Consultant` + Agent 5 Copilot + notification spine** | Clean separation: Counsellor = *authority*, Consultant = *guidance*; new agent = distinct, testable, defensible; notifications close an existing product gap | ~8–10 dev-days full, ~4–5 for MVP | ✅ |

**Role matrix (final):**

| Capability | Student | **Consultant** | Counsellor | Admin |
|---|---|---|---|---|
| Create a consultation request | ✅ | – | – | – |
| Read own request thread | ✅ | ✅ (assigned/pool) | – | ✅ |
| Post a message in a thread | ✅ | ✅ (assigned) | – | ✅ |
| Claim / release / reassign a request | – | ✅ (pool) | – | ✅ |
| Write a `ConsultantAdvice` note onto a `PathwayReview` | – | ✅ | ✅ | ✅ |
| Approve / reject / request revision on a Reality Check | – | **✗** | ✅ | ✅ |
| Escalate a request to the counsellor queue | – | ✅ | – | ✅ |
| Manage consultant accounts & expertise tags | – | – | – | ✅ |
| Read the full Agent 4 approval queue | – | **✗** (only the review attached to their own case) | ✅ | ✅ |
| Agent 5: triage / brief / draft | – | ✅ | – | ✅ |

### D2 — Where to attach (the integration point)

Evaluated the four candidate surfaces:

| Surface | Fit | Reasoning |
|---|---|---|
| **Onboarding (Agent 1)** | Low | Too early — the student has no concrete artefact to ask about yet, and the chat is already conversational with Agent 1. Adding a human queue here mostly produces "hello?" noise |
| **Career Discovery (Agent 2 results + Agent 3 roadmap)** | **High (primary)** | Highest-doubt moment: "why is this #1?", "what does this job actually do?", "which of these 3 should I pick?", "I can't afford this one". A consultant has domain knowledge the scoring model does not |
| **Reality Check (Agent 4)** | **High (primary)** | Highest-consequence moment: risk flags, prerequisite gaps, cost. There is *already* a human gate here, so the student has the mental model. This is also where a consultant can add a clarifying note that the deciding counsellor sees |
| **Student dashboard ("general question")** | Medium (fallback) | Needed as a catch-all, but too context-free to be the only door |

**Decision:** primary anchors on Career Discovery/Pathway Planner and Reality Check, general fallback on the dashboard. Every request stores a `ContextType` + `ContextRefId` + `ContextSnapshotJson`, so a case is never context-free.

### D3 — Async thread, not live chat
Live chat would require websockets (SignalR/SSE), presence, and admin bandwidth — heavy on a 949 MiB VPS that
already runs a LangChain service, and untestable in CI. Async threads also produce a better audit trail and
are naturally resumable from a notification. **Deferred, not rejected:** `INotificationPublisher` and the
message table are designed so a SignalR hub can be added as a transport later with no domain change.

### D4 — Notification transport
Polling a single `GET /api/notifications/unread-count` every 45 s (visibility-aware: paused when the tab is
hidden) plus a full fetch on route change is enough for a class project, costs one indexed query, and works
identically in Flutter. The publisher interface means the transport can change without touching services.

### D5 — Agent 5 is assistive only
The draft-reply agent contradicts nothing in the existing architecture: Agent 4 already proves the pattern of
"agent produces structured, validated evidence → a human makes the decision". Agent 5 follows it for support:
it classifies, summarises and *drafts*, and a consultant must explicitly send.

---

## 4. Domain model

### 4.1 Entity relationship (ASCII)

```
User ──1:1── ConsultantProfile                     (expertise, languages, capacity, accepting?)
  │
  ├──1:N── ConsultationRequest ──1:N── ConsultationMessage   (thread, incl. internal-only notes)
  │              │            └───1:N── ConsultationAudit    (state transitions, staff actions)
  │              │
  │              ├── ContextType + ContextRefId + ContextSnapshotJson  (CareerDiscovery | PathwayPlan |
  │              │                                                      RealityCheck | General)
  │              └── ConsultantGuidanceJson  (structured write-back artefacts)
  │
  ├──1:N── Notification                              (inbox, deep link, dedupe key)
  │
  └──1:N── PathwayReview ── (new) ConsultantAdviceJson   ← cross-role handoff to the Counsellor
                            (existing) AuditEvents
  └──1:N── PathwayPlan  ── (new) ConsultantGuidanceJson  ← roadmap milestone sees the guide
```

### 4.2 `ConsultationRequest`

| Column | Type | Notes |
|---|---|---|
| `Id` | Guid PK | |
| `StudentId` | Guid FK → Users | **Always taken from the JWT**, never the body |
| `AssignedConsultantId` | Guid? FK → Users | Null = unclaimed, sitting in the pool |
| `ContextType` | string(30) | `CareerDiscovery` \| `PathwayPlan` \| `RealityCheck` \| `General` |
| `ContextRefId` | Guid? | `PathwayAnalysisId` / `PathwayPlanId` / `PathwayReviewId` |
| `ContextSnapshotJson` | text | Frozen copy of what the student was looking at: career names, match scores, feasibility score, risk reason, missing skills, current milestone |
| `Category` | string(40) | `GuideRequest` \| `DoubtAnswer` \| `RealityCheckClarification` \| `PathwayAdvice` \| `MarketCourseInfo` \| `Unclassified` |
| `Priority` | string(10) | `P1` (blocked, 24 h SLA) \| `P2` (standard, 48 h) \| `P3` (advisory, 72 h) |
| `Subject` | string(140) | One-line summary for the queue card |
| `Body` | string(4000) | The student's question |
| `Status` | string(30) | see state machine §4.6 |
| `SlaDueAt` | DateTime | `CreatedAt + hours(Priority)`; recomputed on priority change |
| `FirstRespondedAt` / `AnsweredAt` / `ClosedAt` | DateTime? | SLA + metrics |
| `ResolutionSummary` | string(1000)? | Public closing note, shown in the student's timeline |
| `ConsultantGuidanceJson` | text | `{ guides: [], checklists: [], nextSteps: [] }` — the write-back payload |
| `AgentTriageJson` | text? | Raw Agent 5 triage evidence (auditable) |
| `AgentDraftUsed` | bool | Was the consultant's reply based on an Agent 5 draft, and was it edited? AI-usefulness metric |
| `StudentRating` / `StudentFeedback` | int? / string(1000)? | Post-close satisfaction (requires `Status = Closed`) |
| `RowVersion` | byte[] (concurrency token) | Makes concurrent "claim" impossible to double-assign |
| `CreatedAt` / `UpdatedAt` | DateTime | Audit fields, matching every existing entity |

### 4.3 `ConsultationMessage`

| Column | Notes |
|---|---|
| `Id`, `ConsultationRequestId`, `AuthorUserId`, `AuthorRole` | `AuthorRole` snapshot so UI renders correctly even after a role change |
| `Body` (4000) | |
| `IsInternal` (bool) | Consultant-only note ("waiting on the HOD", "this is really about the scholarship") — **never** returned by student endpoints |
| `ResourcesJson` | `[{ label, url, kind }]` — **links only**. No blob storage on the VPS |
| `CreatedAt`, `EditedAt`, `DeletedAt` | Soft delete keeps the audit trail intact |

### 4.4 `ConsultationAudit`
Mirrors `PathwayReviewAudit` exactly: `Id`, `ConsultationRequestId`, `ActorUserId`, `Action`
(`Created`, `Claimed`, `Reassigned`, `Replied`, `InternalNoteAdded`, `Escalated`, `Resolved`, `Closed`,
`Reopened`, `AutoExpired`, `PriorityChanged`), `FromStatus`, `ToStatus`, `Details`, `CreatedAt`.

**Reuse bonus:** escalating a Reality Check clarification writes a `PathwayReviewAudit` row with
`Action = "ConsultantEscalation"`, `FromStatus = ToStatus = "Pending"`, `Details = <summary>`. The existing
audit table already supports this — no schema change, and the counsellor sees it in the same evidence panel.

### 4.5 `Notification`

| Column | Notes |
|---|---|
| `Id`, `UserId`, `Type`, `Title` (120), `Body` (400), `DeepLink` (300) | |
| `EntityType` / `EntityId` | `Consultation` \| `PathwayReview` \| `PathwayPlan` |
| `IsRead`, `ReadAt`, `CreatedAt` | |
| `DedupeKey` (unique index, nullable) | Prevents duplicate "SLA breach" rows for the same request |
| `Priority` | Drives badge colour / future push priority |

Index: `(UserId, IsRead, CreatedAt DESC)` — the unread-count query becomes a single index seek.

### 4.6 Request state machine

```
                 ┌───────────── student withdraws ─────────────┐
                 │                                             ▼
  [Created] → Open ──claim──► Claimed ──reply──► Answered ──student confirms──► Resolved
                │  ▲             │                  │                              │
                │  │             │ asks for info    │ consultant/escalate          │ close
                │  │             ▼                  ▼                              ▼
                │  │      AwaitingStudent ──student replies──► InProgress       Closed ──► Reopened (7 days)
                │  │             │                                              │
                │  │             └──────── auto-expire after 14 days ──────────►│
                │  └────────────────────────────────────────────────────────────┘
                │
                └──escalate──► Escalated ──counsellor decides──► Answered / Closed
```

| Transition | Actor | Notification fired |
|---|---|---|
| `Open → Claimed` | Consultant | **Student:** "Nimal is looking into your question" |
| `Claimed → InProgress/AwaitingStudent` | Consultant | Student (only for AwaitingStudent) |
| `* → Answered` | Consultant | **Student: "You have a reply"** + deep link |
| `Answered → Resolved` | Student | Consultant (low priority) |
| `* → Escalated` | Consultant | Student + Counsellor/Admin queue |
| `* → Closed` | Student / Consultant / auto | Both, with `ResolutionSummary` |
| SLA breach (timer) | System | Consultant + Admin (`P1` only) |

Unclaimed `P1` requests older than 12 h escalate to **Admin** (who can assign or answer).

---

## 5. API contract

All routes require a JWT. Student identity comes from `ClaimTypes.NameIdentifier`; no endpoint accepts a
student ID from the client.

### 5.1 Student

| Method & route | Purpose | Notes |
|---|---|---|
| `POST /api/consultations` | Create a request | Body: `contextType`, `contextRefId`, `category?`, `priority?`, `subject`, `body`. Server resolves + freezes the context snapshot and enforces ownership of `contextRefId`. Agent 5 triage runs in-line (best-effort; failure ⇒ `Unclassified`/`P2`) |
| `GET /api/consultations/me?status=&page=&pageSize=` | My requests | Paged, newest first |
| `GET /api/consultations/{id}` | Thread | Ownership check → `404` (not `403`) to avoid enumeration |
| `POST /api/consultations/{id}/messages` | Student follow-up | Blocked when `Closed`; reopens after a student reply on `AwaitingStudent` |
| `POST /api/consultations/{id}/close` | Confirm resolved | Records `StudentRating`/`StudentFeedback` |
| `POST /api/consultations/{id}/reopen` | Reopen within 7 days | Creates a `Reopened` audit row |
| `GET /api/consultations/context/{contextType}/{refId}` | Badge in the page: existing open request for *this* panel | Lets the Reality Check show "You already asked about this" instead of a duplicate form |
| `GET /api/notifications?unreadOnly=&page=&pageSize=` | Inbox | |
| `GET /api/notifications/unread-count` | Poll target | ~1 ms indexed query |
| `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all` | Mark read | |

### 5.2 Consultant

| Method & route | Purpose |
|---|---|
| `GET /api/consultant/queue?status=&category=&priority=&contextType=&search=&page=&pageSize=` | Pool + my cases, filter/sort/paginate (mirrors `GET /counsellor-review` exactly) |
| `GET /api/consultant/queue/{id}` | Case detail: snapshot, agent brief, thread, audit, SLA clock |
| `POST /api/consultant/queue/{id}/claim` | Atomic claim (`RowVersion`); `409` if already claimed |
| `POST /api/consultant/queue/{id}/release` | Return to pool (with reason) |
| `POST /api/consultant/queue/{id}/reply` | Body: `message`, `resources[]`, `guidance` (write-back payload), `resolutionSummary?`, `closeAfterReply?`, `usedAgentDraftId?`. Single transaction: message + status + audit + notification + write-back |
| `POST /api/consultant/queue/{id}/note` | Internal note (never visible to the student) |
| `POST /api/consultant/queue/{id}/priority` | Override priority (`P1..P3`), recompute SLA |
| `POST /api/consultant/queue/{id}/escalate` | To counsellor queue; writes `PathwayReviewAudit` when context is Reality Check |
| `GET /api/consultant/me/stats` | Open cases, SLA compliance %, avg first-response, resolved this week, satisfaction |
| `PUT /api/consultant/me/profile` | Headline, expertise tags, languages, `IsAcceptingRequests`, capacity |
| `POST /api/consultant/queue/{id}/agent/draft` | Request/refresh an Agent 5 draft reply |

### 5.3 Admin

| Method & route | Purpose |
|---|---|
| `GET /api/admin/consultants` | List consultant accounts + load |
| `POST /api/admin/consultants` | Provision/invite a consultant (this becomes the **only** way to create staff accounts) |
| `PUT /api/admin/consultants/{userId}` | Activate/deactivate, expertise, capacity |
| `GET /api/admin/consultation-metrics` | Volume, SLA breaches, deflection (FAQ) rate, category mix |

### 5.4 Internal AI service (never exposed publicly)

`POST /api/v1/agent-5/triage` · `POST /api/v1/agent-5/brief` · `POST /api/v1/agent-5/draft-reply` ·
`POST /api/v1/agent-5/faq-match` — called only by `AgentService` on the internal Docker network.

**Status codes:** `400` validation · `404` not found/not yours · `409` illegal transition (already claimed,
already closed) · `422` reply references an unknown guidance type · `502` AI service failure (never blocks
the write path) · `503` transient DB drop (existing middleware).

---

## 6. Journey integration map (the heart of the plan)

Every entry point captures context, and every resolution writes back into the component the student was in.
No answer is ever stranded in an inbox.

### 6.1 Career Discovery / Pathway Planner — `web:CareerDiscoveryPage.jsx`, `mobile:career_discovery_screen.dart`

| Where | UI addition | Write-back on reply |
|---|---|---|
| Each `PathwayRecommendationCard` | Small `🙋 Ask about this pathway` action in the card footer | Reply renders as a "Consultant note" under that card and, if the student built a roadmap, as a **`Consultant-recommended` badge on the matching roadmap milestone** (`PathwayPlan.ConsultantGuidanceJson`) |
| "Missing skills" panel (`MissingSkillsPanel.jsx`) | `Where do I learn this?` shortcut, pre-filled with the missing-skill list as the subject | Guide links land in the missing-skills panel as one-tap resources |
| Roadmap milestone (`RoadmapTimeline.jsx`) | Per-stage `Need help with this step?` | `ShareGuide` guidance attaches to that stage: `{ stageKey, resources[], checklist[] }` |
| Agent 5 duplicate guard | Before the form opens, `faq-match` shows up to 3 already-answered public questions with "still ask a human" as an equal option | Deflection metric only; a human door is always one tap away |

### 6.2 Reality Check — `web:StudentRealityCheckPage.jsx`, `mobile:student_reality_screen.dart`

| Where | UI addition | Write-back on reply |
|---|---|---|
| Pending review banner | `❓ Ask a consultant about this review` | **This is the flagship case.** The consultant's `RealityCheckClarification` reply is written to `PathwayReview.ConsultantAdviceJson` and rendered inside the pending panel, so waiting time becomes productive |
| `NeedsRevision` / `Rejected` result | `I don't understand why — ask a consultant` (pre-filled with `CounsellorFeedback` + risk reason) | `Escalate` writes a `PathwayReviewAudit` row (`ConsultantEscalation`) that the counsellor sees before re-deciding |
| `IsHighRisk` block | `Talk to someone about this first` (subject auto-set to `riskReason`) | Guidance becomes part of the review evidence the counsellor inspects |
| Cost guidance | `Is this cost realistic for my budget?` | Reply attaches to the review's evidence panel |

Cross-role handoff is the strategic win here: **the consultant's note travels with the review**, so the
counsellor's decision is better informed instead of the student ping-ponging between two humans.

### 6.3 Student dashboard — `web:DashboardPage.jsx`, `mobile:student_dashboard_screen.dart`

- New `🔔 Notifications` bell in `Navbar.jsx` (web) and the student app bar (mobile) with an unread badge.
- New "Support" tile in `dash-stats`: open requests, last reply, average response time.
- A `JourneySteps.jsx` / `AgentWorkflow.jsx` annotation: stages with an active consultation show a
  "🤝 consultant engaged" chip, so the journey rail reflects reality.
- `Ask a general question` button → `General` context request (subject required).

### 6.4 Notification → deep link → resume

| Notification | Deep link (web) | Deep link (mobile) |
|---|---|---|
| Reply to a Career Discovery question | `/career-discovery?consultation={id}&focus=pathway:{analysisId}` | `/student/career-discovery?consultationId={id}&focus=pathway:{analysisId}` |
| Reply to a Reality Check question | `/student/reality-check?consultation={id}` | `/student/reality-check?consultationId={id}` |
| Reply to a roadmap-stage question | `/career-discovery?consultation={id}&stage={stageKey}` | `/student/career-discovery?consultationId={id}&stage={stageKey}` |
| Generic | `/student/support?consultation={id}` | `/student/support?consultationId={id}` |

Both clients read the query param on mount, open the thread drawer, and **auto-scroll the page to the exact
panel** (`focus`/`stage` params). That is the literal implementation of "see the update and continue their
journey".

---

## 7. Agent 5 — Consultant Copilot (the distinct AI responsibility)

New package: `ai_service/agent_5_consultant_copilot/{router,schemas,agent,state,prompts}.py`, registered in
`main.py` like the other four. Called through `AgentService` (`ProcessAgent5TriageAsync`, `…BriefAsync`,
`…DraftReplyAsync`).

| Endpoint | Input | Output (Pydantic-validated) |
|---|---|---|
| `/agent-5/triage` | subject, body, context snapshot, student stage | `category`, `priority`, `expertise_tags[]`, `language`, `sentiment`, `safety_flags[]`, `duplicate_of?`, `suggested_sla_hours`, `confidence`, `trace[]` |
| `/agent-5/brief` | request + snapshot + student's agent evidence (Agent 1 profile slots, Agent 2 scores, Agent 3 roadmap, Agent 4 feasibility/risk) | Ordered case brief: "who is this student", "what they're looking at", "what they already tried", "the actual question", "suggested evidence to cite", `evidence_sources[]` |
| `/agent-5/draft-reply` | same + consultant's chosen tone/length | `draft_id`, `body`, `citations[]`, `confidence`, `safety_notes[]`, `must_escalate` |
| `/agent-5/faq-match` | student's draft question (pre-submit) | top-3 matches from resolved-and-published answers, `score`, `answer_id` |

**Guardrails (all testable, mirroring Agent 4's style):**
1. Instruction-like input rejected by the same validator pattern used in `agent_4_reality_check/schemas.py`.
2. No network, no DB, no student PII beyond the explicit payload; the only allow-listed tool is the curated
   career/prerequisite knowledge base (reusing Agent 2/4 data).
3. Draft linting blocks promises: admission guarantees, visa outcomes, specific salary guarantees, medical/
   legal/financial directives ⇒ `must_escalate = true` and the UI shows a warning chip.
4. `confidence < 0.55` ⇒ the draft is labelled "low confidence — write your own".
5. **Nothing is ever auto-sent.** The send button is the consultant's; `AgentDraftUsed` + edit-distance
   records how useful the AI actually was (a great viva metric).
6. Safe failure: any Agent 5 error ⇒ the request is still created (`Unclassified`, `P2`), the queue still
   works, the consultant writes manually. The AI is never on the critical path.

**Why this agent is distinct (viva answer):** Agents 1–4 *produce* guidance for the student; Agent 5
*prepares work for a human* — triage, evidence synthesis and drafting — under a human-in-the-loop gate. Its
tools, contract, failure semantics and success metric (draft acceptance rate) are all different.

---

## 8. Implementation plan (file-by-file)

### 8.1 Phase 0 — Hardening (must be first) · ~0.5 day

| File | Change |
|---|---|
| `backend/.../Migrations/*_SeedConsultantRole.cs` | Insert role `10000000-0000-0000-0000-000000000004` = `Consultant`; update `AppDbContextModelCreating().HasData` + model snapshot |
| `backend/.../Program.cs` | Replace the all-or-nothing role seed with a per-role upsert so an existing DB gains `Consultant` |
| `backend/.../AuthService.cs`, `DTOs/Auth/RegisterRequestDto.cs`, `Controllers/AuthController.cs` | Public registration creates **Students only**; ignore/reject client-supplied `RoleName` for anything else. Staff accounts are provisioned by Admin only |
| `web/.../RegisterPage.jsx`, `RegisterPage` tests | Remove the role `<select>` (or show a static "Student account" note) — the web SPA is the only client that offers role choice today |
| `mobile/.../lib/core/constants/roles.dart` | Add `consultant = 'Consultant'`; leave `isStaff` **unchanged** (a consultant is not an approver) and add a separate `isConsultant`/`canAccessConsultantDesk(role)` helper. The Flutter app already hard-codes `roleName: 'Student'` (`auth_repository.dart`), which is correct — the server must now enforce it |
| `backend/.../Data/AppDbContext.cs` | Composite index `(Status, Priority, CreatedAt)` on the new request table; unique index on `Notification(UserId, DedupeKey)` |

### 8.2 Phase 1 — Data + student API + notifications · ~2 days

**New backend files**
- `Models/ConsultationRequest.cs`, `ConsultationMessage.cs`, `ConsultationAudit.cs`, `Notification.cs`, `ConsultantProfile.cs`
- `DTOs/Consultation/{CreateConsultationRequestDto, ConsultationResponseDto, ConsultationMessageDto, ReplyConsultationDto, ConsultationQueueFilterDto, ConsultantStatsDto}.cs`
- `DTOs/Notification/NotificationDto.cs`
- `Services/IConsultationService.cs` + `ConsultationService.cs` (create/list/get/reply/close/reopen/context-lookup)
- `Services/IConsultantQueueService.cs` + `ConsultantQueueService.cs` (queue/claim/release/reply/note/escalate/stats)
- `Services/INotificationService.cs` + `NotificationService.cs`, `Services/INotificationPublisher.cs` (+ `DbNotificationPublisher`)
- `Services/IContextSnapshotResolver.cs` + `ContextSnapshotResolver.cs` (builds the frozen snapshot per `ContextType`, with ownership checks)
- `Controllers/ConsultationController.cs`, `Controllers/ConsultantController.cs`, `Controllers/NotificationController.cs`
- Migrations: `AddConsultationSupport`, `AddConsultantWriteBackFields` (`PathwayPlan.ConsultantGuidanceJson`, `PathwayReview.ConsultantAdviceJson`)

**Conventions to copy from `CounsellorReviewService`:** execution strategy + transaction for multi-row writes,
`Map()` projections, `AllowedStatuses` guards, `AsNoTracking` for reads, `KeyNotFoundException`→404 /
`InvalidOperationException`→409 / `HttpRequestException`→502 mapping in the controller.

### 8.3 Phase 2 — Consultant Desk (web) · ~1.5 days

| New file | Purpose |
|---|---|
| `src/api/consultationApi.js`, `src/api/consultantApi.js`, `src/api/notificationApi.js` | Thin axios wrappers in the existing style |
| `src/pages/ConsultantDashboardPage.jsx` | Three-pane desk: **queue** (filters: status/category/priority/SLA-sorted, search) → **case** (snapshot card, Agent 5 brief, thread, internal notes tab, SLA countdown) → **composer** (draft from Agent 5, resources, guidance picker, reply & close) |
| `src/pages/ConsultantStatsPage.jsx` (or a tab) | `GET /consultant/me/stats` |
| `src/pages/SupportPage.jsx` | Student's "My questions" list + thread view |
| `src/components/consultation/AskConsultantDialog.jsx` | Context-aware request modal (pre-fills subject/body from the anchor, calls `faq-match`, always offers "ask a human") |
| `src/components/consultation/ConsultationThread.jsx` | Shared thread renderer (student + consultant variants) |
| `src/components/consultation/NotificationBell.jsx` | Navbar bell, unread badge, dropdown, "mark all read" |
| `src/components/consultation/AgentDraftPanel.jsx` | Brief + draft + citations + safety chips + "insert into reply" |
| `src/context/NotificationContext.jsx` + hook | 45 s polling, visibility-aware, exposes `unreadCount`, `markRead`, `refresh` |
| `src/utils/consultationContext.js` | Builds `{contextType, contextRefId, subject, prefill}` from the current page |
| `src/styles/pages/support.css` | Desk/support styles (existing token system) |

**Edits:** `App.jsx` (3 routes, `allowedRoles={['Consultant','Admin']}` for the desk),
`utils/roleRoutes.js` (`Consultant → /consultant/dashboard`), `Navbar.jsx` (bell + "Consultant Desk" link
for Consultant/Admin), `CareerDiscoveryPage.jsx`, `PathwayRecommendationCard.jsx`, `MissingSkillsPanel.jsx`,
`RoadmapTimeline.jsx`, `StudentRealityCheckPage.jsx`, `DashboardPage.jsx`.

### 8.4 Phase 3 — Agent 5 · ~1.5 days

`ai_service/agent_5_consultant_copilot/*`, registration in `main.py`, `AgentService.ProcessAgent5*` methods,
`ai_service/tests/test_agent_5.py`, `ai_service/tests/test_backend_contract.py` additions.

### 8.5 Phase 4 — Mobile parity · ~1 day

`mobile/.../lib/features/support/{data,presentation}/` — `consultation_models.dart`,
`consultation_repository.dart`, `notification_repository.dart`, `student_support_screen.dart`,
`ask_consultant_sheet.dart`, `notification_sheet.dart`, `consultant_queue_screen.dart`,
`consultant_case_screen.dart`, `consultant_controller.dart`.

**Edits:** `core/constants/roles.dart` (add `consultant`), `routes.dart`
(`consultant`, `studentSupport`, `notification(consultationId)`), `app/router.dart` (guard
`_isUnder(location, AppRoutes.consultant) → role == consultant || admin`), `app/dependencies.dart`
(two new repositories), `student_dashboard_screen.dart` (bell + Support tile + deep-link handling),
`career_discovery_screen.dart` and `student_reality_screen.dart` (ask buttons + query-param focus),
`test/support/fakes.dart` (fakes), `test/app/router_test.dart` (guard cases).

### 8.6 Phase 5 — Ops polish · ~0.5–1 day

SLA background sweeper (`SlaSweepHostedService`, mirrors `DatabaseKeepAliveHostedService` style), email digest
through `IEmailService`, admin consultant management page, `docs/CONSULTANT_INTEGRATION_PLAN.md` (this file) +
`docs/CONSULTANT_RUN_AND_VERIFY.md` + ADR-005, README/DEPLOYMENT env notes, CI job extension if Agent 5 needs
its own test group.

### 8.7 Effort summary

| Phase | Deliverable | Dev-days | Demoable |
|---|---|---|---|
| 0 | Roles + hardening | 0.5 | Consultant can log in and land on an empty desk |
| 1 | Data + APIs + notifications | 2 | curl/Postman: create → claim → reply → badge |
| 2 | Consultant Desk + student UI | 1.5 | Full web loop |
| 3 | Agent 5 | 1.5 | Triage + brief + draft with human send |
| 4 | Mobile parity | 1 | Full mobile loop |
| 5 | Ops polish + docs | 1 | SLA, metrics, docs |
| **Total** | | **~7.5–8 days** | |

**If the deadline is tight (2–3 days):** Phase 0 + Phase 1 + a *read-only* Phase 2 (queue, case view, reply —
no Agent 5, no stats, no mobile, no admin page). The core student-visible loop is complete and demoable.

---

## 9. Security, privacy & abuse controls

| Threat | Control |
|---|---|
| **Self-registration as staff** (exists today!) | Public register → Student only; Consultant/Admin provisioned by Admin (`POST /api/admin/consultants`). This must ship in Phase 0 |
| **IDOR** on requests/messages | `StudentId` from JWT; `/me` endpoints; consultant scope = assigned ∪ unclaimed pool; single-object lookups return `404` for both "missing" and "not yours" |
| **Consultant reads unrelated student data** | Case payload contains only the frozen snapshot + explicitly linked artefacts; every case view writes a `Viewed` audit row |
| **Concurrent claim** (two consultants) | `RowVersion` concurrency token + `409 Conflict`; UI says "already taken by X" and refreshes |
| **Spam / flooding the queue** | Max 3 open requests per student; 60 s cooldown between creations; max 4000 chars; rate limiting per user on the create endpoint |
| **PII leakage / off-platform contact** | Emails, phone numbers and social handles are masked in message bodies; links are allow-listed (http/https only, no `javascript:`); attachments are links-only |
| **Prompt injection via student text** | Same validator pattern as Agent 4; Agent 5 output is data, never a command; no tool can be invoked by user text |
| **AI hallucination reaching the student** | No auto-send; citations required for factual claims; low-confidence drafts flagged; `must_escalate` blocks publication |
| **Abusive/unsafe messages** | `safety_flags` from triage (`self-harm`, `harassment`, `legal`) route to the Consultant + Admin with a policy banner; resources shown to the student instead of a normal reply |
| **Audit integrity** | Append-only `ConsultationAudit`; soft-deleted messages retained; `AgentTriageJson`/`AgentDraftUsed` retained as AI evidence |
| **Enumeration** | No public IDs, no sequential keys (GUIDs already), 404-over-403 policy |
| **Data retention** | Closed requests purged/archived after 12 months; compliance note in `docs/` |

---

## 10. Testing strategy

| Layer | New tests | Key cases |
|---|---|---|
| **xUnit** (`backend/PathwayNavigator.Tests/`) | `ConsultationServiceTests`, `ConsultantQueueServiceTests`, `NotificationServiceTests` | Ownership/IDOR, illegal transitions (double claim, reply after close, claim after resolve), internal notes never leak into student DTOs, notification fan-out, SLA computation, dedupe, concurrency conflict |
| **Vitest** (`web/…/src/**`) | `ConsultantDashboardPage.test.jsx`, `AskConsultantDialog.test.jsx`, `NotificationBell.test.jsx`, `ConsultantRouteGuard.test.jsx` | Role gating, queue filters, reply validation, draft insertion, badge counts, deep-link query handling, student never sees `IsInternal` |
| **pytest** (`ai_service/tests/`) | `test_agent_5.py` + contract test | Triage determinism on golden inputs, priority for blocked Reality Check, injection rejection, `must_escalate` on guarantee language, low-confidence flag, safe failure → 502 mapping, schema contract with .NET DTOs |
| **Flutter** (`mobile/…/test/`) | `consultation_controller_test.dart`, `consultant_queue_test.dart`, `router_test.dart` extension | Consultant cannot reach `/student/*`, student cannot reach `/consultant`, deep link opens the right thread, badge refresh, offline/error states |
| **Manual E2E script** (`docs/CONSULTANT_RUN_AND_VERIFY.md`) | 10-step two-browser demo | See §12 |

CI impact: no job restructuring — new tests are picked up by the existing four jobs. If the Agent 5 suite grows,
split it into its own job to keep the Python job under a few minutes.

---

## 11. Notifications detail

| Type | Trigger | Recipient | Channel |
|---|---|---|---|
| `ConsultationSubmitted` | Student creates | Consultants (pool) | In-app badge (role-scoped) |
| `ConsultationClaimed` | Claim | Student | In-app + optional email |
| `ConsultationReplied` | Reply | Student | **In-app + email** |
| `ConsultationMoreInfoNeeded` | `AwaitingStudent` | Student | In-app |
| `ConsultationEscalated` | Escalate | Student + Counsellor/Admin | In-app |
| `ConsultationResolved` / `Closed` | Close | Both | In-app |
| `ConsultationSlaBreach` | Sweeper | Consultant + Admin | In-app (+email for P1) |
| `ConsultationAutoExpired` | 14 days idle | Student | In-app |
| `RealityCheckDecision` *(backfill)* | Counsellor decision | Student | In-app + email — fixes the existing "student has to poll" gap using the new spine |

Implementation notes: publish *inside* the same transaction as the state change (outbox-lite: the notification
row is committed with the audit row, so it can never be lost or duplicated); email is dispatched after commit
and failures are swallowed+logged, exactly like the existing `EmailService` usage.

---

## 12. Demo & viva script (5 minutes)

1. **Registration hardening:** show that the role dropdown is gone; new accounts are Students.
2. **Student A** signs in on the React SPA, opens a pathway, taps `🙋 Ask about this pathway`, sends a question.
   Show the Agent 5 triage chip (`PathwayAdvice · P2 · 48 h`).
3. **Consultant** signs in on a second browser, lands on `/consultant/dashboard` (guard demo: also show that
   `Counsellor` can open the desk but `Consultant` cannot open the approval queue).
4. Consultant claims the case → **Student A sees the badge light up without a manual refresh** (notification demo).
5. Consultant reviews the Agent 5 brief + draft, edits it, attaches a guide, replies.
6. **Student A** clicks the notification → lands back on the *same pathway card* with the consultant's note
   inline, and a `Consultant-recommended` resource on the roadmap milestone. **Journey resumed.**
7. **Reality Check case:** Student B, blocked on a `Pending` high-risk review, asks for clarification. Consultant
   replies + escalates. Show the `PathwayReviewAudit` row (`ConsultantEscalation`) and the Counsellor seeing the
   consultant's note in the approval evidence panel before deciding.
8. Postgres evidence: `ConsultationRequests`, `ConsultationMessages`, `ConsultationAudits`, `Notifications` rows.
9. Show all four CI test groups green.

**Likely viva questions**
- *Why isn't Consultant the same as Counsellor?* Authority vs. guidance; least privilege; different audit trails.
- *Why not real-time chat?* Async survives disconnects, is auditable/testable, needs no websocket infra on a 949 MiB box (consistent with ADR-004's reasoning).
- *Does the AI answer students?* No. Agent 5 drafts; a consultant sends. Every published word has a human author.
- *How do you prevent a student from reading another student's thread?* Student ID from the JWT, `/me` endpoints, 404-over-403, and the consultant's scope is the pool plus their own assignments.
- *How do you know the AI helps?* `AgentDraftUsed` + edit distance + time-to-first-response before/after.

---

## 13. Metrics & success criteria

| Metric | Target |
|---|---|
| Median time-to-first-response | < 24 h (`P1`), < 48 h (`P2/P3`) |
| SLA compliance | ≥ 90 % |
| Resolution without escalation | ≥ 80 % |
| Student satisfaction (1–5) | ≥ 4.0 |
| AI draft acceptance (sent with light edits) | ≥ 50 % — proves Agent 5 earns its place |
| FAQ deflection (answered without creating a request) | 15–30 % |
| Duplicate-question rate | < 10 % |

---

## 14. Risks & mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Scope creep (5 phases at once) | Deadline miss | MVP cut list (§8.7); phases are independently demoable |
| Consultant pool is empty at demo | Feature looks dead | Admin can act as a consultant; seed two demo consultant accounts + a scripted case (`docs/CONSULTANT_RUN_AND_VERIFY.md`) |
| Claim race in the demo | Visible failure | `RowVersion` + `409`; UI refresh message. Tested in xUnit |
| Agent 5 latency inflates request creation | Poor UX | Triage is best-effort with a short timeout; creation succeeds without it |
| Notification spam | Annoyance | Dedupe keys, one notification per state change, digest email for low-priority events |
| Reviewer confusion: two human roles | Demo confusion | Distinct copy: **"Approval Centre"** (counsellor) vs **"Consultant Desk"** (consultant); the nav link labels differ |
| New columns break existing Reality Check flow | Regression | Both new columns are nullable and additive; existing tests must stay green (CI gate) |
| Cost of LLM calls per case | Budget | Draft/brief generated on demand (button), not automatically on every request |

---

## 15. Open decisions (recommended defaults in bold)

1. **Consultant account provisioning:** Admin-only creation (**recommended**) vs. invited-by-email with a
   one-time token vs. public Consultant sign-up with admin approval.
2. **Email notifications:** enable for every reply (**recommended: yes, reply + decision only**) or in-app only.
3. **Mobile push (FCM):** defer to a later phase (**recommended**) — it needs Firebase project config that the
   current CI does not have.
4. **Consultant access to the approval queue:** read-only for the linked review only (**recommended**) vs.
   full read access.
5. **Public FAQ:** publish resolved answers anonymously after consultant opt-in (**recommended: opt-in only**)
   or keep everything private in v1.

---

## Appendix A — Copy deck (student-facing)

| Element | Text |
|---|---|
| Card action | 🙋 Ask about this pathway |
| Reality Check pending banner | ❓ Not sure what this review means? Ask a consultant — you'll get an answer here. |
| Missing skills shortcut | 🎓 Where do I learn these skills? |
| Empty state (My questions) | No questions yet. Whenever a pathway, a cost figure or a review confuses you, ask here — a real consultant answers. |
| Success toast (reply) | Your consultant replied. Your pathway is waiting where you left it. |
| Resolved state | ✅ Resolved with a consultant — your answer is saved to this pathway. |

## Appendix B — Definition of done (per phase)

- [ ] EF migration applies cleanly on an existing database and on an empty one
- [ ] `dotnet test` · `pytest -q` · `npm test && npm run build` · `flutter analyze && flutter test` all green
- [ ] Ownership/IDOR tests exist for every new endpoint
- [ ] Every state transition writes a `ConsultationAudit` row
- [ ] No student endpoint can ever return `IsInternal` messages or another student's data
- [ ] `docs/INTEGRATION_VERIFICATION.md` updated with the new endpoint map
- [ ] A two-browser manual run of §12 passes without a manual refresh on the student side

## Appendix C — Demo seed data

```sql
-- Consultant account (password set via the Admin provisioning endpoint in the app, not raw SQL)
-- ConsultantProfile: expertise = ["Software Engineering","AI/ML","Scholarships"], languages = ["English","Sinhala"]
-- 3 open P1/P2 requests: one CareerDiscovery (guide request), one RealityCheck (clarification),
-- one PathwayPlan stage (where do I learn statistics?)
```

---

*Implementation is complete across all five phases in this branch; the remaining gates are the
machine-specific ones in §16.3 (`dotnet build`/`dotnet test`/`dotnet ef database update`, then
`flutter analyze`/`flutter test`) which this environment cannot run.*

---

## 16. Implementation status (as built)

This section records what actually shipped against the plan above, so the code and the document do
not drift. Anything not listed here follows §4–§11 exactly.

### 16.1 Deviations from the plan

| Plan | Built | Why |
| --- | --- | --- |
| `POST /api/consultant/{id}/agent/draft` | `POST /api/consultant/queue/{id}/draft` with `{ "tone": "Supportive" \| "Direct" \| "Detailed" }` | Keeps every case action under the same `queue/{id}/…` prefix the rest of the desk uses. |
| "Confirm EF can map a row-version token before Phase 1" | `ConsultationRequest.RowVersion` is `uint` + `IsRowVersion()` → PostgreSQL `xmin` (`xid` column). `byte[]` was rejected: Postgres has no `rowversion` type and Npgsql has no automatic updater for `bytea`. | Double-claim protection without a hand-rolled version column. |
| Admin consultant management | `GET/POST /api/admin/consultants`, `PUT /api/admin/consultants/{userId}`, `POST /api/admin/consultants/cases/{consultationId}/reassign/{consultantUserId}`, `GET /api/admin/consultation-metrics` | Metrics live on an absolute route so the path reads as a reporting endpoint, not a consultant resource. |

### 16.2 Configuration keys

| Key | Default | Meaning |
| --- | --- | --- |
| `AiServiceSettings:EnableConsultantCopilot` | `true` | When false, the desk never calls Agent 5; brief/draft panels render "unavailable" and the human path is unchanged. |
| `Consultation:SlaSweepEnabled` | `true` | Enables the background SLA worker. |
| `Consultation:SlaSweepSeconds` | `300` | Sweep period, clamped to 60–3600. |

Notification dedupe keys (unique partial index on `DedupeKey`): `sla-breach:{id}:{yyyyMMdd}`,
`sla-escalate-admin:{id}`, `auto-expire:{id}`, and the role fan-out suffix `{key}:{userId}`.

### 16.3 Verification matrix

| Check | Where it runs | Status |
| --- | --- | --- |
| `pytest -q` (agents 1–4, contract, agent 5) | this sandbox | run and green |
| `npm run lint && npm test && npm run build` (web) | this sandbox | run and green |
| `dotnet build` / `dotnet test` / `dotnet ef database update` | developer machine | **not runnable here** (no .NET SDK / NuGet access) — the C# is hand-verified only |
| `flutter analyze` / `flutter test` | developer machine or CI | **not runnable here** (no Flutter SDK / pub.dev) — CI job `test-flutter-mobile` is the gate |

### 16.4 Mobile parity (Phase 4)

`mobile/PathwayNavigator/lib/features/support/` mirrors the web behaviour with the same server
contract: `support_screen.dart` (my questions, thread, ask sheet), `consultant_desk_screen.dart`
(queue + case + composer), `notifications_screen.dart` (DB inbox + unread badge),
`widgets/consultant_note.dart` (guidance written back onto a Reality Check or milestone) and
`widgets/ask_consultant_sheet.dart` (anchored question with prefill). Roles: `Roles.consultant` is a
known role, deliberately **not** `isStaff`, and the router keeps the desk to Consultant/Admin while
never letting a consultant into the approval centre.

---

*Implementation is complete across all five phases in this branch; the remaining gates are the
machine-specific ones in §16.3 (`dotnet build`/`dotnet test`/`dotnet ef database update`, then
`flutter analyze`/`flutter test`) which this environment cannot run.*
