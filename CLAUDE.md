# MujDomecek - Claude Code Instructions

## Projekt

Aplikace pro správu nemovitostí, jednotek a oprav. Přepis legacy ASP.NET Core 8 MVC na moderní stack.

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core 10, EF Core 10, C# 14
- **Frontend:** Svelte 5, SvelteKit, TypeScript
- **Database:** PostgreSQL
- **Styling:** Tailwind CSS

## Povinná dokumentace

**PŘED KAŽDOU IMPLEMENTACÍ SI PŘEČTI RELEVANTNÍ DOKUMENTACI:**

### .NET 10 Reference (POVINNÉ)
```
docs/12-dotnet10-index.md     → Index .NET 10 novinek
docs/12-dotnet10-csharp14.md  → C# 14 syntax (field keyword, extensions, etc.)
docs/12-dotnet10-aspnet.md    → ASP.NET Core 10 patterns
docs/12-dotnet10-efcore.md    → EF Core 10 (ExecuteUpdateAsync, bulk ops)
docs/12-dotnet10-runtime.md   → Runtime optimalizace
docs/12-dotnet10-examples.md  → Příklady použití
```

### Architektura a Patterns
```
docs/11-tech-standards.md     → DDD, SOLID, coding standards
docs/03-architecture.md       → Local-first, sync, API design
docs/09-decisions.md          → Log rozhodnutí (#001-#058)
```

### Datový model a API
```
docs/02-data-model.md         → Entity, vztahy, agregáty
docs/13-api-reference.md      → 80+ API endpointů
docs/04-auth-permissions.md   → Auth, role, práva
```

## Pravidla pro kód

1. **Používej .NET 10 / C# 14 features** - field keyword, extension types, params collections
2. **Respektuj DDD patterns** z `11-tech-standards.md`
3. **API endpointy** podle `13-api-reference.md`
4. **Před rozhodnutím** zkontroluj `09-decisions.md`

## Struktura projektu

```
src/
  api/           → ASP.NET Core 10 Web API
  web/           → Svelte 5 frontend
  shared/        → Shared contracts/DTOs
docs/            → Kompletní specifikace
tests/           → Unit + Integration testy
```

## Spuštění projektu (development)

### Prerekvizity
- .NET 10 SDK
- Node.js 22+ (viz `.nvmrc`)
- PostgreSQL (lokálně nebo přes Docker)

### 1. Databáze (PostgreSQL)
```bash
# Docker varianta (port 5434):
docker-compose up -d db
# Connection string: Host=localhost;Port=5434;Database=mujdomecek;Username=postgres;Password=postgres

# Nebo lokální PostgreSQL (port 5432):
# Změnit port v appsettings.json na 5432
```

### 2. Backend API
```bash
cd src/api/MujDomecek.API
dotnet run
# Běží na http://localhost:5230
```

### 3. Frontend
```bash
cd src/web
cp .env.example .env   # Pouze poprvé!
npm install
npm run dev
# Běží na http://localhost:5173
```

### Dev porty (FIXNÍ)

| Služba | Port | URL |
|--------|------|-----|
| PostgreSQL (Docker) | 5434 | localhost:5434 |
| Backend API | 5230 | http://localhost:5230 |
| Frontend | 5173 | http://localhost:5173 |

### Konfigurace

**Backend:** `src/api/MujDomecek.API/appsettings.json`
- `ConnectionStrings:DefaultConnection` - PostgreSQL
- `Jwt:SigningKey` - JWT klíč (změnit pro produkci!)
- `Cors:AllowedOrigins` - povolené frontend origins

**Frontend:** `src/web/.env` (zkopírovat z `.env.example`)
- `VITE_API_BASE=http://localhost:5230` - URL backendu

### API Dokumentace
- OpenAPI: http://localhost:5230/openapi/v1.json

## Aktuální produkce

Legacy verze běží na: https://mujdomecek.runasp.net/
