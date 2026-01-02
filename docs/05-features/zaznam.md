# Záznam (Record)

## Koncept

Obecný záznam pro domov a zahradu:
- Oprava
- Zásah
- Nákup
- Umístění věci

Záznam patří ke konkrétní jednotce nebo přímo k nemovitosti.

---

## Pole záznamu

### Povinné

| Pole | Popis |
|------|-------|
| Property | Vazba na nemovitost (required) |
| Date | Datum záznamu (default dnes) |
| Title | Název |
| Fotky | Doporučené (soft requirement) – pokud chybí, nastaví se flag `MissingPhoto` |

### Volitelné

| Pole | Popis |
|------|-------|
| Unit | Jednotka (lze změnit později) |
| Cost | Cena |
| Description | Delší popis |
| Flags | Stav/flagy (TODO, Čeká, Důležité...) |
| Tags | Tagy (předdefinované) |
| Documents | PDF (faktury, záruky, návody) |
| Receipts | Účtenky/doklady (foto) |

Volitelná pole jsou řízena nastavením uživatele (např. cena zap/vyp).

---

## Rychlý zápis

### Entry points

| Platforma | UI |
|-----------|-----|
| Mobile | Plovoucí "+" tlačítko |
| Desktop | Primární tlačítko "Nový záznam" |

### Default pole (rychlý zápis)

1. **Property** (required)
   - Default: poslední použitá
   - Auto-select při 1 property
2. **Unit** (optional)
   - Filtrované podle property
   - Hodnota "Bez jednotky"
3. **Date** (required)
   - Default: dnes
4. **Název** (required)
5. **Fotky** (doporučené)
   - Kamera/galerie
   - Více souborů
   - Pokud uživatel uloží bez fotky: UI varování + auto-flag `MissingPhoto`

### Volitelná pole (expand)

Zobrazí se podle nastavení uživatele:
- Cena
- Delší popis
- Flagy
- Tagy
- Dokumenty / účtenky

### Akce

- **Uložit** → uloží a přesměruje na detail
- **Uložit a přidat další** (volitelně)
- **Zrušit**

### Po uložení

1. Toast: "Záznam uložen" + status "Uloženo lokálně / Sync"
2. Přesměrování na detail s CTA "Doplnit detail"

---

## CRUD operace

### Create
- Rychlý zápis (viz výše)
- Plný formulář z detailu

### Read
- Seznam záznamů (s filtry)
- Detail záznamu (dokumenty + timeline změn)

### Update
- Editace všech polí
- Změna jednotky
- Přidání/odebrání dokumentů

### Delete
- Soft delete
- Potvrzovací dialog

---

## Filtry (seznam záznamů)

| Filtr | Typ |
|-------|-----|
| Datum | Range (od-do) |
| Jednotka | Select |
| Cena | Range (od-do) |
| Tagy | Multi-select |
| Flagy | Multi-select |
| Fulltext | Search |

---

## Tagy (UX)

### Rozhodnuto (VP)

- Tagy jsou z předdefinovaného seznamu (reference data); v VP nejsou custom tagy.
- Multi-select (vytvoření/edit i filtry).
- UI komponenta: chip select / toggle buttons se search (na mobilu bottom sheet, na desktopu dropdown/panel).
- UX pro výběr:
  - Zobrazit nejčastěji používané tagy nahoře (per-user).
  - Vybrané tagy jsou vidět jako chips; klik = odebrat.
  - „Vymazat“ akce pro rychlé odznačení všech tagů.
- Offline: seznam tagů je dostupný offline (součást aplikace + uložený v IndexedDB).

---

## Limity podle tieru (konfigurace)

### Rozhodnuto (VP)

- Limity jsou konfigurovatelné a měnitelné za běhu přes Admin panel (`/admin/settings`).
- Výchozí hodnoty jsou v `appsettings.json` (viz Q29 v `docs/10-open-questions.md`).
- Runtime override je uložený v DB (tabulka `AppSetting`); precedence: hardcoded defaults → `appsettings.json` → DB.

**Free vs Premium:**
- Free: max počet fotek/záznam + komprese fotek na klientu před uploadem.
- Premium: vyšší limit fotek/záznam + bez komprese (originál).

**Konkrétní klíče konfigurace:**
- `Constraints.Files.MaxPhotosPerZaznamFree`
- `Constraints.Files.MaxPhotosPerZaznamPremium`
- `Constraints.Files.MaxPhotoSizeBytes`
- `Constraints.Files.MaxDocumentSizeBytes`
- `Constraints.Files.PhotoCompression.FreeEnabled`
- `Constraints.Files.PhotoCompression.FreeMaxDimensionPx`
- `Constraints.Files.PhotoCompression.FreeJpegQuality`

**Validace:**
- Frontend: UX (disabled state, inline chyba).
- Backend: autoritativní kontrola (nepustit nad limity).
