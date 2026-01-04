# API Reference

Kompletní seznam všech API endpointů.

**Base URL:** `https://api.mujdomecek.cz/api`

**Autentizace:** JWT Bearer token v headeru `Authorization: Bearer <token>`

**Content-Type:** `application/json` (pokud není uvedeno jinak)

---

## Přehled

| Skupina | Prefix | Popis |
|---------|--------|-------|
| [Auth](#auth) | `/auth` | Registrace, přihlášení, hesla |
| [Users](#users) | `/users` | Profil, preference |
| [Projects](#projects) | `/projects` | Projekty (skupiny nemovitostí) |
| [Properties](#properties) | `/properties` | Nemovitosti |
| [Units](#units) | `/units` | Jednotky (místnosti, patra) |
| [Zaznamy](#zaznamy) | `/zaznamy` | Záznamy (opravy, údržba) |
| [Comments](#comments) | `/comments` | Komentáře k záznamům |
| [Media](#media) | `/media` | Fotky a dokumenty |
| [Upload](#upload) | `/upload` | Upload souborů |
| [Sharing](#sharing) | `/sharing` | Přehled sdílení |
| [Invitations](#invitations) | `/invitations` | Pozvánky |
| [Contacts](#contacts) | `/contacts` | Kontakty |
| [Contact Groups](#contact-groups) | `/contact-groups` | Skupiny kontaktů |
| [Notifications](#notifications) | `/notifications` | Notifikace |
| [Activity](#activity) | `/activity` | Activity feed |
| [Sync](#sync) | `/sync` | Synchronizace |
| [Admin](#admin) | `/admin` | Admin panel (pouze Admin role) |

---

## Auth

### Registrace a přihlášení

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/auth/register` | POST | - | Registrace nového uživatele |
| `/auth/confirm-email` | GET | - | Potvrzení emailu |
| `/auth/resend-confirmation` | POST | - | Znovu poslat potvrzení |
| `/auth/login` | POST | - | Přihlášení |
| `/auth/logout` | POST | JWT | Odhlášení |
| `/auth/refresh` | POST | Cookie | Obnovení JWT |

### Heslo

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/auth/forgot-password` | POST | - | Požadavek na reset hesla |
| `/auth/validate-reset-token` | GET | - | Ověření platnosti reset tokenu |
| `/auth/reset-password` | POST | - | Nastavení nového hesla |
| `/auth/change-password` | POST | JWT | Změna hesla (přihlášený) |

### Externí přihlášení (OIDC)

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/auth/google` | GET | - | Redirect na Google OIDC |
| `/auth/google/callback` | GET | - | Google callback |
| `/auth/apple` | GET | - | Redirect na Apple OIDC |
| `/auth/apple/callback` | GET | - | Apple callback |
| `/auth/link-google` | POST | JWT | Propojit Google účet |
| `/auth/unlink-google` | POST | JWT | Odpojit Google účet |
| `/auth/link-apple` | POST | JWT | Propojit Apple účet |
| `/auth/unlink-apple` | POST | JWT | Odpojit Apple účet |

### Sessions

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/auth/sessions` | GET | JWT | Seznam aktivních sessions |
| `/auth/sessions/{id}` | DELETE | JWT | Odhlásit konkrétní session |
| `/auth/sessions/revoke-all` | POST | JWT | Odhlásit všude kromě current |

### Request/Response

```typescript
// POST /auth/register
interface RegisterRequest {
  firstName: string;      // 2-50 znaků
  lastName: string;       // 2-50 znaků
  email: string;
  password: string;       // min 6, velké+malé+číslo
}

// POST /auth/login
interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  accessToken: string;
  expiresIn: number;      // sekundy
  user: UserProfileResponse;
}

// POST /auth/forgot-password
interface ForgotPasswordRequest {
  email: string;
}

// POST /auth/reset-password
interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

// POST /auth/change-password
interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
```

---

## Users

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/users/me` | GET | JWT | Profil přihlášeného uživatele |
| `/users/me` | PUT | JWT | Aktualizace profilu |
| `/users/me` | DELETE | JWT | Smazání účtu |
| `/users/me/avatar` | POST | JWT | Upload profilové fotky |
| `/users/me/avatar` | DELETE | JWT | Smazání profilové fotky |
| `/users/me/preferences` | GET | JWT | Získat preference |
| `/users/me/preferences` | PUT | JWT | Aktualizovat preference |
| `/users/me/export` | POST | JWT | Požádat o export dat |

### Request/Response

```typescript
// GET /users/me
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
    google?: string;
    apple?: string;
  };
  createdAt: string;
}

// PUT /users/me
interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
  phone?: string;
  preferredLanguage?: 'cs' | 'en';
  themePreference?: 'light' | 'dark' | 'system';
}

// GET /users/me/preferences
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

// DELETE /users/me
interface DeleteAccountRequest {
  password: string;
  confirmation: "DELETE_MY_ACCOUNT";
}

// POST /users/me/export
interface ExportDataRequest {
  format?: 'json' | 'zip';
}
```

### Avatar

```typescript
// POST /users/me/avatar
// Content-Type: multipart/form-data
// Field: "file" - obrázek

interface AvatarUploadResponse {
  avatarUrl: string;          // Veřejná URL (public bucket)
}

// Omezení:
// - Max velikost: 2 MB
// - Formáty: JPEG, PNG, WebP
// - Server resize na 256x256 (hlavní) + 64x64 (thumbnail)
// - Storage: public bucket, key: avatars/{userId}/{guid}.{ext}

// DELETE /users/me/avatar
// Response: 204 No Content
```

**Poznámky k avatarům:**
- Veřejný S3 bucket (žádné presigned URLs)
- URL se nemění (cache-friendly), pouze při změně avataru
- Default avatar: iniciály na barevném pozadí (generováno klientem z CSS)
- Thumbnail (64x64) pro seznamy členů, komentáře

---

## Projects

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/projects` | GET | JWT | Seznam projektů |
| `/projects` | POST | JWT | Vytvořit projekt |
| `/projects/{id}` | GET | JWT | Detail projektu |
| `/projects/{id}` | PUT | JWT | Upravit projekt |
| `/projects/{id}` | DELETE | JWT | Smazat projekt |
| `/projects/{id}/members` | GET | JWT | Seznam členů |
| `/projects/{id}/members` | POST | JWT | Přidat člena (pozvánka) |
| `/projects/{id}/members/{userId}` | PUT | JWT | Změnit roli člena |
| `/projects/{id}/members/{userId}` | DELETE | JWT | Odebrat člena |
| `/projects/{id}/leave` | POST | JWT | Opustit projekt |

### Request/Response

```typescript
// GET /projects
interface ProjectListResponse {
  items: ProjectDto[];
  total: number;
}

interface ProjectDto {
  id: string;
  name: string;
  description?: string;
  propertyCount: number;
  memberCount: number;
  myRole: 'owner' | 'editor' | 'viewer';
  syncMode: 'local-only' | 'synced';
  syncStatus: 'local' | 'pending' | 'syncing' | 'synced' | 'failed';
  createdAt: string;
  updatedAt: string;
}

// POST /projects
interface CreateProjectRequest {
  name: string;           // max 100 znaků
  description?: string;   // max 500 znaků
}

// PUT /projects/{id}
interface UpdateProjectRequest {
  name?: string;
  description?: string;
}

// GET /projects/{id}
interface ProjectDetailResponse extends ProjectDto {
  properties: PropertyDto[];
  members: MemberDto[];
}
```

---

## Properties

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/properties` | GET | JWT | Seznam nemovitostí |
| `/properties` | POST | JWT | Vytvořit nemovitost |
| `/properties/{id}` | GET | JWT | Detail nemovitosti |
| `/properties/{id}` | PUT | JWT | Upravit nemovitost |
| `/properties/{id}` | DELETE | JWT | Smazat nemovitost |
| `/properties/{id}/stats` | GET | JWT | Statistiky (náklady, počty) |
| `/properties/{id}/members` | GET | JWT | Seznam členů |
| `/properties/{id}/members` | POST | JWT | Přidat člena (pozvánka) |
| `/properties/{id}/members/{userId}` | PUT | JWT | Změnit roli člena |
| `/properties/{id}/members/{userId}` | DELETE | JWT | Odebrat člena |
| `/properties/{id}/cover` | PATCH | JWT | Nastavit titulní foto |
| `/properties/{id}/leave` | POST | JWT | Opustit property |
| `/properties/{id}/activity` | GET | JWT | Activity feed |

### Query parametry pro GET /properties

| Parametr | Typ | Popis |
|----------|-----|-------|
| `projectId` | uuid | Filtr podle projektu |
| `shared` | bool | `true` = sdílené se mnou, `false` = moje |

### Request/Response

```typescript
// GET /properties
interface PropertyListResponse {
  items: PropertyDto[];
  total: number;
}

interface PropertyDto {
  id: string;
  projectId: string;
  projectName: string;
  name: string;
  description?: string;
  propertyType: 'house' | 'apartment' | 'garage' | 'garden' | 'shed' | 'land' | 'other';
  coverMediaId?: string;
  coverUrl?: string;
  latitude?: number;
  longitude?: number;
  geoRadius: number;
  unitCount: number;
  zaznamCount: number;
  totalCost: number;
  myRole: 'owner' | 'editor' | 'viewer';
  isShared: boolean;
  syncMode: 'local-only' | 'synced';
  syncStatus: 'local' | 'pending' | 'syncing' | 'synced' | 'failed';
  createdAt: string;
  updatedAt: string;
}

// POST /properties
interface CreatePropertyRequest {
  projectId: string;
  name: string;           // max 100 znaků
  description?: string;   // max 500 znaků
  propertyType?: 'house' | 'apartment' | 'garage' | 'garden' | 'shed' | 'land' | 'other';
  latitude?: number;
  longitude?: number;
  geoRadius?: number;     // default 100
}

// PUT /properties/{id}
interface UpdatePropertyRequest {
  name?: string;
  description?: string;
  propertyType?: 'house' | 'apartment' | 'garage' | 'garden' | 'shed' | 'land' | 'other';
  latitude?: number;
  longitude?: number;
  geoRadius?: number;
}

// PATCH /properties/{id}/cover
interface SetCoverRequest {
  coverMediaId?: string;  // null = use first photo or default
}

// GET /properties/{id}/stats
interface PropertyStatsResponse {
  totalCost: number;
  zaznamCount: number;
  draftCount: number;
  documentCount: number;
  costByMonth: { month: string; cost: number }[];
  costByYear: { year: number; cost: number }[];
}
```

---

## Units

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/units` | GET | JWT | Seznam jednotek |
| `/units` | POST | JWT | Vytvořit jednotku |
| `/units/{id}` | GET | JWT | Detail jednotky |
| `/units/{id}` | PUT | JWT | Upravit jednotku |
| `/units/{id}` | DELETE | JWT | Smazat jednotku |
| `/units/{id}/cover` | PATCH | JWT | Nastavit titulní foto |

### Query parametry pro GET /units

| Parametr | Typ | Popis |
|----------|-----|-------|
| `propertyId` | uuid | **Required** - filtr podle property |
| `parentUnitId` | uuid | Filtr podle parent unit |

### Request/Response

```typescript
interface UnitDto {
  id: string;
  propertyId: string;
  parentUnitId?: string;
  name: string;
  description?: string;
  coverMediaId?: string;
  coverUrl?: string;
  unitType: 'room' | 'floor' | 'cellar' | 'parking' | 'other';
  childCount: number;
  zaznamCount: number;
  createdAt: string;
  updatedAt: string;
}

// PATCH /units/{id}/cover
interface SetUnitCoverRequest {
  coverMediaId?: string;  // null = use first photo or default
}

// POST /units
interface CreateUnitRequest {
  propertyId: string;
  parentUnitId?: string;
  name: string;           // max 100 znaků
  description?: string;   // max 500 znaků
  unitType: 'room' | 'floor' | 'cellar' | 'parking' | 'other';
}

// PUT /units/{id}
interface UpdateUnitRequest {
  name?: string;
  description?: string;
  unitType?: 'room' | 'floor' | 'cellar' | 'parking' | 'other';
  parentUnitId?: string;
}
```

---

## Zaznamy

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/zaznamy` | GET | JWT | Seznam záznamů |
| `/zaznamy` | POST | JWT | Vytvořit záznam |
| `/zaznamy/{id}` | GET | JWT | Detail záznamu |
| `/zaznamy/{id}` | PUT | JWT | Upravit záznam |
| `/zaznamy/{id}` | DELETE | JWT | Smazat záznam |
| `/zaznamy/{id}/members` | GET | JWT | Seznam členů |
| `/zaznamy/{id}/members` | POST | JWT | Přidat člena (pozvánka) |
| `/zaznamy/{id}/members/{userId}` | PUT | JWT | Změnit roli člena |
| `/zaznamy/{id}/members/{userId}` | DELETE | JWT | Odebrat člena |
| `/zaznamy/{id}/complete` | POST | JWT | Dokončit draft |
| `/zaznamy/drafts` | GET | JWT | Seznam draftů |

### Query parametry pro GET /zaznamy

| Parametr | Typ | Popis |
|----------|-----|-------|
| `propertyId` | uuid | Filtr podle property |
| `unitId` | uuid | Filtr podle unit |
| `status` | string | `draft` nebo `complete` |
| `from` | date | Od data |
| `to` | date | Do data |
| `tags` | string[] | Filtr podle tagů |
| `search` | string | Fulltext v názvu/popisu |
| `page` | int | Stránka (default 1) |
| `pageSize` | int | Velikost stránky (default 20, max 100) |
| `sort` | string | `date`, `createdAt`, `cost` |
| `order` | string | `asc` nebo `desc` |

### Request/Response

```typescript
// GET /zaznamy
interface ZaznamListResponse {
  items: ZaznamDto[];
  total: number;
  page: number;
  pageSize: number;
}

interface ZaznamDto {
  id: string;
  propertyId: string;
  propertyName: string;
  unitId?: string;
  unitName?: string;
  title?: string;
  description?: string;
  date: string;           // YYYY-MM-DD
  cost?: number;
  status: 'draft' | 'complete';
  flags: string[];        // ['important', 'warranty', 'todo']
  tags: string[];
  documentCount: number;
  commentCount: number;
  thumbnailUrl?: string;  // první fotka
  syncMode: 'local-only' | 'synced';
  syncStatus: 'local' | 'synced' | 'syncing' | 'failed';
  createdAt: string;
  updatedAt: string;
  createdBy: { id: string; name: string };
}

// POST /zaznamy
interface CreateZaznamRequest {
  propertyId: string;
  unitId?: string;
  title?: string;         // required pro complete, optional pro draft
  description?: string;   // max 5000 znaků
  date?: string;          // default today
  cost?: number;
  status?: 'draft' | 'complete';  // default 'complete'
  flags?: string[];
  tags?: string[];
}

// PUT /zaznamy/{id}
interface UpdateZaznamRequest {
  unitId?: string;
  title?: string;
  description?: string;
  date?: string;
  cost?: number;
  flags?: string[];
  tags?: string[];
}

// GET /zaznamy/{id}
interface ZaznamDetailResponse extends ZaznamDto {
  media: MediaDto[];
  comments: CommentDto[];
  auditLog: AuditLogEntry[];
}
```

---

## Comments

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/zaznamy/{zaznamId}/comments` | GET | JWT | Seznam komentářů |
| `/zaznamy/{zaznamId}/comments` | POST | JWT | Přidat komentář |
| `/comments/{id}` | PUT | JWT | Upravit komentář |
| `/comments/{id}` | DELETE | JWT | Smazat komentář |

### Request/Response

```typescript
interface CommentDto {
  id: string;
  zaznamId: string;
  content: string;
  mentions: { userId: string; name: string }[];
  author: { id: string; name: string; avatarUrl?: string };
  createdAt: string;
  updatedAt: string;
  isEdited: boolean;
}

// POST /zaznamy/{zaznamId}/comments
interface CreateCommentRequest {
  content: string;        // max 2000 znaků, může obsahovat @mentions
}

// PUT /comments/{id}
interface UpdateCommentRequest {
  content: string;
}
```

---

## Media

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/media` | GET | JWT | Galerie podle ownerType + ownerId |
| `/media` | POST | JWT | Přidat media (po uploadu) |
| `/media/{id}` | PUT | JWT | Upravit caption |
| `/media/{id}` | DELETE | JWT | Smazat media |
| `/media/{id}/url` | GET | JWT | Presigned URL pro download |

### Request/Response

```typescript
interface MediaDto {
  id: string;
  ownerType: 'property' | 'unit' | 'zaznam';
  ownerId: string;
  mediaType: 'photo' | 'document' | 'receipt';
  storageKey: string;
  originalFileName?: string;
  mimeType: string;
  sizeBytes: number;
  caption?: string;
  thumbnailUrl?: string;
  createdAt: string;
}

// GET /media?ownerType=property&ownerId={id}
interface MediaListResponse {
  items: MediaDto[];
}

// POST /media
interface AddMediaRequest {
  ownerType: 'property' | 'unit' | 'zaznam';
  ownerId: string;
  storageKey: string;     // z upload/confirm
  mediaType: 'photo' | 'document' | 'receipt';
  originalFileName?: string;
  mimeType: string;
  sizeBytes: number;
  caption?: string;
}

// PUT /media/{id}
interface UpdateMediaRequest {
  caption?: string;
}

// GET /media/{id}/url
interface MediaUrlResponse {
  url: string;
  expiresAt: string;
}
```

---

## Upload

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/upload/request` | POST | JWT | Získat presigned URL pro upload |
| `/upload/confirm` | POST | JWT | Potvrdit dokončený upload |
| `/upload/{storageKey}` | DELETE | JWT | Zrušit/smazat upload |

### Request/Response

```typescript
// POST /upload/request
interface UploadRequestRequest {
  fileName: string;
  mimeType: string;
  sizeBytes: number;
}

interface UploadRequestResponse {
  storageKey: string;
  uploadUrl: string;      // presigned PUT URL
  expiresAt: string;
}

// POST /upload/confirm
interface UploadConfirmRequest {
  storageKey: string;
}

interface UploadConfirmResponse {
  storageKey: string;
  url: string;            // presigned GET URL
  thumbnailUrl?: string;
}
```

---

## Sharing

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/sharing/my-shares` | GET | JWT | Co sdílím |
| `/sharing/shared-with-me` | GET | JWT | Sdíleno se mnou |
| `/sharing/pending-invitations` | GET | JWT | Čekající pozvánky |

### Request/Response

```typescript
// GET /sharing/my-shares
interface MySharesResponse {
  items: {
    type: 'project' | 'property' | 'zaznam';
    id: string;
    name: string;
    projectName?: string;
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

// GET /sharing/shared-with-me
interface SharedWithMeResponse {
  items: {
    type: 'project' | 'property' | 'zaznam';
    id: string;
    name: string;
    owner: { userId: string; email: string; displayName: string };
    myRole: 'editor' | 'viewer';
    sharedAt: string;
  }[];
}

// GET /sharing/pending-invitations
interface PendingInvitationsResponse {
  invitations: {
    id: string;
    type: 'project' | 'property' | 'zaznam';
    targetId: string;
    targetName: string;
    role: 'owner' | 'editor' | 'viewer';
    invitedBy: { userId: string; email: string; displayName: string };
    createdAt: string;
    expiresAt: string;
  }[];
}
```

---

## Invitations

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/invitations/{id}` | GET | JWT | Detail pozvánky |
| `/invitations/by-token` | GET | JWT | Detail pozvánky podle tokenu |
| `/invitations/{id}/accept` | POST | JWT | Přijmout pozvánku |
| `/invitations/{id}/decline` | POST | JWT | Odmítnout pozvánku |
| `/invitations/{id}/resend` | POST | JWT | Znovu poslat pozvánku |
| `/invitations/{id}` | DELETE | JWT | Zrušit odeslanou pozvánku |

### Request/Response

```typescript
interface InvitationDto {
  id: string;
  type: 'project' | 'property' | 'zaznam';
  targetId: string;
  targetName: string;
  email: string;
  role: 'owner' | 'editor' | 'viewer';
  permissions?: Record<string, boolean>;
  status: 'pending' | 'accepted' | 'declined' | 'expired';
  invitedBy: { userId: string; email: string; displayName: string };
  createdAt: string;
  expiresAt: string;
}

// GET /invitations/by-token?token=...
// Response: InvitationDto
```

---

## Contacts

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/contacts` | GET | JWT | Seznam kontaktů |
| `/contacts` | POST | JWT | Přidat kontakt |
| `/contacts/{id}` | GET | JWT | Detail kontaktu |
| `/contacts/{id}` | PUT | JWT | Upravit kontakt |
| `/contacts/{id}` | DELETE | JWT | Smazat kontakt |

### Request/Response

```typescript
interface ContactDto {
  id: string;
  email: string;
  displayName?: string;
  isRegistered: boolean;  // má účet v systému
  userId?: string;        // pokud registrovaný
  createdAt: string;
}

// POST /contacts
interface CreateContactRequest {
  email: string;
  displayName?: string;
}

// PUT /contacts/{id}
interface UpdateContactRequest {
  displayName?: string;
}
```

---

## Contact Groups

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/contact-groups` | GET | JWT | Seznam skupin |
| `/contact-groups` | POST | JWT | Vytvořit skupinu |
| `/contact-groups/{id}` | GET | JWT | Detail skupiny |
| `/contact-groups/{id}` | PUT | JWT | Upravit skupinu |
| `/contact-groups/{id}` | DELETE | JWT | Smazat skupinu |
| `/contact-groups/{id}/members` | POST | JWT | Přidat člena |
| `/contact-groups/{id}/members/{contactId}` | DELETE | JWT | Odebrat člena |

### Request/Response

```typescript
interface ContactGroupDto {
  id: string;
  name: string;
  memberCount: number;
  members: ContactDto[];
  createdAt: string;
}

// POST /contact-groups
interface CreateContactGroupRequest {
  name: string;           // max 50 znaků
  contactIds?: string[];
}

// POST /contact-groups/{id}/members
interface AddGroupMemberRequest {
  contactId: string;
}
```

---

## Notifications

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/notifications` | GET | JWT | Seznam notifikací |
| `/notifications/unread-count` | GET | JWT | Počet nepřečtených |
| `/notifications/{id}/read` | POST | JWT | Označit jako přečtené |
| `/notifications/read-all` | POST | JWT | Označit vše jako přečtené |
| `/notifications/{id}` | DELETE | JWT | Smazat notifikaci |

### Query parametry pro GET /notifications

| Parametr | Typ | Popis |
|----------|-----|-------|
| `unreadOnly` | bool | Pouze nepřečtené |
| `page` | int | Stránka |
| `pageSize` | int | Velikost stránky |

### Request/Response

```typescript
interface NotificationDto {
  id: string;
  type: 'invitation_received' | 'invitation_accepted' | 'invitation_declined' | 'invitation_expired'
      | 'mention_in_comment' | 'comment_on_zaznam' | 'draft_reminder' | 'draft_expiring';
  payload: Record<string, any>;
  readAt?: string;
  createdAt: string;
}

interface NotificationListResponse {
  items: NotificationDto[];
  total: number;
  unreadCount: number;
}
```

---

## Activity

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/properties/{propertyId}/activity` | GET | JWT | Activity feed property |

### Query parametry

| Parametr | Typ | Popis |
|----------|-----|-------|
| `page` | int | Stránka |
| `pageSize` | int | Velikost stránky (max 50) |

### Request/Response

```typescript
interface ActivityDto {
  id: string;
  type: 'zaznam_created' | 'zaznam_updated' | 'zaznam_deleted'
      | 'comment_added' | 'member_joined' | 'member_left' | 'member_role_changed';
  actor: { id: string; name: string; avatarUrl?: string };
  targetType?: string;
  targetId?: string;
  metadata?: Record<string, any>;
  createdAt: string;
}

interface ActivityListResponse {
  items: ActivityDto[];
  total: number;
}
```

---

## Sync

| Endpoint | Metoda | Auth | Popis |
|----------|--------|------|-------|
| `/sync/status` | GET | JWT | Stav synchronizace (per scope) |
| `/sync/push` | POST | JWT | Push lokálních změn (per scope) |
| `/sync/pull` | GET | JWT | Pull změn ze serveru (per scope) |

### Request/Response

```typescript
// GET /sync/status?scopeType={project|property|zaznam}&scopeId={id}
interface SyncStatusResponse {
  lastSyncAt?: string;
  pendingChanges: number;
  serverTime: string;
}

// POST /sync/push
interface SyncPushRequest {
  correlationId: string;
  scopeType: 'project' | 'property' | 'zaznam';
  scopeId: string;
  changes: SyncChange[];
}

interface SyncChange {
  id: string;
  entityType: 'project' | 'property' | 'unit' | 'zaznam' | 'document' | 'comment';
  entityId: string;
  operation: 'create' | 'update' | 'delete';
  data?: Record<string, any>;
  clientTimestamp: string;
}

interface SyncPushResponse {
  correlationId: string;
  accepted: string[];
  rejected: { id: string; reason: string }[];
  conflicts: { id: string; serverVersion: any }[];
  serverTimestamp: string;
}

// GET /sync/pull?since={timestamp}&scopeType={project|property|zaznam}&scopeId={id}
interface SyncPullResponse {
  changes: {
    entityType: string;
    entityId: string;
    operation: 'create' | 'update' | 'delete';
    data?: Record<string, any>;
    serverTimestamp: string;
  }[];
  serverTimestamp: string;
  hasMore: boolean;
}
```

---

## Společné typy

### Pagination

```typescript
interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
```

### Error Response

```typescript
interface ErrorResponse {
  error: {
    code: string;
    message: string;
    details?: Record<string, string[]>;
  };
}
```

### Členové (Members)

```typescript
interface MemberDto {
  userId: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  role: 'owner' | 'editor' | 'viewer';
  permissions?: Record<string, boolean>;
  status: 'active' | 'pending';
  joinedAt?: string;
}

// Sdílení u property/záznamů může spravovat pouze owner projektu.

// POST /properties/{id}/members, /projects/{id}/members nebo /zaznamy/{id}/members
interface AddMemberRequest {
  email: string;
  role: 'editor' | 'viewer';
  permissions?: Record<string, boolean>;
}

// Response: 202 Accepted + InvitationLinkResponse
interface InvitationLinkResponse {
  invitationId: string;
  inviteUrl: string;
  expiresAt: string;
}

// POST /invitations/{id}/resend
// Response: InvitationLinkResponse

// PUT /properties/{id}/members/{userId} nebo /zaznamy/{id}/members/{userId}
interface UpdateMemberRequest {
  role?: 'editor' | 'viewer';
  permissions?: Record<string, boolean>;
}
```

---

## Error kódy

| Kód | HTTP | Popis |
|-----|------|-------|
| `UNAUTHORIZED` | 401 | Chybí nebo neplatný JWT |
| `FORBIDDEN` | 403 | Nedostatečná práva |
| `NOT_FOUND` | 404 | Entita nenalezena |
| `VALIDATION_ERROR` | 400 | Neplatná data (viz details) |
| `CONFLICT` | 409 | Konflikt (např. duplicitní email) |
| `RATE_LIMITED` | 429 | Příliš mnoho požadavků |
| `INTERNAL_ERROR` | 500 | Interní chyba serveru |

### Příklady

```json
// 400 Validation Error
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Neplatná data",
    "details": {
      "email": ["Email je povinný", "Neplatný formát emailu"],
      "password": ["Heslo musí mít minimálně 6 znaků"]
    }
  }
}

// 403 Forbidden
{
  "error": {
    "code": "FORBIDDEN",
    "message": "Nemáte oprávnění k této akci"
  }
}

// 409 Conflict
{
  "error": {
    "code": "CONFLICT",
    "message": "Uživatel s tímto emailem již existuje"
  }
}
```

---

## Rate Limiting

| Endpoint | Limit |
|----------|-------|
| `/auth/login` | 5/min per IP |
| `/auth/register` | 3/min per IP |
| `/auth/forgot-password` | 3/hour per email |
| `/sync/push` | 60/min per user |
| Ostatní | 100/min per user |

Response header při překročení:
```
Retry-After: 60
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1703600000
```

---

## Admin

**Vyžaduje:** Role `Admin`

### Dashboard & Stats

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/admin/stats/dashboard` | GET | Dashboard metriky |
| `/admin/stats/health` | GET | System health check |

### Users

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/admin/users` | GET | Seznam uživatelů (paginated) |
| `/admin/users/{id}` | GET | Detail uživatele |
| `/admin/users/{id}/block` | POST | Blokovat uživatele |
| `/admin/users/{id}/unblock` | POST | Odblokovat uživatele |
| `/admin/users/{id}` | DELETE | Smazat uživatele (GDPR) |
| `/admin/users/{id}/sessions` | GET | Seznam sessions |
| `/admin/users/{id}/sessions` | DELETE | Revoke all sessions |

### Tags

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/admin/tags` | GET | Seznam tagů |
| `/admin/tags` | POST | Vytvořit tag |
| `/admin/tags/{id}` | PUT | Upravit tag |
| `/admin/tags/{id}/deactivate` | POST | Deaktivovat tag |
| `/admin/tags/reorder` | POST | Změnit pořadí |

### Settings

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/admin/settings` | GET | Seznam systémových nastavení (effective values + source) |
| `/admin/settings` | PUT | Uložit systémová nastavení (bulk update) |

### Audit Log

| Endpoint | Metoda | Popis |
|----------|--------|-------|
| `/admin/audit` | GET | Seznam audit logů (paginated) |
| `/admin/audit/export` | GET | CSV export |

### Request/Response

```typescript
// GET /admin/stats/dashboard
interface AdminDashboardResponse {
  users: {
    total: number;
    active7d: number;
    blocked: number;
  };
  content: {
    projects: number;
    properties: number;
    zaznamy: number;
    zaznamy24h: number;
  };
  storage: {
    totalBytes: number;
    fileCount: number;
  };
  invitations: {
    pending: number;
  };
}

// GET /admin/stats/health
interface HealthCheckResponse {
  status: 'healthy' | 'degraded' | 'unhealthy';
  checks: {
    database: { status: string; responseTimeMs: number };
    storage: { status: string };
    email: { status: string };
    backgroundJobs: { status: string; pending: number };
  };
}

// GET /admin/users
interface AdminUserListResponse {
  items: AdminUserDto[];
  total: number;
  page: number;
  pageSize: number;
}

interface AdminUserDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  emailVerified: boolean;
  avatarUrl?: string;
  status: 'active' | 'blocked' | 'deleted';
  projectCount: number;
  lastLoginAt?: string;
  createdAt: string;
}

// POST /admin/users/{id}/block
interface BlockUserRequest {
  reason: string;  // Required
}

// POST /admin/tags
interface CreateTagRequest {
  name: string;      // max 50
  icon: string;      // Lucide icon name
  sortOrder?: number;
}

// PUT /admin/tags/{id}
interface UpdateTagRequest {
  name?: string;
  icon?: string;
  sortOrder?: number;
}

// GET /admin/settings
type AdminSettingSource = 'default' | 'appsettings' | 'db';

interface AdminSettingDto {
  key: string;            // např. "Constraints.Files.MaxPhotosPerZaznamFree"
  value: unknown;         // JSON value
  source: AdminSettingSource;
  updatedAt?: string;
  updatedBy?: { id: string; name: string };
}

interface AdminSettingsResponse {
  items: AdminSettingDto[];
}

// PUT /admin/settings
interface UpdateAdminSettingsRequest {
  items: Array<{ key: string; value: unknown }>;
}

// POST /admin/tags/reorder
interface ReorderTagsRequest {
  order: { id: number; sortOrder: number }[];
}

// GET /admin/audit
interface AuditLogListResponse {
  items: AuditLogDto[];
  total: number;
  page: number;
  pageSize: number;
}

interface AuditLogDto {
  id: string;
  entityType: string;
  entityId: string;
  action: 'create' | 'update' | 'delete';
  actor: { id: string; name: string; email: string };
  diffSummary?: Record<string, { old: any; new: any }>;
  createdAt: string;
}
```

### Query parametry

**GET /admin/users:**

| Parametr | Typ | Popis |
|----------|-----|-------|
| `status` | string | `active`, `blocked`, `deleted` |
| `search` | string | Hledání v jménu/emailu |
| `from` | date | Registrace od |
| `to` | date | Registrace do |
| `page` | int | Stránka |
| `pageSize` | int | Velikost (max 100) |
| `sort` | string | `createdAt`, `lastLoginAt`, `name` |
| `order` | string | `asc`, `desc` |

**GET /admin/audit:**

| Parametr | Typ | Popis |
|----------|-----|-------|
| `userId` | uuid | Filtr podle uživatele |
| `entityType` | string | Typ entity |
| `action` | string | `create`, `update`, `delete` |
| `from` | datetime | Od |
| `to` | datetime | Do |
| `page` | int | Stránka |
| `pageSize` | int | Velikost (max 100) |
