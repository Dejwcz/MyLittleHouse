# ASP.NET Core 10

[<< Zpět na index](12-dotnet10-index.md)

---

## 1. Minimal API - Built-in validace

```csharp
// ========== Konfigurace ==========
var builder = WebApplication.CreateBuilder(args);

// Přidat validaci
builder.Services.AddValidation();

var app = builder.Build();

// ========== Endpoint s validací ==========
app.MapPost("/api/zaznamy", async (CreateZaznamRequest request, IZaznamService service) =>
{
    var result = await service.CreateAsync(request);
    return result.IsSuccess
        ? Results.Created($"/api/zaznamy/{result.Value.Id}", result.Value)
        : Results.BadRequest(result.Error);
})
.WithValidation<CreateZaznamRequest>();  // Automatická validace

// ========== Request s DataAnnotations ==========
public record CreateZaznamRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Title,

    [Required]
    Guid PropertyId,

    Guid? UnitId,

    [Range(0, 10_000_000)]
    decimal? Price,

    [StringLength(5000)]
    string? Description,

    DateOnly Date = default
)
{
    public DateOnly Date { get; init; } = Date == default ? DateOnly.FromDateTime(DateTime.Today) : Date;
}

// ========== Vlastní validátor (FluentValidation styl) ==========
public class CreateZaznamRequestValidator : IValidator<CreateZaznamRequest>
{
    public ValueTask<ValidationResult> ValidateAsync(CreateZaznamRequest instance, CancellationToken ct)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Title))
            errors.Add(new("Title", "Title is required"));

        if (instance.PropertyId == Guid.Empty)
            errors.Add(new("PropertyId", "PropertyId must be a valid GUID"));

        return ValueTask.FromResult(errors.Count == 0
            ? ValidationResult.Success
            : ValidationResult.Failed(errors));
    }
}

// ========== Vypnutí validace pro specifické endpointy ==========
app.MapPost("/api/internal/import", ImportHandler)
    .DisableValidation();  // Interní endpoint bez validace

// ========== Validace query a header parametrů ==========
app.MapGet("/api/products",
    ([Range(1, 100)] int pageSize,
     [Required] string category,
     [FromHeader(Name = "X-Tenant-Id")][Required] string tenantId) =>
    {
        // Parametry jsou automaticky validovány
        return Results.Ok();
    });

// ========== Validace pro records (AOT-compatible) ==========
[ValidatableType]
public record CreateProductRequest(
    [Required][StringLength(200)] string Name,
    [Range(0.01, 1_000_000)] decimal Price,
    [Required] Guid CategoryId
);
```

Poznámky (.NET 10):
- Validace API se přesunula do balíčku `Microsoft.Extensions.Validation` (namespace/veřejné API zůstává, staré reference se přesměrují).
- Validační chyby lze sjednotit přes vlastní implementaci `IProblemDetailsService`.
- Při `[FromForm]` bindování komplexního typu se prázdný string mapuje na `null` pro nullable value types (`DateOnly?`, `int?`, ...).

```csharp
app.MapPost("/todo", ([FromForm] Todo todo) => TypedResults.Ok(todo));

public sealed class Todo
{
    public DateOnly? DueDate { get; set; } // "" -> null
}
```

---

## 2. OpenAPI 3.1 + YAML

```csharp
var builder = WebApplication.CreateBuilder(args);

// ========== Konfigurace OpenAPI 3.1 ==========
builder.Services.AddOpenApi(options =>
{
    // Verze OpenAPI
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;

    // Document transformer
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title = "MujDomecek API",
            Version = "v1",
            Description = "API pro správu nemovitostí",
            Contact = new() { Email = "support@mujdomecek.cz" }
        };
        return Task.CompletedTask;
    });

    // Schema transformer
    options.AddSchemaTransformer((schema, context, ct) =>
    {
        // Přidat příklady, popisy atd.
        if (context.JsonTypeInfo.Type == typeof(ZaznamDto))
        {
            schema.Description = "Záznam o údržbě nebo opravě";
        }
        return Task.CompletedTask;
    });

    // Operation transformer
    options.AddOperationTransformer((operation, context, ct) =>
    {
        // Přidat security requirements
        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new() { [new OpenApiSecurityScheme { Reference = new() { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>() }
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// ========== Endpointy pro OpenAPI ==========
app.MapOpenApi();                              // /openapi/v1.json (default)
app.MapOpenApi("/openapi.yaml", "yaml");       // YAML výstup
app.MapOpenApi("/openapi/{version}.json");     // S verzí v URL

// ========== Scalar UI (náhrada za Swagger UI) ==========
app.MapScalarApiReference();  // /scalar/v1
```

**OpenAPI 3.1 změny v schématu:**

```csharp
// Nullable typy - NOVÉ chování
public record ZaznamDto(
    Guid Id,
    string Title,
    string? Description,  // V OpenAPI 3.1: type: ["string", "null"]
    decimal? Price        // V OpenAPI 3.1: type: ["number", "null"]
);

// STARÉ (OpenAPI 3.0): nullable: true
// NOVÉ (OpenAPI 3.1): type: ["string", "null"]
```

**Doplňky a tipy (.NET 10):**

```csharp
// OpenAPI 3.1 je v .NET 10 default.
// Pokud chceš aby `int`/`long` byly v OpenAPI jako `type: integer` (místo `pattern`), nastav strict number handling:
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
```

```xml
<!-- XML doc comments se umí propsat do OpenAPI (přes source generator) -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

Poznámky:
- XML doc comments nelze v C# získat z lambda expression; pro Minimal API endpointy použij method handler (např. `app.MapGet("/x", HandlerMethod);`).
- V .NET 10 lze injectovat `IOpenApiDocumentProvider` z DI a generovat/číst dokument i mimo HTTP request (např. background service).
- `ProducesResponseTypeAttribute` a příbuzné atributy mají nově `Description` pro přesnější OpenAPI response popisy.

---

## 3. Autentizace - změny

```csharp
// ========== Cookie auth - nové chování pro API ==========
// API endpointy už NEREDIREKTUJÍ na login, vrací 401/403

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;

    // .NET 10: cookie auth pro "known API endpoints" (IApiEndpointMetadata) automaticky vrací 401/403 místo redirectu.
    // Pokud chceš vždy redirect (i pro API), přepiš events:
    // options.Events.OnRedirectToLogin = ctx => { ctx.Response.Redirect(ctx.RedirectUri); return Task.CompletedTask; };
    // options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.Redirect(ctx.RedirectUri); return Task.CompletedTask; };
});

// ========== JWT + Refresh token ==========
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// ========== OIDC (Google, Apple) ==========
builder.Services.AddAuthentication()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Auth:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Auth:Google:ClientSecret"]!;
    options.Scope.Add("email");
    options.Scope.Add("profile");
})
.AddApple(options =>
{
    options.ClientId = builder.Configuration["Auth:Apple:ClientId"]!;
    options.TeamId = builder.Configuration["Auth:Apple:TeamId"]!;
    options.KeyId = builder.Configuration["Auth:Apple:KeyId"]!;
    options.PrivateKey = (keyId, _) =>
        Task.FromResult(builder.Configuration["Auth:Apple:PrivateKey"]!.AsMemory());
});

// ========== Passkeys (WebAuthn) - NOVÉ ==========
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddPasskeys();  // WebAuthn support
```

**Nové v .NET 10:** ASP.NET Core má built-in metriky pro autentizaci/autorizaci (např. challenge/forbid/sign-in/out, duration), použitelné v observability stacku (OpenTelemetry/Aspire).

---

## 4. Rate limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    // Globální limiter
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Named policies
    options.AddSlidingWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(15);
        opt.SegmentsPerWindow = 3;
    });

    options.AddSlidingWindowLimiter("sync", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6;
    });

    // Rejection response
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests",
            retryAfter = retryAfter.TotalSeconds
        }, ct);
    };
});

var app = builder.Build();
app.UseRateLimiter();

// Použití na endpointech
app.MapPost("/api/auth/login", Login).RequireRateLimiting("auth");
app.MapPost("/api/sync/push", SyncPush).RequireRateLimiting("sync");
```

---

## 5. Server-Sent Events (SSE)

Real-time streaming bez WebSocketů - ideální pro dashboardy, notifikace a live updates:

```csharp
// ========== Endpoint pro SSE ==========
app.MapGet("/api/stock-prices", (CancellationToken ct) =>
    TypedResults.ServerSentEvents(GetStockPrices(ct)));

app.MapGet("/api/notifications", (CancellationToken ct) =>
    TypedResults.ServerSentEvents(GetNotifications(ct)));

// ========== Generator pro SSE ==========
async IAsyncEnumerable<SseItem<StockPrice>> GetStockPrices(
    [EnumeratorCancellation] CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        var price = new StockPrice("MSFT", Random.Shared.Next(300, 400));
        yield return new SseItem<StockPrice>(price);
        await Task.Delay(2000, ct);  // Update každé 2 sekundy
    }
}

// ========== S event type a ID ==========
async IAsyncEnumerable<SseItem<Notification>> GetNotifications(
    [EnumeratorCancellation] CancellationToken ct)
{
    var id = 0;
    while (!ct.IsCancellationRequested)
    {
        var notification = await _notificationService.GetNextAsync(ct);
        yield return new SseItem<Notification>(notification)
        {
            EventType = notification.Type,  // "message", "alert", etc.
            EventId = (++id).ToString()
        };
    }
}

// ========== Klient (JavaScript) ==========
// const source = new EventSource('/api/stock-prices');
// source.onmessage = (e) => console.log(JSON.parse(e.data));
```

---

## 6. Blazor

### Circuit State Persistence

Řeší problém ztráty stavu při přerušení WebSocket spojení:

```csharp
// ========== Automatická perzistence s atributem ==========
@code {
    // Stav se automaticky uloží a obnoví při reconnect
    [PersistentState]
    public List<Movie>? Movies { get; set; }

    // S povolením aktualizací během enhanced navigation
    [PersistentState(AllowUpdates = true)]
    public UserPreferences? Preferences { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Null-coalescing - načte jen pokud není obnoveno z persistence
        Movies ??= await MovieService.GetAllAsync();
        Preferences ??= await PreferencesService.GetAsync();
    }
}
```

```javascript
// ========== JavaScript API pro manuální kontrolu ==========
// Pauza před opuštěním stránky (např. při přepnutí tabu)
document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
        Blazor.pause();  // Uloží stav do browser storage
    }
});

// Obnovení při návratu
window.addEventListener('focus', () => {
    Blazor.resume();  // Obnoví stav i po eviction na serveru
});
```

### JavaScript Interop - konstruktory

```csharp
// ========== STARÉ - volání přes wrapper funkci ==========
// V JS: window.createChart = (el, config) => new Chart(el, config);
var chart = await JS.InvokeAsync<IJSObjectReference>("createChart", canvas, config);

// ========== NOVÉ - přímé volání konstruktoru ==========
var chart = await JS.InvokeConstructorAsync<IJSObjectReference>(
    "Chart", canvasElement, chartConfig);

// Přístup k properties
var currentValue = await chart.GetValueAsync<int>("currentValue");

// Volání metod
await chart.InvokeVoidAsync("update");
```

### Validace Source Generator (AOT-compatible)

```csharp
// Reflexe nahrazena kompilací - funguje s AOT
[ValidatableType]
public class OrderForm
{
    [Required(ErrorMessage = "Jméno je povinné")]
    [StringLength(100)]
    public string CustomerName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Range(1, 1000)]
    public int Quantity { get; set; }
}
```

### NotFound navigace

```csharp
@code {
    [Parameter] public Guid ProductId { get; set; }
    private Product? _product;

    protected override async Task OnInitializedAsync()
    {
        _product = await ProductService.GetByIdAsync(ProductId);

        if (_product is null)
        {
            Nav.NotFound();  // Elegantní 404 handling
            return;
        }
    }
}
```

### Performance

- **Bundle size**: `blazor.web.js` zmenšen z ~183 KB na ~43 KB (76% redukce)
- **Boot manifest**: Vložen do `dotnet.js` (o 1 HTTP request méně)
- **Asset preloading**: Automatické Link headers pro rychlejší načítání

---

## 7. Misc (error handling, redirect safety, JSON parsing)

**Exception handler diagnostika:** lze potlačit log/telemetry pro vybrané scénáře přes `ExceptionHandlerOptions.SuppressDiagnosticsCallback`.

```csharp
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = ctx =>
        ctx.Exception is OperationCanceledException // typicky noise při disconnectu
});
```

**Bezpečné redirecty (open redirect defense):**

```csharp
if (RedirectHttpResult.IsLocalUrl(url))
{
    return Results.LocalRedirect(url);
}

return Results.BadRequest("Invalid redirect url.");
```

**Json + PipeReader (dopad na custom `JsonConverter`):** MVC/Minimal APIs/`ReadFromJsonAsync` používají PipeReader; custom konvertory musí korektně pracovat s `Utf8JsonReader.HasValueSequence`.

```csharp
public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
    var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
    // ... původní parsing logika nad `span` ...
}
```

Workaround (dočasně): `AppContext.SetSwitch("Microsoft.AspNetCore.UseStreamBasedJsonParsing", true);`
