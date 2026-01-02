# .NET 10 Reference Guide

Referenční dokumentace pro AI asistenty. .NET 10 (LTS) vyšel 11. listopadu 2025, podpora do listopadu 2028.

**Používej tyto vzory při generování kódu pro projekt MujDomecek.**

---

## Quick Reference

| Téma | Soubor | Klíčová slova |
|------|--------|---------------|
| C# 14 | [12-dotnet10-csharp14.md](12-dotnet10-csharp14.md) | `field`, `extension`, null-conditional assignment, partial members, Span |
| SDK & Tooling | [12-dotnet10-sdk.md](12-dotnet10-sdk.md) | file-based apps, `dotnet run file.cs`, `dnx`, container publish |
| ASP.NET Core 10 | [12-dotnet10-aspnet.md](12-dotnet10-aspnet.md) | validace, OpenAPI 3.1, SSE, Blazor, `[PersistentState]` |
| EF Core 10 | [12-dotnet10-efcore.md](12-dotnet10-efcore.md) | `LeftJoin`, `RightJoin`, named query filters, JSON columns |
| Runtime & BCL | [12-dotnet10-runtime.md](12-dotnet10-runtime.md) | JIT, NativeAOT, `JsonSerializerOptions.Strict`, breaking changes |
| Příklady | [12-dotnet10-examples.md](12-dotnet10-examples.md) | MujDomecek entity, API endpoints, repository |

---

## Nejčastější vzory

### C# 14 - field keyword
```csharp
public string Name
{
    get;
    set => field = value ?? throw new ArgumentNullException(nameof(value));
}
```

### EF Core 10 - LeftJoin
```csharp
var query = context.Products
    .LeftJoin(context.Reviews, p => p.Id, r => r.ProductId,
        (p, r) => new { p.Name, Rating = (int?)r.Rating ?? 0 });
```

### ASP.NET Core 10 - Validace
```csharp
builder.Services.AddValidation();
app.MapPost("/api/items", Handler).WithValidation<CreateItemRequest>();
```

### ASP.NET Core 10 - SSE
```csharp
app.MapGet("/api/events", (CancellationToken ct) =>
    TypedResults.ServerSentEvents(GetEventsAsync(ct)));
```

---

## Konfigurace projektu

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

```json
// global.json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

---

## Zdroje

- [What's new in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [What's new in ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0)
- [What's new in EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew)
- [Breaking changes in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0)
