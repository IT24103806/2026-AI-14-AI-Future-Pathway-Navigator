# PathwayNavigator · Web Client

React + Vite single-page app for the AI Future Pathway Navigator. It talks only to
the ASP.NET Core API (`/api`); the Python AI service is called by that API, never by
the browser.

The UI is built on a single token-driven design system — **Aurora Glass**. See
[`THEME.md`](./THEME.md) for the full style guide.

---

## Scripts

```bash
npm install
npm run dev      # Vite dev server (host 0.0.0.0, /api proxied to the backend)
npm run lint     # oxlint
npm test         # vitest (component + page tests)
npm run build    # production build
npm run preview  # preview the production build
```

## Environment

Copy `.env.example` to `.env.local`:

| Variable | Purpose |
| --- | --- |
| `VITE_API_BASE_URL` | API base URL. Use `/api` together with the dev proxy to avoid CORS. |
| `VITE_PROXY_TARGET` | Where Vite forwards `/api` (default `http://localhost:5081`). |
| `VITE_API_TIMEOUT_MS` | Axios timeout (agent calls can be slow). |
| `VITE_GOOGLE_CLIENT_ID` | Optional; the Google button is hidden when empty. |

---

## Routes

| Path | Access | Page |
| --- | --- | --- |
| `/` | public | Marketing home (hero, agent workflow, FAQ, CTA) |
| `/about` | public | About — mission, principles, four-agent architecture |
| `/contact` | public | Contact — info cards + validated enquiry form |
| `/login`, `/register`, `/forgot-password`, `/reset-password` | public | Auth flows (split glass layout) |
| `/onboarding` | Student | Agent 1 conversational onboarding + standard form |
| `/career-discovery` | Student | Agent 2 ranked pathways, Agent 3 roadmap tracker, Agent 4 form |
| `/student/reality-check` | Student | Agent 4 evidence, counsellor decision, revise & resubmit |
| `/dashboard` | any (role-routed) | Redirects to the dashboard for the signed-in role |
| `/student/dashboard` | Student | Student dashboard (progress, next actions, profile) |
| `/admin/dashboard` | Admin | Administration & governance overview |
| `/counsellor/dashboard` | Counsellor / Admin | Approval centre (queue, evidence, decision) |
| `*` | public | 404 |

---

## Project structure

```
src/
  api/          axios client + endpoint wrappers (auth, profile, pathway, onboarding, review)
  components/
    common/     Navbar, Footer, ThemeToggle, AuthLayout, Reveal, AgentWorkflow,
                JourneySteps, ScrollProgress, BrandMark, form primitives, route guards
    onboarding/ Agent 1 chat window, slot sidebar, standard form
    pathway/    ScoreRing, MarketMeter, TrendBadge, MissingSkillsPanel,
                RoadmapTimeline, PathwayRecommendationCard
  config/       constants + shared agent/journey content
  context/      AuthContext, ThemeContext (+ definition modules)
  hooks/        useAuth, useTheme
  pages/        one file per route
  styles/       theme layers (see THEME.md)
  utils/        apiError, roleRoutes, tokenUtils
```

---

## Conventions

- **Styling**: never hard-code colours/spacing in components; use the tokens in
  `src/styles/tokens.css`. Page-specific CSS goes in `src/styles/pages/` and is
  imported by `src/index.css`.
- **Theme**: dark by default, light available through `useTheme()`; the choice is
  persisted and applied before first paint.
- **Accessibility**: keep `aria-*` attributes, `aria-label`s on icon buttons and
  the `:focus-visible` ring; respect `prefers-reduced-motion`.
- **Testing**: page tests mock the API modules (`vi.mock`) and render with
  `MemoryRouter`; keep user-visible strings that tests assert on intact when
  restyling.
