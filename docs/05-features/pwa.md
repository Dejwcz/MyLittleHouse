# PWA (Progressive Web App)

## Cíl

- Aplikace instalovatelná na plochu (mobil i desktop)
- Funguje offline (alespoň základní shell)
- Rychlá a responzivní

---

## MVP rozsah

### Manifest

```json
{
  "name": "Můj Domeček",
  "short_name": "Domeček",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#...",
  "icons": [...]
}
```

### Ikony

| Velikost | Použití |
|----------|---------|
| 192x192 | Android |
| 512x512 | Android splash |
| 180x180 | iOS |
| 32x32 | Favicon |

### Service Worker

**Cache strategie (MVP):**

| Typ | Strategie |
|-----|-----------|
| Static assets (JS, CSS, fonty) | Cache-first |
| HTML shell | Cache-first, update in background |
| API calls | Network-first, fallback to cache |
| Images | Cache-first with expiration |

### Offline shell

- Základní UI se načte offline
- Zobrazí data z IndexedDB
- Indikace offline stavu
- Sync po obnovení spojení
- Offline zápis: vytvoření/upravy se ukládají do IndexedDB + sync queue (local-first)

---

## Later

### Push notifikace

- Notifikace o změnách ve sdílených projektech
- Opt-in (uživatel musí povolit)
- Server-side push (Web Push API)

### Background sync (vylepšení)

- Periodický sync na pozadí (kde to prohlížeč dovolí)
- Retry/backoff strategie

---

## Technické detaily

### Service Worker registrace

```javascript
if ('serviceWorker' in navigator) {
  navigator.serviceWorker.register('/sw.js');
}
```

### Cache verze

- Při deployi se změní verze cache
- Starý cache se smaže

### Update flow

1. SW detekuje novou verzi
2. Stáhne na pozadí
3. Zobrazí "Nová verze dostupná" toast
4. Uživatel klikne → refresh

---

## Testování

- Lighthouse audit (PWA score)
- Offline mode v DevTools
- Různé sítě (slow 3G, offline)

---

## Rozhodnuto (VP)

### Jaké stránky cachovat?

- SPA shell + statické assety (JS/CSS/fonty, manifest, ikony).
- Data pro UI se načítají z IndexedDB (ne z HTTP cache API odpovědí).

### Max velikost cache

- Cíl: ~50 MB pro shell + assety.
- Images: best-effort cache s LRU evikcí (prohlížeče mají různou kvótu a mohou cache kdykoli vyčistit).

### Velké obrázky offline

- Thumbnaily: cachovat při zobrazení (Service Worker HTTP cache), LRU evikce.
- Originály (presigned URLs): necachovat automaticky; pokud bude potřeba, explicitní akce „Uložit offline“ → uložit blob do IndexedDB (keyed by `StorageKey`) + LRU.
- Offline bez cache: zobrazit placeholder („K dispozici po připojení“).

### Push notifikace

- Later.

### Background sync API

- Later (omezená podpora v prohlížečích); v VP sync probíhá při otevřené aplikaci.
