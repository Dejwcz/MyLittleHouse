# Architektura

## Přehled

Aplikace používá **local-first** architekturu:
- Data jsou primárně uložena lokálně v prohlížeči (IndexedDB)
- Server drží účet a metadata pro přihlášení
- Upload na server je opt-in (zálohy, sdílení)

## Local-first princip

### Výchozí stav
- Uživatel se přihlásí (účet na serveru)
- Data se ukládají pouze lokálně v IndexedDB
- Aplikace funguje offline

### Opt-in sync
- Uživatel může zapnout zálohy na server
- Při sdílení se sync zapne automaticky
- Před uploadem je uživatel informován, že data budou na serveru

### Důsledky
- Při ztrátě zařízení bez zálohy = ztráta dat
- Sdílení vyžaduje zapnutý sync pro daný scope

---

## Hybrid Sync Architecture

### Per-Scope Sync Mode

Každý scope (Project / Property / Zaznam) může být v jednom ze dvou režimů:

| Režim | Popis | Use case |
|-------|-------|----------|
| `local-only` | Data pouze v IndexedDB | Soukromé poznámky, offline práce |
| `synced` | IndexedDB + server sync | Sdílení, backup, multi-device |

### Data Flow

```
┌─────────────────────────────────────────────────────────┐
│                    Svelte Pages                         │
├─────────────────────────────────────────────────────────┤
│                   Unified API Layer                     │
│            (vždy ukládá do IndexedDB FIRST)            │
├─────────────────────────────────────────────────────────┤
│      IndexedDB (Dexie)      │      Sync Queue          │
│   (primární úložiště)       │   (pending changes)      │
├─────────────────────────────────────────────────────────┤
│                    Sync Manager                         │
│   - sleduje online/offline stav                        │
│   - spouští sync pro "synced" scope                    │
│   - řeší konflikty                                     │
├─────────────────────────────────────────────────────────┤
│                    Backend API                          │
│   (jen pro synced projekty)                            │
└─────────────────────────────────────────────────────────┘
```

1. **Všechny operace** jdou nejdřív do IndexedDB (offline-first)
2. Pro `synced` scope se změny přidají do SyncQueue
3. SyncManager periodicky/automaticky synchronizuje s backendem
4. Při konfliktu uživatel rozhodne kterou verzi ponechat

### Conflict Resolution

Při konfliktu (změna lokálně i na serveru):
1. Zobrazí se ConflictDialog s oběma verzemi
2. Uživatel vybere: lokální / serverová
3. Vybraná verze se aplikuje, druhá se zahodí

### Sync Status

Každá entita má `syncStatus`:
- `local` - pouze lokálně, nesynced
- `pending` - čeká na sync
- `syncing` - právě se synchronizuje
- `synced` - synchronizováno
- `failed` - sync selhal

### Scope Sync Mode

Project / Property / Zaznam mají `syncMode`:
- `local-only` - data se nikdy nesynchronizují
- `synced` - data se synchronizují s backendem

Pravidla scope:
- Child scope může být `synced` i když parent je `local-only`
- Při syncu se posílají pouze data z vybraného scope
- Pro Property / Zaznam se na server posílají jen minimální metadata rodičů

### Sync Mode Toggle

V nastavení scope (Project/Property/Zaznam) uživatel může:
1. **Zapnout sync** - data scope se nahrají na server a lze je sdílet
2. **Vypnout sync** - dialog nabídne:
   - Ponechat kopii na serveru (archiv)
   - Smazat data ze serveru

Viz rozhodnutí [09-decisions.md](09-decisions.md) #065

---

## Technický stack

### Frontend
- **Framework:** Svelte 5 + SvelteKit
- **Jazyk:** TypeScript
- **IndexedDB:** Dexie.js
- **Styling:** Tailwind CSS + vlastní komponenty
- **PWA:** Service Worker, Web App Manifest

### Backend
- **Framework:** .NET 10 (nové repo, clean start)
- **Architektura:** DDD + Clean Architecture
- **Databáze:** PostgreSQL (Hetzner VPS)
- **ORM:** Entity Framework Core
- **Auth:** ASP.NET Identity
- **Email:** SendGrid
- **Patterns:** CQRS, Repository, Result

Detaily: [11-tech-standards.md](11-tech-standards.md)

### Storage
- **Soubory:** Hetzner Object Storage (S3 compatible)
- **Thumbnaily:** imgproxy (on-the-fly processing)
- **CDN:** cdn.mujdomecek.cz (S3 direct, bez CF proxy)
- **Images:** img.mujdomecek.cz (imgproxy, přes CF)

Detaily: [09-decisions.md](09-decisions.md) #017

### Deployment
- **Platforma:** Docker Compose na Hetzner VPS
- **Reverse proxy:** Traefik v3 (auto SSL)
- **Databáze:** PostgreSQL 16 (container)

Detaily: [09-decisions.md](09-decisions.md) #022

### CI/CD Pipeline

**Platforma:** GitHub Actions

#### Triggers

| Event | Pipeline |
|-------|----------|
| Push to `main` | Build → Test → Deploy staging |
| Push to `production` | Build → Test → Deploy production |
| Pull Request | Build → Test → Security scan |

#### Jobs

```yaml
# .github/workflows/ci.yml
name: CI/CD

on:
  push:
    branches: [main, production]
  pull_request:
    branches: [main]

jobs:
  build-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal

  build-frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
      - run: npm ci
      - run: npm run lint
      - run: npm run check  # Svelte check
      - run: npm run build
      - run: npm test

  security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: dotnet list package --vulnerable --include-transitive
      - run: npm audit --audit-level=high

  deploy-staging:
    needs: [build-backend, build-frontend, security]
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    environment: staging
    steps:
      - name: Deploy to staging
        run: |
          # SSH deploy nebo Docker push
          echo "Deploying to staging..."

  deploy-production:
    needs: [build-backend, build-frontend, security]
    if: github.ref == 'refs/heads/production'
    runs-on: ubuntu-latest
    environment: production
    steps:
      - name: Deploy to production
        run: |
          # SSH deploy nebo Docker push
          echo "Deploying to production..."
```

#### Quality Gates

| Check | Povinné | Blokuje merge |
|-------|---------|---------------|
| Build passes | ✅ | ✅ |
| All tests pass | ✅ | ✅ |
| Lint (ESLint, dotnet format) | ✅ | ✅ |
| Type check (Svelte, TypeScript) | ✅ | ✅ |
| No vulnerable dependencies | ✅ | ⚠️ (high/critical) |
| Code coverage > 70% | ⚠️ | ❌ (jen warning) |

#### Environments

| Environment | Branch | URL | Auto-deploy |
|-------------|--------|-----|-------------|
| Development | feature/* | localhost | N/A |
| Staging | main | staging.mujdomecek.cz | ✅ |
| Production | production | mujdomecek.cz | ✅ (po merge) |

---

## IndexedDB schéma

Dexie.js schema (v3 - s hybrid sync):

```typescript
db.version(3).stores({
  projects: 'id, name, ownerId, updatedAt, syncStatus, syncMode',
  properties: 'id, projectId, name, updatedAt, syncStatus, syncMode',
  units: 'id, propertyId, parentUnitId, updatedAt, syncStatus',
  zaznamy: 'id, propertyId, unitId, date, updatedAt, status, syncStatus, syncMode',
  dokumenty: 'id, zaznamId, updatedAt, syncStatus',
  tags: 'id, name',
  zaznamTags: '[zaznamId+tagId], zaznamId, tagId',
  syncQueue: 'id, scopeType, scopeId, projectId, entityType, entityId, action, status, createdAt, attempts'
});
```

### Project entita (rozšířená)

```typescript
interface Project {
  id: string;
  name: string;
  description?: string;
  ownerId: string;
  memberCount: number;
  propertyCount: number;
  myRole: 'owner' | 'editor' | 'viewer';
  updatedAt: number;
  syncStatus: SyncStatus;
  // Hybrid sync fields:
  syncMode: 'local-only' | 'synced';  // default: 'local-only'
  lastSyncAt?: number;                 // timestamp posledního úspěšného sync
  remoteId?: string;                   // ID na serveru (může být jiné než local ID)
}
```

Property a Zaznam mají stejné `syncStatus` a `syncMode` jako Project.

### SyncQueue struktura

```typescript
interface SyncQueueItem {
  id: string;
  scopeType: 'project' | 'property' | 'zaznam';
  scopeId: string;
  projectId?: string;           // pomocné filtrování v UI
  entityType: 'projects' | 'properties' | 'units' | 'zaznamy' | 'dokumenty';
  entityId: string;
  action: "create" | "update" | "delete";
  payload: object | null;       // null pro delete
  status: "pending" | "syncing" | "failed";  // po úspěchu smazat
  attempts: number;
  lastError?: string;
  nextRetryAt?: number;         // timestamp (ms)
  createdAt: number;
  lastAttemptAt?: number;
}
```

- Max attempts: 5
- Backoff: 1s → 5s → 30s → 2min → 10min
- Po úspěšném sync: smazat z fronty

---

## Sync protokol

### Základní flow

1. Klient vytvoří/upraví záznam lokálně
2. Záznam se přidá do sync fronty jen pokud je jeho scope `synced`
3. Při online stavu se fronta odešle na server
4. Server zpracuje a vrátí potvrzení
5. Klient označí položky jako synchronizované

### Push & pull (MVP)

- **Push (klient → server):** okamžitě po změně, pokud je online; batch max 50 položek; vždy v rámci scope.
- **Pull (server → klient):** při startu app, po úspěšném push a každých 5 minut.
- **Delta sync:** `GET /api/sync/pull?since={timestamp}&scopeType={...}&scopeId={...}`.

### Retry

- Exponential backoff: 1s, 5s, 30s, 1min, 5min
- Max 5 pokusů, pak stav `failed` + viditelné v UI

### Conflict resolution

**Strategie:** Last-write-wins s server revision

Čistý klientský `updatedAt` je náchylný na rozjeté hodiny mezi zařízeními.

**Řešení:**

| Pole | Kde | Účel |
|------|-----|------|
| `updatedAt` | Klient | Kdy uživatel změnil záznam |
| `serverRevision` | Server | Pořadí přijetí serverem (auto-increment nebo timestamp) |

**Sync flow:**
1. Klient pošle změnu s `updatedAt`
2. Server přiřadí `serverRevision` při přijetí
3. Při konfliktu: primárně `serverRevision`, `updatedAt` jako tie-break
4. Audit log uchovává obojí

**Příklad konfliktu:**
```
Klient A: updatedAt=10:05, serverRevision=42
Klient B: updatedAt=10:03, serverRevision=43  ← vyhrává (vyšší serverRevision)
```

**Důvody:**
- Server revision je autoritativní (žádné problémy s hodinami)
- `updatedAt` zachován pro UI (zobrazení "naposledy upraveno")
- Audit log pro debugging a historii

Viz rozhodnutí [09-decisions.md](09-decisions.md) #038

### Soubory (fotky, dokumenty)

- Metadata se synchronizují přes `/api/sync/*`.
- Upload/download souborů přes presigned URLs (bypass CloudFlare).
- Thumbnaily přes imgproxy (cachované přes CloudFlare).

**DNS setup:**

| Subdoména | CloudFlare | Účel |
|-----------|------------|------|
| `img.mujdomecek.cz` | Proxy ON | imgproxy, thumbnaily (cachované) |
| `cdn.mujdomecek.cz` | DNS only | Presigned URLs (upload/download) |

**Upload flow:**
1. `POST /api/upload/request` → server vrátí presigned PUT URL
2. Klient PUT přímo na S3 (`cdn.mujdomecek.cz`)
3. `POST /api/upload/confirm`

**Download flow:**
- Thumbnaily: `img.mujdomecek.cz/...` (veřejné, cachované)
- Originály: presigned GET URL na `cdn.mujdomecek.cz`

**Expirace presigned URLs:**
- Upload: 15 minut
- Download: 1 hodina

**Validace souborů:**
- Extension + magic bytes (backend)
- Povolené typy: JPEG, PNG, WebP, HEIC, PDF

---

## API

### Autentizace

**Rozhodnutí:** ASP.NET Identity + JWT + externí OIDC providery

**Metody přihlášení:**
- Email/heslo (ASP.NET Identity)
- Google (OIDC)
- Apple (OIDC)

**Tokeny:**
- Access token: JWT, 15 min expirace, v paměti
- Refresh token: HttpOnly cookie, 7 dní, rotace při použití

**Flow:**
1. Login (jakákoliv metoda) → API vrátí JWT + refresh cookie
2. Request: `Authorization: Bearer <jwt>`
3. JWT expiruje → `POST /auth/refresh` → nový JWT
4. Refresh expiruje → re-login

**Offline chování:**
- Login/registrace vyžaduje online
- Offline read/write funguje bez serveru
- Sync se spustí po obnovení spojení a validní session
- Při expirovaném tokenu se sync pozastaví a UI vyžádá re-login

### Endpoints

```
POST   /api/auth/login
POST   /api/auth/register
POST   /api/auth/refresh
POST   /api/auth/google         # OIDC callback
POST   /api/auth/apple          # OIDC callback

GET    /api/sync/status
POST   /api/sync/push
GET    /api/sync/pull?since={timestamp}

POST   /api/upload/request      # Získání presigned URL
POST   /api/upload/confirm      # Potvrzení uploadu
GET    /api/files/{id}/url      # Presigned URL pro download
DELETE /api/upload/{id}
```

### Rate limiting

Konfigurovatelné v appsettings.json, sliding window:

| Endpoint | Limit | Window |
|----------|-------|--------|
| /auth/login | 5 | 15 min |
| /auth/register | 3 | 1 hour |
| /sync/push | 100 | 1 min |
| /sync/pull | 30 | 1 min |
| /upload | 20 | 1 min |

### Sync envelope

```typescript
interface SyncEnvelope {
  entityType: string;
  entityId: string;
  action: "create" | "update" | "delete";
  payload: object | null;     // null pro delete
  clientUpdatedAt: string;
  clientId: string;
  correlationId: string;      // Povinné
}
```

---

## Diagram

```
┌─────────────────────────────────────────────────────┐
│                    Klient (PWA)                     │
├─────────────────────────────────────────────────────┤
│  UI Components                                      │
│       ↓                                             │
│  Service Layer                                      │
│       ↓                                             │
│  IndexedDB (Dexie.js?)                             │
│       ↓                                             │
│  Sync Queue ←→ Sync Service ←→ Server API          │
└─────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────┐
│                    Server                           │
├─────────────────────────────────────────────────────┤
│  ASP.NET Core API                                   │
│       ↓                                             │
│  Service Layer                                      │
│       ↓                                             │
│  PostgreSQL                                         │
└─────────────────────────────────────────────────────┘
```

---

## Monitoring & Observability

### Logging

**Stack:** Serilog → Seq (self-hosted) nebo Loki

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MujDomecek")
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341"));  // nebo Loki
```

**Structured logging:**
```csharp
_logger.LogInformation(
    "User {UserId} created zaznam {ZaznamId} for property {PropertyId}",
    userId, zaznamId, propertyId);
```

**Log levels:**
| Level | Použití |
|-------|---------|
| Error | Neočekávané chyby, výjimky |
| Warning | Očekávatelné problémy (rate limit, auth fail) |
| Information | Business události (CRUD, sync) |
| Debug | Diagnostika (pouze dev/staging) |

### Metriky

**Stack:** Prometheus + Grafana

**Sbírané metriky:**

| Metrika | Typ | Popis |
|---------|-----|-------|
| `http_requests_total` | Counter | Celkový počet requestů |
| `http_request_duration_seconds` | Histogram | Latence requestů |
| `sync_push_total` | Counter | Počet sync pushů |
| `sync_conflicts_total` | Counter | Sync konflikty |
| `active_users` | Gauge | Aktivní uživatelé (15min window) |
| `background_jobs_duration` | Histogram | Délka background jobů |
| `storage_bytes_total` | Gauge | Využité úložiště |

**Endpoint:** `GET /metrics` (Prometheus format)

### Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database")
    .AddUrlGroup(new Uri("http://minio:9000/minio/health/live"), name: "storage")
    .AddHangfire(options => { }, name: "hangfire");
```

**Endpoints:**
- `GET /health` - Základní liveness
- `GET /health/ready` - Readiness (včetně dependencies)

### Alerting

**Pravidla (Grafana/Alertmanager):**

| Alert | Podmínka | Severity |
|-------|----------|----------|
| HighErrorRate | error_rate > 5% (5min) | Critical |
| SlowResponses | p95 latency > 2s (5min) | Warning |
| DatabaseDown | health check fails | Critical |
| StorageDown | health check fails | Critical |
| DiskSpaceLow | disk usage > 85% | Warning |
| BackgroundJobFailed | job failed 3x | Warning |

**Notifikace:**
- Critical → Email + (later) SMS/PagerDuty
- Warning → Email

### Dashboards (Grafana)

**Hlavní dashboard:**
- Request rate & latency (p50, p95, p99)
- Error rate
- Active users
- Sync activity
- Background job status
- System resources (CPU, RAM, disk)

**Business dashboard:**
- Nové registrace / den
- Aktivní uživatelé / týden
- Počet záznamů / den
- Storage growth

### Frontend Monitoring (Later)

- Error tracking: Sentry
- Analytics: Plausible (privacy-friendly)
- Performance: Web Vitals

### Konfigurace

```json
// appsettings.json
{
  "Monitoring": {
    "Seq": {
      "Url": "http://seq:5341",
      "ApiKey": "..."
    },
    "Prometheus": {
      "Enabled": true
    },
    "HealthChecks": {
      "Enabled": true,
      "Path": "/health"
    }
  }
}
```
