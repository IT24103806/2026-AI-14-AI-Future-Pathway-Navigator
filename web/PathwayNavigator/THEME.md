# PathwayNavigator — "Aurora Glass" Theme Guide

A single, token-driven design system for the whole web app. Every page, component
and animation in `web/PathwayNavigator` is styled through this system, so a change
here propagates everywhere instead of being patched page by page.

---

## 1. Design language

| Pillar | What it means in this app |
| --- | --- |
| **Liquid glass** | Translucent panes (`--glass-1…4`) over a drifting aurora mesh, with `backdrop-filter: blur() saturate()`, a hairline gradient border and an inset specular highlight. |
| **Aurora canvas** | Two fixed background layers on `body`: a slow-moving radial mesh gradient and a fine grain/vignette film. Both are pure CSS (no images to ship). |
| **Depth through light** | Elevation is expressed with soft, coloured shadows + inner highlights rather than hard borders. |
| **Motion with intent** | Reveal-on-scroll, hover lift, sheen sweeps, agent pipeline pulses, typing indicators. All motion is disabled under `prefers-reduced-motion`. |
| **Fluid & responsive** | `clamp()` typography, container-width caps, auto-fit grids, and a glass drawer for navigation under 1080px. |

### Palette

Cyan → Blue → Violet → Pink (`--gradient-brand`) is the identity gradient. Cyan is
used for "intelligence/agent" signals, mint for success/completion, amber for
caution/market competition, rose for risk/rejection, violet for human/counsellor
touchpoints.

---

## 2. File map

```
src/styles/
  tokens.css        design tokens + dark & light themes (the single source of truth)
  base.css          reset, aurora canvas, typography, utilities, keyframes, reveal
  components.css    glass surfaces, buttons, forms, badges, meters, rings, loaders,
                    journey rail, alerts, pagination, decision panels
  layout.css        app shell, glass header + mobile drawer, footer, scroll affordances
  pages/
    system.css      404, access denied, boot splash
    home.css        landing page, about, contact
    auth.css        login / register / forgot-password split layout
    dashboard.css   student & admin dashboards
    onboarding.css  Agent 1 chat, slot sidebar, standard form
    career.css      career discovery, pathway cards, roadmap tracker, deep dive
    reality.css     Agent 4 reality check (student view)
    review.css      counsellor approval centre
src/index.css       imports the layers above, in order (entry stylesheet)
```

`src/index.css` is the only stylesheet imported by `main.jsx`. If you add a new
page, either extend the closest file in `pages/` or add a new one and import it in
`index.css` **after** `components.css`.

---

## 3. Tokens

Everything is a CSS custom property defined in `tokens.css`. Never hard-code a
colour, radius, shadow or timing in a component stylesheet.

```css
/* Surfaces */            --glass-1 … --glass-4, --glass-border, --glass-blur
/* Text */                --text-main, --text-muted, --text-dim
/* Brand */               --primary, --secondary, --accent-{cyan,violet,pink,mint,amber}
/* Gradients */           --gradient-brand, --gradient-cool, --gradient-mint, --gradient-sheen
/* State */               --success, --warning, --danger, --info (+ -soft and -border pairs)
/* Shape */               --radius-xs … --radius-2xl, --radius-pill
/* Elevation */           --shadow-xs … --shadow-lg, --shadow-glow, --ring
/* Motion */              --ease-out, --ease-spring, --dur-fast … --dur-slower
/* Layout */              --container-max, --container-wide, --gutter, --nav-height, --section-y
```

### Light & dark mode

Dark is the default. `:root[data-theme='light']` re-maps the same token names to a
"frosted daylight" palette, so components never branch on theme.

- `ThemeProvider` (`src/context/ThemeContext.jsx`) owns the state, persists the
  choice in `localStorage['pathwaynavigator-theme']` and writes `data-theme` on
  `<html>`.
- `index.html` applies the stored theme **before first paint** to avoid a flash.
- `useTheme()` exposes `{ theme, isDark, setTheme, toggleTheme }`.
- `ThemeToggle` in the header is the user-facing switch.

---

## 4. Component primitives

Use these classes instead of inventing new surfaces:

| Purpose | Classes |
| --- | --- |
| Glass pane | `.glass`, `.glass-card`, `.glass-panel`, `.glass--sheen`, `.glass--top-line` |
| Card | `.dash-card`, `.feature-card`, `.action-tile`, `.stat-tile`, `.principle-card`, `.audience-card`, `.bento-card` |
| Buttons | `.btn` + `.btn-primary` / `.btn-secondary` / `.btn-outline-primary` / `.btn-outline-danger` / `.btn-glass` / `.btn-ghost`, sizes `.btn-sm` / `.btn-lg` / `.btn-icon` |
| Forms | `.form-group`, `.form-label`, `.form-input`, `.form-select`, `.form-error`, `.input-wrapper` |
| Badges & chips | `.hero-pill-badge`, `.data-source-badge`, `.slot-pill`, `.chip-row`, `.role-tag`, `.trend-badge`, `.journey-step` |
| Data viz | `.progress-bar-container/.progress-bar-fill`, `.market-meter-*`, `.score-ring-*`, `.stat-ring` |
| Feedback | `.alert-banner`, `.review-alert`, `.spinner`, `.large-spinner`, `.full-page-loader`, `.skeleton` |
| Motion helpers | `.reveal` / `.reveal--{up,left,right,scale}`, `.lift`, `.orb-field` + `.orb--{cyan,violet,pink,mint}` |

React helpers:

- `<Reveal variant delay>` — IntersectionObserver scroll reveal (falls back to
  visible when unsupported, e.g. in jsdom).
- `<AgentWorkflow />` — the animated four-agent pipeline, driven by
  `src/config/agentJourney.js`.
- `<JourneySteps current={1|2|3} />` — the student journey rail.
- `<ScrollProgress />` — top reading bar + back-to-top button.

---

## 5. Responsive rules

| Breakpoint | Behaviour |
| --- | --- |
| `≥ 1081px` | Full desktop nav, 2-column hero/contact/auth layouts, 3–4 column grids |
| `≤ 1080px` | Nav collapses into the drawer; hero/auth/contact stack to one column |
| `≤ 900px` | User e-mail hidden, avatar shown, drawer links become full-width |
| `≤ 760px` | Bento cards go full width, workflow track becomes 2 columns, dashboards stack |
| `≤ 620px` | Compact header/theme switch, single-column footers, mobile OTP sizing |
| `≤ 430px` | Header controls shrink; brand text hides below 360px |

Grids use `repeat(auto-fit, minmax(…, 1fr))` wherever possible so new breakpoints
rarely need to be added by hand.

---

## 6. Adding a new page

1. Create the page in `src/pages/`, reusing `.section`, `.section-head`,
   `.dash-card`, `.btn` and the form primitives.
2. Add page-specific rules to `src/styles/pages/<name>.css` and import that file
   in `src/index.css` (after `components.css`).
3. Wrap marketing-style blocks in `<Reveal>` for entrance motion.
4. Verify: `npm run lint && npm test && npm run build`.

## 7. Accessibility & motion

- Focus rings come from `:focus-visible` using `--ring`; never remove them.
- All interactive icons have `aria-label`s; decorative layers are `aria-hidden`.
- `prefers-reduced-motion: reduce` neutralises animations and reveals.
- Colour pairings keep ≥ 4.5:1 contrast for body text in both themes; verify with
  a contrast checker before introducing a new text/background pair.
