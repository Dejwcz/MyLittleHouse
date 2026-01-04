# UX/UI specifikace

## Princip

- Mobile-first design
- Rychlý zápis na prvním místě
- Jednoduchost a přehlednost

---

## Informační architektura

Aplikace používá **projekt-centrickou** navigaci - uživatel nejprve vybere projekt a pak pracuje s jeho obsahem.

### Route struktura

```
/                           ← Landing page
/projects                   ← Seznam projektů
/projects/[projectId]       ← Projekt dashboard (vstup do projektu)
/projects/[projectId]/properties          ← Seznam nemovitostí v projektu
/projects/[projectId]/properties/[id]     ← Detail nemovitosti
/projects/[projectId]/properties/new      ← Nová nemovitost
/projects/[projectId]/units/[id]          ← Detail jednotky
/projects/[projectId]/zaznamy             ← Seznam záznamů v projektu
/projects/[projectId]/zaznamy/[id]        ← Detail záznamu
/projects/[projectId]/zaznamy/new         ← Nový záznam
/projects/[projectId]/settings            ← Nastavení projektu (sync, členové)
/notifications              ← Notifikace
/settings                   ← Uživatelské nastavení
```

### Navigační kontexty

**Hlavní sidebar (mimo projekt):**
- Projekty
- Notifikace
- Nastavení

**Projekt sidebar (uvnitř projektu):**
- Dashboard (projekt)
- Nemovitosti
- Záznamy
- Nastavení projektu
- ← Zpět na projekty

### Hierarchie obsahu

```
Projekty (výběr)
└── Projekt dashboard
    ├── Quick stats (nemovitosti, záznamy, náklady)
    ├── Poslední záznamy
    ├── Sync status
    └── Členové (pokud sdíleno)

Nemovitosti (v projektu)
├── Seznam nemovitostí
└── Nemovitost detail
    ├── Jednotky (stromová hierarchie)
    ├── Galerie (fotky + dokumenty)
    ├── Záznamy (souhrn + link)
    ├── Aktivita (pro sdílené)
    └── Statistiky

Jednotka detail
├── Child units
├── Galerie (fotky + dokumenty)
├── Záznamy (seznam)
└── Drafty (rozpracované)

Záznam detail
├── Metadata
├── Galerie (fotky + dokumenty)
└── Timeline změn

Uživatelské nastavení
├── Profil
│   ├── Osobní údaje
│   └── Fotka profilu
├── Zabezpečení
│   ├── Změna hesla
│   ├── Propojené účty (Google/Apple)
│   └── Aktivní sessions
├── Sdílení
│   ├── Co sdílím
│   ├── Sdíleno se mnou
│   └── Čekající pozvánky
├── Notifikace
│   ├── Push notifikace
│   └── Email notifikace
├── Kontakty & skupiny
├── Vzhled
│   ├── Jazyk
│   └── Tmavý režim
└── Data & sync
    ├── Sync nastavení
    └── Export/Smazání dat
```

---

## CTA konzistence

Primární akce je vždy vpravo v `PageHeader`.

| Stránka | Primary (default) | Secondary (ghost) |
|---------|-------------------|-------------------|
| Projekt dashboard | Nový záznam | Nová nemovitost |
| Seznam nemovitostí | Nová nemovitost | — |
| Detail nemovitosti | Nový záznam | Nová jednotka |
| Seznam jednotek | — (filtr + inline akce u property) | — |
| Detail jednotky | Nový záznam | — |

### Button varianty

| Varianta | Použití | Vzhled |
|----------|---------|--------|
| `primary` | Hlavní akce (CTA) | Zelené pozadí, bílý text |
| `secondary` | Alternativní akce | Průhledné, tmavší border |
| `ghost` | Sekundární akce v header | Průhledné, jemný border |
| `danger` | Destruktivní akce | Červené pozadí |
| `outline` | Neutrální akce | Průhledné, jemný border |

**Pravidla:**
- Primary = vytvoření hlavní entity (záznam)
- Ghost = vytvoření vedlejší entity (nemovitost, jednotka)
- Secondary = akce v kartách, dialozích
- Danger = mazání, odhlášení

---

## Obrazovky

### Seznam projektů

Vstupní bod do aplikace po přihlášení.

| Prvek | Popis |
|-------|-------|
| Přepínač | Moje / Sdílené se mnou |
| Karta | Název, popis, počet nemovitostí, sync status badge |
| Akce | Vstoupit, Edit, Delete |
| FAB | "+" nový projekt |

### Projekt dashboard

Po vstupu do projektu - přehled jeho obsahu.

| Sekce | Obsah |
|-------|-------|
| Quick stats | Počet nemovitostí, jednotek, záznamů, celkové náklady |
| Poslední záznamy | 5-10 posledních záznamů (rychlý přístup) |
| Sync status | Toggle local-only / synced + badge |
| Členové | Seznam členů s rolemi (pokud sdíleno) |

### Seznam nemovitostí (v projektu)

| Prvek | Popis |
|-------|-------|
| Karta | Název, popis, počet jednotek, sync status |
| Akce | Detail, Edit, Delete |
| FAB | "+" nová nemovitost |

**Vytvoření nemovitosti (flow):**
1. Výběr typu (karty s ikonou: Dům, Byt, Garáž, Zahrada, Kůlna, Pozemek, Jiné)
2. Název + popis
3. Otázka: "Chceš přidat jednotky?" → Ano / Přeskočit
4. (Volitelně) Preset jednotek podle typu
   - Dům → Podlaží
   - Byt → Místnosti
   - Garáž → Parkovací stání / nic
   - Ostatní → nic

**PropertyType (karty s ikonou):**
| Hodnota | CZ label |
|---------|----------|
| house | Dům |
| apartment | Byt |
| garage | Garáž |
| garden | Zahrada |
| shed | Kůlna |
| land | Pozemek |
| other | Jiné |

**UnitType (zjednodušené):**
| Hodnota | CZ label |
|---------|----------|
| room | Místnost |
| floor | Podlaží |
| cellar | Sklep |
| parking | Parkovací stání |
| other | Jiné |

### Property detail

| Sekce | Obsah |
|-------|-------|
| Header | Název, popis, akce, GPS badge (📍 pokud nastaveno) |
| Galerie | Fotky + dokumenty, akce pro nastavení titulní fotky |
| Jednotky | Seznam karet jednotek |
| Záznamy | Poslední záznamy + link "Zobrazit vše" + toggle Seznam/Timeline |
| Aktivita | Feed událostí (pouze pro sdílené properties) |
| Stats | Celkové náklady, počet záznamů |
| Synchronizace | Toggle local-only / synced + sync status badge |

### Unit detail

| Sekce | Obsah |
|-------|-------|
| Header | Název, typ, parent breadcrumb |
| Galerie | Fotky + dokumenty, akce pro nastavení titulní fotky |
| Child units | Pokud existují |
| Záznamy | Seznam s filtry + toggle Seznam/Timeline |
| Drafty | Collapsible sekce s rozpracovanými záznamy |

### Záznam detail

| Sekce | Obsah |
|-------|-------|
| Header | Název, datum, cena |
| Popis | Delší text |
| Galerie | Fotky a dokumenty |
| Metadata | Vytvořeno, upraveno, kým |
| Timeline | Historie změn (audit) |
| Synchronizace | Toggle local-only / synced + sync status badge |

### Rychlý zápis (modal/stránka)

Viz [05-features/zaznam.md](05-features/zaznam.md).

---

## Nastavení (Settings)

### Struktura navigace

```
/settings
├── /settings/profile        ← Profil
├── /settings/security       ← Zabezpečení
├── /settings/sharing        ← Sdílení
├── /settings/notifications  ← Notifikace
├── /settings/contacts       ← Kontakty & skupiny
├── /settings/appearance     ← Vzhled
└── /settings/data           ← Data & sync
```

### Profil (`/settings/profile`)

| Sekce | Pole | Typ | Popis |
|-------|------|-----|-------|
| Fotka | Avatar | Image upload | Kruhová fotka, max 2MB, JPEG/PNG/WebP |
| Osobní údaje | Jméno | Text input | Required, 2-50 znaků |
| | Příjmení | Text input | Required, 2-50 znaků |
| | Email | Text (readonly) | Změna přes Support |
| | Telefon | Text input | Optional, pro budoucí 2FA |

**Akce:**
- Uložit změny
- Smazat fotku

**Avatar:**
- Default: Iniciály (první písmeno jména + příjmení) na barevném pozadí
- Barva pozadí: Generována z hash userId (konzistentní pro uživatele)
- Po uploadu: Server resize na 256x256, veřejná URL (public bucket)

### Zabezpečení (`/settings/security`)

| Sekce | Obsah |
|-------|-------|
| Změna hesla | Současné heslo, Nové heslo, Potvrdit heslo |
| Propojené účty | Google (propojit/odpojit), Apple (propojit/odpojit) |
| Aktivní sessions | Seznam zařízení s možností "Odhlásit všude" |

**Změna hesla - flow:**
```
1. Zobrazit formulář: Současné heslo, Nové heslo, Potvrdit
2. Validace na klientu (min 6 znaků, velké+malé+číslo)
3. Submit → POST /api/auth/change-password
4. Success → Toast "Heslo změněno"
5. Error → Inline chybová hláška
```

**Propojené účty - UI:**
```
┌─────────────────────────────────────┐
│ Google                    [Propojit]│  ← Nepropojeno
├─────────────────────────────────────┤
│ ✓ Google                            │
│   jan.novak@gmail.com    [Odpojit]  │  ← Propojeno
├─────────────────────────────────────┤
│ Apple                     [Propojit]│
└─────────────────────────────────────┘
```

**Odpojení - validace:**
- Nelze odpojit pokud je to jediná metoda přihlášení
- UI: "Nejdřív nastavte heslo" nebo "Propojte jiný účet"

### Notifikace (`/settings/notifications`)

| Kategorie | Nastavení | Default |
|-----------|-----------|---------|
| **Push notifikace** | | |
| Nové komentáře | Toggle | ON |
| @mentions | Toggle | ON |
| Aktivita na sdílených | Toggle | ON |
| Připomínky draftů | Toggle | ON |
| **Email notifikace** | | |
| Týdenní souhrn | Toggle | OFF |
| Pozvánky ke sdílení | Toggle | ON |
| Důležité upozornění | Toggle | ON (locked) |

**UI layout:**
```
Push notifikace
────────────────────────────────
Nové komentáře               [ON]
Když někdo přidá komentář k vašemu záznamu

@mentions                    [ON]
Když vás někdo označí v komentáři

Aktivita na sdílených        [ON]
Nové záznamy na sdílených nemovitostech
────────────────────────────────

Email notifikace
────────────────────────────────
Týdenní souhrn              [OFF]
Přehled aktivity za poslední týden
...
```

### Sdílení (`/settings/sharing`)

Přehled všeho co uživatel sdílí a co je s ním sdíleno.

**Struktura:**
```
/settings/sharing
├── Co sdílím (moje nemovitosti sdílené s ostatními)
└── Sdíleno se mnou (nemovitosti ostatních)
```

**Tab: Co sdílím**

Seznam properties/projektů které vlastním a sdílím:

```
┌─────────────────────────────────────────────────┐
│ Chalupa Krkonoše                        [Upravit]│
│ Projekt: Rodinné nemovitosti                    │
├─────────────────────────────────────────────────┤
│ 👤 Jan Novák          Editor           [Odebrat]│
│    jan@email.cz       Přijato 15.12.2024        │
├─────────────────────────────────────────────────┤
│ 👤 Marie Svobodová    Viewer           [Odebrat]│
│    marie@email.cz     Přijato 10.12.2024        │
├─────────────────────────────────────────────────┤
│ 👤 Petr Dvořák        Editor    ⏳ Čeká [Zrušit]│
│    petr@email.cz      Odesláno 20.12.2024       │
├─────────────────────────────────────────────────┤
│                              [+ Přidat člena]   │
└─────────────────────────────────────────────────┘
```

**Akce:**
- Upravit → změna role, granulární práva
- Odebrat → odebrání přístupu (s potvrzením)
- Zrušit → zrušení pending pozvánky
- Přidat člena → nová pozvánka

**Tab: Sdíleno se mnou**

Seznam properties/projektů kde jsem členem:

```
┌─────────────────────────────────────────────────┐
│ Byt Praha 3                                     │
│ Vlastník: Eva Králová                           │
│ Moje role: Editor                    [Opustit]  │
│ Sdíleno od: 5.11.2024                           │
├─────────────────────────────────────────────────┤
│ Garáž Brno                                      │
│ Vlastník: Tomáš Horák                           │
│ Moje role: Viewer                    [Opustit]  │
│ Sdíleno od: 1.10.2024                           │
└─────────────────────────────────────────────────┘
```

**Akce:**
- Opustit → odejít ze sdílení (s potvrzením)

**Pending pozvánky (banner nahoře):**

```
┌─────────────────────────────────────────────────┐
│ 📩 2 čekající pozvánky                          │
├─────────────────────────────────────────────────┤
│ Chata Šumava                                    │
│ Od: Karel Marek (karel@email.cz)                │
│ Role: Editor                                    │
│                         [Přijmout] [Odmítnout]  │
├─────────────────────────────────────────────────┤
│ Dům Olomouc                                     │
│ Od: Anna Bílá (anna@email.cz)                   │
│ Role: Viewer                                    │
│                         [Přijmout] [Odmítnout]  │
└─────────────────────────────────────────────────┘
```

**Filtry:**
- Všechny / Projekty / Properties
- Role: Všechny / Owner / Editor / Viewer

**Empty states:**
- "Zatím nic nesdílíte" + CTA "Pozvat někoho"
- "Nikdo s vámi zatím nic nesdílí"

### Kontakty & skupiny (`/settings/contacts`)

Viz existující dokumentace kontaktů.

**Sekce:**
- Seznam kontaktů (email, jméno)
- Skupiny kontaktů
- Přidat kontakt
- Import kontaktů (budoucí)

### Vzhled (`/settings/appearance`)

| Nastavení | Možnosti | Default |
|-----------|----------|---------|
| Jazyk | Čeština, English | Čeština |
| Tmavý režim | Světlý, Tmavý, Systémový | Systémový |

**Jazyk:**
- Změna okamžitě aplikuje překlad
- Uloženo v cookie + user preference (pokud přihlášen)

**Tmavý režim:**
```
○ Světlý
○ Tmavý
● Podle systému
```

### Data & sync (`/settings/data`)

| Sekce | Obsah |
|-------|-------|
| Sync status | Online/Offline, počet čekajících změn |
| Auto-sync | Toggle (default ON) |
| Sync přes mobilní data | Toggle (default OFF) |
| Export dat | Stáhnout vše jako JSON (lokální) nebo ZIP (server) |
| Import dat | Nahrát zálohu z JSON souboru |
| Smazání lokálních dat | Vymazat IndexedDB |
| Smazání účtu | Danger zone (server) |

**Sync status UI:**
```
┌─────────────────────────────────────┐
│ ● Online                            │
│ Poslední sync: před 2 minutami      │
│ Čeká na sync: 0 položek             │
│                      [Sync nyní]    │
└─────────────────────────────────────┘
```

**Export dat (lokální - guest/offline):**
```
1. Klik "Exportovat data"
2. IndexedDB → JSON (včetně media jako base64)
3. Okamžité stažení souboru mujdomecek-backup-YYYY-MM-DD.json
```

**Import dat (lokální):**
```
1. Klik "Importovat data"
2. Výběr JSON souboru
3. Modal s náhledem (počet projektů, nemovitostí, záznamů)
4. Volba: "Smazat stávající data před importem" (checkbox)
5. Klik "Importovat"
6. Toast s výsledkem
```

**Export dat (server - přihlášený):**
```
1. Klik "Exportovat data"
2. Vybrat formát: JSON / ZIP (s fotkami)
3. Server připraví export (může trvat)
4. Email s download linkem (platnost 24h)
```

**Varování o lokálních datech:**
- Amber banner na /settings/data
- Modal při prvním "Pokračovat" na homepage (pokud jsou lokální data)
- Text: "Vymazání dat prohlížeče, čističe (CCleaner), přeinstalace může smazat záznamy"

**Smazání účtu - flow:**
```
1. Klik "Smazat účet" (červené tlačítko)
2. Modal: "Opravdu chcete smazat účet?"
   - Varování: "Tato akce je nevratná"
   - Checkbox: "Rozumím, že přijdu o všechna data"
3. Zadejte heslo pro potvrzení
4. Klik "Trvale smazat účet"
5. Server:
   - Soft delete všech dat
   - Anonymizace (GDPR)
   - Odhlášení
6. Redirect na landing page
```

**Retenční perioda:**
- 30 dní soft delete (možnost obnovení přes support)
- Po 30 dnech trvalé smazání

---

## Komponenty

### Navigace

| Platforma | Typ |
|-----------|-----|
| Mobile | Bottom navigation (4-5 položek) |
| Desktop | Sidebar |

**Položky:**
- Dashboard
- Nemovitosti
- Záznamy (všechny)
- Notifikace
- Profil/Nastavení

### FAB (Floating Action Button)

- Pozice: vpravo dole (mobile)
- Akce: Nový záznam (rychlý zápis)
- Barva: primární

### Karty

- Property card
- Unit card
- Záznam card (preview)

### Filtry

- Drawer na mobile
- Inline na desktopu
- Chips pro aktivní filtry

### Empty states

| Stav | Obsah |
|------|-------|
| Žádné nemovitosti | Ilustrace + "Přidejte první nemovitost" |
| Žádné záznamy | Ilustrace + "Přidejte první záznam" |
| Žádné výsledky | "Nic nenalezeno" + návrhy |

### Loading states

- Skeleton pro seznamy
- Spinner pro akce
- Progress bar pro upload

### Error states

- Toast pro drobné chyby
- Modal pro kritické chyby
- Inline error pro formuláře

---

## Responsive breakpointy

| Breakpoint | Šířka | Layout |
|------------|-------|--------|
| Mobile | < 768px | Single column, bottom nav |
| Tablet | 768-1024px | Two column možné |
| Desktop | > 1024px | Sidebar + content |

---

## Design system

### Rozhodnutí

- **Framework:** Tailwind CSS
- **Komponenty:** Vlastní (plná kontrola)
- **Dark mode:** Od začátku (`darkMode: 'class'`)

### Rozhodnuto

#### Barevné schéma

**Princip:** Semantic color tokens + CSS variables = snadné skinování.

**Pravidlo:** NIKDY nepoužívat raw Tailwind barvy (`bg-green-500`). VŽDY semantic tokeny (`bg-primary`).

```css
/* src/web/src/app.css */
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  :root {
    /* Primary - zelená (z mujdomecek.runasp.net) */
    --color-primary-50: #f0fdf4;
    --color-primary-100: #dcfce7;
    --color-primary-200: #bbf7d0;
    --color-primary-300: #86efac;
    --color-primary-400: #4ade80;
    --color-primary-500: #22c55e;   /* hlavní */
    --color-primary-600: #16a34a;   /* hover */
    --color-primary-700: #15803d;
    --color-primary-800: #166534;
    --color-primary-900: #14532d;
    --color-primary-950: #052e16;

    /* Semantic colors */
    --color-success: var(--color-primary-500);
    --color-warning: #f59e0b;
    --color-error: #ef4444;
    --color-info: #3b82f6;

    /* Neutral (gray) */
    --color-neutral-50: #fafafa;
    --color-neutral-100: #f5f5f5;
    --color-neutral-200: #e5e5e5;
    --color-neutral-300: #d4d4d4;
    --color-neutral-400: #a3a3a3;
    --color-neutral-500: #737373;
    --color-neutral-600: #525252;
    --color-neutral-700: #404040;
    --color-neutral-800: #262626;
    --color-neutral-900: #171717;
    --color-neutral-950: #0a0a0a;

    /* Background & Surface */
    --color-bg: #ffffff;
    --color-bg-secondary: var(--color-neutral-50);
    --color-surface: #ffffff;
    --color-surface-elevated: #ffffff;

    /* Text */
    --color-text: var(--color-neutral-900);
    --color-text-secondary: var(--color-neutral-600);
    --color-text-muted: var(--color-neutral-400);
    --color-text-inverse: #ffffff;

    /* Border */
    --color-border: var(--color-neutral-200);
    --color-border-focus: var(--color-primary-500);
  }

  .dark {
    --color-bg: var(--color-neutral-950);
    --color-bg-secondary: var(--color-neutral-900);
    --color-surface: var(--color-neutral-900);
    --color-surface-elevated: var(--color-neutral-800);

    --color-text: var(--color-neutral-100);
    --color-text-secondary: var(--color-neutral-400);
    --color-text-muted: var(--color-neutral-600);

    --color-border: var(--color-neutral-800);
  }
}
```

```javascript
// tailwind.config.js
export default {
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Primary palette
        primary: {
          50: 'var(--color-primary-50)',
          100: 'var(--color-primary-100)',
          200: 'var(--color-primary-200)',
          300: 'var(--color-primary-300)',
          400: 'var(--color-primary-400)',
          500: 'var(--color-primary-500)',
          600: 'var(--color-primary-600)',
          700: 'var(--color-primary-700)',
          800: 'var(--color-primary-800)',
          900: 'var(--color-primary-900)',
          950: 'var(--color-primary-950)',
          DEFAULT: 'var(--color-primary-500)',
        },
        // Semantic
        success: 'var(--color-success)',
        warning: 'var(--color-warning)',
        error: 'var(--color-error)',
        info: 'var(--color-info)',
        // Surfaces
        bg: 'var(--color-bg)',
        'bg-secondary': 'var(--color-bg-secondary)',
        surface: 'var(--color-surface)',
        'surface-elevated': 'var(--color-surface-elevated)',
        // Text
        foreground: 'var(--color-text)',
        'foreground-secondary': 'var(--color-text-secondary)',
        'foreground-muted': 'var(--color-text-muted)',
        'foreground-inverse': 'var(--color-text-inverse)',
        // Border
        border: 'var(--color-border)',
        'border-focus': 'var(--color-border-focus)',
      },
    },
  },
}
```

**Použití v komponentách:**

```html
<!-- ✅ Správně - semantic tokens -->
<button class="bg-primary hover:bg-primary-600 text-foreground-inverse">
  Uložit
</button>

<div class="bg-surface border border-border rounded-lg">
  <h2 class="text-foreground">Nadpis</h2>
  <p class="text-foreground-secondary">Popis</p>
</div>

<!-- ❌ Špatně - raw colors -->
<button class="bg-green-500 hover:bg-green-600 text-white">
  Uložit
</button>
```

**Změna tématu = změna CSS variables:**

```css
/* Modrý theme - stačí změnit :root */
:root {
  --color-primary-500: #3b82f6;
  --color-primary-600: #2563eb;
  /* ... */
}
```

#### Typografie

**Font stack:** System fonts (rychlé, nativní vzhled)

```javascript
// tailwind.config.js
fontFamily: {
  sans: [
    'Inter',
    'system-ui',
    '-apple-system',
    'BlinkMacSystemFont',
    'Segoe UI',
    'Roboto',
    'sans-serif',
  ],
  mono: [
    'JetBrains Mono',
    'Fira Code',
    'Consolas',
    'monospace',
  ],
}
```

**Typografická škála:**

| Token | Velikost | Použití |
|-------|----------|---------|
| `text-xs` | 12px | Badges, helper text |
| `text-sm` | 14px | Secondary text, labels |
| `text-base` | 16px | Body text (default) |
| `text-lg` | 18px | Lead paragraphs |
| `text-xl` | 20px | Card titles |
| `text-2xl` | 24px | Section headers |
| `text-3xl` | 30px | Page titles |
| `text-4xl` | 36px | Hero text |

#### Spacing system

**Base unit:** 4px (Tailwind default)

| Token | Hodnota | Použití |
|-------|---------|---------|
| `space-1` | 4px | Tight spacing |
| `space-2` | 8px | Icon gaps |
| `space-3` | 12px | Small gaps |
| `space-4` | 16px | Standard padding |
| `space-6` | 24px | Section padding |
| `space-8` | 32px | Large gaps |
| `space-12` | 48px | Section margins |

#### Ikony

**Sada:** Lucide Icons

**Důvody:**
- Konzistentní styl
- Tree-shakeable (malý bundle)
- Svelte komponenty k dispozici (`lucide-svelte`)
- MIT licence
- 1400+ ikon

```svelte
<script>
  import { Home, Settings, Plus, Check } from 'lucide-svelte';
</script>

<Home class="w-5 h-5 text-foreground" />
<button>
  <Plus class="w-4 h-4" />
  Přidat
</button>
```

**Velikosti ikon:**

| Velikost | Třída | Použití |
|----------|-------|---------|
| 16px | `w-4 h-4` | Inline, buttons |
| 20px | `w-5 h-5` | Navigation, list items |
| 24px | `w-6 h-6` | Cards, headers |
| 32px | `w-8 h-8` | Empty states |
| 48px | `w-12 h-12` | Hero sections |

---

## Offline UX indikátory

### Badges na záznamech

| Badge | Význam |
|-------|--------|
| 🔵 | Local-only (jen v zařízení, není zálohováno) |
| ✅ | Synced (zálohováno na serveru) |
| 🔄 | Syncing (probíhá synchronizace) |
| ⚠️ | Sync failed (kliknutelné pro detail) |
| 👥 | Shared (sdíleno s ostatními) |

### Globální indikátor v headeru

| Stav | Zobrazení |
|------|-----------|
| Offline | `📴 Offline` (šedá lišta) |
| Online, vše ok | ✓ nebo nic |
| Syncuje se | `🔄 (3)` s počtem položek |
| Má chyby | `⚠️ (2)` kliknutelné |

### Sync problems screen

Přístup přes klik na ⚠️ v headeru:

- Seznam failed položek s popisem chyby
- Tlačítko "Zkusit znovu" (jednotlivě + hromadně)
- Možnost "Zahodit změnu" pro nevyřešitelné případy

---

## Pokročilé UX patterny

### Activity feed na sdílených properties

Inspirace: Facebook collaborative albums.

| Událost | Zobrazení |
|---------|-----------|
| Nový záznam | "Jan přidal záznam: Oprava střechy" |
| Editace | "Marie upravila záznam: Výměna kotle" |
| Komentář | "Petr okomentoval: Oprava střechy" |
| Nový člen | "Eva se připojila jako Editor" |

**Umístění:**
- Tab "Aktivita" v Property detail
- Notifikace pro členy (konfigurovatelné)

**@mention:**
- V komentářích lze označit spoluvlastníky (@Jan)
- Označený uživatel dostane notifikaci

---

### GPS auto-tagging

Inspirace: Instagram location suggestions.

**Flow:**
1. Uživatel otevře app nebo klikne na FAB
2. App zkontroluje GPS pozici
3. Pokud je v blízkosti některé property → auto-select
4. "Vypadá to, že jste na Chalupě. Přidat záznam sem?"

**Konfigurace per property:**
```
GPS souřadnice: 50.0755° N, 14.4378° E
Radius: 100m (default)
```

**Privacy:**
- GPS se používá pouze lokálně pro matching
- Souřadnice se neukládají u záznamů (pokud uživatel nechce)
- Lze vypnout v nastavení

---

### Timeline view (alternativní zobrazení záznamů)

Inspirace: Facebook Timeline.

**Vizuální layout:**
```
2024 ─┬─ Prosinec
      │   ├─ 15. Výměna kotle ●●● (15 000 Kč)
      │   └─ 03. Oprava okna ● (800 Kč)
      ├─ Listopad
      │   └─ 22. Revize komína ●● (2 500 Kč)
      └─ ...
2023 ─┬─ ...
```

**Funkce:**
- Vertikální timeline s roky/měsíci na levé straně
- Sticky headers při scrollování
- Quick jump na rok (dropdown)
- Velikost bubliny = počet fotek nebo výše nákladů
- Kliknutí → detail záznamu

**Přepínání view:**
- Toggle v headeru seznamu: `[Seznam] [Timeline]`
- Uložení preference per user

---

### Draft záznamy (rozpracované)

Inspirace: Instagram Stories drafts.

**Účel:** Rychle vyfotit, doplnit detaily později.

**Flow:**
1. Quick capture → fotka se uloží jako draft
2. Minimal info: datum (auto), property (auto z GPS nebo poslední použitá)
3. Draft badge: 📝 v seznamu
4. Uživatel může kdykoliv otevřít a doplnit

**Správa draftů:**
- Sekce "Rozpracované" v seznamu záznamů (collapsible)
- Auto-reminder po 3 dnech: "Máte 2 rozpracované záznamy"
- Auto-delete po 30 dnech (s upozorněním 7 dní předem)

**Stavy záznamu:**
| Stav | Badge | Popis |
|------|-------|-------|
| Draft | 📝 | Rozpracovaný, chybí povinné údaje |
| Complete | (žádný) | Všechny údaje vyplněny |
| Synced | ✅ | Synchronizováno na server |

---

## Accessibility

- Semantic HTML
- ARIA labels
- Keyboard navigation
- Contrast ratio 4.5:1 min
- Focus indicators

---

## Lokalizace

| Jazyk | Kód |
|-------|-----|
| Čeština | cs |
| Angličtina | en |

- Přepínač v patičce / nastavení
- Persistence přes cookie
