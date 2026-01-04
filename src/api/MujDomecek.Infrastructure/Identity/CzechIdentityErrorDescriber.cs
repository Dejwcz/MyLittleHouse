using Microsoft.AspNetCore.Identity;

namespace MujDomecek.Infrastructure.Identity;

public class CzechIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
        => new() { Code = nameof(DefaultError), Description = "Došlo k neznámé chybě." };

    public override IdentityError ConcurrencyFailure()
        => new() { Code = nameof(ConcurrencyFailure), Description = "Chyba souběžnosti. Záznam byl změněn jiným uživatelem." };

    public override IdentityError PasswordMismatch()
        => new() { Code = nameof(PasswordMismatch), Description = "Nesprávné heslo." };

    public override IdentityError InvalidToken()
        => new() { Code = nameof(InvalidToken), Description = "Neplatný token." };

    public override IdentityError LoginAlreadyAssociated()
        => new() { Code = nameof(LoginAlreadyAssociated), Description = "Uživatel s tímto přihlášením již existuje." };

    public override IdentityError InvalidUserName(string? userName)
        => new() { Code = nameof(InvalidUserName), Description = $"Uživatelské jméno '{userName}' je neplatné. Může obsahovat pouze písmena a číslice." };

    public override IdentityError InvalidEmail(string? email)
        => new() { Code = nameof(InvalidEmail), Description = $"Email '{email}' je neplatný." };

    public override IdentityError DuplicateUserName(string userName)
        => new() { Code = nameof(DuplicateUserName), Description = $"Uživatel s emailem '{userName}' již existuje." };

    public override IdentityError DuplicateEmail(string email)
        => new() { Code = nameof(DuplicateEmail), Description = $"Uživatel s emailem '{email}' již existuje." };

    public override IdentityError InvalidRoleName(string? role)
        => new() { Code = nameof(InvalidRoleName), Description = $"Název role '{role}' je neplatný." };

    public override IdentityError DuplicateRoleName(string role)
        => new() { Code = nameof(DuplicateRoleName), Description = $"Role '{role}' již existuje." };

    public override IdentityError UserAlreadyHasPassword()
        => new() { Code = nameof(UserAlreadyHasPassword), Description = "Uživatel již má nastavené heslo." };

    public override IdentityError UserLockoutNotEnabled()
        => new() { Code = nameof(UserLockoutNotEnabled), Description = "Uzamčení účtu není pro tohoto uživatele povoleno." };

    public override IdentityError UserAlreadyInRole(string role)
        => new() { Code = nameof(UserAlreadyInRole), Description = $"Uživatel již má přiřazenou roli '{role}'." };

    public override IdentityError UserNotInRole(string role)
        => new() { Code = nameof(UserNotInRole), Description = $"Uživatel nemá přiřazenou roli '{role}'." };

    public override IdentityError PasswordTooShort(int length)
        => new() { Code = nameof(PasswordTooShort), Description = $"Heslo musí mít alespoň {length} znaků." };

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Heslo musí obsahovat alespoň jeden speciální znak (např. !@#$%)." };

    public override IdentityError PasswordRequiresDigit()
        => new() { Code = nameof(PasswordRequiresDigit), Description = "Heslo musí obsahovat alespoň jednu číslici (0-9)." };

    public override IdentityError PasswordRequiresLower()
        => new() { Code = nameof(PasswordRequiresLower), Description = "Heslo musí obsahovat alespoň jedno malé písmeno (a-z)." };

    public override IdentityError PasswordRequiresUpper()
        => new() { Code = nameof(PasswordRequiresUpper), Description = "Heslo musí obsahovat alespoň jedno velké písmeno (A-Z)." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Heslo musí obsahovat alespoň {uniqueChars} různých znaků." };

    public override IdentityError RecoveryCodeRedemptionFailed()
        => new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Obnovovací kód nebyl použit." };
}
