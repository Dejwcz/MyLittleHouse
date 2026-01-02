# EF Core 10

[<< Zpět na index](12-dotnet10-index.md)

---

## 1. LeftJoin a RightJoin

```csharp
// ========== STARÉ (EF Core 9 a dříve) ==========
var query = context.Properties
    .GroupJoin(
        context.Zaznamy,
        p => p.Id,
        z => z.PropertyId,
        (p, zaznamy) => new { Property = p, Zaznamy = zaznamy })
    .SelectMany(
        x => x.Zaznamy.DefaultIfEmpty(),
        (x, z) => new { x.Property, Zaznam = z });

// ========== NOVÉ (EF Core 10) ==========
var query = context.Properties
    .LeftJoin(
        context.Zaznamy,
        p => p.Id,
        z => z.PropertyId,
        (p, z) => new { Property = p, Zaznam = z });

// SQL: SELECT ... FROM Properties p LEFT JOIN Zaznamy z ON p.Id = z.PropertyId
```

**Komplexnější příklady:**

```csharp
// LeftJoin s podmínkou
var propertiesWithRecentZaznamy = context.Properties
    .LeftJoin(
        context.Zaznamy.Where(z => z.Date >= DateOnly.FromDateTime(DateTime.Today.AddMonths(-1))),
        p => p.Id,
        z => z.PropertyId,
        (p, z) => new
        {
            PropertyName = p.Name,
            ZaznamTitle = z != null ? z.Title : "No recent records",
            ZaznamDate = z != null ? z.Date : (DateOnly?)null
        });

// RightJoin
var zaznamyWithOptionalProperty = context.Zaznamy
    .RightJoin(
        context.Properties,
        z => z.PropertyId,
        p => p.Id,
        (z, p) => new { Property = p, Zaznam = z });

// Multiple joins
var fullData = context.Projects
    .LeftJoin(context.Properties, proj => proj.Id, p => p.ProjectId, (proj, p) => new { proj, p })
    .LeftJoin(context.Zaznamy, x => x.p.Id, z => z.PropertyId, (x, z) => new
    {
        ProjectName = x.proj.Name,
        PropertyName = x.p != null ? x.p.Name : null,
        ZaznamTitle = z != null ? z.Title : null
    });
```

**Best Practice - Null handling v projekci:**

```csharp
// ========== ŠPATNĚ - může způsobit NullReferenceException ==========
var query = context.Products
    .LeftJoin(
        context.Reviews,
        p => p.Id,
        r => r.ProductId,
        (p, r) => new
        {
            p.Name,
            Rating = r.Rating,        // r může být null!
            Comment = r.Comment       // r může být null!
        });

// ========== SPRÁVNĚ - explicitní null handling ==========
var query = context.Products
    .LeftJoin(
        context.Reviews,
        p => p.Id,
        r => r.ProductId,
        (p, r) => new
        {
            p.Name,
            Rating = (int?)r.Rating ?? 0,           // Fallback na 0
            Comment = r.Comment ?? "Bez recenze",   // Fallback text
            HasReview = r != null                   // Boolean flag
        });

// ========== Pro DTO s nullable properties ==========
var query = context.Products
    .LeftJoin(
        context.Reviews,
        p => p.Id,
        r => r.ProductId,
        (p, r) => new ProductWithReviewDto
        {
            ProductId = p.Id,
            ProductName = p.Name,
            ReviewRating = r != null ? r.Rating : null,  // Nullable int?
            ReviewComment = r != null ? r.Comment : null  // Nullable string?
        });
```

**Poznámka:** C# query syntax (`from ... select ...`) zatím nepodporuje `left join` keyword. Používejte method syntax.

---

## 2. Named Query Filters

```csharp
// ========== Model Builder konfigurace ==========
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // STARÉ - jeden filtr
    // modelBuilder.Entity<Zaznam>().HasQueryFilter(z => !z.IsDeleted);

    // NOVÉ - pojmenované filtry
    modelBuilder.Entity<Zaznam>()
        .HasQueryFilter("SoftDelete", z => !z.IsDeleted)
        .HasQueryFilter("TenantFilter", z => z.TenantId == _tenantId)
        .HasQueryFilter("ActiveOnly", z => z.Status == ZaznamStatus.Active);

    modelBuilder.Entity<Property>()
        .HasQueryFilter("SoftDelete", p => !p.IsDeleted)
        .HasQueryFilter("TenantFilter", p => p.TenantId == _tenantId);
}

// ========== Použití ==========

// Všechny filtry aktivní (default)
var activeZaznamy = context.Zaznamy.ToList();

// Vypnout konkrétní filtr
var allZaznamy = context.Zaznamy
    .IgnoreQueryFilters(["SoftDelete"])
    .ToList();

// Vypnout více filtrů
var allTenantZaznamy = context.Zaznamy
    .IgnoreQueryFilters(["SoftDelete", "TenantFilter"])
    .ToList();

// Vypnout všechny filtry (jako dříve)
var absolutelyAll = context.Zaznamy
    .IgnoreQueryFilters()
    .ToList();

// Užitečné pro admin dashboard
public async Task<List<ZaznamDto>> GetAllZaznamyForAdmin()
{
    return await _context.Zaznamy
        .IgnoreQueryFilters(["TenantFilter"])  // Admin vidí všechny tenanty
        // SoftDelete filtr zůstává aktivní
        .Select(z => new ZaznamDto(z.Id, z.Title, z.Date))
        .ToListAsync();
}
```

---

## 3. JSON columns pro complex types

```csharp
// ========== Entity ==========
public class Zaznam
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public ZaznamMetadata Metadata { get; set; }  // Uloží se jako JSON
    public List<ZaznamTag> Tags { get; set; }     // Kolekce v JSON
}

public class ZaznamMetadata
{
    public string? Source { get; set; }
    public string? DeviceInfo { get; set; }
    public Dictionary<string, string> CustomFields { get; set; } = new();
    public GeoLocation? Location { get; set; }
}

public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class ZaznamTag
{
    public string Name { get; set; }
    public string Color { get; set; }
}

// ========== Konfigurace ==========
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Zaznam>(entity =>
    {
        // Varianta A (EF Core 8+): owned entities do JSON (ToJson)
        entity.OwnsOne(z => z.Metadata, metadata =>
        {
            metadata.ToJson();  // Uloží jako JSON column
            metadata.OwnsOne(m => m.Location);
        });

        entity.OwnsMany(z => z.Tags, tags =>
        {
            tags.ToJson();  // Kolekce jako JSON array
        });
    });

    // Varianta B (EF Core 10): complex types do JSON (doporučeno; nutné pro ExecuteUpdate nad JSON properties)
    // modelBuilder.Entity<Zaznam>()
    //     .ComplexProperty(z => z.Metadata, metadata => metadata.ToJson());
}

// ========== Query s JSON ==========
// Filtrování podle JSON property
var zaznamyWithLocation = context.Zaznamy
    .Where(z => z.Metadata.Location != null)
    .ToList();

// Filtrování podle nested property
var pragueZaznamy = context.Zaznamy
    .Where(z => z.Metadata.Location.Latitude > 50.0)
    .ToList();

// Filtrování v kolekci
var electricZaznamy = context.Zaznamy
    .Where(z => z.Tags.Any(t => t.Name == "Elektrika"))
    .ToList();
```

---

## 4. ExecuteUpdate/ExecuteDelete zjednodušení

```csharp
// ========== STARÉ (EF Core 9) ==========
await context.Zaznamy
    .Where(z => z.PropertyId == propertyId)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(z => z.UpdatedAt, DateTime.UtcNow)
        .SetProperty(z => z.Status, ZaznamStatus.Archived));

// ========== NOVÉ (EF Core 10) - setter lambda nemusí být expression tree ==========
await context.Zaznamy
    .Where(z => z.PropertyId == propertyId)
    .ExecuteUpdateAsync(setters =>
    {
        setters.SetProperty(z => z.UpdatedAt, DateTime.UtcNow);
        setters.SetProperty(z => z.Status, ZaznamStatus.Archived);

        // Podmíněné settery (dříve velmi nepříjemné)
        // if (archiveAlsoFlags) setters.SetProperty(z => z.Flags, ZaznamFlags.Archived);
    });

// Bulk delete
await context.Zaznamy
    .Where(z => z.IsDeleted && z.DeletedAt < DateTime.UtcNow.AddYears(-1))
    .ExecuteDeleteAsync();

// S JSON columns (vyžaduje mapování JSON jako complex type, ne owned entity)
await context.Zaznamy
    .Where(z => z.Id == zaznamId)
    .ExecuteUpdateAsync(setters =>
        setters.SetProperty(z => z.Metadata.Source, "Updated"));
```

---

## 5. Vector search (SQL Server 2025 / Azure SQL)

```csharp
// ========== Entity ==========
public class Document
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public float[] Embedding { get; set; }  // Vector
}

// ========== Konfigurace ==========
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Document>(entity =>
    {
        entity.Property(d => d.Embedding)
            .IsVectorProperty(dimensions: 1536);  // OpenAI embedding size

        entity.HasIndex(d => d.Embedding)
            .IsVectorIndex();  // HNSW index
    });
}

// ========== Queries ==========
float[] queryVector = await _embeddingService.GetEmbedding("search query");

// Semantic search
var similar = await context.Documents
    .OrderBy(d => EF.Functions.VectorDistance(d.Embedding, queryVector))
    .Take(10)
    .ToListAsync();

// S minimální podobností
var relevant = await context.Documents
    .Where(d => EF.Functions.VectorDistance(d.Embedding, queryVector) < 0.5f)
    .OrderBy(d => EF.Functions.VectorDistance(d.Embedding, queryVector))
    .ToListAsync();
```

---

## 6. Security-related improvements (logování + SQL injection)

- EF Core 10 už při logování SQL standardně rediguje *inlined* konstanty (nahradí je `?`) – menší riziko úniku citlivých hodnot do logů.
- Přibyl analyzér, který varuje při string concatenation uvnitř `FromSqlRaw(...)` a podobných "raw SQL" API (pomáhá proti SQL injection).

```csharp
// Preferuj parametrizaci (FormattableString / interpolated varianta)
var users = await context.Users
    .FromSqlInterpolated($"SELECT * FROM Users WHERE [Name] = {name}")
    .ToListAsync();

// Pokud skládáš dynamické SQL fragmenty, sanitizuj a potlač warning jen pokud je to bezpečné.
```
