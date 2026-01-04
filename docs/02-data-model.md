# Datový model

## Přehled entit

```
User
  └── Project (1:N)
        └── Property (1:N)
              ├── Unit (1:N, hierarchie)
              ├── Media (1:N) ← galerie property
              └── Activity (1:N) ← activity feed

Zaznam (samostatný aggregate)
  ├── Media (1:N)
  ├── ZaznamTag (M:N → Tag)
  └── Comment (1:N)
        └── CommentMention (M:N → User)

Unit
  └── Media (1:N) ← galerie jednotky
```

## Entity

### User

Rozšiřuje ASP.NET Identity.

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK (IdentityUser<Guid>) |
| FirstName | string | Jméno (2-50 znaků) |
| LastName | string | Příjmení (2-50 znaků) |
| Email | string | Email (Identity) |
| Phone | string? | Telefon (pro budoucí 2FA) |
| AvatarStorageKey | string? | S3 key pro profilovou fotku |
| GoogleId | string? | ID pro Google OIDC |
| AppleId | string? | ID pro Apple OIDC |
| PreferredLanguage | string | "cs" nebo "en" (default "cs") |
| ThemePreference | enum | Light / Dark / System (default System) |
| CreatedAt | datetime | Vytvořeno |
| LastLoginAt | datetime? | Poslední přihlášení |
| IsBlocked | bool | Blokovaný adminem (default false) |
| BlockedReason | string? | Důvod blokace |
| IsDeleted | bool | Soft delete |
| DeletedAt | datetime? | Kdy smazáno |

### UserPreferences

Nastavení notifikací a dalších preferencí.

| Pole | Typ | Popis |
|------|-----|-------|
| UserId | uuid (Guid) | PK + FK → User |
| PushNewComments | bool | Push: nové komentáře (default true) |
| PushMentions | bool | Push: @mentions (default true) |
| PushSharedActivity | bool | Push: aktivita na sdílených (default true) |
| PushDraftReminders | bool | Push: připomínky draftů (default true) |
| EmailWeeklySummary | bool | Email: týdenní souhrn (default false) |
| EmailInvitations | bool | Email: pozvánky (default true) |
| SyncEnabled | bool | Auto-sync zapnutý (default true) |
| SyncOnMobileData | bool | Sync přes mobilní data (default false) |

**Poznámka:** 1:1 vztah s User, vytvoří se automaticky při registraci.

### RefreshToken

Refresh tokeny pro JWT autentizaci.

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| UserId | uuid (Guid) | FK → User |
| TokenHash | string | Hash refresh tokenu |
| DeviceInfo | string? | User-Agent / device name |
| CreatedAt | datetime | Vytvořeno |
| ExpiresAt | datetime | Expirace (7 dní) |
| RevokedAt | datetime? | Kdy revokován |
| ReplacedByTokenId | uuid? | FK → RefreshToken (při rotaci) |

**Poznámka:** Umožňuje správu sessions a "Odhlásit všude".

### AppSetting

Runtime konfigurace aplikace (přes admin panel).

Slouží pro přepsání vybraných hodnot z `appsettings.json` za běhu (např. `Constraints.Files.*`).

| Pole | Typ | Popis |
|------|-----|-------|
| Key | string | PK (např. `Constraints.Files.MaxPhotosPerZaznamFree`) |
| Value | json (jsonb) | Hodnota jako JSON (number/bool/string/object) |
| UpdatedAt | datetime | Kdy změněno |
| UpdatedByUserId | uuid (Guid)? | FK → User (admin) |

### Project

Nová entita pro seskupení nemovitostí.

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| OwnerId | uuid (Guid) | FK → User |
| Name | string | Název projektu |
| CreatedAt | datetime | Vytvořeno |
| UpdatedAt | datetime | Upraveno |
| SyncMode | enum | `local-only` / `synced` |
| SyncStatus | enum | local / pending / syncing / synced / failed |
| LastSyncAt | datetime? | Poslední úspěšný sync |
| IsDeleted | bool | Soft delete |
| DeletedAt | datetime? | Kdy smazáno |

### Property

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| ProjectId | uuid (Guid) | FK → Project |
| Name | string | Název |
| Description | string? | Popis |
| PropertyType | enum | Typ nemovitosti |
| Latitude | decimal? | GPS šířka (pro auto-tagging) |
| Longitude | decimal? | GPS délka (pro auto-tagging) |
| GeoRadius | int | Radius v metrech (default 100) |
| CoverMediaId | uuid (Guid)? | Titulní fotka (nullable) |
| CreatedAt | datetime | Vytvořeno |
| UpdatedAt | datetime | Upraveno |
| SyncMode | enum | `local-only` / `synced` |
| SyncStatus | enum | local / pending / syncing / synced / failed |
| LastSyncAt | datetime? | Poslední úspěšný sync |
| IsDeleted | bool | Soft delete |
| DeletedAt | datetime? | Kdy smazáno |

### Unit

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| PropertyId | uuid (Guid) | FK → Property |
| ParentUnitId | uuid (Guid)? | FK → Unit (hierarchie) |
| Name | string | Název |
| Description | string? | Popis |
| UnitType | enum | Typ jednotky |
| CoverMediaId | uuid (Guid)? | Titulní fotka (nullable) |
| CreatedAt | datetime | Vytvořeno |
| UpdatedAt | datetime | Upraveno |
| IsDeleted | bool | Soft delete |
| DeletedAt | datetime? | Kdy smazáno |

**PropertyType enum:**
- House
- Apartment
- Garage
- Garden
- Shed
- Land
- Other

**UnitType enum:**
- Room
- Floor
- Cellar
- Parking
- Other

### Zaznam

Obecný záznam (nahrazuje Repair).

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| PropertyId | uuid (Guid) | FK → Property |
| UnitId | uuid (Guid)? | FK → Unit (volitelné) |
| Title | string? | Název (required pro Complete, optional pro Draft) |
| Description | string? | Popis |
| Date | date | Datum záznamu (default dnes) |
| Cost | int? | Cena (volitelné) |
| Status | enum | Draft / Complete |
| Flags | int | Bitové flagy (MissingPhoto, TODO, Čeká, Důležité...) |
| CreatedAt | datetime | Vytvořeno |
| UpdatedAt | datetime | Upraveno |
| SyncMode | enum | `local-only` / `synced` |
| SyncStatus | enum | local / pending / syncing / synced / failed |
| LastSyncAt | datetime? | Poslední úspěšný sync |
| IsDeleted | bool | Soft delete |
| DeletedAt | datetime? | Kdy smazáno |

**ZaznamStatus enum:**
- `Draft` - Rozpracovaný, nemusí mít všechny povinné údaje
- `Complete` - Kompletní, validovaný záznam

**Poznámky:**
- `Flags` je `[Flags] enum` uložený jako int (viz rozhodnutí #016 a #019).
- `Tags` jsou many-to-many přes junction tabulku (`ZaznamTag`) na reference data `Tag`.
- Draft záznamy se po 30 dnech automaticky mažou (s upozorněním 7 dní předem).

### Media (nahrazuje ZaznamDokument)

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| OwnerType | enum | Property / Unit / Zaznam |
| OwnerId | uuid (Guid) | ID vlastníka |
| MediaType | enum | Photo / Document / Receipt |
| StorageKey | string | S3 key (ne URL) |
| OriginalFileName | string? | Původní název souboru |
| MimeType | string | Content-Type |
| SizeBytes | long | Velikost |
| Description | string? | Popis |
| CreatedAt | datetime | Vytvořeno |
| UpdatedAt | datetime | Upraveno |
| IsDeleted | bool | Soft delete |
| DeletedAt | datetime? | Kdy smazáno |

**Poznámky:**
- `Id` je client-generated UUID (pro offline sync bez remappingu).
- Index na `(OwnerType, OwnerId)` pro rychlé načtení galerie.

### Tag

Reference data pro tagy (předdefinovaný seznam).

| Pole | Typ | Popis |
|------|-----|-------|
| Id | smallint | PK |
| Name | string | Název tagu (max 50) |
| Icon | string | Název ikony (Lucide) |
| SortOrder | smallint | Pořadí zobrazení |
| IsActive | bool | Aktivní (default true) |

**Seed data (appsettings.json → DB migration):**

| Id | Name | Icon | Popis |
|----|------|------|-------|
| 1 | Oprava | Wrench | Opravy a servis |
| 2 | Údržba | Settings | Pravidelná údržba |
| 3 | Vylepšení | TrendingUp | Modernizace, upgrady |
| 4 | Nákup | ShoppingCart | Nákupy vybavení |
| 5 | Revize | ClipboardCheck | Povinné revize |
| 6 | Havárie | AlertTriangle | Nouzové opravy |
| 7 | Pojištění | Shield | Pojistné události |
| 8 | Smlouva | FileText | Smlouvy, dokumenty |
| 9 | Platba | CreditCard | Platby, účty |
| 10 | Záruka | BadgeCheck | Záruční opravy |

**Konfigurace:**

```jsonc
// appsettings.json
{
  "Tags": {
    "Seed": [
      { "Id": 1, "Name": "Oprava", "Icon": "Wrench" },
      // ...
    ]
  }
}
```

**Správa:**
- **VP:** Seed z appsettings, ruční úpravy v DB
- **Later:** Admin panel pro správu tagů (přidat, upravit, deaktivovat)

### ZaznamTag

Junction tabulka (many-to-many).

| Pole | Typ | Popis |
|------|-----|-------|
| ZaznamId | uuid (Guid) | FK → Zaznam |
| TagId | smallint | FK → Tag |

---

## Entity pro sdílení

### ProjectMember

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| ProjectId | uuid (Guid) | FK → Project |
| UserId | uuid (Guid) | FK → User |
| Role | enum | Owner / Editor / Viewer |
| Permissions | json? | Granulární práva (override) |
| CreatedAt | datetime | Vytvořeno |

### PropertyMember

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| PropertyId | uuid (Guid) | FK → Property |
| UserId | uuid (Guid) | FK → User |
| Role | enum | Owner / Editor / Viewer |
| Permissions | json? | Granulární práva (override) |
| CreatedAt | datetime | Vytvořeno |

### ZaznamMember

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| ZaznamId | uuid (Guid) | FK → Zaznam |
| UserId | uuid (Guid) | FK → User |
| Role | enum | Owner / Editor / Viewer |
| Permissions | json? | Granulární práva (override) |
| CreatedAt | datetime | Vytvořeno |

### Invitation

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| TokenHash | string | Hash tajného tokenu pro invite link (neukládat plaintext) |
| TargetType | enum | Project / Property / Zaznam |
| TargetId | uuid (Guid) | ID cíle |
| Email | string | Email pozvaného |
| Role | enum | Owner / Editor / Viewer |
| Permissions | json? | Granulární práva |
| Status | enum | Pending / Accepted / Declined / Expired |
| ExpiresAt | datetime | Expirace (7 dní) |
| CreatedAt | datetime | Vytvořeno |
| CreatedBy | uuid (Guid) | FK → User (kdo pozval) |

---

## Entity pro activity feed

### Activity

Záznam aktivity na sdílených properties (inspirace: Facebook activity feed).

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| PropertyId | uuid (Guid) | FK → Property |
| ActorUserId | uuid (Guid) | FK → User (kdo udělal akci) |
| Type | enum | Typ aktivity |
| TargetType | string? | Typ cíle (Zaznam, Comment, Member...) |
| TargetId | uuid (Guid)? | ID cíle |
| Metadata | json? | Dodatečná data (název záznamu, role člena...) |
| CreatedAt | datetime | Kdy |

**ActivityType enum:**
- `ZaznamCreated` - Nový záznam vytvořen
- `ZaznamUpdated` - Záznam upraven
- `ZaznamDeleted` - Záznam smazán
- `CommentAdded` - Přidán komentář
- `MemberJoined` - Nový člen se připojil
- `MemberLeft` - Člen odešel
- `MemberRoleChanged` - Změna role člena

**Poznámky:**
- Activity se generuje automaticky při změnách
- Uchovává se max. 90 dní (cleanup job)
- Není editovatelná (immutable log)

### Comment

Komentáře k záznamům s podporou @mentions.

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| ZaznamId | uuid (Guid) | FK → Zaznam |
| AuthorUserId | uuid (Guid) | FK → User |
| Content | string | Text komentáře |
| CreatedAt | datetime | Vytvořeno |
| UpdatedAt | datetime | Upraveno |
| IsDeleted | bool | Soft delete |
| DeletedAt | datetime? | Kdy smazáno |

### CommentMention

Junction tabulka pro @mentions v komentářích.

| Pole | Typ | Popis |
|------|-----|-------|
| CommentId | uuid (Guid) | FK → Comment |
| MentionedUserId | uuid (Guid) | FK → User |

**Poznámky:**
- Mention se extrahuje z textu při ukládání (@Jan → najdi User s jménem Jan v member listu)
- Při uložení se vytvoří notifikace pro zmíněného uživatele

---

## Entity pro notifikace a kontakty

### Notification

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| UserId | uuid (Guid) | FK → User |
| Type | enum | Typ notifikace |
| Payload | json | Data notifikace |
| ReadAt | datetime? | Kdy přečteno |
| CreatedAt | datetime | Vytvořeno |

**NotificationType enum:**
- `InvitationReceived` - Přijata pozvánka ke sdílení
- `InvitationAccepted` - Někdo přijal vaši pozvánku
- `InvitationExpired` - Pozvánka vypršela
- `MentionInComment` - Někdo vás označil v komentáři (@mention)
- `CommentOnYourZaznam` - Komentář k vašemu záznamu
- `DraftReminder` - Připomínka rozpracovaných záznamů
- `DraftExpiring` - Varování před smazáním draftu

### Contact

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| OwnerUserId | uuid (Guid) | FK → User |
| Email | string | Email kontaktu |
| DisplayName | string? | Zobrazované jméno |
| CreatedAt | datetime | Vytvořeno |

### ContactGroup

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| OwnerUserId | uuid (Guid) | FK → User |
| Name | string | Název skupiny |
| CreatedAt | datetime | Vytvořeno |

### ContactGroupMember

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| GroupId | uuid (Guid) | FK → ContactGroup |
| ContactId | uuid (Guid) | FK → Contact |

---

## Audit

### AuditLog

| Pole | Typ | Popis |
|------|-----|-------|
| Id | uuid (Guid) | PK |
| EntityType | string | Typ entity |
| EntityId | uuid (Guid) | ID entity |
| Action | string | Create / Update / Delete |
| ActorUserId | uuid (Guid) | FK → User (kdo udělal) |
| DiffSummary | json? | Co se změnilo |
| CreatedAt | datetime | Kdy |

---

## Aggregate boundaries

Agregáty jsou navrženy s ohledem na sync a výkon:

```
┌─────────────────────────────────────────┐
│  Project (aggregate root)               │
│  └── ProjectMember                      │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│  Property (aggregate root)              │
│  ├── Unit                               │
│  ├── PropertyMember                     │
│  └── Activity                           │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│  Zaznam (aggregate root)                │
│  ├── ZaznamDokument                     │
│  ├── ZaznamTag                          │
│  ├── Comment                            │
│  └── CommentMention                     │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│  User (aggregate root)                  │
│  ├── Contact                            │
│  ├── ContactGroup                       │
│  └── ContactGroupMember                 │
└─────────────────────────────────────────┘
```

**Proč Zaznam jako samostatný aggregate root:**
- Property → Units → Zaznamy → Dokumenty je příliš hluboká hierarchie
- Při syncu by se musel načítat celý strom (performance)
- Zaznam je nezávislý na životním cyklu Property
- Má vlastní konzistenční hranici (dokumenty, tagy, komentáře)
- Reference na Property/Unit přes `PropertyId` a `UnitId` (ne navigační property)

**Proč Activity pod Property:**
- Activity je vázaná na Property, ne na Zaznam
- Nemá smysl ji synchronizovat samostatně
- Je to append-only log, nezávislý na transakcích záznamu

**Důsledky:**
- Zaznam lze načíst a synchronizovat samostatně
- Mazání Property = soft delete + cascade nastavení přes ZaznamRepository
- Validace existence Property probíhá v Application vrstvě
- Activity se generuje jako side-effect v Application vrstvě

Viz rozhodnutí [09-decisions.md](09-decisions.md) #038

---

## Indexy pro PostgreSQL

### Foreign Keys (základní)

| Tabulka | Index | Sloupce |
|---------|-------|---------|
| Property | IX_Property_ProjectId | ProjectId |
| Unit | IX_Unit_PropertyId | PropertyId |
| Unit | IX_Unit_ParentUnitId | ParentUnitId |
| Zaznam | IX_Zaznam_PropertyId | PropertyId |
| Zaznam | IX_Zaznam_UnitId | UnitId |
| Comment | IX_Comment_ZaznamId | ZaznamId |
| ZaznamDokument | IX_ZaznamDokument_ZaznamId | ZaznamId |
| ZaznamTag | IX_ZaznamTag_TagId | TagId |
| Notification | IX_Notification_UserId | UserId |
| RefreshToken | IX_RefreshToken_UserId | UserId |

### Kompozitní pro listování

| Tabulka | Index | Sloupce |
|---------|-------|---------|
| Zaznam | IX_Zaznam_PropertyId_Date | PropertyId, Date DESC |
| Comment | IX_Comment_ZaznamId_CreatedAt | ZaznamId, CreatedAt DESC |
| Activity | IX_Activity_PropertyId_CreatedAt | PropertyId, CreatedAt DESC |
| Notification | IX_Notification_UserId_Unread | UserId, ReadAt (partial: WHERE ReadAt IS NULL) |

### Sdílení

| Tabulka | Index | Sloupce |
|---------|-------|---------|
| ProjectMember | UQ_ProjectMember_ProjectId_UserId | ProjectId, UserId (UNIQUE) |
| ProjectMember | IX_ProjectMember_UserId | UserId |
| PropertyMember | UQ_PropertyMember_PropertyId_UserId | PropertyId, UserId (UNIQUE) |
| PropertyMember | IX_PropertyMember_UserId | UserId |

### Pozvánky

| Tabulka | Index | Sloupce |
|---------|-------|---------|
| Invitation | IX_Invitation_TargetType_TargetId | TargetType, TargetId |
| Invitation | IX_Invitation_Email_Status | Email, Status |
| Invitation | IX_Invitation_Status_ExpiresAt | Status, ExpiresAt |
| Invitation | UQ_Invitation_TokenHash | TokenHash (UNIQUE) |

### Unique constraints

| Tabulka | Index | Sloupce |
|---------|-------|---------|
| User | UQ_User_Email | Email (UNIQUE, lowercase) |
| User | UQ_User_GoogleId | GoogleId (UNIQUE, nullable) |
| User | UQ_User_AppleId | AppleId (UNIQUE, nullable) |
| RefreshToken | UQ_RefreshToken_TokenHash | TokenHash (UNIQUE) |
| AppSetting | UQ_AppSetting_Key | Key (UNIQUE / PK) |
| Contact | UQ_Contact_OwnerUserId_Email | OwnerUserId, Email (UNIQUE, lowercase) |
| ZaznamTag | UQ_ZaznamTag_ZaznamId_TagId | ZaznamId, TagId (UNIQUE / composite PK) |

### Filtrování

| Tabulka | Index | Sloupce |
|---------|-------|---------|
| Zaznam | IX_Zaznam_Status | Status |

### Full-text search

Odloženo - local-first search běží na klientu (IndexedDB).

Pokud bude potřeba server-side: jeden `tsvector(Title, Description)` + GIN, jazyk `czech`.

---

## Délky stringů

| Entity | Pole | Max délka | Poznámka |
|--------|------|-----------|----------|
| User | FirstName | 50 | |
| User | LastName | 50 | |
| User | Phone | 20 | |
| User | GoogleId | 100 | |
| User | AppleId | 100 | |
| User | BlockedReason | 500 | Admin poznámka |
| Project | Name | 100 | |
| Property | Name | 100 | |
| Property | Description | 500 | |
| Unit | Name | 100 | |
| Unit | Description | 500 | |
| Zaznam | Title | 200 | |
| Zaznam | Description | 5000 | Delší text |
| Comment | Content | 2000 | |
| Tag | Name | 50 | |
| Tag | Icon | 50 | Lucide icon name |
| Contact | Email | 254 | RFC 5321 |
| Contact | DisplayName | 100 | |
| ContactGroup | Name | 100 | |
| ZaznamDokument | StorageKey | 500 | S3 key |
| ZaznamDokument | OriginalFileName | 255 | |
| ZaznamDokument | MimeType | 100 | |
| ZaznamDokument | Description | 500 | |
| RefreshToken | TokenHash | 128 | SHA-256/512 |
| RefreshToken | DeviceInfo | 500 | User-Agent |
| Invitation | TokenHash | 128 | |
| Invitation | Email | 254 | RFC 5321 |
| AuditLog | EntityType | 100 | |
| AuditLog | Action | 50 | |
| Activity | TargetType | 100 | |
| Notification | Type | 50 | Enum jako string |

---

## File Storage (S3)

### Buckety

| Bucket | Přístup | Obsah |
|--------|---------|-------|
| `mujdomecek-public` | Veřejný | Avatary uživatelů |
| `mujdomecek-private` | Presigned URLs | Dokumenty, fotky záznamů |

### Storage Keys

| Typ | Pattern | Příklad |
|-----|---------|---------|
| Avatar (256x256) | `avatars/{userId}/{guid}.{ext}` | `avatars/abc123/def456.jpg` |
| Avatar thumbnail (64x64) | `avatars/{userId}/{guid}_thumb.{ext}` | `avatars/abc123/def456_thumb.jpg` |
| Dokument záznamu | `documents/{propertyId}/{zaznamId}/{guid}.{ext}` | `documents/abc/def/ghi.pdf` |
| Thumbnail dokumentu | `documents/{propertyId}/{zaznamId}/{guid}_thumb.{ext}` | `documents/abc/def/ghi_thumb.jpg` |

### Avatary

- **Bucket:** Veřejný (žádné presigned URLs)
- **Upload:** `POST /api/users/me/avatar` (multipart/form-data)
- **Zpracování:** Server resize na 256x256 + 64x64 thumbnail
- **Formáty:** JPEG, PNG, WebP
- **Max velikost:** 2 MB
- **Default:** Iniciály na barevném pozadí (CSS, bez obrázku)

### Dokumenty záznamů

- **Bucket:** Privátní (presigned URLs, expirace 1h)
- **Upload:** Presigned PUT URL z `/api/upload/request`
- **Thumbnail:** Generován serverem pro obrázky
- **Max velikost:** 20 MB
