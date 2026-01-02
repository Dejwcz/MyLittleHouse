# Příklady pro MujDomecek

[<< Zpět na index](12-dotnet10-index.md)

---

## Entity s C# 14 features

```csharp
public class Zaznam
{
    public Guid Id { get; init; }

    // Field keyword pro validaci
    public string Title
    {
        get;
        set => field = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
    }

    public string? Description
    {
        get;
        set => field = value?.Trim();
    }

    // Auto-flag při změně
    public ZaznamFlags Flags
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    public decimal? Price
    {
        get;
        set => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
}
```

---

## Minimal API endpoint

```csharp
app.MapPost("/api/zaznamy", async (
    CreateZaznamRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new CreateZaznamCommand(
        request.PropertyId,
        request.UnitId,
        request.Title,
        request.Description,
        request.Price,
        request.Date);

    var result = await mediator.Send(command, ct);

    return result.Match(
        zaznam => Results.Created($"/api/zaznamy/{zaznam.Id}", zaznam),
        error => Results.BadRequest(error));
})
.WithValidation<CreateZaznamRequest>()
.RequireAuthorization()
.RequireRateLimiting("sync")
.WithOpenApi(operation =>
{
    operation.Summary = "Vytvoří nový záznam";
    operation.Description = "Vytvoří nový záznam pro danou nemovitost nebo jednotku";
    return operation;
});
```

---

## EF Core repository s novými features

```csharp
public class ZaznamRepository : IZaznamRepository
{
    private readonly AppDbContext _context;

    public async Task<List<ZaznamDto>> GetByPropertyAsync(
        Guid propertyId,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var query = _context.Zaznamy
            .Where(z => z.PropertyId == propertyId);

        if (includeDeleted)
            query = query.IgnoreQueryFilters(["SoftDelete"]);

        return await query
            .LeftJoin(
                _context.ZaznamDokumenty,
                z => z.Id,
                d => d.ZaznamId,
                (z, d) => new { Zaznam = z, Dokument = d })
            .GroupBy(x => x.Zaznam)
            .Select(g => new ZaznamDto(
                g.Key.Id,
                g.Key.Title,
                g.Key.Description,
                g.Key.Date,
                g.Key.Price,
                g.Count(x => x.Dokument != null)))
            .ToListAsync(ct);
    }

    public async Task BulkUpdateStatusAsync(
        Guid propertyId,
        ZaznamStatus newStatus,
        CancellationToken ct = default)
    {
        await _context.Zaznamy
            .Where(z => z.PropertyId == propertyId)
            .ExecuteUpdateAsync(setters =>
            {
                setters.SetProperty(z => z.Status, newStatus);
                setters.SetProperty(z => z.UpdatedAt, DateTime.UtcNow);
            }, ct);
    }
}
```

---

## Zdroje

- [What's new in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [What's new in .NET 10 - Runtime](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/runtime)
- [What's new in .NET 10 - Libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/libraries)
- [What's new in .NET 10 - SDK](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/sdk)
- [File-based apps](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps)
- [What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [C# 14 extension members (`extension`)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/extension)
- [What's new in ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0)
- [What's new in EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew)
- [Breaking changes in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0)
- [C# 14 field keyword](https://www.thomasclaudiushuber.com/2025/10/22/csharp-14-the-field-keyword/)
- [EF Core 10 LeftJoin/RightJoin](https://www.milanjovanovic.tech/blog/whats-new-in-ef-core-10-leftjoin-and-rightjoin-operators-in-linq)
