# MujDomecek - Dokumentace

Kompletní specifikace pro vývoj aplikace MujDomecek (MyLittleHouse).

**Verze dokumentace:** 1.0
**Poslední aktualizace:** 2024-12-26

---

## Quick Start

```
1. Vize a cíle        → 01-vision.md
2. Datový model       → 02-data-model.md
3. Architektura       → 03-architecture.md
4. Implementace       → 11-tech-standards.md + 12-dotnet10-index.md
5. API                → 13-api-reference.md
```

---

## Mapa dokumentace

### Základní specifikace

| # | Soubor | Popis | Řádky |
|---|--------|-------|-------|
| 01 | [01-vision.md](01-vision.md) | Vize, cíle, scope, milníky | ~70 |
| 02 | [02-data-model.md](02-data-model.md) | Entity, vztahy, agregáty | ~425 |
| 03 | [03-architecture.md](03-architecture.md) | Local-first, sync, API design | ~290 |
| 04 | [04-auth-permissions.md](04-auth-permissions.md) | Auth flows, role, práva, API | ~700 |

### Features

| # | Soubor | Popis |
|---|--------|-------|
| 05 | [05-features/](05-features/) | Adresář s feature specifikacemi |
| | [zaznam.md](05-features/zaznam.md) | Záznamy - CRUD, rychlý zápis, drafty |
| | [sharing.md](05-features/sharing.md) | Sdílení, pozvánky, role |
| | [pwa.md](05-features/pwa.md) | PWA, offline, service worker |
| | [contacts.md](05-features/contacts.md) | Kontakty a skupiny |
| | [admin.md](05-features/admin.md) | Admin panel - uživatelé, tagy, audit |

### UX/UI

| # | Soubor | Popis | Řádky |
|---|--------|-------|-------|
| 06 | [06-ux-ui.md](06-ux-ui.md) | Obrazovky, komponenty, flows, Settings | ~610 |

### Provoz

| # | Soubor | Popis | Řádky |
|---|--------|-------|-------|
| 07 | [07-migration.md](07-migration.md) | Migrace ze současného stavu | ~50 |
| 08 | [08-security.md](08-security.md) | Bezpečnost, validace, OWASP | ~250 |

### Reference (pro AI a vývojáře)

| # | Soubor | Popis | Řádky |
|---|--------|-------|-------|
| 11 | [11-tech-standards.md](11-tech-standards.md) | DDD, SOLID, patterns, testy | ~400 |
| 12 | [12-dotnet10-index.md](12-dotnet10-index.md) | .NET 10 reference (index + moduly: C# 14 / SDK / ASP.NET / EF Core / runtime) | 1725 |
| 13 | [13-api-reference.md](13-api-reference.md) | Kompletní API dokumentace (80+ endpointů) | ~1000 |
| 14 | [14-background-jobs.md](14-background-jobs.md) | Background jobs, scheduling | ~700 |

### Rozhodnutí a historie

| # | Soubor | Popis | Řádky |
|---|--------|-------|-------|
| 09 | [09-decisions.md](09-decisions.md) | Log rozhodnutí (#001-#058) | ~1350 |
| 10 | [10-open-questions.md](10-open-questions.md) | ~~Otevřené otázky~~ (ARCHIV - vše vyřešeno) | ~730 |

---

## Tech Stack

| Komponenta | Technologie |
|------------|-------------|
| Backend | .NET 10, ASP.NET Core, EF Core 10 |
| Frontend | Svelte 5, SvelteKit, TypeScript |
| Styling | Tailwind CSS |
| Database | PostgreSQL |
| Local Storage | IndexedDB (Dexie.js) |
| File Storage | S3-compatible (MinIO/Cloudflare R2) |
| Hosting | Hetzner VPS |

---

## Jak používat dokumentaci

### Pro vývojáře

1. **Začínáš?** → Přečti [01-vision.md](01-vision.md), pak [02-data-model.md](02-data-model.md)
2. **Implementuješ feature?** → Najdi v [05-features/](05-features/) + [06-ux-ui.md](06-ux-ui.md)
3. **Píšeš API?** → [13-api-reference.md](13-api-reference.md) + [04-auth-permissions.md](04-auth-permissions.md)
4. **Potřebuješ .NET příklady?** → [12-dotnet10-examples.md](12-dotnet10-examples.md)

### Pro AI asistenty

Při práci s tímto projektem:
1. Vždy načti [12-dotnet10-index.md](12-dotnet10-index.md) a pak relevantní modul pro správnou syntaxi
2. Respektuj patterns z [11-tech-standards.md](11-tech-standards.md)
3. Kontroluj rozhodnutí v [09-decisions.md](09-decisions.md)

### Struktura rozhodnutí

Všechna rozhodnutí jsou v `09-decisions.md` s formátem:
- `#001-#020` - Základní model a features
- `#021-#040` - Architektura a sync
- `#041-#058` - UX patterns a technologie

---

## Stav projektu

| Aspekt | Stav |
|--------|------|
| Dokumentace | ✅ Kompletní |
| Datový model | ✅ Definován |
| API design | ✅ Specifikován |
| UI/UX flows | ✅ Navrženy |
| Implementace | ⏳ Nezačata |

**Aktuální produkce:** ASP.NET Core 8 MVC na https://mujdomecek.runasp.net/
**Cílový stav:** Kompletní přepis na .NET 10 + Svelte 5

---

## Konvence

### Pojmenování souborů

```
[číslo]-[název].md           # Hlavní dokumenty
[číslo]-[název]/[sub].md     # Podadresáře
```

### Formát rozhodnutí

```markdown
### [XXX] Název rozhodnutí

**Datum:** YYYY-MM-DD
**Stav:** Rozhodnuto / Otevřené / Zrušeno

**Kontext:** Proč?
**Rozhodnutí:** Co jsme zvolili.
**Důsledky:** Co to znamená.
```

---

## Changelog

| Datum | Změna |
|-------|-------|
| 2024-12-26 | Přidány: 12-dotnet10-reference, 13-api-reference, 14-background-jobs |
| 2024-12-26 | Rozšířeno: 04-auth-permissions (flows), 06-ux-ui (Settings) |
| 2024-12-26 | Aktualizováno: 02-data-model (GPS, Activity, Comments, Drafts) |
| 2024-12-26 | Vyřešeno: Všech 19 open questions (Q21-Q39) |
