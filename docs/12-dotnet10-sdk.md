# .NET SDK & Tooling

[<< Zpět na index](12-dotnet10-index.md)

---

## 1. File-based apps (single-file, bez `.csproj`)

Spuštění `.cs` souboru přímo bez projektu:

```bash
dotnet run app.cs
dotnet run app.cs -- arg1 arg2

dotnet build app.cs -c Release
dotnet publish app.cs -c Release -r win-x64 --self-contained

dotnet restore app.cs
dotnet project convert app.cs
```

**Direktivy v souboru (`#:`):**

```csharp
#!/usr/bin/env dotnet

#:sdk Microsoft.NET.Sdk
#:package Newtonsoft.Json@13.0.3
#:property Nullable=enable
#:property LangVersion=14
#:property PublishAot=false // publish je v .NET 10 defaultně NativeAOT, toto ho vypne

using Newtonsoft.Json;

Console.WriteLine(JsonConvert.SerializeObject(new { Name = "Test" }));
```

**Reference na project (bez NuGet):**

```csharp
#:project ../ClassLib/ClassLib.csproj
```

**Cache pro file-based apps:**

```bash
dotnet clean file-based-apps
```

Poznámky:
- Pro shebang používej `LF` line endings a soubor bez BOM.
- File-based apps respektují `global.json`, `nuget.config`, `Directory.Build.*`, `Directory.Packages.props` v parent adresářích.

---

## 2. One-shot .NET tools: `dotnet tool exec` + `dnx`

```bash
# Spustí tool bez instalace (stáhne a provede)
dotnet tool exec dotnetsay "Hello, World!"

# Kratší wrapper (forwarduje na dotnet CLI)
dnx dotnetsay "Hello, World!"
```

---

## 3. CLI novinky (kvalita života)

```bash
# CLI introspection (machine-readable popis příkazů/argumentů)
dotnet clean --cli-schema

# Noun-first aliasy (verb-first formy pořád fungují)
dotnet package add Some.Package
dotnet reference add ..\\Other\\Other.csproj

# Tab-completion (PowerShell) do $PROFILE
dotnet completions script pwsh | Out-String | Invoke-Expression
```

Poznámka: v interaktivních terminálech je `--interactive` defaultně zapnuté; pro CI použij `--interactive false`.

---

## 4. Container publish i pro console apps

```bash
dotnet publish -c Release /t:PublishContainer
```

```xml
<!-- V .csproj -->
<PropertyGroup>
  <ContainerImageFormat>OCI</ContainerImageFormat>
</PropertyGroup>
```

---

## 5. `dotnet test` + Microsoft Testing Platform (MTP)

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

---

## 6. Pruning framework package references (NuGet Audit)

.NET 10 umí automaticky odstranit nepoužité framework-provided package reference (rychlejší restore, méně "false positives" ve skenech).

```xml
<!-- Vypnutí, pokud by způsobovalo problémy -->
<PropertyGroup>
  <RestoreEnablePackagePruning>false</RestoreEnablePackagePruning>
</PropertyGroup>
```
