# Bezpečnost

## Autentizace

### Současný stav

- ASP.NET Core Identity
- Email + heslo
- Email confirmation
- Password reset

### Požadavky na heslo

- Min 6 znaků
- Velké písmeno
- Malé písmeno
- Číslice

### Local-first (rozhodnuto)

- **Auth:** ASP.NET Identity + JWT access tokeny
- **Externí login:** Google/Apple jako OIDC poskytovatelé (vlastní OIDC server neprovozujeme)
- **Access token:** 15 min, pouze v paměti (ne localStorage)
- **Refresh token:** 7 dní, HttpOnly cookie, rotace při každém refresh, revokace při logoutu
- **Refresh endpoint:** `POST /auth/refresh` vrací nový access + nastaví novou refresh cookie
- **CSRF ochrana refresh:** SameSite + kontrola `Origin`/`Referer` (nebo CSRF token)
- **Offline chování:** login/refresh vyžaduje online; offline read/write OK, po otevření app se zkusí refresh, při selhání se vyžádá login

---

## Autorizace

### Kontroly vlastnictví

Každá operace musí ověřit:

1. **Property** - patří uživateli nebo má sdílený přístup
2. **Unit** - patří k property, ke které má uživatel přístup
3. **Zaznam** - patří k property/unit s přístupem
4. **Dokument** - patří k záznamu s přístupem

### Service layer

Centralizovat pravidla přístupu v service vrstvě:

```csharp
// Příklad
bool CanAccess(User user, Property property)
{
    return property.OwnerId == user.Id
        || HasSharedAccess(user, property);
}
```

### Anti-patterns (vyhnout se)

- ❌ Kontrola pouze v controlleru
- ❌ Kontrola pouze na frontendu
- ❌ Spoléhat na "security through obscurity"

---

## Validace

### Server-side (povinné)

| Pole | Validace |
|------|----------|
| Název | Required, max 200 znaků |
| Popis | Max 5000 znaků |
| Email | Formát emailu |
| Datum | Valid date, ne v budoucnosti? |
| Cena | >= 0 |

### Client-side (UX)

- Stejné validace jako server
- Okamžitá zpětná vazba
- Nikdy nespoléhat pouze na klienta

---

## Upload souborů

### Limity

| Parametr | Hodnota |
|----------|---------|
| Max velikost (foto) | 10 MB |
| Max velikost (PDF) | 20 MB |
| Povolené typy (foto) | JPEG, PNG, HEIC, WebP |
| Povolené typy (PDF) | PDF |
| Max počet na záznam | Tier-based (Free/Premium), konfigurovatelné v `Constraints.Files.*`, viz rozhodnutí #028 |

Pozn.: Limity jsou konfigurovatelné v `appsettings.json` (viz Q29):
- `Constraints.Files.MaxPhotoSizeBytes` (default 10 MB)
- `Constraints.Files.MaxDocumentSizeBytes` (default 20 MB)
- `Constraints.Files.MaxPhotosPerZaznamFree` / `Constraints.Files.MaxPhotosPerZaznamPremium`

Runtime změny: hodnoty lze přepsat za běhu přes Admin panel (`/admin/settings`) a uložit do DB (`AppSetting`); fallback je `appsettings.json` a hardcoded defaults.

### Bezpečnost

- Whitelist MIME types
- Whitelist extensions
- Validace skutečného obsahu (magic bytes)
- Unikátní názvy souborů (UUID)
- Ukládání do S3 (StorageKey), přístup přes imgproxy/presigned URLs (viz rozhodnutí #017)

### Malware

- Nekontrolujeme aktivně (mimo scope)
- Soubory se nepouštějí na serveru

---

## API bezpečnost

### Rate limiting

**Strategie (VP):** sliding window, konfigurovatelné v `appsettings.json` (viz Q30).

**Klíčování:**
- Anonymous endpointy (login/register/forgot password): per IP (s ohledem na proxy headers).
- Auth endpointy (sync/upload): per user (UserId), fallback per IP pokud není uživatel.

```json
{
  "RateLimiting": {
    "Login": { "Limit": 5, "WindowMinutes": 15 },
    "Register": { "Limit": 3, "WindowMinutes": 60 },
    "ForgotPassword": { "Limit": 3, "WindowMinutes": 60 },
    "SyncPush": { "Limit": 100, "WindowMinutes": 1 },
    "SyncPull": { "Limit": 30, "WindowMinutes": 1 },
    "Upload": { "Limit": 20, "WindowMinutes": 1 }
  }
}
```

**Response při překročení:**
- `HTTP 429 Too Many Requests`
- `Retry-After: <seconds>`
- `X-RateLimit-Remaining: 0`

### CORS

- Povolit pouze vlastní doménu
- Credentials: true pro cookies

### HTTPS

- Vždy HTTPS v produkci
- HSTS header
- Redirect HTTP → HTTPS

---

### CSP (baseline)

Baseline pro web frontend (`mujdomecek.cz`) v produkci:

- Cíl: blokovat inline skripty a cizí zdroje, povolit jen vlastní domény + `cdn/img` subdomény.
- Pozn.: `connect-src` musí povolit upload na `cdn.mujdomecek.cz` (presigned URL) a API na `api.mujdomecek.cz`.
- Pokud bude potřeba inline script (SSR/hydration), použít nonces/hashes, ne `unsafe-inline` v `script-src`.
- Fonty: preferovat self-host (Inter servírovat z `mujdomecek.cz`). Pokud použijeme Google Fonts, upravit CSP: přidat `https://fonts.googleapis.com` do `style-src` a `https://fonts.gstatic.com` do `font-src`.

```
Content-Security-Policy:
  default-src 'self';
  base-uri 'self';
  object-src 'none';
  frame-ancestors 'none';
  form-action 'self';
  script-src 'self';
  style-src 'self' 'unsafe-inline';
  img-src 'self' data: blob: https://img.mujdomecek.cz https://cdn.mujdomecek.cz;
  font-src 'self' data:;
  connect-src 'self' https://api.mujdomecek.cz https://cdn.mujdomecek.cz;
  worker-src 'self' blob:;
  manifest-src 'self';
  upgrade-insecure-requests;
```

Implementace: ideálně v aplikační vrstvě (SvelteKit) nebo jako Traefik middleware (pokud nepotřebujeme nonces/hashes).

---

## Lokální data

### IndexedDB

- Data lze šifrovat na klientu (aplikace, ne prohlížeč).
- Cíl: ochrana at-rest (ztráta zařízení, kopie profilu/zálohy).
- Neřeší XSS ani kompromitovaný prohlížeč (po odemknutí jsou data v paměti).

### Rozhodnutí (VP)

- Varianta A: lokálně šifrujeme at-rest, ale při syncu se data posílají v plaintextu přes HTTPS a server je ukládá do PostgreSQL jako běžné sloupce (nejde o E2EE).

### Doporučený přístup

- Šifrovat pouze citlivá pole (free-form text), metadata pro listy/filtry ponechat plaintext.
- Šifrovaná pole (at-rest v IndexedDB):
  - `Property.Description`, `Unit.Description`
  - `Zaznam.Title`, `Zaznam.Description`
  - `ZaznamDokument.Description`, `ZaznamDokument.OriginalFileName`
  - `Comment.Content`
  - `Contact.Email`, `Contact.DisplayName`
- Nešifrovaná pole (plaintext): ID/vazby, datumy, `Cost`, `Flags`, `Status`, sync metadata (kvůli filtrům, řazení a syncu).
- Klíče: envelope encryption (DEK pro data, KEK z passphrase), volitelně „remember device“ přes non-extractable device key.
- Odemykání vaultu (VP): passphrase povinná + volitelně „remember device“ (doporučený default: ON mobile, OFF desktop), DEK jen v paměti.
- Doporučené defaulty: auto-lock 15 min neaktivity, trusted device TTL 30 dní (konfigurovatelné).
- Zapomenutá passphrase: pokud jsou data zálohovaná/syncnutá na serveru, lze resetnout lokální vault a stáhnout je znovu; pokud byla data jen lokálně, jsou ztracena.

### Kryptografie (implementační parametry)

- Algoritmus: AES-256-GCM (WebCrypto), 96-bit nonce per item, 128-bit auth tag.
- AAD: entity type + ID + schema version (sváže ciphertext s konkrétním záznamem).
- KDF: Argon2id (WASM) -> KEK 32 bytes, salt 16 bytes, memory 64 MB, iterations 3, parallelism 1 (mobile) / 4 (desktop).
- Cíl: derivace ~200-300 ms na středním zařízení; low-end fallback 32 MB + 2 iterations.
- Fallback bez Argon2id: PBKDF2-HMAC-SHA256, 310k iterations, salt 16 bytes, output 32 bytes.
- Metadata vaultu: ukládat KDF parametry, salt, crypto version kvůli rotaci/migraci.
- "Remember device": DEK zabalit (wrap) pomocí non-extractable device key (CryptoKey) uloženého v IndexedDB; žádné plaintext klíče v úložišti.

---

## Audit log

### Co logovat

| Akce | Data |
|------|------|
| Create | Entity type, ID, user, timestamp |
| Update | Entity type, ID, user, timestamp, diff |
| Delete | Entity type, ID, user, timestamp |
| Share | Target, recipient, permissions |
| Login | User, IP, timestamp, success/fail |

### Retence

| Typ logu | Retence | Poznámka |
|----------|---------|----------|
| CRUD operace | 1 rok | Záznamy, dokumenty, komentáře |
| Auth události | 90 dní | Login, logout, failed attempts |
| Admin akce | 2 roky | Blokování, mazání uživatelů |
| Sharing změny | 1 rok | Pozvánky, změny rolí |

**Implementace:**
- Background job `AuditLogCleanupJob` (weekly)
- Konfigurovatelné v `appsettings.json`

```json
{
  "AuditLog": {
    "RetentionDays": {
      "Crud": 365,
      "Auth": 90,
      "Admin": 730,
      "Sharing": 365
    }
  }
}
```

## Pozvánky (invite token)

- Token musí být kryptograficky náhodný a **v DB uložený jako `Invitation.TokenHash`** (ne plaintext).
- Při validaci invite linku se porovnává hash(token).

---

## Backup

### Server

- Denní backup PostgreSQL
- Retence: 30 dní

### Klient (local-first)

- Export do JSON
- Opt-in upload na server

---

## OWASP Top 10

| Riziko | Řešení | Stav |
|--------|--------|------|
| Injection | Parametrizované dotazy (EF Core), žádné raw SQL | ✅ |
| Broken Auth | ASP.NET Identity + JWT + refresh token rotation | ✅ |
| Sensitive Data | HTTPS only, HSTS, hesla hashované (Argon2/PBKDF2) | ✅ |
| XXE | N/A (nepoužíváme XML, System.Text.Json only) | ✅ |
| Broken Access Control | Authorization policies + resource-based auth v service layer | ✅ |
| Security Misconfiguration | Hardening checklist níže | ✅ |
| XSS | Svelte auto-escaping, CSP header, HttpOnly cookies | ✅ |
| Insecure Deserialization | System.Text.Json (safe by default), no custom deserializers | ✅ |
| Using Vulnerable Components | Dependabot + `dotnet list package --vulnerable` v CI | ✅ |
| Insufficient Logging | AuditLog entita + Serilog structured logging | ✅ |

### Broken Access Control - Detail

```csharp
// Resource-based authorization v service layer
public class PropertyAuthorizationHandler : AuthorizationHandler<PropertyOperation, Property>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PropertyOperation requirement,
        Property resource)
    {
        var userId = context.User.GetUserId();

        // Owner má vše
        if (resource.OwnerId == userId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check shared access
        var member = GetMember(resource.Id, userId);
        if (member != null && HasPermission(member, requirement))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

### Security Hardening Checklist

| Oblast | Kontrola | Jak |
|--------|----------|-----|
| HTTPS | Forced redirect | `UseHttpsRedirection()` |
| HSTS | Enabled | `UseHsts()` s preload |
| Cookies | Secure, HttpOnly, SameSite | Cookie policy middleware |
| Headers | Security headers | `X-Content-Type-Options`, `X-Frame-Options` |
| CSP | Baseline nasazena | Viz CSP sekce výše |
| Secrets | Ne v kódu | Azure Key Vault / env vars |
| Logs | Bez citlivých dat | Serilog destructuring policies |
| Debug | Vypnuto v prod | `ASPNETCORE_ENVIRONMENT=Production` |

---

## Před produkcí (checklist)

- [ ] Rate limiting implementace a test
- [ ] CSP nasazena (report-only → enforce)
- [ ] Dependency audit (`dotnet list package --vulnerable`, `npm audit`)
- [ ] Security headers test (securityheaders.com)
- [ ] SSL test (ssllabs.com)
- [ ] OWASP ZAP basic scan (optional)
- [ ] Penetration test (optional, doporučeno před public launch)
