# Technické standardy

## Cílový stack

| Komponenta | Technologie |
|------------|-------------|
| Framework | **.NET 10** (LTS, release Nov 2025) |
| Databáze | PostgreSQL |
| ORM | Entity Framework Core |
| Frontend | Svelte 5 + SvelteKit + TypeScript |
| Local-first storage | IndexedDB + Dexie.js |
| Hosting | Hetzner VPS |

**Poznámka:** Nové repository - čistý začátek bez legacy kódu.

---

## Architektonické principy

### Domain-Driven Design (DDD)

Aplikace používá DDD pro jasné oddělení domény od infrastruktury.

#### Struktura projektu

```
/
├── .editorconfig
├── .gitattributes
├── .gitignore
├── .nvmrc                       # Node 22
├── global.json                  # .NET 10
├── Directory.Build.props
├── package.json                 # Root (husky)
├── .husky/
│   └── pre-commit               # lint-staged + dotnet format
│
├── src/
│   ├── api/                     # .NET API + Clean Architecture
│   │   ├── MujDomecek.Domain/
│   │   │   ├── Aggregates/
│   │   │   ├── ValueObjects/
│   │   │   ├── Events/
│   │   │   ├── Exceptions/
│   │   │   └── Abstractions/
│   │   ├── MujDomecek.Application/
│   │   │   ├── Features/
│   │   │   │   └── {Feature}/
│   │   │   │       ├── Commands/
│   │   │   │       ├── Queries/
│   │   │   │       └── Validators/
│   │   │   ├── DTOs/
│   │   │   └── Behaviors/
│   │   ├── MujDomecek.Infrastructure/
│   │   └── MujDomecek.API/
│   │
│   └── web/                     # SvelteKit (PWA + local-first)
│       ├── eslint.config.js
│       ├── prettier.config.js
│       └── ...
│
└── tests/
    ├── MujDomecek.Domain.Tests/
    ├── MujDomecek.Application.Tests/
    ├── MujDomecek.Infrastructure.Tests/
    ├── MujDomecek.Api.Tests/
    └── web/
        ├── unit/                # Vitest
        └── e2e/                 # Playwright
```

#### Bounded Contexts

| Context | Odpovědnost |
|---------|-------------|
| PropertyManagement | Properties, Units, Zaznamy |
| Identity | Users, Authentication |
| Sharing | Invitations, Permissions, Members |
| Notifications | In-app notifications |
| Contacts | Contacts, Groups |

#### Aggregates

- **Project** (aggregate root) → ProjectMember
- **Property** (aggregate root) → Units, PropertyMember
- **Zaznam** (aggregate root) → ZaznamDokument, ZaznamTag
- **User** (aggregate root) → Contact, ContactGroup

**Poznámka:** Zaznam je samostatný aggregate root (ne vnořený pod Property) kvůli sync performance a konzistenčním hranicím. Viz [09-decisions.md](09-decisions.md) #038

---

## SOLID principy

### S - Single Responsibility Principle

Každá třída má jednu odpovědnost.

```csharp
// ✅ Správně
public class ZaznamCreator { }
public class ZaznamValidator { }
public class ZaznamNotifier { }

// ❌ Špatně
public class ZaznamService
{
    void Create() { }
    void Validate() { }
    void SendNotification() { }
}
```

### O - Open/Closed Principle

Otevřené pro rozšíření, uzavřené pro modifikaci.

```csharp
// ✅ Správně - nový typ = nová třída
public interface IExportStrategy { }
public class PdfExportStrategy : IExportStrategy { }
public class CsvExportStrategy : IExportStrategy { }
```

### L - Liskov Substitution Principle

Podtřídy musí být zaměnitelné za rodičovské třídy.

### I - Interface Segregation Principle

Menší, specifické interfaces místo velkých obecných.

```csharp
// ✅ Správně
public interface IZaznamReader { }
public interface IZaznamWriter { }

// ❌ Špatně
public interface IZaznamRepository
{
    // 20 metod...
}
```

### D - Dependency Inversion Principle

Závislost na abstrakcích, ne na konkrétních implementacích.

```csharp
// ✅ Správně
public class ZaznamService(IZaznamRepository repository) { }

// ❌ Špatně
public class ZaznamService(SqlZaznamRepository repository) { }
```

---

## DRY (Don't Repeat Yourself)

- Extrahovat společnou logiku do shared services
- Používat generika kde dává smysl
- Centralizovat validační pravidla
- Shared DTOs pro podobné struktury

---

## Clean Code pravidla

### Naming

```csharp
// ✅ Správně
public class ZaznamCreationCommand { }
public async Task<Zaznam> CreateZaznamAsync() { }
private readonly ILogger _logger;

// ❌ Špatně
public class ZCC { }
public async Task<Zaznam> Create() { }
private readonly ILogger l;
```

### Metody

- ~20 řádků jako orientační guideline (ne hard limit)
- Max 3 parametry (jinak objekt)
- Jeden level abstrakce
- Early return pattern
- Důležitější je single responsibility a čitelnost než striktní počet řádků

```csharp
// ✅ Správně
public async Task<Result<Zaznam>> CreateAsync(CreateZaznamCommand command)
{
    if (!command.IsValid)
        return Result.Failure("Invalid command");

    var zaznam = Zaznam.Create(command);

    await _repository.AddAsync(zaznam);

    return Result.Success(zaznam);
}
```

### Třídy

- Max 200 řádků
- Single responsibility
- Favor composition over inheritance

### Komentáře

- Kód by měl být self-documenting
- Komentáře jen pro "proč", ne "co"
- XML docs pro public API

---

## Patterns

### Repository Pattern

```csharp
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

public interface IZaznamRepository : IRepository<Zaznam>
{
    Task<IEnumerable<Zaznam>> GetByPropertyIdAsync(Guid propertyId);
    Task<IEnumerable<Zaznam>> GetByDateRangeAsync(DateOnly from, DateOnly to);
}
```

### CQRS (Command Query Responsibility Segregation)

```csharp
// Command
public record CreateZaznamCommand(
    Guid PropertyId,
    Guid? UnitId,
    string Title,
    DateOnly Date,
    List<IFormFile> Photos
) : ICommand<Guid>;

// Query
public record GetZaznamByIdQuery(Guid Id) : IQuery<ZaznamDto>;

// Handlers
public class CreateZaznamHandler : ICommandHandler<CreateZaznamCommand, Guid> { }
public class GetZaznamByIdHandler : IQueryHandler<GetZaznamByIdQuery, ZaznamDto> { }
```

### Result Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

### Specification Pattern (pro queries)

```csharp
public class ZaznamByPropertySpec : Specification<Zaznam>
{
    public ZaznamByPropertySpec(Guid propertyId)
    {
        Query.Where(z => z.PropertyId == propertyId)
             .OrderByDescending(z => z.Date);
    }
}
```

---

## Knihovny (doporučené)

| Účel | Knihovna |
|------|----------|
| Validation | FluentValidation |
| Mapping | Mapster |
| CQRS | MediatR |
| Logging | Serilog |
| Testing | xUnit, FluentAssertions, NSubstitute |
| API Docs | Swagger / OpenAPI |
| Background jobs | Hangfire nebo Quartz.NET |

---

## Testování

### Struktura testů

```
tests/
├── MujDomecek.Domain.Tests/
├── MujDomecek.Application.Tests/
├── MujDomecek.Infrastructure.Tests/
├── MujDomecek.Api.Tests/
└── web/
    ├── unit/      # Vitest
    └── e2e/       # Playwright
```

### Test strategie per layer

| Vrstva | Typ | Co testovat | Mocking |
|--------|-----|-------------|---------|
| Domain | Unit | Entity invariants, value objects, domain events | Žádný (pure logic) |
| Application | Unit | Handlers, validators, mappers | NSubstitute |
| Infrastructure | Integration | EF repos, S3 client | TestContainers (Postgres) |
| API | Integration | Endpoints, auth, permissions | TestContainers |
| Web | Unit | Stores, utils, komponenty | Vitest + Testing Library |
| Web | E2E | Kritické flows (login, záznam, sync) | Playwright |

### Naming convention

```csharp
// MethodName_StateUnderTest_ExpectedBehavior
public void CreateZaznam_WithValidData_ReturnsSuccess() { }
public void CreateZaznam_WithoutTitle_ReturnsValidationError() { }
```

### Nástroje

| Účel | Nástroj |
|------|---------|
| .NET test framework | xUnit |
| Mocking | NSubstitute |
| Assertions | FluentAssertions |
| DB pro testy | TestContainers (Postgres) |
| Frontend unit | Vitest |
| E2E | Playwright |

### Coverage target

- Celkově: 70%
- CI blokuje merge pod 60%

---

## Code Quality

### Static Analysis

- .NET Analyzers (built-in)
- StyleCop Analyzers
- SonarQube (optional)

### EditorConfig

Jednotný coding style across team.

### CI/CD Checks

- Build must pass
- All tests must pass
- No analyzer warnings (treat as errors)
- Code coverage minimum (např. 80%)

---

## Git Conventions

### Branch naming

```
feature/zaznam-quick-create
bugfix/sync-conflict-resolution
refactor/repository-pattern
```

### Commit messages

```
feat: add quick zaznam creation
fix: resolve sync conflict on duplicate IDs
refactor: extract validation to separate service
docs: update architecture documentation
test: add unit tests for ZaznamService
```

---

## Rozhodnuto

- ✅ .NET 10 (LTS, release Nov 2025) - viz [12-dotnet10-index.md](12-dotnet10-index.md)
- ✅ Node 22 LTS
- ✅ Tailwind CSS + vlastní komponenty
