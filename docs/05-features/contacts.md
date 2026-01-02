# Kontakty a skupiny

## Přehled

Jednosměrný seznam kontaktů pro rychlé pozvání ke sdílení.

---

## Kontakt

| Pole | Popis |
|------|-------|
| Email | Email kontaktu (required) |
| DisplayName | Zobrazované jméno (optional) |

### Operace

- Přidat kontakt (email + jméno)
- Upravit jméno
- Smazat kontakt

---

## Skupiny

Skupiny kontaktů pro hromadné pozvání.

| Pole | Popis |
|------|-------|
| Name | Název skupiny (např. "Rodina", "Práce") |
| Members | Seznam kontaktů |

### Operace

- Vytvořit skupinu
- Přejmenovat skupinu
- Přidat/odebrat členy
- Smazat skupinu

---

## Použití při sdílení

1. Uživatel klikne "Sdílet"
2. V dialogu vybere:
   - Jednotlivé kontakty
   - Celou skupinu
   - Nebo zadá email ručně
3. Vytvoří se pozvánky:
   - Registrovaní uvidí in-app notifikaci
   - Pro neregistrované Owner zkopíruje invite link/kód a pošle mimo aplikaci

---

## UI

### Seznam kontaktů

| Sloupec | Popis |
|---------|-------|
| Jméno | DisplayName nebo email |
| Email | Email |
| Skupiny | Badge se skupinami |
| Akce | Edit / Delete |

### Seznam skupin

| Sloupec | Popis |
|---------|-------|
| Název | Název skupiny |
| Počet | Počet členů |
| Akce | Edit / Delete |

### Přidání kontaktu

- Modal s polem Email + DisplayName
- Volitelně přiřazení do skupin

---

## Scope

| Fáze | Funkcionalita |
|------|---------------|
| MVP | Základní kontakty + skupiny |
| Later | Import z Google Contacts, autocomplete |

---

## Rozhodnuto

- **Validace emailu:** Standardní frontend + backend validace (RFC 5321)
- **Deduplikace:** Unique constraint na `OwnerUserId + Email` (lowercase) - viz indexy v data-model
- **Merge při registraci:** Ne. Contact zůstává jako adresář. Při zobrazení se doplní avatar z User pokud existuje.
