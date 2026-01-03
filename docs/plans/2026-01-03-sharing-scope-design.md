# Sdílení a scope-based sync

Datum: 2026-01-03
Stav: Návrh

## Kontext
- Aplikace je local-first (IndexedDB jako primární úložiště).
- Sdílení je opt-in a vyžaduje sync.
- Uživatel chce mít plnou kontrolu nad tím, co opouští zařízení.

## Cíle
- Umožnit sdílení a sync na úrovni Project/Property/Zaznam.
- Zachovat offline-first UX bez registrace.
- Minimalizovat metadata na serveru mimo sdílený scope.

## Ne-cíle
- Peer-to-peer offline sdílení mezi zařízeními.
- Automatická migrace starých projektů do synced scope bez potvrzení.

## Scope model
- ScopeType: `project` | `property` | `zaznam`.
- Každý scope má `syncMode` (`local-only` | `synced`) a `syncStatus`.
- Child scope může být `synced`, i když parent je `local-only`.
- Sync posílá pouze data daného scope + minimální metadata rodičů.

## Data flow (client)
- Všechny změny jdou nejdřív do IndexedDB.
- Pokud je scope `synced`, změna se přidá do `SyncQueue`.
- SyncManager pushuje změny po dávkách, pak dělá pull delta.
## Sync queue
- SyncQueueItem nese `scopeType`, `scopeId`, `entityType`, `entityId`, `action`.
- `projectId` je volitelné pro filtrování v UI.
- Po úspěchu se položky mažou, retry/backoff zůstává.

## API
- `GET /sync/status?scopeType=&scopeId=`
- `POST /sync/push` (scopeType/scopeId + changes)
- `GET /sync/pull?since=&scopeType=&scopeId=`
- Server filtruje data podle scope permissions.

## Konflikty
- LWW se `serverRevision` zůstává.
- Konfliktní dialog se objevuje jen u `synced` scope.

## UX
- Uživatel volí scope sdílení (Project/Property/Zaznam).
- UI ukazuje kontext projektu, ale data jsou omezená na scope.
- Při zapnutí sync je uživatel informován, co se odešle na server.
- Vypnutí sync: archivovat kopii nebo smazat ze serveru.

## Bezpečnost a soukromí
- Server drží pouze sdílený subset dat.
- Minimální metadata rodičů (např. názvy) bez citlivých detailů.
- Lokální data zůstávají mimo server bez explicitního opt-in.

## Otevřené otázky
- Potřebujeme zobrazovat `scopeType` v notifikacích?
- Potřebujeme export/import pro power users?

## Implementacni kroky (Capacitor wrapper)
1. Pridat Capacitor dependencies a init projektu.
2. Pripravit build output pro web view (SvelteKit build + static output).
3. Pridat iOS/Android platformy a otestovat offline/IndexedDB.
4. Nastavit push notifikace jako volitelne (az po MVP flow).
5. Zahrnout do CI/CD build kroky pro store release.
