# Migrace

## Současný stav

- ASP.NET Core 8 MVC
- SQL Server (MonsterASPConnectionDeployed)
- Entity: Property, Unit, Repair, RepairDocument, AppUser
- Nasazeno: https://mujdomecek.runasp.net/

---

## Cílový stav

- Local-first PWA
- IndexedDB (klient) + PostgreSQL (server)
- Nové entity: Project, Zaznam, sdílení, notifikace

---

## Strategie migrace

**Rozhodnuto:** Clean break (viz rozhodnutí #024).

- Nová aplikace od nuly, prázdná DB
- Stará data ani soubory se nemigrují
- Stará aplikace je archivovaná pouze v gitu (bez provozu)

---

## Praktický postup (clean break)

1. Stará aplikace se neprovozuje; archiv je pouze v gitu.
2. Nová aplikace se nasadí s prázdnou DB + prázdným S3 bucketem.
3. Uživatelé začnou “od nuly”.

---

## Rizika

| Riziko | Mitigace |
|--------|----------|
| Ztráta dat ve staré app | Akceptováno: clean break, archiv pouze v gitu (bez dat) |
| Chaos v deploy | Docker Compose + staging prostředí |

---

## Rozhodnuto

- Archiv = pouze git repo, aplikace se neprovozuje.
