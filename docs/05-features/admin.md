# Admin Panel

## Přehled

Admin panel pro správu systému, uživatelů a konfigurace.

**Přístup:** Pouze uživatelé s rolí `Admin`

**URL:** `/admin` (oddělená SvelteKit route group)

---

## Dashboard (`/admin`)

### Metriky

| Metrika | Popis |
|---------|-------|
| Celkem uživatelů | Registrovaní uživatelé |
| Aktivní uživatelé (7d) | Přihlášení za posledních 7 dní |
| Celkem záznamů | Všechny záznamy v systému |
| Nové záznamy (24h) | Záznamy za posledních 24 hodin |
| Storage využití | Velikost souborů v S3 |
| Pending pozvánky | Nevyřízené pozvánky |

### System Health

| Komponenta | Check |
|------------|-------|
| Database | Connection + response time |
| S3 Storage | Connection |
| Background Jobs | Hangfire status |
| Email Service | SMTP connection |

### Quick Links

- Hangfire Dashboard
- Audit Log
- Users
- Tags

---

## Uživatelé (`/admin/users`)

### Seznam uživatelů

| Sloupec | Popis |
|---------|-------|
| Jméno | FirstName + LastName |
| Email | S indikací verified/unverified |
| Registrace | Datum registrace |
| Poslední login | LastLoginAt |
| Stav | Active / Blocked / Deleted |
| Projekty | Počet vlastněných projektů |
| Akce | Detail, Block, Delete |

**Filtry:**
- Stav (Active / Blocked / Deleted)
- Registrace od-do
- Search (jméno, email)

**Řazení:**
- Registrace (desc default)
- Poslední login
- Jméno

### Detail uživatele (`/admin/users/{id}`)

**Sekce:**

| Sekce | Obsah |
|-------|-------|
| Profil | Základní údaje, avatar, linked accounts |
| Statistiky | Počet projektů, properties, záznamů |
| Aktivita | Posledních 20 aktivit |
| Sessions | Aktivní sessions s možností revoke |
| Audit log | Akce uživatele |

**Akce:**
- Block / Unblock uživatele
- Force logout (revoke all sessions)
- Delete účet (soft delete + anonymizace)
- Impersonate (later - přihlásit se jako uživatel pro debug)

### Blokování uživatele

```
1. Admin klikne "Blokovat"
2. Modal: Důvod blokace (required)
3. Potvrzení
4. Systém:
   - Nastaví User.IsBlocked = true
   - Revokuje všechny RefreshTokeny
   - Zapíše do AuditLog
5. Uživatel při dalším requestu dostane 403
```

### Mazání uživatele (GDPR)

```
1. Admin klikne "Smazat účet"
2. Varování: "Tato akce je nevratná"
3. Potvrzení zadáním emailu uživatele
4. Systém:
   - Soft delete (IsDeleted = true)
   - Anonymizace: FirstName = "Deleted", LastName = "User", Email = hash
   - Smazání avataru z S3
   - Zachování záznamů (CreatedBy → anonymní ID)
   - Zapíše do AuditLog
```

---

## Tagy (`/admin/tags`)

### Seznam tagů

| Sloupec | Popis |
|---------|-------|
| Ikona | Lucide ikona |
| Název | Name |
| Použití | Počet záznamů s tímto tagem |
| Pořadí | SortOrder |
| Stav | Active / Inactive |
| Akce | Edit, Deactivate |

**Akce:**
- Přidat nový tag
- Upravit tag (název, ikona, pořadí)
- Deaktivovat (ne mazat - zachovat historii)
- Změnit pořadí (drag & drop)

### Přidání/Úprava tagu

| Pole | Typ | Validace |
|------|-----|----------|
| Název | Text | Required, max 50, unique |
| Ikona | Icon picker | Required, Lucide icons |
| Pořadí | Number | Auto-increment |
| Aktivní | Toggle | Default true |

---

## Audit Log (`/admin/audit`)

### Seznam

| Sloupec | Popis |
|---------|-------|
| Čas | CreatedAt |
| Uživatel | Actor (jméno + email) |
| Akce | Create / Update / Delete |
| Entita | EntityType + EntityId |
| Změny | DiffSummary (expandable) |

**Filtry:**
- Uživatel
- Typ entity
- Akce
- Datum od-do

**Export:**
- CSV export (filtered)

---

## Background Jobs (`/admin/jobs`)

Embed Hangfire Dashboard nebo vlastní UI.

### Přehled

| Info | Popis |
|------|-------|
| Pending | Čekající joby |
| Processing | Právě běžící |
| Succeeded (24h) | Úspěšné za 24h |
| Failed (24h) | Neúspěšné za 24h |

### Joby

| Job | Popis | Schedule |
|-----|-------|----------|
| DraftCleanupJob | Mazání starých draftů | Daily 3:00 |
| DraftReminderJob | Připomínky draftů | Daily 9:00 |
| InvitationExpirationJob | Expirace pozvánek | Hourly |
| ActivityCleanupJob | Mazání staré aktivity | Weekly |
| OrphanedFilesCleanupJob | Smazání osiřelých souborů | Daily 4:00 |
| ... | Další viz 14-background-jobs.md | |

**Akce:**
- Trigger job manually
- View job history
- Retry failed job

---

## Systémová nastavení (`/admin/settings`)

### Runtime konfigurace (VP)

| Nastavení | Popis | Default |
|-----------|-------|---------|
| Registrace povolena | Allow new registrations | true |
| Max upload size | Maximum file size (MB) | 20 |
| Max photo size (MB) | Maximum photo upload size | 10 |
| Max document size (MB) | Maximum document upload size | 20 |
| Max photos per záznam (Free) | Tier limit (Free) | 10 |
| Max photos per záznam (Premium) | Tier limit (Premium) | 50 |
| Free photo compression max dimension (px) | Client-side resize limit | 2000 |
| Free photo compression quality (%) | Client-side JPEG quality | 80 |
| Draft expiration days | Po kolika dnech smazat draft | 30 |
| Invitation expiration days | Expirace pozvánek | 7 |
| Maintenance mode | Zobrazit maintenance page | false |

**Uložení a precedence:**
- Runtime hodnoty se ukládají do DB (tabulka `AppSetting`).
- `appsettings.json` drží výchozí hodnoty (fallback).
- Hardcoded defaults v kódu jsou poslední záchrana.
- Precedence: hardcoded defaults → `appsettings.json` → DB.

---

## API Endpointy

### Admin Users

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/admin/users` | GET | Seznam uživatelů |
| `/admin/users/{id}` | GET | Detail uživatele |
| `/admin/users/{id}/block` | POST | Blokovat uživatele |
| `/admin/users/{id}/unblock` | POST | Odblokovat uživatele |
| `/admin/users/{id}/delete` | DELETE | Smazat uživatele |
| `/admin/users/{id}/sessions` | GET | Seznam sessions |
| `/admin/users/{id}/sessions` | DELETE | Revoke all sessions |

### Admin Tags

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/admin/tags` | GET | Seznam tagů |
| `/admin/tags` | POST | Vytvořit tag |
| `/admin/tags/{id}` | PUT | Upravit tag |
| `/admin/tags/{id}/deactivate` | POST | Deaktivovat tag |
| `/admin/tags/reorder` | POST | Změnit pořadí |

### Admin Settings

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/admin/settings` | GET | Seznam systémových nastavení (effective values + source) |
| `/admin/settings` | PUT | Uložit systémová nastavení (bulk update) |

### Admin Stats

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/admin/stats/dashboard` | GET | Dashboard metriky |
| `/admin/stats/health` | GET | System health check |

### Admin Audit

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/admin/audit` | GET | Seznam audit logů |
| `/admin/audit/export` | GET | CSV export |

---

## Bezpečnost

### Autorizace

```csharp
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    // ...
}
```

### Audit

Všechny admin akce se logují do AuditLog s `ActorUserId` admina.

### Rate Limiting

| Endpoint | Limit |
|----------|-------|
| `/admin/*` | 60/min per admin |
| `/admin/users/{id}/delete` | 5/hour per admin |

---

## UI/UX

### Layout

```
┌─────────────────────────────────────────────────────┐
│ 🏠 MůjDomeček Admin              [Admin Name] [Logout]
├──────────────┬──────────────────────────────────────┤
│              │                                      │
│  Dashboard   │                                      │
│  Uživatelé   │         [Main Content Area]          │
│  Tagy        │                                      │
│  Audit Log   │                                      │
│  Jobs        │                                      │
│  Nastavení   │                                      │
│              │                                      │
└──────────────┴──────────────────────────────────────┘
```

### Komponenty

- Data tables s pagination, sorting, filtering
- Confirmation modals pro destruktivní akce
- Toast notifikace
- Breadcrumbs

### Responzivita

- Desktop-first (admin se používá hlavně na desktopu)
- Sidebar collapsible na menších obrazovkách
- Tables horizontally scrollable na mobilu

---

## Rozhodnuto

### Kdo je Admin?

**Rozhodnutí:** Role v databázi, přiřazuje se ručně.

- Není self-service (žádný "request admin" flow)
- První admin se vytvoří při deploymentu (seed)
- Další adminy přidává existující admin

### Oddělená aplikace nebo součást hlavní?

**Rozhodnutí:** Součást hlavní aplikace, oddělená route group.

- `/admin/*` routes
- Shared komponenty (design system)
- Separate layout (admin sidebar)
- Lazy loading admin bundlu

### Impersonation?

**Rozhodnutí:** Later (nice-to-have pro support).

- Admin se může "přihlásit jako" uživatel
- Všechny akce se logují jako impersonated
- Banner "Přihlášen jako X" + tlačítko "Zpět na admin"
