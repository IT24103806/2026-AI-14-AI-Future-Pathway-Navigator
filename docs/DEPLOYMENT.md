# Production Deployment Guide

Deploying **AI Future Pathway Navigator** to an AlmaLinux 10.2 VPS with a
GitHub-driven CI/CD pipeline, plus Cloudflare Pages for the web app.

---

## 0. The design, and why it looks like this

Your VPS reports **949 MiB of total RAM**. That single number decides the whole
architecture: compiling .NET or bundling Vite on that box will get the build
OOM-killed. So **nothing is ever built on the VPS** — GitHub Actions builds
everything and the VPS only pulls finished images.

```
                       push to main
                            │
        ┌───────────────────┴────────────────────┐
        │                                        │
        ▼                                        ▼
  Cloudflare Pages                        GitHub Actions
  (builds React itself)                   1. test
        │                                 2. build 2 images → GHCR
        │                                 3. dotnet ef database update
        │                                 4. ssh → pull + up -d
        ▼                                        │
  https://yourdomain.com                         ▼
  (React SPA, free, CDN)              ┌──────────────────────┐
        │                             │  AlmaLinux 10 VPS    │
        │  HTTPS + JWT                │                      │
        └────────────────────────────►│  Caddy  :80/:443     │
                                      │    └─► api  :8080    │
                                      │          └─► ai :8000│
                                      └──────────┬───────────┘
                                                 │ TLS
                                                 ▼
                                     Managed PostgreSQL (Neon)
```

| Piece | Where | Why there |
|---|---|---|
| React SPA (`web/`) | **Cloudflare Pages** | Free, global CDN, and it keeps the Node build off your 949 MiB box entirely. |
| .NET API (`backend/`) | **VPS**, container | Needs to reach Postgres and the AI service; long agent calls. |
| Python AI (`ai_service/`) | **VPS**, container | Internal only — the .NET API calls it at `http://ai:8000`. Never exposed. |
| PostgreSQL | **Neon / Supabase free tier** | `DatabaseConfiguration.cs` already implements pool-recycling and transient retries *specifically for these providers*. Self-hosting Postgres on 949 MiB alongside two app containers is the least reliable option available. |
| Flutter app (`mobile/`) | **Neither** | It is a compiled artifact you distribute (Play Store / APK), not something you host. |

Total VPS footprint: Caddy ~30 MB + API ~150 MB + AI ~250 MB + Docker ~120 MB.
It fits inside 949 MiB with the 2 GiB of swap you already have.

### Files added for this deployment

```
backend/Dockerfile                 # multi-stage .NET 8, non-root, port 8080
backend/.dockerignore
ai_service/Dockerfile              # Python 3.12 slim, non-root, port 8000
ai_service/.dockerignore
docker-compose.yml                 # the VPS stack (caddy + api + ai)
deploy/Caddyfile                   # TLS termination + reverse proxy
deploy/pn-api.env.example          # template for API secrets
deploy/pn-ai.env.example           # template for AI secrets
.github/workflows/deploy.yml       # test → build → migrate → deploy
web/PathwayNavigator/.node-version # pins Node 22 for Cloudflare Pages
```

No application code was changed. Every production fix below is achieved through
configuration.

---

## 1. Prerequisites

- A domain. Two DNS `A` records pointing at the VPS public IP are used here:
  - `api.yourdomain.com` → VPS (served by Caddy)
  - `yourdomain.com` → Cloudflare Pages (set up in Part A)
- A GitHub repository — yours is `MenuraDev/2026-AI-14-AI-Future-Pathway-Navigator`.
- A Cloudflare account (free).
- A Neon account (free) — <https://neon.tech>. Supabase or Aiven work identically.

---

## Part A — Web app on Cloudflare Pages

Cloudflare Pages builds straight from GitHub, so it deploys on every push
independently of the VPS pipeline.

1. **Cloudflare dashboard → Workers & Pages → Create → Pages → Connect to Git.**
   Authorise Cloudflare and select the repository.

2. **Build configuration** — this is the part people get wrong:

   | Setting | Value |
   |---|---|
   | Project name | `pathway-navigator` |
   | Production branch | `main` |
   | **Root directory** | `web/PathwayNavigator` |
   | Framework preset | Vite |
   | Build command | `npm run build` |
   | Build output directory | `dist` |

   The **Root directory** is mandatory. The app is not at the repository root,
   and without it Cloudflare finds no `package.json` and the build fails.

3. **Environment variables** (Settings → Environment variables, applied to
   Production *and* Preview):

   | Variable | Value |
   |---|---|
   | `NODE_VERSION` | `22` |
   | `VITE_API_BASE_URL` | `https://api.yourdomain.com/api` |
   | `VITE_GOOGLE_CLIENT_ID` | your OAuth client id *(optional)* |

   Two things to note:

   - **`NODE_VERSION` is not optional.** Vite `8.2.1` declares
     `"engines": {"node": "^20.19.0 || >=22.12.0"}`, and Cloudflare Pages'
     default Node is far older than that — the build dies with a syntax error.
     `web/PathwayNavigator/.node-version` is committed as a second guarantee.
   - **No trailing slash** on `VITE_API_BASE_URL`. `apiClient.js` sets it as the
     axios `baseURL` and every route in `constants.js` already begins with `/`,
     so `https://api.yourdomain.com/api` + `/auth/login` is correct.

4. **Save and Deploy.** Cloudflare serves `index.html` for unmatched routes, so
   `react-router` deep links work without extra config.

5. Point `yourdomain.com` at the Pages project (Pages → Custom domains), and
   note the generated `*.pages.dev` URL — you need both in the API's CORS list.

> `VITE_*` variables are baked into the JavaScript bundle **at build time**.
> Changing one requires a redeploy, not a restart.

---

## Part B — Prepare the VPS

SSH in as `opc`.

### B1. Open ports 80 and 443 — in **two** places

This is the single most common reason an Oracle Cloud deployment "doesn't work".
AlmaLinux/Oracle images block inbound web traffic at both the network and the OS.

**(a) OCI console:** Networking → Virtual Cloud Networks → your VCN →
**Security Lists** → Default Security List → **Add Ingress Rules**:

| Stateless | Source CIDR | IP Protocol | Destination Port |
|---|---|---|---|
| no | `0.0.0.0/0` | TCP | 80 |
| no | `0.0.0.0/0` | TCP | 443 |

**(b) On the host**, open them in firewalld as well:

```bash
sudo firewall-cmd --permanent --add-service=http
sudo firewall-cmd --permanent --add-service=https
sudo firewall-cmd --reload
sudo firewall-cmd --list-services          # should list http https
```

Confirm from your laptop, not from the VPS:

```bash
nc -zv <VPS_PUBLIC_IP> 443
```

### B2. Install Docker

AlmaLinux 10 is `platform:el10` with DNF5, so it uses Docker's **rhel** repo
path (not the `centos` one, which is for el9):

```bash
sudo dnf install -y dnf-plugins-core
sudo dnf config-manager --add-repo https://download.docker.com/linux/rhel/docker-ce.repo
sudo dnf install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo systemctl enable --now docker
docker --version && docker compose version
```

If `dnf` reports a transaction conflict with `podman`, remove it first
(`sudo dnf remove -y podman buildah runc`) and re-run the install.

Let your user drive Docker without `sudo`, then **log out and back in**:

```bash
sudo usermod -aG docker $USER
```

### B3. Create the deploy directory and its secrets

```bash
sudo mkdir -p /opt/pathway-navigator/deploy
sudo chown -R $USER:$USER /opt/pathway-navigator
cd /opt/pathway-navigator
```

The deploy pipeline copies `docker-compose.yml` and `deploy/Caddyfile` here
automatically. The two env files below are **never** overwritten by it — create
them once:

```bash
# Generate the JWT signing key
openssl rand -base64 64 | tr -d '\n'; echo
```

```bash
nano deploy/pn-api.env      # paste from deploy/pn-api.env.example
chmod 600 deploy/pn-api.env

nano deploy/pn-ai.env       # paste from deploy/pn-ai.env.example
chmod 600 deploy/pn-ai.env
```

Fill in, at minimum:

**`deploy/pn-api.env`**
- `ConnectionStrings__DefaultConnection` — from Part C
- `JwtSettings__Secret` — the base64 string you just generated. **Required:**
  `Program.cs` throws on startup without it.
- `Cors__AllowedOrigins__0` — `https://yourdomain.com`
- `Cors__AllowedOrigins__1` — `https://pathway-navigator.pages.dev`
- `Google__ClientId` — if using Google Sign-In

**`deploy/pn-ai.env`**
- `GOOGLE_API_KEY` (or `OPENAI_API_KEY`)
- `ADZUNA_APP_ID` / `ADZUNA_APP_KEY` — optional; Agent 3 degrades without them

> Nested config keys use `__`, and array items use `__0`, `__1`. There is **no
> `appsettings.json`** in this repository — only `appsettings.example.json` — so
> anything not present in this env file simply does not exist at runtime.

### B4. Let the VPS pull from GHCR

If the repository is private, the images are private too. Create a GitHub
**classic** personal access token scoped to `read:packages`, then:

```bash
echo 'ghp_xxxxxxxxxxxx' | docker login ghcr.io -u <your-github-username> --password-stdin
```

(If the repo is public you can instead make the two packages public in the GHCR
UI and skip this.)

---

## Part C — PostgreSQL

Neon free tier: create a project, then **Connection Details → Pooled
connection**. You get something like:

```
postgresql://user:pass@ep-xxxx-pooler.eu-central-1.aws.neon.tech/neondb?sslmode=require
```

Convert it to the Npgsql format the API expects:

```
Host=ep-xxxx-pooler.eu-central-1.aws.neon.tech;Port=5432;Database=neondb;Username=user;Password=pass;SslMode=Require;Trust Server Certificate=false
```

Put that in **both** places, and they must be the same database:

1. `deploy/pn-api.env` → `ConnectionStrings__DefaultConnection`
2. GitHub secret `PROD_DB_CONNECTION` (Part D) — used by the migration step

The connection drops and `EndOfStreamException`s that managed providers cause
are already handled: `DatabaseConfiguration.cs` recycles pooled connections via
idle-lifetime and keep-alives, and enables `EnableRetryOnFailure`.

---

## Part D — GitHub secrets and variables

Repository → **Settings → Secrets and variables → Actions**.

**Secrets** (New repository secret):

| Name | Value |
|---|---|
| `PROD_DB_CONNECTION` | the Npgsql connection string from Part C |
| `VPS_HOST` | VPS public IP or hostname |
| `VPS_USER` | `opc` |
| `VPS_SSH_KEY` | the **private** key, see below |

Generate a deploy key — do not reuse your personal one:

```bash
ssh-keygen -t ed25519 -f ./pn_deploy -N "" -C "github-actions-deploy"
cat ./pn_deploy.pub >> ~/.ssh/authorized_keys   # run ON the VPS
cat ./pn_deploy                                 # paste into VPS_SSH_KEY
rm ./pn_deploy ./pn_deploy.pub
```

**Variables** (Variables tab — these are not secret):

| Name | Value | Purpose |
|---|---|---|
| `API_HOST` | `api.yourdomain.com` | smoke test after deploy |
| `REGISTRY` | `ghcr.io/menuradev` | optional; this is the default |
| `RUN_MIGRATIONS_IN_CI` | `false` | optional; set only if your DB is not reachable from GitHub |

---

## Part E — First deployment

Edit one line before you push: in `deploy/Caddyfile`, replace
`api.yourdomain.com` (all three occurrences) and the `email` address. DNS must
already resolve to the VPS, or Caddy cannot obtain a certificate.

```bash
git add -A
git commit -m "Add production deployment pipeline"
git push origin main
```

Then watch **Actions → Build & Deploy**. The four stages run in order:

1. **Test** — backend xunit, web vitest + production build, `pytest` (44 tests).
   Flutter is deliberately excluded; it is covered by `ci.yml`.
2. **Image (pn-api)** and **Image (pn-ai-service)** — build and push to GHCR,
   tagged with the commit SHA and `latest`.
3. **Database migrations** — `dotnet ef database update --connection ...`.
4. **Deploy to VPS** — copies the compose file and Caddyfile, then
   `docker compose pull && docker compose up -d`, and finally curls
   `https://api.yourdomain.com/swagger/index.html` until it answers.

**Why the migration step exists:** `Program.cs:165` reads

```csharp
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Database:AutoMigrate", true))
```

so with `ASPNETCORE_ENVIRONMENT=Production` the API **never** migrates. Without
this job you get a healthy container serving a completely empty database.

**Why `--connection` is used:** `AppDbContextFactory` builds its configuration
from `appsettings.json` alone — there is no `.AddEnvironmentVariables()` — so a
`ConnectionStrings__DefaultConnection` env var would be silently ignored. The
`--connection` flag overrides the design-time factory's connection string.

---

## Part F — Day to day

```bash
git push origin main
```

That is the entire deployment procedure. Cloudflare Pages rebuilds the SPA and
the Actions workflow rebuilds, migrates and redeploys the API. Rollback:

```bash
ssh opc@<VPS> 'cd /opt/pathway-navigator && IMAGE_TAG=<old-sha> docker compose up -d'
```

Every image is tagged with its commit SHA and retained in GHCR.

---

## Part G — Verifying it works

```bash
# On the VPS
cd /opt/pathway-navigator
docker compose ps                       # api + ai should be healthy
docker compose logs -f api
docker compose logs -f ai
curl -I https://api.yourdomain.com/swagger/index.html
```

Then, from a browser:

1. `https://yourdomain.com` loads the marketing page.
2. Register an account → if the browser console shows a CORS error, the Pages
   origin is missing from `Cors__AllowedOrigins`.
3. Run Agent 1 onboarding → proves `api → ai` connectivity and your LLM key.
4. `docker compose exec api env | grep -c .` to confirm the env file was read.

**Roles:** the API seeds `Student`, `Counsellor` and `Admin` roles on first
boot, so a fresh database is usable immediately.

---

## Part H — Mobile app

Not hosted anywhere. Build it against the deployed API:

```bash
cd mobile/PathwayNavigator
flutter build apk --release \
  --dart-define=API_BASE_URL=https://api.yourdomain.com/api \
  --dart-define=PRIVACY_CONTACT=you@yourdomain.com
```

`AppConfig.validate()` throws at startup if a **release** build is given a
non-HTTPS `API_BASE_URL`, so the VPS must have a valid certificate before the
app will run.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `nc -zv <ip> 443` times out | OCI security list or firewalld | Part B1 — both layers must be open |
| Caddy stuck obtaining certificate | DNS not pointing at the VPS yet, or rate-limited | Fix DNS; use the staging `acme_ca` line in the Caddyfile |
| Browser: CORS blocked | Pages origin not in the API's allowlist | Add it to `Cors__AllowedOrigins__N`, then `docker compose up -d api` |
| Every request returns a 307 loop | `ASPNETCORE_FORWARDEDHEADERS_ENABLED` missing | It is set in `backend/Dockerfile`; do not remove it |
| API healthy but 404/500 on every endpoint | Database never migrated | Re-run the **Database migrations** job |
| `pn-api` container `unhealthy` | Healthcheck probe failing | It hits `/swagger/index.html`; if you gate Swagger behind `IsDevelopment`, repoint the probe |
| Cloudflare build: syntax error | Default Node too old for Vite 8 | `NODE_VERSION=22` |
| Cloudflare: no `package.json` | Root directory unset | Set it to `web/PathwayNavigator` |
| Agent calls return 504 | AI container OOM-killed | `docker compose logs ai`; raise `mem_limit` or lower `DotNet`/Python footprint |
| Deploy fails: `MISSING: .../pn-api.env` | Env files not created on the VPS | Part B3 |
| `docker compose pull` → `denied` | Not logged in to GHCR | Part B4 |

---

## Operational notes

- **Swagger is public in production.** `Program.cs` calls `UseSwagger()` and
  `UseSwaggerUI()` unconditionally. It is convenient and it is the container's
  healthcheck target, but if you would rather not expose it, gate both calls
  behind `IsDevelopment()`, add `app.MapHealthChecks("/health")`, and repoint
  the `HEALTHCHECK` in `backend/Dockerfile`.
- **`Google:ClientSecret` in `appsettings.example.json` is never read.** The
  only key the API consumes is `Google:ClientId` (`AuthService.cs:111`); the
  flow validates a browser-issued ID token. Don't waste time wiring a secret.
- **Memory headroom is thin.** `docker compose stats` after the first deploy. If
  the AI service is swapping hard, the cheapest win is moving Postgres off the
  box (already the design) and reducing `mem_limit` on `caddy`.
- **Backups.** Neon takes automatic snapshots; this deployment adds nothing.
  Export periodically if the data matters to you.
