# Autentizace a práva

## Role

| Role | Popis |
|------|-------|
| User | Spravuje svoje nemovitosti a data |
| Admin | Správa systému, podpora, cleanup a moderace |

## Sdílení - role

| Role | Popis |
|------|-------|
| Owner | Plná kontrola, může sdílet a mazat |
| Editor | Vytváření a úpravy, omezené mazání |
| Viewer | Pouze čtení |

---

## Permission matrix

### Role defaults

| Akce | Owner | Editor | Viewer |
|------|-------|--------|--------|
| View záznamy | ✓ | ✓ | ✓ |
| View fotky | ✓ | ✓ | ✓ |
| View cena | ✓ | ✓ | ✓* |
| Create záznam | ✓ | ✓ | ✗ |
| Update záznam | ✓ | ✓ | ✗ |
| Delete záznam | ✓ | ✗* | ✗ |
| Upload fotky | ✓ | ✓ | ✗ |
| Delete fotky | ✓ | ✗* | ✗ |
| Invite users | ✓ | ✗ | ✗ |
| Change permissions | ✓ | ✗ | ✗ |
| Transfer ownership | ✓ | ✗ | ✗ |

*Lze změnit per-user override

### Granulární práva (checkboxy)

**View:**
- záznamy
- fotky
- popis
- cena
- tagy

**Edit:**
- název
- popis
- datum
- jednotka
- cena
- tagy

**Media:**
- přidat fotku
- smazat fotku

**Delete:**
- smazat záznam

**Share:**
- zvání uživatelů
- změna práv

### Override pravidla

1. Role nastavuje default
2. Per-user override může zvednout nebo snížit konkrétní práva
3. Override se ukládá jako JSON v `Permissions` poli

```json
{
  "canDeletePhotos": true,
  "canViewPrice": false
}
```

---

## Autentizace

### Metody přihlášení

| Metoda | Popis |
|--------|-------|
| Email/heslo | ASP.NET Identity |
| Google | OIDC provider |
| Apple | OIDC provider |

### Požadavky na heslo
- Min 6 znaků
- Velké písmeno
- Malé písmeno
- Číslice

### Token strategie

| Token | Expirace | Úložiště |
|-------|----------|----------|
| Access (JWT) | 15 min | Paměť (ne localStorage!) |
| Refresh | 7 dní | HttpOnly cookie |

**Flow:**
1. Login → API vrátí JWT + nastaví refresh cookie
2. Request: `Authorization: Bearer <jwt>`
3. JWT expiruje → `POST /auth/refresh` → nový JWT
4. Refresh expiruje → uživatel se přihlásí znovu

**Refresh token rotace:** Při každém refresh se vydá nový token, starý se invaliduje.

### Offline chování

- Login/registrace vyžaduje online
- Offline read/write funguje (data v IndexedDB)
- Token v paměti přetrvá dokud je app otevřená
- Po zavření app: při dalším otevření se pokusí refresh
- Pokud refresh selže → UI vyžádá re-login

### Propojení účtů

Uživatel může mít propojené více metod přihlášení:

```csharp
public class AppUser : IdentityUser<Guid>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? GoogleId { get; set; }
    public string? AppleId { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## Flow: Registrace (Email/Heslo)

### Krok za krokem

```
1. Uživatel otevře /register
2. Vyplní: Jméno, Příjmení, Email, Heslo, Heslo znovu
3. Client-side validace (okamžitá zpětná vazba)
4. POST /api/auth/register
5. Server:
   a) Validace (email unikátní, heslo splňuje požadavky)
   b) Vytvoření AppUser (EmailConfirmed = false)
   c) Generování confirmation tokenu
   d) Odeslání emailu s linkem
6. UI: "Registrace úspěšná, zkontrolujte email"
7. Uživatel klikne na link v emailu
8. GET /api/auth/confirm-email?userId=X&token=Y
9. Server: EmailConfirmed = true
10. Redirect na /login s hláškou "Email potvrzen"
```

### API Endpoints

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/api/auth/register` | POST | Registrace nového uživatele |
| `/api/auth/confirm-email` | GET | Potvrzení emailu |
| `/api/auth/resend-confirmation` | POST | Znovu odeslat potvrzovací email |

### Request/Response

```typescript
// POST /api/auth/register
interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

interface RegisterResponse {
  success: boolean;
  message: string;
  userId?: string;
}
```

### Validace

| Pole | Pravidla |
|------|----------|
| firstName | Required, 2-50 znaků |
| lastName | Required, 2-50 znaků |
| email | Required, valid email, unikátní |
| password | Min 6 znaků, velké + malé + číslice |

### Email template

```
Předmět: Potvrďte svůj účet v MujDomecek

Dobrý den {FirstName},

děkujeme za registraci v MujDomecek.

Pro aktivaci účtu klikněte na tento odkaz:
{ConfirmationLink}

Odkaz je platný 24 hodin.

S pozdravem,
Tým MujDomecek
```

### Expirace confirmation tokenu

- Platnost: 24 hodin
- Po expiraci: uživatel může požádat o nový (`/resend-confirmation`)
- Rate limit: max 3 requesty za hodinu

---

## Flow: Registrace (Google/Apple)

### Krok za krokem

```
1. Uživatel klikne "Pokračovat s Google/Apple"
2. Redirect na OIDC provider
3. Uživatel se přihlásí u providera
4. Redirect zpět s authorization code
5. Server:
   a) Výměna code za tokeny
   b) Získání user info (email, jméno)
   c) Kontrola: existuje účet s tímto email?
      - NE → vytvoř nový účet (EmailConfirmed = true)
      - ANO + stejný provider → přihlaš
      - ANO + jiný provider → nabídni propojení
6. Vydání JWT + refresh cookie
7. Redirect na dashboard
```

### Propojení s existujícím účtem

Pokud email už existuje (ale přihlášen přes jinou metodu):

```
UI: "Účet s tímto emailem už existuje.
     Přihlaste se heslem pro propojení s Google."

1. Uživatel se přihlásí heslem
2. Server propojí GoogleId/AppleId s účtem
3. Příště se může přihlásit oběma způsoby
```

---

## Flow: Přihlášení

### Email/Heslo

```
1. POST /api/auth/login { email, password }
2. Server:
   a) Najdi uživatele podle email
   b) Ověř heslo (Identity.CheckPasswordAsync)
   c) Kontrola: EmailConfirmed == true?
      - NE → 403 "Potvrďte email" + nabídka resend
   d) Generuj JWT + refresh token
3. Response: { accessToken } + Set-Cookie: refresh
4. Client uloží JWT do paměti (ne localStorage!)
```

### Google/Apple

```
1. Redirect na OIDC provider
2. Callback s authorization code
3. Server ověří a najde uživatele podle GoogleId/AppleId
4. Vydání JWT + refresh cookie
```

### Chybové stavy

| Stav | HTTP | Zpráva |
|------|------|--------|
| Neexistující email | 401 | "Nesprávné přihlašovací údaje" |
| Špatné heslo | 401 | "Nesprávné přihlašovací údaje" |
| Email nepotvrzený | 403 | "Potvrďte prosím svůj email" |
| Účet zablokovaný | 403 | "Účet byl zablokován" |
| Příliš mnoho pokusů | 429 | "Příliš mnoho pokusů, zkuste později" |

**Poznámka:** Nerozlišujeme mezi "email neexistuje" a "špatné heslo" (prevence enumeration).

---

## Flow: Zapomenuté heslo

### Krok za krokem

```
1. Uživatel klikne "Zapomněli jste heslo?"
2. Zadá email
3. POST /api/auth/forgot-password { email }
4. Server:
   a) Najdi uživatele (pokud neexistuje → stejná odpověď!)
   b) Generuj reset token (kryptograficky náhodný)
   c) Ulož hash tokenu + expirace (1 hodina)
   d) Odešli email s reset linkem
5. UI: "Pokud účet existuje, odeslali jsme email"
6. Uživatel klikne na link v emailu
7. GET /reset-password?token=X → formulář pro nové heslo
8. POST /api/auth/reset-password { token, newPassword }
9. Server:
   a) Validuj token (hash match + not expired)
   b) Změň heslo
   c) Invaliduj všechny refresh tokeny (force re-login)
10. Redirect na /login s hláškou "Heslo změněno"
```

### API Endpoints

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/api/auth/forgot-password` | POST | Požadavek na reset |
| `/api/auth/reset-password` | POST | Nastavení nového hesla |
| `/api/auth/validate-reset-token` | GET | Ověření platnosti tokenu |

### Request/Response

```typescript
// POST /api/auth/forgot-password
interface ForgotPasswordRequest {
  email: string;
}

// POST /api/auth/reset-password
interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}
```

### Bezpečnost

| Aspekt | Řešení |
|--------|--------|
| Token storage | Pouze hash v DB (ne plaintext) |
| Token délka | Min 32 bytů (kryptograficky náhodný) |
| Expirace | 1 hodina |
| Single use | Token se po použití smaže |
| Rate limit | 3 requesty/hodinu per email |
| Enumeration | Stejná odpověď pro existující i neexistující email |

### Email template

```
Předmět: Obnovení hesla v MujDomecek

Dobrý den,

obdrželi jsme požadavek na obnovení hesla pro váš účet.

Pro nastavení nového hesla klikněte na tento odkaz:
{ResetLink}

Odkaz je platný 1 hodinu.

Pokud jste o reset nežádali, tento email ignorujte.

S pozdravem,
Tým MujDomecek
```

---

## Flow: Změna hesla (přihlášený uživatel)

```
1. Uživatel jde do Nastavení → Zabezpečení
2. Zadá: Současné heslo, Nové heslo, Nové heslo znovu
3. POST /api/auth/change-password { currentPassword, newPassword }
4. Server:
   a) Ověř současné heslo
   b) Validuj nové heslo
   c) Změň heslo
   d) Volitelně: invaliduj ostatní sessions
5. UI: "Heslo bylo změněno"
```

---

## Flow: Propojení externího účtu

Uživatel má účet s heslem a chce přidat Google/Apple:

```
1. Nastavení → Propojené účty
2. Klikne "Propojit s Google"
3. Redirect na Google OIDC
4. Callback
5. Server:
   a) Ověř, že Google email == email účtu
   b) Ulož GoogleId k účtu
6. UI: "Google účet propojen"
```

### Odpojení

```
1. Nastavení → Propojené účty
2. Klikne "Odpojit Google"
3. Server:
   a) Kontrola: má uživatel jinou metodu přihlášení?
      - Musí mít heslo NEBO jiný provider
   b) Smaže GoogleId
4. UI: "Google účet odpojen"
```

---

## Přehled Auth API

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/api/auth/register` | POST | - | Registrace |
| `/api/auth/confirm-email` | GET | - | Potvrzení emailu |
| `/api/auth/resend-confirmation` | POST | - | Znovu poslat potvrzení |
| `/api/auth/login` | POST | - | Přihlášení |
| `/api/auth/logout` | POST | JWT | Odhlášení |
| `/api/auth/refresh` | POST | Cookie | Obnovení JWT |
| `/api/auth/forgot-password` | POST | - | Požadavek na reset |
| `/api/auth/validate-reset-token` | GET | - | Ověření reset tokenu |
| `/api/auth/reset-password` | POST | - | Nastavení nového hesla |
| `/api/auth/change-password` | POST | JWT | Změna hesla |
| `/api/auth/me` | GET | JWT | Info o přihlášeném uživateli |
| `/api/auth/google` | GET | - | Redirect na Google OIDC |
| `/api/auth/google/callback` | GET | - | Google callback |
| `/api/auth/apple` | GET | - | Redirect na Apple OIDC |
| `/api/auth/apple/callback` | GET | - | Apple callback |
| `/api/auth/link-google` | POST | JWT | Propojit Google účet |
| `/api/auth/unlink-google` | POST | JWT | Odpojit Google účet |
| `/api/auth/link-apple` | POST | JWT | Propojit Apple účet |
| `/api/auth/unlink-apple` | POST | JWT | Odpojit Apple účet |
| `/api/auth/sessions` | GET | JWT | Seznam aktivních sessions |
| `/api/auth/sessions/{id}` | DELETE | JWT | Odhlásit konkrétní session |
| `/api/auth/sessions/revoke-all` | POST | JWT | Odhlásit všude kromě current |

---

## User Settings API

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/api/users/me` | GET | Profil přihlášeného uživatele |
| `/api/users/me` | PUT | Aktualizace profilu |
| `/api/users/me/avatar` | POST | Upload profilové fotky |
| `/api/users/me/avatar` | DELETE | Smazání profilové fotky |
| `/api/users/me/preferences` | GET | Získat preference |
| `/api/users/me/preferences` | PUT | Aktualizovat preference |
| `/api/users/me/export` | POST | Požádat o export dat |
| `/api/users/me` | DELETE | Smazání účtu |

### Request/Response typy

```typescript
// GET /api/users/me
interface UserProfileResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  avatarUrl?: string;
  preferredLanguage: 'cs' | 'en';
  themePreference: 'light' | 'dark' | 'system';
  hasPassword: boolean;
  linkedAccounts: {
    google?: string;  // email pokud propojeno
    apple?: string;
  };
  createdAt: string;
}

// PUT /api/users/me
interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
  phone?: string;
  preferredLanguage?: 'cs' | 'en';
  themePreference?: 'light' | 'dark' | 'system';
}

// GET /api/users/me/preferences
interface UserPreferencesResponse {
  push: {
    newComments: boolean;
    mentions: boolean;
    sharedActivity: boolean;
    draftReminders: boolean;
  };
  email: {
    weeklySummary: boolean;
    invitations: boolean;
  };
  sync: {
    enabled: boolean;
    onMobileData: boolean;
  };
}

// PUT /api/users/me/preferences
interface UpdatePreferencesRequest {
  push?: {
    newComments?: boolean;
    mentions?: boolean;
    sharedActivity?: boolean;
    draftReminders?: boolean;
  };
  email?: {
    weeklySummary?: boolean;
    invitations?: boolean;
  };
  sync?: {
    enabled?: boolean;
    onMobileData?: boolean;
  };
}

// GET /api/auth/sessions
interface SessionsResponse {
  sessions: {
    id: string;
    deviceInfo: string;
    createdAt: string;
    lastUsedAt: string;
    isCurrent: boolean;
  }[];
}

// DELETE /api/users/me
interface DeleteAccountRequest {
  password: string;
  confirmation: "DELETE_MY_ACCOUNT";
}
```

---

## Sharing API

Endpointy pro správu sdílení v Settings.

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/api/sharing/my-shares` | GET | Co sdílím (moje properties s členy) |
| `/api/sharing/shared-with-me` | GET | Sdíleno se mnou |
| `/api/sharing/pending-invitations` | GET | Čekající pozvánky pro mě |
| `/api/invitations/{id}/accept` | POST | Přijmout pozvánku |
| `/api/invitations/{id}/decline` | POST | Odmítnout pozvánku |
| `/api/invitations/{id}` | DELETE | Zrušit odeslanou pozvánku |
| `/api/properties/{id}/members` | GET | Seznam členů property |
| `/api/properties/{id}/members` | POST | Přidat člena (pozvánka) |
| `/api/properties/{id}/members/{userId}` | PUT | Změnit roli/práva člena |
| `/api/properties/{id}/members/{userId}` | DELETE | Odebrat člena |
| `/api/properties/{id}/leave` | POST | Opustit sdílenou property |
| `/api/projects/{id}/members` | GET | Seznam členů projektu |
| `/api/projects/{id}/members` | POST | Přidat člena do projektu |
| `/api/projects/{id}/members/{userId}` | PUT | Změnit roli člena projektu |
| `/api/projects/{id}/members/{userId}` | DELETE | Odebrat člena z projektu |
| `/api/projects/{id}/leave` | POST | Opustit sdílený projekt |

### Request/Response typy

```typescript
// GET /api/sharing/my-shares
interface MySharesResponse {
  items: {
    type: 'project' | 'property';
    id: string;
    name: string;
    projectName?: string;  // pro properties
    members: {
      userId: string;
      email: string;
      displayName: string;
      role: 'owner' | 'editor' | 'viewer';
      status: 'active' | 'pending';
      joinedAt?: string;
      invitedAt?: string;
      hasOverrides: boolean;
    }[];
  }[];
}

// GET /api/sharing/shared-with-me
interface SharedWithMeResponse {
  items: {
    type: 'project' | 'property';
    id: string;
    name: string;
    owner: {
      userId: string;
      email: string;
      displayName: string;
    };
    myRole: 'editor' | 'viewer';
    sharedAt: string;
  }[];
}

// GET /api/sharing/pending-invitations
interface PendingInvitationsResponse {
  invitations: {
    id: string;
    type: 'project' | 'property';
    targetId: string;
    targetName: string;
    role: 'owner' | 'editor' | 'viewer';
    invitedBy: {
      userId: string;
      email: string;
      displayName: string;
    };
    createdAt: string;
    expiresAt: string;
  }[];
}

// POST /api/properties/{id}/members
interface AddMemberRequest {
  email: string;
  role: 'editor' | 'viewer';
  permissions?: {
    canDeletePhotos?: boolean;
    canViewPrice?: boolean;
    // ... další granulární práva
  };
}

// PUT /api/properties/{id}/members/{userId}
interface UpdateMemberRequest {
  role?: 'editor' | 'viewer';
  permissions?: {
    canDeletePhotos?: boolean;
    canViewPrice?: boolean;
  };
}
```

---

## Flow: Pozvánky

1. Owner otevře Project/Property detail → **Sdílet**
2. Zvolí scope (Project/Property) a roli (Owner/Editor/Viewer)
3. Volitelně nastaví per-user overrides (checkboxy)
4. Vybere příjemce z kontaktů/skupin nebo zadá email
5. Potvrdí → vytvoří se `Invitation` (expirace 7 dní) + vygeneruje se invite link (token)
6. Invitee vidí in-app notifikaci (pokud už má účet)
7. Accept → přidá se do member listu, spustí se auto sync
8. Decline → pozvánka označena jako declined

**Invite link API:** `GET /api/invitations/by-token?token=...` → detail pozvánky

### Neregistrovaný uživatel

- Pozvánka se uloží podle emailu a Owner zkopíruje invite link (pošle přes messenger)
- Po registraci/loginu pozvaný otevře invite link a pozvánku přijme/odmítne

### Expirace pozvánek

**Mechanismus:** Lazy check + background job + notifikace

1. **Lazy check:** Při čtení pozvánky zkontrolovat `ExpiresAt`
2. **Background job:** 1x denně projít pending pozvánky, označit expired
3. **Notifikace vlastníkovi:** Informovat s možností "Poslat znovu"

```csharp
public bool IsExpired => ExpiresAt < DateTime.UtcNow;
```

---

## Permission precedence

Uživatel může mít roli na Project i Property úrovni.

**Pravidlo:** Explicitní PropertyMember přebíjí implicitní ProjectMember

**Příklad:**
```
Projekt "Rodina" - User je Editor
  └── Property "Chalupa" - User je explicitně Viewer → Viewer
  └── Property "Byt" - User nemá explicitní roli → Editor (dědí)
```

Umožňuje výjimky (např. citlivá Property jen pro čtení).

---

## UI: Správa členů

- Seznam členů s rolí, stavem (active/pending), indikací overrides
- Akce: změna role, úprava práv, odebrání, resend invite
- Owner-only: transfer ownership, správa sdílení
- Odkaz na AuditLog (kdo a co změnil)

---

## Email Templates

### Přehled

| Template | Trigger | Subject (cs) |
|----------|---------|--------------|
| `email-confirmation` | Registrace | Potvrzení emailu - Můj Domeček |
| `password-reset` | Forgot password | Obnovení hesla - Můj Domeček |
| `invitation` | Pozvánka ke sdílení | {inviter} vás pozval do {project/property} |
| `invitation-accepted` | Přijetí pozvánky | {user} přijal vaši pozvánku |
| `draft-reminder` | Background job (7d) | Máte rozpracované záznamy |
| `weekly-summary` | Background job (weekly) | Týdenní souhrn - Můj Domeček |

### Template struktura

```
/templates/emails/
├── _layout.html          # Společný layout (logo, footer)
├── email-confirmation.html
├── password-reset.html
├── invitation.html
├── invitation-accepted.html
├── draft-reminder.html
└── weekly-summary.html
```

### Technologie

- **Engine:** Razor views nebo Fluid templates
- **Styling:** Inline CSS (email kompatibilita)
- **Responzivita:** Mobile-first, max-width 600px
- **Obrázky:** Hostované na cdn.mujdomecek.cz

### Email Confirmation

**Proměnné:**
- `{{userName}}` - Jméno uživatele
- `{{confirmUrl}}` - URL pro potvrzení (platnost 24h)

**Obsah:**
```
Dobrý den {{userName}},

děkujeme za registraci v aplikaci Můj Domeček.

Pro dokončení registrace potvrďte svůj email kliknutím na tlačítko níže:

[Potvrdit email]

Odkaz je platný 24 hodin.

Pokud jste se neregistrovali, tento email ignorujte.
```

### Password Reset

**Proměnné:**
- `{{userName}}` - Jméno uživatele
- `{{resetUrl}}` - URL pro reset (platnost 1h)

**Obsah:**
```
Dobrý den {{userName}},

obdrželi jsme žádost o obnovení hesla k vašemu účtu.

Pro nastavení nového hesla klikněte na tlačítko níže:

[Obnovit heslo]

Odkaz je platný 1 hodinu.

Pokud jste o reset hesla nežádali, tento email ignorujte.
Vaše současné heslo zůstává beze změny.
```

### Invitation

**Proměnné:**
- `{{inviterName}}` - Jméno zvaícího
- `{{targetType}}` - "projektu" / "nemovitosti"
- `{{targetName}}` - Název projektu/property
- `{{role}}` - "přispěvatele" / "pozorovatele"
- `{{inviteUrl}}` - URL pro přijetí

**Obsah:**
```
Dobrý den,

{{inviterName}} vás pozval jako {{role}} do {{targetType}} "{{targetName}}"
v aplikaci Můj Domeček.

[Zobrazit pozvánku]

Pozvánka je platná 7 dní.
```

### Draft Reminder

**Proměnné:**
- `{{userName}}` - Jméno uživatele
- `{{drafts}}` - Seznam draftů (title, property, daysOld)
- `{{appUrl}}` - URL do aplikace

**Obsah:**
```
Dobrý den {{userName}},

máte {{drafts.length}} rozpracovaných záznamů:

{{#each drafts}}
• {{title}} ({{property}}) - {{daysOld}} dní
{{/each}}

Rozpracované záznamy starší než 30 dní budou automaticky smazány.

[Dokončit záznamy]
```

### Konfigurace

```json
// appsettings.json
{
  "Email": {
    "Provider": "SMTP",  // nebo "SendGrid", "Mailgun"
    "From": "noreply@mujdomecek.cz",
    "FromName": "Můj Domeček",
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "...",
      "Password": "..."  // → secrets
    }
  }
}
```

### Lokalizace

- VP: Pouze čeština
- Later: Angličtina (podle `User.PreferredLanguage`)
