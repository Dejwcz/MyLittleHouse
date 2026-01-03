# Sdílení

## Přehled

- Sdílení je opt-in
- Při zapnutí sdílení se zapne automatický sync na server
- Uživatel je informován, že data budou na serveru
- Sdílený scope určuje, která data mohou opustit zařízení

---

## Úrovně sdílení

| Úroveň | Popis |
|--------|-------|
| Project | Sdílení celého projektu (všechny nemovitosti) |
| Property | Sdílení konkrétní nemovitosti |
| Zaznam | Sdílení konkrétního záznamu |

Pozn.: Při sdílení Property/Zaznam se v UI zobrazí kontext projektu, ale dostupná data jsou omezená na sdílený scope.

---

## Role

| Role | Popis |
|------|-------|
| Owner | Plná kontrola |
| Editor | Vytváření a úpravy |
| Viewer | Pouze čtení |

Detaily práv viz [04-auth-permissions.md](../04-auth-permissions.md).

---

## Pozvánky

### Vytvoření pozvánky

1. Owner otevře detail Project/Property
2. Klikne "Sdílet"
3. Zvolí:
   - Scope (Project/Property)
   - Roli (Owner/Editor/Viewer)
   - Per-user overrides (volitelně)
4. Vybere příjemce:
   - Z kontaktů
   - Ze skupiny
   - Ruční zadání emailu
5. Potvrdí → vytvoří se pozvánka + zobrazí se invite link/kód ke zkopírování

### Stav pozvánky

| Stav | Popis |
|------|-------|
| Pending | Čeká na odpověď |
| Accepted | Přijato |
| Declined | Odmítnuto |
| Expired | Vypršelo (7 dní) |

### Expirace

- Pozvánka vyprší po **7 dnech**
- Owner může poslat novou

### Neregistrovaný uživatel

- Pozvánka se uloží podle emailu
- Owner zkopíruje invite link/kód a pošle mimo aplikaci
- Pozvaný po registraci/loginu otevře invite link a pozvánku přijme/odmítne

---

## Notifikace

### Typy

| Typ | Příjemce | Obsah |
|-----|----------|-------|
| Invitation | Invitee | "X vás pozval do projektu Y" |
| InvitationAccepted | Owner | "X přijal pozvánku" |
| InvitationDeclined | Owner | "X odmítl pozvánku" |

### Zobrazení

- In-app notifikace (badge + dropdown)
- Push notifikace (later)

### Akce

- Accept / Decline (pro pozvánky)
- Mark as read
- View detail

---

## Sync při sdílení

### Automatický sync

Při zapnutí sdílení:
1. Sync se zapne automaticky
2. Na server se uploadují jen data z vybraného scope
3. Změny se synchronizují průběžně

### Konflikt

- Strategie: Last-write-wins
- Audit log uchovává historii
- Owner vidí kdo a co změnil

---

## UI: Správa sdílení

### Seznam členů

| Sloupec | Popis |
|---------|-------|
| Uživatel | Jméno / email |
| Role | Owner / Editor / Viewer |
| Stav | Active / Pending |
| Overrides | Indikace custom práv |
| Akce | Edit / Remove / Resend |

### Akce (Owner only)

- Změna role
- Úprava práv
- Odebrání člena
- Resend invite
- Transfer ownership

---

## Rozhodnuto

### Sdílení per Project vs per Property vs per Zaznam

**Rozhodnutí:** Obojí + Zaznam.

- **Project-level:** Sdílí všechny properties v projektu (bulk operace)
- **Property-level:** Sdílí konkrétní property (pro výjimky)
- **Zaznam-level:** Sdílí konkrétní záznam (nejmenší scope)
- **Precedence:** Zaznam-level přebíjí Property-level, Property-level přebíjí Project-level (viz 04-auth-permissions.md)

**Příklad:**
```
Projekt "Rodina" - Jan je Editor
├── Chalupa - Jan je explicitně Viewer → Viewer (přebito)
│   └── Revize střechy (Zaznam) - Jan je explicitně Viewer → Viewer (přebito)
├── Byt - Jan nemá explicitní roli → Editor (dědí)
└── Garáž - Jan nemá explicitní roli → Editor (dědí)
```

### Může Editor zvát další uživatele?

**Rozhodnutí:** Ne (default). Lze povolit per-user.

- Default: Editor NEMŮŽE zvát
- Owner může přidat granulární právo `canInviteUsers: true`
- Toto právo se ukládá v `Permissions` JSON

```json
{
  "role": "editor",
  "permissions": {
    "canInviteUsers": true
  }
}
```

### Notifikace při změně záznamu

**Rozhodnutí:** Ano, konfigurovatelné.

- Default: ON (v UserPreferences.PushSharedActivity)
- Uživatel může vypnout v Settings → Notifikace
- Typy notifikací:
  - Nový záznam přidán
  - Záznam upraven (pokud jsem autor nebo @mentioned)
  - Nový komentář

### Offline pozvánky

**Rozhodnutí:** Pozvánky vyžadují online.

- Vytvoření pozvánky = server operace (generování tokenu)
- Accept/decline = server operace
- V offline režimu: UI zobrazí "Pro sdílení je potřeba připojení"
- Pending pozvánky jsou vidět v cache (read-only)
