# Vize a cíl

## Vize

Mít rychlý zápis a dohledatelný kontext, co jsem kde dělal a kolik to stálo.

Jednoduchá evidence nemovitostí, jednotek a oprav, aby se po letech dalo rychle zjistit co, kdy, proč, za kolik.

## Cílová skupina

Kdokoli s domem, garáží, bytovkou nebo zahradou.

## Hlavní problém

V čase se ztrácí informace, chybí kontext k minulým pracím.

## Požadavky na aplikaci

- Rychlá, srozumitelná a spolehlivá na mobilu i desktopu
- Nástroj pro správu oprav i drobných zásahů v domě a na zahradě
- Záznam = fotky + krátký popisek + detailnější zápis
- Co nejjednodušší flow, aby to uživatel chtěl používat

## Mimo scope (zatím)

- Obecný tracker jiných oblastí mimo domácnost

## Metrika úspěšnosti

Zatím nedefinována. Úspěch = reálné použití.

## Omezení

Žádná tvrdá. Hosting na Hetzner VPS je k dispozici.

---

## Scope

### MVP

- Pozn.: V tomto projektu MVP = VP (nebudeme ořezávat scope jen kvůli minimu).

- CRUD: Properties, Units (hierarchie), Záznamy
- Rychlý zápis záznamu (základní pole)
- Upload dokumentů a fotek k záznamům
- Základní report: seznam záznamů + suma nákladů
- PWA základ (manifest, ikony, instalace na plochu, service worker pro offline shell)
- Full local-first (offline read + write)
- Opt-in backup/sdílení → automatický sync na server
- Sdílení: pozvánky + notifikace (v1)
- Kontakty a skupiny (rychlé pozvání)
- Lokalizace cs/en
- Účet, role, základní audit (created/updated)

### Later

- Tagy, připomínky, plánovaná údržba
- Export PDF/CSV
- Sdílení přístupu (rodina/technik) + pokročilé pozvánky
- Notifikace (pokročilé nastavení)
- Push notifikace
- Background sync vylepšení + pokročilé řešení konfliktů

---

## Milníky (návrh)

| Milník | Obsah |
|--------|-------|
| M1 | Stabilizace, bezpečnost + PWA základ + local-first |
| M2 | UX refresh + filtry + přehledy |
| M3 | Exporty + sdílení |
