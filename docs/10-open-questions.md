# Otevřené otázky

> **⚠️ ARCHIV** - Tento dokument je historický. Všechny otázky (Q1-Q39) byly vyřešeny.
> Rozhodnutí viz [09-decisions.md](09-decisions.md).
> Poslední aktualizace: 2024-12-26

Otázky k dořešení před implementací.

---

## Priorita: Vysoká

### Q1: Která volitelná pole v MVP?

**Kontext:** Rychlý zápis má volitelná pole řízená nastavením.

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Viz [09-decisions.md](09-decisions.md) #016

---

### Q2: PWA rozsah v MVP

**Kontext:** Jak moc offline funkcionalita v MVP?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Full local-first (offline read + write). Viz [09-decisions.md](09-decisions.md) #018

---

### Q3: Fotka - edge cases (soft requirement)

**Kontext:** Fotka je doporučená (soft requirement). Jak řešit edge cases?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Soft requirement - lze uložit bez fotky, auto-flag MissingPhoto. Viz [09-decisions.md](09-decisions.md) #019

---

### Q4: Frontend framework

**Kontext:** Současně Razor Views (server-side). Local-first vyžaduje client-side.

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Svelte 5 + SvelteKit + TypeScript. Viz [09-decisions.md](09-decisions.md) #020

---

### Q5: IndexedDB knihovna

**Kontext:** Jakou knihovnu pro IndexedDB?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Dexie.js 4.x. Viz [09-decisions.md](09-decisions.md) #021

---

### Q6: Sync protokol detaily

**Kontext:** Last-write-wins rozhodnuto, ale detaily chybí.

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Push immediate, pull periodic (5min), batch 50, retry 5x. Viz [09-decisions.md](09-decisions.md) #023

---

### Q7: Migrace strategie

**Kontext:** Jak přejít ze současné aplikace?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Clean break - žádná migrace. Školní projekt, žádní uživatelé. Viz [09-decisions.md](09-decisions.md) #024

---

## Priorita: Střední

### Q16: MediatR vs vlastní CQRS

**Kontext:** CQRS pattern rozhodnut. Jakou implementaci?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** MediatR. Viz [09-decisions.md](09-decisions.md) #034

---

### Q17: Mapster vs AutoMapper

**Kontext:** DTO mapping.

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Mapster. Viz [09-decisions.md](09-decisions.md) #035

---

### Q18: Monorepo vs separate repos

**Kontext:** API + Web frontend.

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Monorepo. Viz [09-decisions.md](09-decisions.md) #036

---

### Q19: Code coverage target

**Kontext:** Jaké minimum pro CI/CD?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** 70% celkově, CI blokuje pod 60%. Viz [09-decisions.md](09-decisions.md) #037

---

### Q8: Sdílení per Project vs per Property

**Kontext:** Plán zmiňuje obojí. Je to nutné?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Obojí - Project jako workspace (rodina/firma), Property jako granulární. Viz [09-decisions.md](09-decisions.md) #025

---

### Q9: UUID vs auto-increment ID

**Kontext:** Local-first = klient generuje ID před sync.

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** UUID - local-first requirement. Viz [09-decisions.md](09-decisions.md) #026

---

### Q10: Komprese fotek

**Kontext:** Fotky mohou být velké (10MB limit).

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Free = komprese (2000px, 80%), Premium = originál. Viz [09-decisions.md](09-decisions.md) #027

---

### Q11: Max počet fotek na záznam

**Kontext:** Není definováno.

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Free = 10, Premium = 50. Viz [09-decisions.md](09-decisions.md) #028

---

### Q12: Typ záznamu (enum)

**Kontext:** Jaké typy záznamů?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Bez enum typu, stačí tagy. Viz [09-decisions.md](09-decisions.md) #029

---

## Priorita: Nízká (Later)

### Q13: Push notifikace

**Kontext:** In-app notifikace v MVP. Push later?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** MVP = in-app only, Push = later. Viz [09-decisions.md](09-decisions.md) #031

---

### Q14: Export formáty

**Kontext:** Later scope zahrnuje export.

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** JSON (free), PDF+CSV (premium). Viz [09-decisions.md](09-decisions.md) #032

---

### Q15: Automatický sync vs ruční

**Kontext:** Při sdílení je sync automatický. Co bez sdílení?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Auto sync (pokud opt-in backup). Viz [09-decisions.md](09-decisions.md) #033

---

---

## Priorita: Vysoká (MVP blocker)

### Q20: MVP scope - které bounded contexts?

**Kontext:** Máme 5 bounded contexts. Které jsou MVP, které later?

**Možnosti:**

| Context | MVP? | Poznámka |
|---------|------|----------|
| PropertyManagement | ✅ | Core funkcionalita |
| Identity | ✅ | Auth nutný |
| Sharing | ✅ | Invite + členové + práva (MVP-min) |
| Notifications | ✅ | In-app inbox (MVP-min) |
| Contacts | ✅ | Kontakty + skupiny (MVP-min) |

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Všechny bounded contexts jsou v MVP, ale v minimální podobě. Viz [01-vision.md](01-vision.md) (sekce MVP).

---

### Q21: Legacy MujDomecek složka

**Kontext:** Současný projekt v /MujDomecek. Co s ním při vytváření nové struktury?

**Možnosti:**
1. Smazat (clean break, máme git historii)
2. Přesunout do /archive
3. Nechat vedle nové struktury (přechodně)

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Smazat. Git historie slouží jako archiv.

---

### Q22: DDD namespace/layer konvence

**Kontext:** Jak strukturovat vrstvy a pojmenovávat třídy?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:**

Domain vrstva:
```
MujDomecek.Domain/
├── Aggregates/
├── ValueObjects/
├── Events/
├── Exceptions/
└── Abstractions/
```

Application vrstva (feature-based):
```
MujDomecek.Application/
├── Features/
│   └── {Feature}/
│       ├── Commands/    # Command + Handler spolu v jednom souboru
│       ├── Queries/     # Query + Handler spolu
│       └── Validators/
├── DTOs/
└── Behaviors/
```

Naming: `CreateZaznamCommand`, `CreateZaznamHandler`, `ZaznamDto`

---

### Q23: Change envelope pro sync

**Kontext:** Jaký formát pro sync payloady mezi klientem a serverem?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:**
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

- Version pole: ne v MVP (last-write-wins), přidat později pokud bude potřeba
- Delete payload: null (server loguje před smazáním)

---

### Q24: SyncQueue struktura v IndexedDB

**Kontext:** Jaké stavy a pole má mít sync fronta?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:**
```typescript
interface SyncQueueItem {
  id: string;
  entityType: string;
  entityId: string;
  action: "create" | "update" | "delete";
  payload: object | null;

  status: "pending" | "syncing" | "failed";  // bez "synced"
  attempts: number;
  lastError?: string;
  nextRetryAt?: number;      // timestamp (ms)

  createdAt: number;         // timestamp (ms)
  lastAttemptAt?: number;
}
```

- Max attempts: 5
- Po úspěšném sync: smazat ihned (není potřeba stav "synced")
- Backoff: 1s → 5s → 30s → 2min → 10min

---

### Q25: Offline UX - vizuální indikátory

**Kontext:** Jak uživateli zobrazit stav synchronizace?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:**

**Badges na záznamech:**
- 🔵 Local-only (jen v zařízení, není zálohováno)
- ✅ Synced (zálohováno na serveru)
- 🔄 Syncing (probíhá synchronizace)
- ⚠️ Sync failed (chyba - kliknutelné pro detail)
- 👥 Shared (sdíleno s ostatními)

**Globální indikátor v headeru:**
- Offline: `📴 Offline` (šedá lišta)
- Online, vše ok: ✓ nebo nic
- Syncuje se: `🔄 (3)` s počtem položek
- Má chyby: `⚠️ (2)` kliknutelné → sync problems screen

**Sync problems screen:**
- Seznam failed položek s popisem chyby
- Tlačítko "Zkusit znovu" (jednotlivě + hromadně)
- Možnost "Zahodit změnu" pro nevyřešitelné případy

---

### Q26: Presigned URLs strategie

**Kontext:** Jak řešit upload/download souborů s S3?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Hybridní přístup (CloudFlare modifikuje hlavičky → presigned URLs nefungují přes proxy)

**DNS setup:**
| Subdoména | CloudFlare | Účel |
|-----------|------------|------|
| `img.mujdomecek.cz` | Proxy ON | imgproxy, thumbnaily (cachované) |
| `cdn.mujdomecek.cz` | DNS only (proxy OFF) | Presigned URLs pro upload/download |

**Upload flow:**
1. `POST /api/upload/request` → server vrátí presigned PUT URL na `cdn.mujdomecek.cz`
2. Klient PUT přímo na S3 (bypass CloudFlare)
3. `POST /api/upload/confirm`

**Download flow:**
- Thumbnaily: `img.mujdomecek.cz` (veřejné, cachované přes CloudFlare)
- Originály: presigned GET URL na `cdn.mujdomecek.cz` (bypass CloudFlare)

**Expirace:**
- Upload URL: 15 minut
- Download URL: 1 hodina

**Offline cache:**
- Thumbnaily: Service Worker HTTP cache
- Originály: explicitní "Uložit offline" → IndexedDB Blob

---

### Q27: Permission precedence

**Kontext:** Uživatel může mít roli na Project i Property úrovni. Co má přednost?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Explicitní PropertyMember přebíjí implicitní ProjectMember

**Příklad:**
```
Projekt "Rodina" - User je Editor
  └── Property "Chalupa" - User je explicitně Viewer → Viewer
  └── Property "Byt" - User nemá explicitní roli → Editor (dědí)
```

Umožňuje výjimky (např. citlivá Property jen pro čtení).

---

### Q28: Invitation expiration mechanismus

**Kontext:** Pozvánky expirují po 7 dnech. Jak to technicky řešit?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Kombinace lazy check + background job + notifikace

1. **Lazy check:** Při čtení pozvánky zkontrolovat `expiresAt`, označit jako expired
2. **Background job:** Periodicky (1x denně) projít pending pozvánky a označit expired
3. **Notifikace vlastníkovi:** Při expiraci informovat s možností "Poslat znovu"

**Job frekvence:** 1x denně (stačí, lazy check pokryje real-time)

---

### Q36: Target runtime verze (.NET + Node)

**Kontext:** Dokumentace zmiňuje .NET 10, ale aktuálně je v prostředí jen .NET 9 SDK. Potřebujeme sjednotit target framework a připnout toolchain (aby buildy byly deterministické).

**Status:** ✅ Rozhodnuto

**Rozhodnutí:**
- **.NET 10** (LTS, vyšel 11/2025, podpora do 2028)
- **Node 22 LTS**
- **Pinning:** global.json + .nvmrc

```json
// global.json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

```
// .nvmrc
22
```

**Reference:** Viz [12-dotnet10-index.md](12-dotnet10-index.md) pro příklady C# 14 / EF Core 10 / ASP.NET Core 10

---

### Q37: Autentizace pro PWA (cookies vs JWT)

**Kontext:** PWA + API potřebuje bezpečné přihlášení, obnovu session a sync.

**Status:** ✅ Rozhodnuto

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

**DB:** `AppUser` má `GoogleId`, `AppleId` pro propojení účtů

---

## Priorita: Střední

### Q29: Domain constraints - sdílené konstanty

**Kontext:** Délky stringů, limity, validační pravidla. Kde je definovat?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** hardcoded defaults + `appsettings.json` + runtime override v DB (Admin)

```json
{
  "Constraints": {
    "Zaznam": {
      "TitleMaxLength": 200,
      "DescriptionMaxLength": 5000
    },
    "Files": {
      "MaxPhotosPerZaznamFree": 10,
      "MaxPhotosPerZaznamPremium": 50,
      "MaxPhotoSizeBytes": 10485760,
      "MaxDocumentSizeBytes": 20971520,
      "PhotoCompression": {
        "FreeEnabled": true,
        "FreeMaxDimensionPx": 2000,
        "FreeJpegQuality": 80
      }
    }
  }
}
```

- Výchozí hodnoty v `appsettings.json` (verzované v gitu) + hardcoded defaults v kódu.
- Runtime změny přes Admin panel (`/admin/settings`) → uložené v DB (`AppSetting`) a načítané bez restartu.
- Fallback: pokud DB není dostupná, použijí se hodnoty z `appsettings.json` (a poté hardcoded defaults).

---

### Q30: Rate limiting specifika

**Kontext:** Které endpointy a jaké limity?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Sliding window + appsettings.json

```json
{
  "RateLimiting": {
    "Login": { "Limit": 5, "WindowMinutes": 15 },
    "Register": { "Limit": 3, "WindowMinutes": 60 },
    "ForgotPassword": { "Limit": 3, "WindowMinutes": 60 },
    "SyncPush": { "Limit": 100, "WindowMinutes": 1 },
    "SyncPull": { "Limit": 30, "WindowMinutes": 1 },
    "Upload": { "Limit": 20, "WindowMinutes": 1 }
  }
}
```

**Response při překročení:**
```
HTTP 429 Too Many Requests
Retry-After: <seconds>
X-RateLimit-Remaining: 0
```

---

### Q31: File type validace

**Kontext:** Jak validovat nahrávané soubory?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Extension + magic bytes

| Kontrola | Kde | Účel |
|----------|-----|------|
| Extension | Frontend + Backend | Rychlé, UX (filtr v inputu) |
| Magic bytes | Backend | Bezpečnost (skutečný obsah) |

**Povolené typy:** JPEG, PNG, WebP, HEIC, PDF

**Implementace:** MimeDetective knihovna nebo vlastní kontrola magic bytes

---

### Q32: Contacts - deduplikace a normalizace

**Kontext:** Uživatel přidá kontakt "jan@email.cz". Později přidá "Jan@Email.CZ".

**Status:** ✅ Rozhodnuto

**Rozhodnutí:**
- Email normalizace: Ano (lowercase při ukládání)
- Duplicity: Blokovat (vrátit existující kontakt, ne chyba)
- Audit: Ano (`CreatedBy`, `CreatedAt`)

**DB constraint:**
```sql
CREATE UNIQUE INDEX ix_contacts_email ON contacts (LOWER(email), project_id);
```

---

### Q33: Test struktura per layer

**Kontext:** Co testovat na které vrstvě?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:**

| Vrstva | Typ | Co | Mocking |
|--------|-----|-----|---------|
| Domain | Unit | Entity invariants, value objects | Žádný (pure) |
| Application | Unit | Handlers, validators | NSubstitute |
| Infrastructure | Integration | EF repos, S3 client | TestContainers |
| API | Integration | Endpoints, auth, permissions | TestContainers |
| Web | Unit | Stores, utils, komponenty | Vitest |
| Web | E2E | Kritické flows | Playwright |

**Struktura:**
```
tests/
├── MujDomecek.Domain.Tests/
├── MujDomecek.Application.Tests/
├── MujDomecek.Infrastructure.Tests/
├── MujDomecek.Api.Tests/
└── web/tests/
    ├── unit/
    └── e2e/
```

**Nástroje:** NSubstitute, TestContainers (Postgres), Vitest, Playwright

---

### Q34: Root tooling setup

**Kontext:** Jaké nástroje v rootu monorepa?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Všechno ano

```
/
├── .editorconfig
├── .gitattributes
├── .gitignore
├── .nvmrc
├── Directory.Build.props
├── package.json            # Root (husky)
├── .husky/
│   └── pre-commit
├── src/
│   ├── api/
│   └── web/
│       ├── eslint.config.js
│       └── prettier.config.js
```

**Pre-commit:** lint-staged (web) + dotnet format (api)

---

### Q35: Styling framework pro Svelte

**Kontext:** Jaký CSS framework/approach?

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** Tailwind CSS + vlastní komponenty

- Plná kontrola nad designem
- Dark mode od začátku (`darkMode: 'class'`)
- Žádná UI knihovna - custom look pro PWA

---

### Q38: Sdílené kontrakty mezi API a Web (DRY)

**Kontext:** Chceme minimalizovat duplicity DTO/typů mezi .NET API a Svelte webem.

**Status:** ✅ Rozhodnuto

**Rozhodnutí:** OpenAPI → TypeScript (codegen)

- Source of truth: .NET API (OpenAPI spec)
- Generování: `openapi-typescript` nebo `NSwag`
- Build step v CI

```
API → swagger.json → openapi-typescript → api-types.ts
```

Generujeme typy + volitelně API klient. Žádná ruční duplicita.

---

### Q39: CI/CD baseline pro monorepo

**Kontext:** V architektuře je TODO pro CI/CD. Potřebujeme minimální pipeline pro kvalitu (build/test/lint) a jasné gatekeepery pro PR.

**Status:** ⏳ Later

**Rozhodnutí:** Zatím neřešit. Pre-commit hooky (Q34) stačí. CI/CD přidat až při potřebě automatického deploymentu.

---

## Tracking

| Otázka | Status | Rozhodnutí |
|--------|--------|------------|
| Q1 | ✅ Rozhodnuto | Cena, popis, flagy, tagy, přílohy |
| Q2 | ✅ Rozhodnuto | Full local-first |
| Q3 | ✅ Rozhodnuto | Soft requirement + auto-flag |
| Q4 | ✅ Rozhodnuto | Svelte 5 + SvelteKit |
| Q5 | ✅ Rozhodnuto | Dexie.js |
| Q6 | ✅ Rozhodnuto | Push immediate, pull 5min |
| Q7 | ✅ Rozhodnuto | Clean break |
| Q8 | ✅ Rozhodnuto | Obojí (workspace + granulární) |
| Q9 | ✅ Rozhodnuto | UUID |
| Q10 | ✅ Rozhodnuto | Komprese podle tieru |
| Q11 | ✅ Rozhodnuto | Free 10 / Premium 50 |
| Q12 | ✅ Rozhodnuto | Jen tagy |
| Q13 | ✅ Rozhodnuto | In-app only, push later |
| Q14 | ✅ Rozhodnuto | JSON free, PDF+CSV premium |
| Q15 | ✅ Rozhodnuto | Auto sync |
| Q16 | ✅ Rozhodnuto | MediatR |
| Q17 | ✅ Rozhodnuto | Mapster |
| Q18 | ✅ Rozhodnuto | Monorepo |
| Q19 | ✅ Rozhodnuto | 70% coverage |
| Q20 | ✅ Rozhodnuto | Všechny 5 BC (MVP-min) |
| Q21 | ✅ Rozhodnuto | Smazat legacy, git jako archiv |
| Q22 | ✅ Rozhodnuto | Feature-based struktura, Aggregates/, Abstractions/ |
| Q23 | ✅ Rozhodnuto | SyncEnvelope s correlationId povinným |
| Q24 | ✅ Rozhodnuto | SyncQueue bez "synced" stavu, smazat po úspěchu |
| Q25 | ✅ Rozhodnuto | Badges + globální indikátor + sync problems screen |
| Q26 | ✅ Rozhodnuto | Presigned URLs přes cdn.mujdomecek.cz (bypass CF) |
| Q27 | ✅ Rozhodnuto | PropertyMember přebíjí ProjectMember |
| Q28 | ✅ Rozhodnuto | Lazy check + background job + notifikace |
| Q29 | ✅ Rozhodnuto | appsettings.json + Options pattern |
| Q30 | ✅ Rozhodnuto | Sliding window, konfigurovatelné v appsettings |
| Q31 | ✅ Rozhodnuto | Extension + magic bytes validace |
| Q32 | ✅ Rozhodnuto | Email lowercase, blokovat duplicity |
| Q33 | ✅ Rozhodnuto | NSubstitute, TestContainers, Vitest, Playwright |
| Q34 | ✅ Rozhodnuto | Vše ano (.editorconfig, husky, prettier...) |
| Q35 | ✅ Rozhodnuto | Tailwind CSS + vlastní komponenty |
| Q36 | ✅ Rozhodnuto | .NET 10 + Node 22 LTS |
| Q37 | ✅ Rozhodnuto | ASP.NET Identity + JWT + Google/Apple OIDC |
| Q38 | ✅ Rozhodnuto | OpenAPI → TypeScript codegen |
| Q39 | ⏳ Later | CI/CD až při auto-deploy |
