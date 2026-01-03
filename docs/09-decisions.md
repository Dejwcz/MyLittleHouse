# Rozhodnutí

Log důležitých rozhodnutí - co a proč.

---

## Formát

```
### [ČÍSLO] Název rozhodnutí

**Datum:** YYYY-MM-DD
**Stav:** Rozhodnuto / Otevřené / Zrušeno

**Kontext:** Proč řešíme toto rozhodnutí?

**Možnosti:**
1. Možnost A - popis
2. Možnost B - popis

**Rozhodnutí:** Co jsme zvolili a proč.

**Důsledky:** Co to znamená pro implementaci.
```

---

## Rozhodnutí

### [001] Název záznamu v UI

**Datum:** 2024 (z původního PLAN.md)
**Stav:** Rozhodnuto

**Kontext:** Jak pojmenovat obecný záznam v UI?

**Možnosti:**
1. Oprava (Repair) - současný název
2. Záznam (Record) - obecnější

**Rozhodnutí:** Záznam. Je to obecnější a pokrývá více use-cases (opravy, zásahy, nákupy, umístění).

**Důsledky:** Přejmenovat v UI, model může zůstat jako Zaznam.

---

### [002] Vazba záznamu na Property vs Unit

**Datum:** 2024
**Stav:** Rozhodnuto

**Kontext:** Kam může záznam patřit?

**Možnosti:**
1. Pouze k Unit
2. Pouze k Property
3. K Property nebo Unit

**Rozhodnutí:** Záznam může být na Property i na Unit. PropertyId je required, UnitId je optional.

**Důsledky:** Flexibilnější model, UI musí podporovat oba případy.

---

### [003] Rychlý zápis - výchozí pole

**Datum:** 2024
**Stav:** Rozhodnuto

**Kontext:** Jaká pole jsou ve výchozím rychlém zápisu?

**Rozhodnutí:**
- Property (required, default poslední)
- Unit (optional)
- Datum (default dnes)
- Název (required)
- Fotky (required)

Ostatní pole jsou volitelná a řízená nastavením uživatele.

---

### [004] Local-first architektura

**Datum:** 2024
**Stav:** Rozhodnuto

**Kontext:** Kde jsou data primárně uložena?

**Možnosti:**
1. Server-first (současný stav)
2. Local-first (IndexedDB)
3. Hybrid

**Rozhodnutí:** Local-first. Data defaultně lokálně, upload na server je opt-in.

**Důsledky:** Zásadní změna architektury, PWA nutnost, sync logika.

---

### [005] PWA od začátku

**Datum:** 2024
**Stav:** Rozhodnuto

**Kontext:** Implementovat PWA v MVP nebo později?

**Rozhodnutí:** PWA od začátku. Je to klíčové pro local-first a mobile UX.

**Důsledky:** Service worker, manifest, offline shell v MVP.

---

### [006] Sdílení - role

**Datum:** 2024
**Stav:** Rozhodnuto

**Kontext:** Jaké role pro sdílení?

**Rozhodnutí:** Owner / Editor / Viewer s možností granulárních per-user overrides.

**Důsledky:** Permission matrix, JSON pole pro overrides.

---

### [007] Sync strategie - konflikty

**Datum:** 2024
**Stav:** Rozhodnuto

**Kontext:** Jak řešit konflikty při sync?

**Možnosti:**
1. Last-write-wins
2. Merge
3. Ruční řešení

**Rozhodnutí:** Last-write-wins + audit log. Owner vidí kdo a co změnil.

**Důsledky:** Jednodušší implementace, audit log nutný.

---

### [008] Povinná fotka u záznamu

**Datum:** 2024
**Stav:** Zrušeno (nahrazeno #019)

**Kontext:** Je fotka povinná?

**Rozhodnutí:** Původní rozhodnutí bylo zpřísněné. Nahrazeno rozhodnutím #019 (fotka = soft requirement + auto-flag).

**Důsledky:** Validace: název required, fotka doporučená (warning + `MissingPhoto`).

---

### [009] Editor - mazání fotek

**Datum:** 2024
**Stav:** Rozhodnuto

**Kontext:** Může Editor mazat fotky?

**Rozhodnutí:** Defaultně NE. Owner to může zapnout per-user override.

**Důsledky:** Granulární práva v permission matrix.

---

### [010] Notifikace - typ

**Datum:** 2024
**Stav:** Rozhodnuto

**Kontext:** Jaký typ notifikací?

**Rozhodnutí:** In-app only (v MVP). Push notifikace later.

**Důsledky:** Jednodušší implementace, notification center v UI.

---

### [011] Expirace pozvánky

**Datum:** 2024
**Stav:** Rozhodnuto

**Kontext:** Jak dlouho platí pozvánka?

**Rozhodnutí:** 7 dní.

**Důsledky:** Cron job nebo lazy check pro expiraci.

---

### [012] Pozvánky - doručení bez emailu

**Datum:** 2024
**Stav:** Rozhodnuto

**Kontext:** Notifikace jsou in-app only. Jak pozvat uživatele, který ještě nemá účet?

**Rozhodnutí:** Pozvánka se vytvoří pro email a vygeneruje se tajný token (invite link/kód). Registrovaní uvidí in-app notifikaci. Neregistrovanému Owner pošle invite link mimo aplikaci (messenger).

**Důsledky:** `Invitation` musí mít `TokenHash` (hash tajného tokenu) a UI musí umožnit kopírování linku. Accept/Decline probíhá přes otevření invite linku po loginu/registraci.

---

### [013] .NET 10 + nové repository

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Současná aplikace je v .NET 8. Chceme modernizovat.

**Rozhodnutí:** Nové repository s .NET 10 (LTS, release Nov 2025). Čistý začátek bez legacy kódu.

**Důsledky:** Čekáme na .NET 10 release nebo začínáme s preview. Migrace dat řeší se separátně.

---

### [014] DDD + Clean Architecture

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jakou architekturu použít pro novou aplikaci?

**Rozhodnutí:** Domain-Driven Design s Clean Architecture. Oddělené vrstvy: Domain, Application, Infrastructure, API.

**Důsledky:**
- Bounded contexts: PropertyManagement, Identity, Sharing, Notifications, Contacts
- Aggregates: Property, Project, User
- Patterns: CQRS, Repository, Result

Detaily: [11-tech-standards.md](11-tech-standards.md)

---

### [015] SOLID + Clean Code principy

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jaké coding standardy dodržovat?

**Rozhodnutí:** Striktní dodržování SOLID, DRY, Clean Code. Static analysis, EditorConfig, CI checks.

**Důsledky:**
- Single Responsibility - malé, fokusované třídy
- Dependency Injection všude
- Max 20 řádků metoda, max 200 řádků třída
- Code review + analyzer warnings = build failure

Detaily: [11-tech-standards.md](11-tech-standards.md)

---

### [016] Volitelná pole záznamu v MVP

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jaká volitelná pole má mít záznam v MVP?

**Rozhodnutí:**

| Pole | Typ | Povinné | Popis |
|------|-----|---------|-------|
| Cena | int | Ne | Náklady na záznam |
| Popis | text | Ne | Delší textový popis |
| Flagy | multi-select | Ne | Stav záznamu (TODO, Čeká, Důležité, Záruka, Oblíbené) |
| Tagy | multi-select | Ne | Kategorie (Elektrika, Voda, Střecha, Zahrada...) |
| Fotky | soubory | Doporučené (soft) | JPG, PNG, HEIC, WebP – limity a komprese dle tieru (viz #027, #028) |
| Dokumenty | PDF | Ne | Faktury, záruky, návody - max 20MB, max 10ks |
| Účtenky | soubory | Ne | Doklady o nákupu |

**Flagy (stav):**
- 🚧 K dodělání
- ⏳ Čeká na něco
- ⚠️ Důležité
- 💰 Reklamace/záruka
- ⭐ Oblíbené

**Tagy (kategorie) - předdefinované:**

*Dům:* Elektrika, Voda/Topení, Okna/Dveře, Stavba/Zdi, Střecha, Interiér/Malování, Podlahy, Bezpečnost

*Zahrada:* Stromy/Keře, Záhony/Výsadba, Zahradní technika, Zavlažování, Zahradní stavby

*Obecné:* Údržba, Nákup, Umístění věci, Jiné

**Důsledky:**
- Flagy jako `[Flags]` enum (bitové pole)
- Tagy jako enum + junction tabulka
- Přílohy rozdělené podle typu (DokumentTyp enum)
- Celkový limit na záznam: 50MB

---

### [017] Úložiště souborů - S3 + imgproxy

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Kam ukládat fotky a dokumenty? Jak řešit thumbnaily?

**Rozhodnutí:**

1. **Storage:** Hetzner Object Storage (S3 compatible)
2. **Thumbnaily:** imgproxy (on-the-fly processing)
3. **Cloudflare:** S3 mimo CF proxy (DNS only)

**Architektura:**

| Komponenta | Doména | CF Proxy | Účel |
|------------|--------|----------|------|
| API | mujdomecek.cz | ☁️ Ano | Backend API |
| imgproxy | img.mujdomecek.cz | ☁️ Ano | Resize, thumbnaily |
| S3 | cdn.mujdomecek.cz | DNS only | Originály, presigned URLs |

**Flow:**
- Upload: Klient → API → S3 (pouze originál)
- Seznam: Klient ← imgproxy ← S3 (300x300)
- Detail: Klient ← imgproxy ← S3 (800x600)
- Fullsize: Klient ← S3 direct (presigned URL)

**imgproxy URL struktura:**
```
https://img.mujdomecek.cz/{signature}/rs:fit:{w}:{h}/{path}
```

**S3 struktura:**
```
mujdomecek/
└── uploads/
    └── {userId}/
        └── {zaznamId}/
            ├── photos/{uuid}.jpg
            ├── documents/{uuid}.pdf
            └── receipts/{uuid}.jpg
```

**Důsledky:**
- Žádné předgenerování thumbnailů
- Jeden soubor = jeden originál v S3
- imgproxy cachuje transformace
- Signed URLs pro zabezpečení imgproxy
- Presigned URLs pro přímý download originálů

---

### [018] PWA rozsah - full local-first

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jak moc offline funkcionalita v MVP?

**Rozhodnutí:** Full local-first (offline read + write)

**Offline capabilities:**

| Funkce | Offline | Online |
|--------|---------|--------|
| Zobrazit záznamy | ✅ IndexedDB | ✅ IndexedDB (sync) |
| Vytvořit záznam | ✅ IndexedDB + queue | ✅ IndexedDB → Server |
| Editovat záznam | ✅ IndexedDB + queue | ✅ IndexedDB → Server |
| Smazat záznam | ✅ IndexedDB + queue | ✅ IndexedDB → Server |
| Nahrát fotku | ✅ IndexedDB (blob) | ✅ IndexedDB → S3 |
| Stáhnout fotku | ⚠️ Jen cached | ✅ S3/imgproxy |
| Login/Registrace | ❌ Vyžaduje online | ✅ |

**Technické důsledky:**
- IndexedDB jako primární storage (Dexie.js)
- Sync queue pro offline operace
- UUID generované na klientu (ne server auto-increment)
- Service Worker pro offline shell

**Later:**
- Background sync (sync i když app zavřená)
- Push notifications
- Conflict UI (teď: last-write-wins tiše)

---

### [019] Fotka - soft requirement

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Je fotka povinná? Co edge cases?

**Rozhodnutí:** Soft requirement (doporučeno, ne vynuceno)

**Pravidla:**
- Záznam lze uložit bez fotky (s varováním)
- Automaticky se přidá flag `MissingPhoto`
- Lze smazat všechny fotky ze záznamu
- UI zobrazí doporučení přidat fotku

**UI flow:**
```
⚠️ Doporučujeme přidat alespoň jednu fotku
[Uložit bez fotky]  [Přidat fotku]
```

**Auto-flag logika:**
```csharp
// Při uložení bez fotky
if (!zaznam.HasPhotos)
    zaznam.Flags |= ZaznamFlags.MissingPhoto;

// Při přidání fotky
if (zaznam.HasPhotos)
    zaznam.Flags &= ~ZaznamFlags.MissingPhoto;
```

**Důvody:**
- Rychlý zápis nesmí být blokován
- Reálné use cases bez fotky (nákup, telefonát, plán)
- Flag připomíná doplnění, neblokuje

---

### [020] Frontend framework - Svelte 5

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Local-first PWA potřebuje client-side framework.

**Rozhodnutí:** Svelte 5 + SvelteKit + TypeScript

**Důvody:**
- Nejmenší bundle (~10kB) = ideální pro PWA
- Kompiluje pryč framework overhead
- Svelte 5 runes = moderní reaktivita
- SvelteKit = routing, SSR (optional), API routes
- Jednoduchá syntaxe = rychlý vývoj

**Stack:**
```
Frontend:
- Svelte 5 (runes)
- SvelteKit
- TypeScript
- Dexie.js (IndexedDB)
- Tailwind CSS (semantic tokens + CSS variables)
```

**Příklad komponenty:**
```svelte
<script lang="ts">
  import { liveQuery } from '$lib/db';

  let zaznamy = $derived(liveQuery(() => db.zaznamy.toArray()));
</script>

{#each zaznamy as z (z.id)}
  <ZaznamCard {...z} />
{/each}
```

**Důsledky:**
- Nový jazyk pro tým (Svelte syntax)
- Menší ekosystém knihoven
- Co nebude, dopíšeme

---

### [021] IndexedDB - Dexie.js

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Local-first potřebuje client-side storage.

**Rozhodnutí:** Dexie.js 4.x

**Důvody:**
- `liveQuery` = reaktivní queries (Svelte friendly)
- Čistá Promise API
- Dobrá TypeScript podpora
- ~25kB bundle

**Schema:**
```typescript
db.version(1).stores({
  projects: 'id, name, ownerId, updatedAt',
  properties: 'id, projectId, name, updatedAt',
  units: 'id, propertyId, parentUnitId, updatedAt',
  zaznamy: 'id, propertyId, unitId, date, updatedAt, flags',
  dokumenty: 'id, zaznamId, typ, updatedAt',
  syncQueue: 'id, entityType, entityId, action, createdAt'
});
```

---

### [022] Deployment - Docker na Hetzner VPS

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jak deploynout aplikaci?

**Rozhodnutí:** Docker Compose na Hetzner VPS

**Stack:**
```yaml
services:
  # Reverse proxy + SSL
  traefik:
    image: traefik:v3
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - ./traefik:/etc/traefik

  # .NET API
  api:
    build: ./src/MujDomecek.API
    environment:
      - ConnectionStrings__Default=Host=db;Database=mujdomecek;...
      - S3__Endpoint=https://fsn1.your-objectstorage.com
    labels:
      - "traefik.http.routers.api.rule=Host(`api.mujdomecek.cz`)"

  # SvelteKit frontend
  web:
    build: ./src/web
    labels:
      - "traefik.http.routers.web.rule=Host(`mujdomecek.cz`)"

  # PostgreSQL
  db:
    image: postgres:16
    volumes:
      - postgres_data:/var/lib/postgresql/data
    environment:
      - POSTGRES_DB=mujdomecek
      - POSTGRES_USER=${DB_USER}
      - POSTGRES_PASSWORD=${DB_PASSWORD}

  # Image processing
  imgproxy:
    image: darthsim/imgproxy:latest
    environment:
      - IMGPROXY_KEY=${IMGPROXY_KEY}
      - IMGPROXY_SALT=${IMGPROXY_SALT}
      - IMGPROXY_USE_S3=true
    labels:
      - "traefik.http.routers.img.rule=Host(`img.mujdomecek.cz`)"

volumes:
  postgres_data:
```

**Infrastruktura:**

| Služba | Container | Doména |
|--------|-----------|--------|
| Reverse proxy | Traefik | - |
| API | .NET 10 | api.mujdomecek.cz |
| Frontend | SvelteKit | mujdomecek.cz |
| Database | PostgreSQL 16 | internal |
| Images | imgproxy | img.mujdomecek.cz |
| Storage | Hetzner S3 | cdn.mujdomecek.cz |

**Důsledky:**
- Vše v kontejnerech = easy scaling, updates
- Traefik = automatické SSL (Let's Encrypt)
- Docker Compose = jednoduchý deployment
- Secrets v .env nebo Docker secrets

---

### [023] Sync protokol

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jak synchronizovat data mezi klientem a serverem?

**Rozhodnutí:**

**Push (klient → server):**
- Ihned při online + změně
- Batch max 50 položek
- Sync queue v IndexedDB

**Pull (server → klient):**
- Při startu app
- Každých 5 min (background)
- Po úspěšném push
- Delta sync (`?since={timestamp}`)

**Soubory:**
- Upload separátně od metadat
- POST /api/upload → S3 key
- Download lazy, on-demand
- Cache v IndexedDB (optional)

**Retry strategie:**
- Exponential backoff: 1s, 5s, 30s, 1min, 5min
- Max 5 pokusů
- Po selhání: označit jako failed, zobrazit uživateli

**Conflict resolution:**
- Last-write-wins na úrovni entity
- Porovnání `updatedAt` timestamps
- Audit log pro každou změnu

**API:**
```
POST /api/sync/push    - batch push změn
GET  /api/sync/pull    - delta pull od timestamp
POST /api/upload       - upload souboru → S3
GET  /api/sync/status  - stav synchronizace
```

---

### [024] Migrace - clean break

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jak přejít ze současné aplikace?

**Rozhodnutí:** Clean break - nová aplikace, žádná migrace dat.

**Důvody:**
- Školní projekt, žádní reální uživatelé
- Úplně jiná architektura (local-first vs server-first)
- Čistý start bez legacy kódu
- Rychlejší development

**Důsledky:**
- Nové repository
- Nový deployment
- Stará app může zůstat jako reference/archiv
- Žádný migrační skript

---

### [025] Sdílení - Project i Property

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Na jaké úrovni sdílet?

**Rozhodnutí:** Obojí - Project jako workspace, Property jako granulární.

**Project = Workspace:**
- Rodina, firma, tým
- Člen projektu má přístup ke všem Properties v projektu
- Use case: "Všichni v rodině vidí všechny naše nemovitosti"

**Property = Granulární:**
- Sdílení konkrétní nemovitosti
- Use case: "Řemeslník má přístup jen k chalupě"

**Příklad:**
```
Project "Rodina Novákovi"
├── 👨‍👩‍👧‍👦 Členové: Táta, Máma, Syn (vidí vše)
├── 🏠 Byt Praha
├── 🏡 Chalupa
│   └── 👷 Řemeslník (jen tato property)
└── 🌳 Zahrada
```

**Logika přístupu:**
```
HasAccess(User, Property) =
    IsProjectMember(User, Property.ProjectId)
    OR IsPropertyMember(User, Property.Id)
```

---

### [026] UUID pro entity

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Local-first vyžaduje generování ID na klientu.

**Rozhodnutí:** UUID (GUID) pro všechny entity.

**Implementace:**
- Klient: `crypto.randomUUID()`
- Server: `Guid.NewGuid()`
- Žádné kolize, žádná koordinace

---

### [027] Komprese fotek - podle tieru

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Fotky z mobilu jsou velké (5-15MB).

**Rozhodnutí:** Komprese závisí na subscription tieru.

| Tier | Komprese | Max rozměr | Kvalita |
|------|----------|------------|---------|
| Free | Ano | 2000px | 80% JPEG |
| Premium | Ne | Originál | Originál |

**Komprese na klientu** před uploadem (šetří bandwidth).

---

### [028] Limity fotek podle tieru

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Kolik fotek povolit?

**Rozhodnutí:**
- Free: max 10 fotek/záznam
- Premium: max 50 fotek/záznam

---

### [029] Typ záznamu - jen tagy

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Mít enum pro typ záznamu?

**Rozhodnutí:** Ne, stačí tagy.

**Důvody:**
- Tagy už pokrývají typy (Údržba, Nákup, Instalace...)
- Multi-select je flexibilnější
- Méně polí = jednodušší model

---

### [030] Freemium model + QR platby

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Monetizace aplikace.

**Rozhodnutí:** Freemium s QR platbami (CZ bankovní převod).

**Free tier:**
| Funkce | Limit |
|--------|-------|
| Projekty | 1 |
| Properties | 3 |
| Fotky/záznam | 10 |
| Kvalita fotek | Komprese (2000px) |
| Sdílení | 2 členové |
| Storage | 500 MB |

**Premium tier (~99 Kč/měsíc):**
| Funkce | Limit |
|--------|-------|
| Projekty | Unlimited |
| Properties | Unlimited |
| Fotky/záznam | 50 |
| Kvalita fotek | Originál |
| Sdílení | Unlimited |
| Storage | 50 GB |
| Export PDF/CSV | ✅ |

**Platby:**
- QR kód (SPAYD formát) - 0 Kč poplatky
- Párování přes variabilní symbol
- Automatizace: Fio API (nebo jiná banka s API)
- Fallback: manuální aktivace

**Launch strategie:**
- 30 dní trial zdarma (premium features)
- Nebo ruční přiřazení premium early users
- Freemium limity od začátku v kódu

**Later:**
- Stripe pro zahraniční uživatele (pokud potřeba)

---

### [031] Push notifikace - later

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jaké notifikace v MVP?

**Rozhodnutí:**
- **MVP:** In-app notifikace (vidíš jen když máš app otevřenou)
- **Later:** Push notifikace (přijdou i když app zavřená)

---

### [032] Export formáty

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jaké exporty podporovat?

**Rozhodnutí:**

| Formát | Tier | Popis |
|--------|------|-------|
| JSON | Free | Backup dat |
| PDF | Premium | Report pro pojišťovnu, přehled |
| CSV | Premium | Tabulka pro Excel |

---

### [033] Auto sync

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Sync bez sdílení - automaticky nebo ručně?

**Rozhodnutí:** Automaticky.

- Pokud má uživatel opt-in backup → sync běží automaticky
- Ruční trigger zbytečně komplikuje UX
- Uživatel nemusí myslet na "zmáčkni sync"

---

### [034] MediatR pro CQRS

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jakou CQRS implementaci?

**Rozhodnutí:** MediatR

**Důvody:**
- Ověřený, velká komunita
- Pipeline behaviors (validation, logging, caching)
- Méně boilerplate

---

### [035] Mapster pro DTO mapping

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jak mapovat entity na DTO?

**Rozhodnutí:** Mapster

**Důvody:**
- Rychlejší než AutoMapper (compile-time)
- Méně magie, explicitnější
- Jednodušší konfigurace

---

### [036] Monorepo

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Monorepo nebo separate repos?

**Rozhodnutí:** Monorepo

**Struktura:**
```
muj-domecek/
├── src/
│   ├── api/           # .NET API
│   ├── web/           # SvelteKit
│   └── shared/        # Shared types
├── tests/
├── docker-compose.yml
├── docs/
└── README.md
```

**Důvody:**
- Jednodušší správa
- Shared types mezi FE a BE
- Jeden PR = celá feature
- Atomic commits

---

### [037] Code coverage 70%

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jaký minimální code coverage?

**Rozhodnutí:** 70% celkově

**Pravidla:**
- Domain/Services: 80%+ (kritický kód)
- API Controllers: 70%
- UI komponenty: 50% (těžší testovat)
- CI blokuje merge pod 60%

**Nástroje:**
- .NET: Coverlet + ReportGenerator
- Svelte: Vitest + c8/istanbul

---

### [038] Zaznam jako aggregate root + server revision

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Revize DDD architektury na základě sync requirements.

**Rozhodnutí:**

**1. Zaznam jako samostatný aggregate root**

Původně: Property → Units → Zaznamy → Dokumenty (hluboká hierarchie)

Nově:
```
Property (aggregate root) → Units
Zaznam (aggregate root) → ZaznamDokumenty, ZaznamTags
```

**Důvody:**
- Property aggregate by příliš bobtnala
- Sync celého stromu = performance problém
- Zaznam má vlastní konzistenční hranici
- Reference přes `PropertyId` / `UnitId` (ne navigační property)

**2. Server revision pro sync**

Čistý klientský `updatedAt` je náchylný na rozjeté hodiny.

| Pole | Kde | Účel |
|------|-----|------|
| `updatedAt` | Klient | Kdy uživatel změnil |
| `serverRevision` | Server | Pořadí přijetí (auto-increment) |

**Conflict resolution:**
1. Primárně `serverRevision` (vyšší = novější)
2. `updatedAt` jako tie-break (pokud stejné serverRevision)
3. Audit log uchovává obojí

**3. Pravidlo 20 řádků jako guideline**

"Max 20 řádků na metodu" je orientační, ne hard limit. Důležitější je:
- Single responsibility
- Čitelnost
- Jeden level abstrakce

Příliš striktní dodržování vede k mikro-tříštění kódu.

**Důsledky:**
- Upravit 02-data-model.md (aggregate boundaries)
- Upravit 03-architecture.md (sync protocol)
- Upravit 11-tech-standards.md (guideline)

---

### [039] .NET 10 od začátku

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Jakou verzi .NET použít pro nový projekt?

**Možnosti:**
1. .NET 8 (current LTS) → upgrade na 10 později
2. .NET 9 (installed) → upgrade na 10 později
3. .NET 10 preview → rovnou cílová verze

**Rozhodnutí:** .NET 10 od začátku.

**Důvody:**
- Cílová verze je .NET 10 LTS (release Nov 2025)
- Školní projekt = můžeme riskovat preview
- Žádný upgrade path, čistý start
- Nejnovější features (Native AOT improvements, etc.)

**Důsledky:**
- Stáhnout .NET 10 SDK preview
- Některé NuGet balíčky možná nebudou mít preview support
- Sledovat breaking changes před release

---

### [040] Freemium implementace od začátku

**Datum:** 2024-12
**Stav:** Rozhodnuto

**Kontext:** Kdy implementovat tier limity a freemium logiku?

**Možnosti:**
1. MVP bez tierů → přidat later
2. Tier limity v kódu od začátku, platby later
3. Plná implementace včetně plateb

**Rozhodnutí:** Tier limity od začátku, platební flow later.

**Implementace v MVP:**
- `UserSubscription` entita s `Tier` (Free/Premium)
- Limity vyhodnocované v Application vrstvě
- UI zobrazuje tier a zbývající limity
- Premium se přiřazuje ručně (admin)

**Later:**
- QR platby (SPAYD)
- Automatické párování plateb
- Trial period (30 dní)

**Důvody:**
- Limity ovlivňují architekturu (validace, UI)
- Přidání later = refactoring
- Platby jsou izolovaná funkcionalita

**Důsledky:**
- UserSubscription v datovém modelu
- TierService pro vyhodnocování limitů
- UI komponenty pro zobrazení limitů

---

### [041] Legacy složka - smazat

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Co s /MujDomecek složkou při vytváření nové struktury?

**Rozhodnutí:** Smazat. Git historie slouží jako archiv.

---

### [042] DDD namespace konvence

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak strukturovat vrstvy a pojmenovávat třídy?

**Rozhodnutí:** Feature-based struktura v Application vrstvě.

**Domain:**
- `Aggregates/` (ne Entities)
- `ValueObjects/`
- `Events/`
- `Exceptions/`
- `Abstractions/` (ne Interfaces)

**Application:**
- `Features/{Feature}/Commands/` - Command + Handler spolu
- `Features/{Feature}/Queries/`
- `Features/{Feature}/Validators/`
- `DTOs/`
- `Behaviors/`

**Naming:** `CreateZaznamCommand`, `CreateZaznamHandler`, `ZaznamDto`

---

### [043] Presigned URLs strategie

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** CloudFlare modifikuje hlavičky → presigned URLs nefungují přes proxy.

**Rozhodnutí:** Hybridní přístup - bypass CloudFlare pro presigned URLs.

**DNS:**
- `img.mujdomecek.cz` - CloudFlare ON (imgproxy, thumbnaily)
- `cdn.mujdomecek.cz` - DNS only (presigned URLs)

**Expirace:**
- Upload: 15 minut
- Download: 1 hodina

---

### [044] Permission precedence

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Uživatel může mít roli na Project i Property úrovni.

**Rozhodnutí:** Explicitní PropertyMember přebíjí implicitní ProjectMember.

Umožňuje výjimky (např. Editor na projektu, ale Viewer na citlivé Property).

---

### [045] Invitation expiration mechanismus

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Pozvánky expirují po 7 dnech.

**Rozhodnutí:** Kombinace lazy check + background job + notifikace.

1. Lazy check při čtení pozvánky
2. Background job 1x denně
3. Notifikace vlastníkovi s možností "Poslat znovu"

---

### [046] Domain constraints v appsettings

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Kde definovat limity (délky stringů, max fotek...)?

**Rozhodnutí:** hardcoded defaults + `appsettings.json` + runtime override v DB (Admin Settings).

- Výchozí hodnoty jsou v `appsettings.json` (Options pattern).
- Runtime změny přes Admin panel (`/admin/settings`) → DB (`AppSetting`) bez restartu.
- Fallback: při výpadku DB běží `appsettings.json` a poté hardcoded defaults.

---

### [047] Rate limiting

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Které endpointy a jaké limity?

**Rozhodnutí:** Sliding window, konfigurovatelné v appsettings.json.

| Endpoint | Limit | Window |
|----------|-------|--------|
| /auth/login | 5 | 15 min |
| /auth/register | 3 | 1 hour |
| /sync/push | 100 | 1 min |
| /sync/pull | 30 | 1 min |
| /upload | 20 | 1 min |

---

### [048] File type validace

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak validovat nahrávané soubory?

**Rozhodnutí:** Extension + magic bytes.

- Extension: Frontend + Backend (rychlé, UX)
- Magic bytes: Backend only (bezpečnost)
- Povolené typy: JPEG, PNG, WebP, HEIC, PDF

---

### [049] Contacts deduplikace

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak řešit duplicitní emaily kontaktů?

**Rozhodnutí:**
- Email normalizace: lowercase při ukládání
- Duplicity: blokovat (vrátit existující kontakt)
- Audit: `CreatedBy`, `CreatedAt`

---

### [050] Test struktura

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Co testovat na které vrstvě?

**Rozhodnutí:**

| Vrstva | Typ | Mocking |
|--------|-----|---------|
| Domain | Unit | Žádný |
| Application | Unit | NSubstitute |
| Infrastructure | Integration | TestContainers |
| API | Integration | TestContainers |
| Web | Unit + E2E | Vitest + Playwright |

---

### [051] Root tooling

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jaké nástroje v rootu monorepa?

**Rozhodnutí:** Vše od začátku:
- .editorconfig
- .gitattributes
- .nvmrc (Node 22)
- global.json (.NET 10)
- Directory.Build.props
- husky + lint-staged
- eslint + prettier (web)
- dotnet format (api)

---

### [052] Tailwind CSS

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jaký CSS framework?

**Rozhodnutí:** Tailwind CSS + vlastní komponenty.

- Plná kontrola nad designem
- Dark mode od začátku (`darkMode: 'class'`)
- Žádná UI knihovna

---

### [053] Autentizace - Identity + JWT + OIDC

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak řešit autentizaci pro PWA?

**Rozhodnutí:** ASP.NET Identity + JWT + externí OIDC providery.

**Metody:** Email/heslo, Google, Apple

**Tokeny:**
- Access: JWT, 15 min, v paměti
- Refresh: HttpOnly cookie, 7 dní, rotace

---

### [054] Sdílené kontrakty - OpenAPI codegen

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak sdílet typy mezi API a Web?

**Rozhodnutí:** OpenAPI → TypeScript codegen.

- Source of truth: .NET API (OpenAPI spec)
- Generování: openapi-typescript nebo NSwag
- Build step generuje `api-types.ts`

---

### [055] Activity feed na sdílených properties

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak zobrazit aktivitu na sdílených nemovitostech?

**Rozhodnutí:** Activity feed inspirovaný Facebook collaborative albums.

- Tab "Aktivita" v Property detail
- Události: nový záznam, editace, komentář, nový člen
- @mention v komentářích → notifikace
- Konfigurovatelné notifikace per user

**Inspirace:** Facebook collaborative albums

---

### [056] GPS auto-tagging

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak usnadnit výběr property při rychlém zápisu?

**Rozhodnutí:** Automatický návrh property na základě GPS pozice.

- Property může mít GPS souřadnice + radius (default 100m)
- Při otevření app/FAB se zkontroluje poloha
- Pokud match → "Vypadá to, že jste na Chalupě"
- GPS se používá pouze lokálně, lze vypnout

**Inspirace:** Instagram location suggestions

---

### [057] Timeline view pro záznamy

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak zobrazit historii záznamů přehledněji?

**Rozhodnutí:** Alternativní Timeline view vedle klasického seznamu.

- Vertikální timeline s roky/měsíci
- Sticky headers při scrollování
- Quick jump na rok
- Velikost bubliny = počet fotek nebo náklady
- Toggle: `[Seznam] [Timeline]`

**Inspirace:** Facebook Timeline

---

### [058] Draft záznamy (rozpracované)

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak umožnit rychlé focení bez nutnosti vyplnit vše hned?

**Rozhodnutí:** Draft záznamy s odloženým doplněním.

- Quick capture → draft s fotkou + auto-datum + auto-property
- Badge 📝 v seznamu
- Sekce "Rozpracované" (collapsible)
- Auto-reminder po 3 dnech
- Auto-delete po 30 dnech (s upozorněním)

**Stavy:** Draft → Complete → Synced

**Inspirace:** Instagram Stories drafts

---

### [059] Design System - Theming

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak zajistit konzistentní barvy a snadné skinování?

**Rozhodnutí:** CSS variables + Tailwind semantic tokens.

- Nikdy raw Tailwind barvy (`bg-green-500`)
- Vždy semantic tokeny (`bg-primary`)
- Změna theme = změna CSS variables v `:root`
- Dark mode přes `.dark` class

**Barvy:** Zelená paleta (inspirace mujdomecek.runasp.net)
**Ikony:** Lucide Icons
**Fonty:** Inter + system stack

---

### [060] Sdílení - Project vs Property scope

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Sdílet na úrovni projektu, property nebo obojí?

**Rozhodnutí:** Obojí s precedencí.

- Project-level: sdílí všechny properties (bulk)
- Property-level: sdílí konkrétní property (výjimky)
- Property přebíjí Project

---

### [061] Editor invite permissions

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Může Editor zvát další uživatele?

**Rozhodnutí:** Ne (default), ale lze povolit.

- Default: Editor nemůže zvát
- Owner může přidat `canInviteUsers: true` v Permissions JSON

---

### [062] Offline pozvánky

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Jak řešit pozvánky v offline režimu?

**Rozhodnutí:** Pozvánky vyžadují online.

- Vytvoření/accept/decline = server operace
- Offline: UI zobrazí "Pro sdílení je potřeba připojení"
- Pending pozvánky viditelné v cache (read-only)

---

### [063] Admin Panel

**Datum:** 2024-12-26
**Stav:** Rozhodnuto

**Kontext:** Potřebujeme admin rozhraní pro správu systému.

**Rozhodnutí:**
- Admin panel jako součást hlavní aplikace (route `/admin/*`)
- Přístup pouze pro roli `Admin` (přiřazuje se ručně v DB)
- Scope pro VP:
  - Dashboard s metrikami a health checky
  - Správa uživatelů (seznam, blokování, mazání)
  - Správa tagů (CRUD, řazení)
  - Audit log prohlížení a export
  - Link na Hangfire dashboard

**Later:**
- Impersonation (přihlášení jako uživatel pro debug)
- Premium management
- Rozšíření runtime konfigurace (feature flags, další settings)

**Důsledky:**
- Lazy loading admin bundlu (performance)
- Všechny admin akce se logují do AuditLog
- Rate limiting na admin endpointy

---

### [064] Runtime konfigurace (Admin Settings)

**Datum:** 2025-12-26
**Stav:** Rozhodnuto

**Kontext:** Některé limity a chování (např. tier limity pro fotky/kompresi) chceme měnit bez redeploye a bez restartu.

**Rozhodnutí:** Key-value nastavení v DB (tabulka `AppSetting`) s fallbackem.

- Precedence: hardcoded defaults → `appsettings.json` → DB (`AppSetting`).
- Admin UI: `/admin/settings` + API `/admin/settings` (GET/PUT).
- Upravitelné jen pro whitelist klíčů (např. `Constraints.Files.*`).
- Každá změna se audituje (kdo/kdy/co) a validuje (rozsahy, typ).

**Důsledky:**
- Provozní změny bez restartu.
- Při výpadku DB konfigurace nespadne (fallback na `appsettings.json`/defaults).

---

### [065] Hybrid Sync (per-scope sync mode)

**Datum:** 2025-12-31
**Stav:** Rozhodnuto

**Kontext:** Uživatelé chtějí mít možnost rozhodnout, která data se synchronizují na server a která zůstanou pouze lokálně. Při local-first architektuře je potřeba umožnit volbu režimu synchronizace na úrovni scope (Project/Property/Zaznam).

**Možnosti:**
1. **Vše local, opt-in sync per projekt** - uživatel explicitně zapíná sync pro každý projekt
2. **Vše local, opt-in sync per scope (Project/Property/Zaznam)** - jemná granularita bez nutnosti per-field nastavení
3. **Vše synced, opt-out** - příliš invazivní, uživatel nemusí chtít posílat data na server

**Rozhodnutí:** Možnost 2 - Per-scope sync mode s volbami:
- `local-only`: data pouze v IndexedDB, nikdy se nesynchronizují
- `synced`: data v IndexedDB + synchronizace na server

**Conflict resolution:** Při konfliktu (změna lokálně i na serveru) se zobrazí dialog s oběma verzemi a uživatel rozhodne, kterou verzi ponechat.

**Kdy synchronizovat:**
- Automaticky při změně (s debounce)
- Při přechodu z offline do online
- Manuální tlačítko pro vynucení sync

**Vypnutí sync:** Dialog nabídne:
- "Ponechat kopii na serveru (archiv)"
- "Smazat ze serveru"

**Důsledky:**
- Přidat `syncMode` a `lastSyncAt` do Project/Property/Zaznam
- SyncQueueItem musí nést `scopeType` a `scopeId`
- API sync endpointy musí respektovat scope (push/pull/status)
- UI: SyncBadge, ConflictDialog, DisableSyncDialog
- Sync toggle v nastavení Project/Property/Zaznam
- Global sync status v hlavičce (pending count, offline status)

**Příklad uživatelského flow:**
```
1. Guest vytvoří projekt "Chalupa" → local-only, v IndexedDB
2. Guest se rozhodne registrovat → stále local-only
3. U projektu zapne sync pro Property "Střecha" → nahraje jen vybraný scope
4. Přidá člena rodiny → sdílí pouze vybraný scope
5. Vytvoří nový projekt "Soukromé poznámky" → nechá local-only
6. Offline: edituje oba projekty → změny v IndexedDB
7. Online: "Střecha" se synchronizuje, "Soukromé" zůstává local
```

---

### [066] Mobilni distribuce (PWA + Capacitor wrapper)

**Datum:** 2026-01-03
**Stav:** Rozhodnuto

**Kontext:** Potrebujeme verejnou distribuci na iOS/Android (App Store / Google Play) a pritom zachovat local-first architekturu a rychly vyvoj.

**Moznosti:**
1. **PWA only** - nejmensi effort, ale omezeni na iOS (push, background).
2. **PWA + Capacitor wrapper** - zachova SvelteKit kod, prida store distribuci a native API.
3. **Full native (RN/Flutter/Tauri)** - nejlepsi native feel, ale rewrite.

**Rozhodnuti:** Moznost 2 - PWA jako primarni klient + Capacitor wrapper pro store distribuci.

**Dusledky:**
- Udrzujeme jednu kodovou bazi (SvelteKit) + Capacitor konfiguraci.
- Build pipeline musi produkovat iOS/Android buildy.
- Push/notifikace a pristup k native API budou reseny pres Capacitor pluginy.
- PWA zustava jako rychla webova distribuce (Add to Home Screen).

---

### [067] Offline fotky (Capacitor storage + volitelny export do galerie)

**Datum:** 2026-01-03
**Stav:** Rozhodnuto

**Kontext:** PWA IndexedDB storage je na mobilech nepredvidatelne (kvoty, eviction). Potrebujeme stabilni offline uloziste pro fotky a zaroven dat kontrolu uzivateli.

**Moznosti:**
1. **IndexedDB bloby** - rychle nasazeni, ale riziko ztraty dat pri eviction.
2. **Native storage pres Capacitor** - stabilni uloziste, potrebuje wrapper.
3. **Systemova galerie jako primary** - uzivatelsky prijatelne, ale ztrata kontroly (uzivatel muze smazat).

**Rozhodnuti:** Moznost 2 + doplnkova moznost exportu do galerie.

**Dusledky:**
- Offline originaly jsou ulozene v app-privatnim ulozisti (Capacitor Filesystem).
- PWA bez Capacitoru drzi jen metadata + nahledy, originaly ne.
- UI prida akci "Ulozit do galerie" (kopie do systemove galerie).
- Pri smazani z galerie app data zustavaji beze zmeny.

---

### [068] Projekt-centrická navigace (Frontend routing)

**Datum:** 2026-01-03
**Stav:** Rozhodnuto

**Kontext:** Frontend potřebuje jasnou navigační strukturu. Projekty jsou hlavní kontejnery pro nemovitosti, jednotky a záznamy.

**Možnosti:**
1. **Route-based scoping** - `/projects/[projectId]/properties`, `/projects/[projectId]/zaznamy`
   - Přehledné URL, snadné sdílení odkazů
   - Nested layouts s project-aware sidebarem
   - Jasná hierarchie

2. **Context-based (flat routes)** - `/properties`, `/zaznamy` + context v store
   - Kratší URL
   - Méně explicitní vazba na projekt

**Rozhodnutí:** Možnost 1 - Route-based scoping.

**Route struktura:**
```
/projects                              → Seznam projektů
/projects/[projectId]                  → Projekt dashboard
/projects/[projectId]/properties       → Nemovitosti v projektu
/projects/[projectId]/properties/[id]  → Detail nemovitosti
/projects/[projectId]/units/[id]       → Detail jednotky
/projects/[projectId]/zaznamy          → Záznamy v projektu
/projects/[projectId]/zaznamy/[id]     → Detail záznamu
/projects/[projectId]/zaznamy/new      → Nový záznam
/projects/[projectId]/settings         → Nastavení projektu
```

**Navigace:**
- Hlavní sidebar (mimo projekt): Projekty, Notifikace, Nastavení
- Projekt sidebar (uvnitř projektu): Dashboard, Nemovitosti, Záznamy, Nastavení projektu + "← Zpět"

**Důsledky:**
- `[projectId]/+layout.svelte` poskytuje project context přes Svelte Context API
- Stránky uvnitř projektu čtou project data z contextu
- URL jsou sdílitelné (obsahují project ID)
- Čistší separace hlavního a projekt-specifického layoutu

---

## Otevřená rozhodnutí

Všechna rozhodnutí jsou uzavřena. Viz [10-open-questions.md](10-open-questions.md) pro historii.
