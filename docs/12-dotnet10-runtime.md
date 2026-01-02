# Runtime, BCL & Breaking Changes

[<< Zpět na index](12-dotnet10-index.md)

---

## Runtime & Performance

### JIT vylepšení

```csharp
// .NET 10: lepší codegen (struct args, loop inversion, code layout, inlining)
// + devirtualizace pro array interface metody a rychlejší enumerace polí

// Stack allocation pro malá pole - díky escape analysis může být automaticky na stacku
int[] smallValues = new int[16];
object?[] smallRefs = new object?[8];

// Lepší inlining - nic speciálního v kódu, JIT optimalizuje
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static int Add(int a, int b) => a + b;
```

### NativeAOT

```xml
<!-- V .csproj -->
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

### Post-quantum cryptography

```csharp
using System.Security.Cryptography;

// ML-DSA (digital signatures) - quantum-resistant
using var mlDsa = MLDsa.Create(MLDsaParameters.MLDsa65);
byte[] signature = mlDsa.SignData(data);
bool valid = mlDsa.VerifyData(data, signature);

// ML-KEM (key encapsulation) - quantum-resistant
using var mlKem = MLKem.Create(MLKemParameters.MLKem768);
var (encapsulation, sharedSecret) = mlKem.Encapsulate();
```

---

## .NET Libraries (BCL)

### System.Text.Json (bezpečnost + výkon)

```csharp
// Strict preset (read-only singleton) - vhodné pro ne-důvěryhodný input
var strict = JsonSerializerOptions.Strict;

// Duplicate JSON properties: defaultně "poslední vyhraje", lze zakázat (ochrana proti ambiguity / útokům)
string json = """{ "Value": 1, "Value": -1 }""";

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    AllowDuplicateProperties = false
};

JsonSerializer.Deserialize<Dictionary<string, int>>(json, options); // throws JsonException
```

Poznámky:
- `JsonSerializer` má nové overloady pro `PipeReader` (rychlejší cesta než převádět na `Stream`).
- `JsonSourceGenerationOptionsAttribute` nově podporuje `ReferenceHandler` (např. Preserve) i pro source-generated kontexty.

### Diagnostics (OpenTelemetry)

`ActivitySource`/`Meter` podporují schema URL a je k dispozici `ActivitySourceOptions` pro pohodlnější konstrukci.

```csharp
var source = new ActivitySource(new ActivitySourceOptions("MujDomecek")
{
    Version = "1.0.0",
    TelemetrySchemaUrl = "https://opentelemetry.io/schemas/1.27.0"
});
```

### ZIP + WebSockets (vybrané)

- ZIP: přibyly `async` API (`ZipFile.ExtractToDirectoryAsync`, `ZipFile.CreateFromDirectoryAsync`, ...).
- WebSockets: `WebSocketStream` přidává `Stream` abstrakci nad `WebSocket` (méně boilerplate pro streaming protokoly).

### TLS 1.3 na macOS (client, opt-in)

```csharp
AppContext.SetSwitch("System.Net.Security.UseNetworkFramework", true);
```

---

## Breaking Changes

### Důležité změny

| Změna | Dopad | Řešení |
|-------|-------|--------|
| Container images na Ubuntu | Dockerfile může potřebovat úpravy | Aktualizovat base image |
| OpenAPI 3.1 je default + OpenAPI.NET 2 | Transformery (`OpenApiAny` pryč, typy často jako `IOpenApi*`) | Přepsat na `JsonNode` + upravit práci se schématy |
| Cookie auth pro known API endpoints neredirectuje | API vrací 401/403 místo redirectu | Pokud chceš redirect, přepiš `OnRedirectToLogin/OnRedirectToAccessDenied` |
| Validace Minimal APIs: balíček/namespace move | Přesun do `Microsoft.Extensions.Validation` | Obvykle bez změn (stará reference se přesměruje) |
| ASP.NET JSON parsing přes PipeReader | Custom `JsonConverter` může selhat na `HasValueSequence` | Opravit konvertor / dočasně `Microsoft.AspNetCore.UseStreamBasedJsonParsing=true` |
| NuGet package pruning (>= net10) | Méně restored packages / jiný `.deps.json` | Vypnout `RestoreEnablePackagePruning=false` pokud potřeba |
| File-based apps: publish default NativeAOT | Některé balíčky nejsou AOT/trim safe | `#:property PublishAot=false` (file-based app) / `PublishAot` v `.csproj` |
| `IActionContextAccessor` obsolete | Compilation warning | Použít alternativy |
| Razor runtime compilation obsolete | Nelze kompilovat za běhu | Precompilace |
| `WebHostBuilder` obsolete | Starý hosting model | Použít `WebApplicationBuilder` |
| EF: nvarchar(max) JSON → json typ | Automatická migrace | Nic, proběhne automaticky |

---

## Konfigurace projektu

### csproj

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

### global.json

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```
