# Background Jobs

Přehled všech background jobs v systému.

**Framework:** Hangfire nebo Quartz.NET (viz 11-tech-standards.md)

---

## Přehled

| Job | Typ | Frekvence | Popis |
|-----|-----|-----------|-------|
| [DraftCleanup](#draftcleanup) | Recurring | 1x denně | Mazání starých draftů |
| [DraftReminder](#draftreminder) | Recurring | 1x denně | Připomínka rozpracovaných |
| [InvitationExpiration](#invitationexpiration) | Recurring | 1x denně | Označení expirovaných pozvánek |
| [ActivityCleanup](#activitycleanup) | Recurring | 1x týdně | Mazání starých aktivit |
| [NotificationCleanup](#notificationcleanup) | Recurring | 1x týdně | Mazání starých notifikací |
| [RefreshTokenCleanup](#refreshtokencleanup) | Recurring | 1x denně | Mazání expirovaných tokenů |
| [WeeklySummaryEmail](#weeklysummaryemail) | Recurring | 1x týdně | Týdenní souhrn emailem |
| [SyncRetry](#syncretry) | Recurring | Každých 5 min | Retry failed sync operací |
| [ExportDataJob](#exportdatajob) | Fire-and-forget | On demand | Generování exportu dat |
| [SendEmailJob](#sendemailJob) | Fire-and-forget | On demand | Odeslání emailu |

---

## DraftCleanup

Automatické mazání draft záznamů starších než 30 dní.

### Konfigurace

```json
// appsettings.json
{
  "Jobs": {
    "DraftCleanup": {
      "Enabled": true,
      "CronSchedule": "0 3 * * *",  // 3:00 AM denně
      "WarningDaysBeforeDelete": 7,
      "DeleteAfterDays": 30,
      "BatchSize": 100
    }
  }
}
```

### Logika

```
1. Najdi drafty kde CreatedAt < (now - 30 dní)
2. Pro každý draft:
   a) Smaž související dokumenty z S3
   b) Soft delete záznamu
3. Loguj počet smazaných
```

### Implementace

```csharp
public class DraftCleanupJob : IRecurringJob
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_options.DeleteAfterDays);

        var draftsToDelete = await _db.Zaznamy
            .Where(z => z.Status == ZaznamStatus.Draft)
            .Where(z => z.CreatedAt < cutoffDate)
            .Where(z => !z.IsDeleted)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        foreach (var draft in draftsToDelete)
        {
            // Smaž dokumenty z S3
            var docs = await _db.ZaznamDokumenty
                .Where(d => d.ZaznamId == draft.Id)
                .ToListAsync(ct);

            foreach (var doc in docs)
            {
                await _s3Client.DeleteObjectAsync(doc.StorageKey, ct);
                doc.IsDeleted = true;
            }

            // Soft delete draft
            draft.IsDeleted = true;
            draft.DeletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("DraftCleanup: Deleted {Count} drafts", draftsToDelete.Count);
    }
}
```

---

## DraftReminder

Připomínka uživatelům o rozpracovaných záznamech.

### Konfigurace

```json
{
  "Jobs": {
    "DraftReminder": {
      "Enabled": true,
      "CronSchedule": "0 10 * * *",  // 10:00 AM denně
      "ReminderAfterDays": 3,
      "ExpirationWarningDays": 7,
      "BatchSize": 500
    }
  }
}
```

### Logika

```
1. Najdi drafty starší než 3 dny bez reminder notifikace
2. Pro každý draft vytvoř notifikaci "draft_reminder"
3. Najdi drafty které budou smazány za 7 dní
4. Pro každý vytvoř notifikaci "draft_expiring"
```

### Notifikace

**Draft reminder (po 3 dnech):**
```json
{
  "type": "draft_reminder",
  "payload": {
    "zaznamId": "...",
    "propertyName": "Chalupa",
    "createdAt": "2024-12-20T10:00:00Z",
    "daysOld": 3
  }
}
```

**Draft expiring (7 dní před smazáním):**
```json
{
  "type": "draft_expiring",
  "payload": {
    "zaznamId": "...",
    "propertyName": "Chalupa",
    "expiresAt": "2025-01-20T00:00:00Z",
    "daysRemaining": 7
  }
}
```

---

## InvitationExpiration

Označení expirovaných pozvánek a notifikace vlastníkům.

### Konfigurace

```json
{
  "Jobs": {
    "InvitationExpiration": {
      "Enabled": true,
      "CronSchedule": "0 4 * * *",  // 4:00 AM denně
      "BatchSize": 200
    }
  }
}
```

### Logika

```
1. Najdi pozvánky kde:
   - Status = Pending
   - ExpiresAt < now
2. Pro každou:
   a) Nastav Status = Expired
   b) Vytvoř notifikaci pro vlastníka (invitation_expired)
```

### Implementace

```csharp
public class InvitationExpirationJob : IRecurringJob
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var expiredInvitations = await _db.Invitations
            .Where(i => i.Status == InvitationStatus.Pending)
            .Where(i => i.ExpiresAt < DateTime.UtcNow)
            .Include(i => i.CreatedByUser)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        foreach (var invitation in expiredInvitations)
        {
            invitation.Status = InvitationStatus.Expired;

            // Notifikace vlastníkovi
            await _notificationService.CreateAsync(new Notification
            {
                UserId = invitation.CreatedBy,
                Type = NotificationType.InvitationExpired,
                Payload = JsonSerializer.Serialize(new
                {
                    invitationId = invitation.Id,
                    email = invitation.Email,
                    targetType = invitation.TargetType,
                    targetName = await GetTargetName(invitation)
                })
            }, ct);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("InvitationExpiration: Expired {Count} invitations",
            expiredInvitations.Count);
    }
}
```

---

## ActivityCleanup

Mazání starých activity log záznamů.

### Konfigurace

```json
{
  "Jobs": {
    "ActivityCleanup": {
      "Enabled": true,
      "CronSchedule": "0 2 * * 0",  // 2:00 AM v neděli
      "RetentionDays": 90,
      "BatchSize": 1000
    }
  }
}
```

### Logika

```
1. Smaž activity záznamy starší než 90 dní
2. Prováděj v batches po 1000 (performance)
3. Opakuj dokud jsou záznamy ke smazání
```

### Implementace

```csharp
public class ActivityCleanupJob : IRecurringJob
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);
        var totalDeleted = 0;

        while (!ct.IsCancellationRequested)
        {
            var deleted = await _db.Activities
                .Where(a => a.CreatedAt < cutoffDate)
                .Take(_options.BatchSize)
                .ExecuteDeleteAsync(ct);

            totalDeleted += deleted;

            if (deleted < _options.BatchSize)
                break;

            // Krátká pauza mezi batches
            await Task.Delay(100, ct);
        }

        _logger.LogInformation("ActivityCleanup: Deleted {Count} activities", totalDeleted);
    }
}
```

---

## NotificationCleanup

Mazání starých přečtených notifikací.

### Konfigurace

```json
{
  "Jobs": {
    "NotificationCleanup": {
      "Enabled": true,
      "CronSchedule": "0 2 * * 0",  // 2:00 AM v neděli
      "RetentionDaysRead": 30,
      "RetentionDaysUnread": 90,
      "BatchSize": 1000
    }
  }
}
```

### Logika

```
1. Smaž přečtené notifikace starší než 30 dní
2. Smaž nepřečtené notifikace starší než 90 dní
```

---

## RefreshTokenCleanup

Mazání expirovaných a revokovaných refresh tokenů.

### Konfigurace

```json
{
  "Jobs": {
    "RefreshTokenCleanup": {
      "Enabled": true,
      "CronSchedule": "0 5 * * *",  // 5:00 AM denně
      "BatchSize": 500
    }
  }
}
```

### Logika

```
1. Smaž tokeny kde ExpiresAt < now
2. Smaž tokeny kde RevokedAt is not null AND RevokedAt < (now - 7 dní)
```

---

## WeeklySummaryEmail

Týdenní souhrn aktivity emailem.

### Konfigurace

```json
{
  "Jobs": {
    "WeeklySummaryEmail": {
      "Enabled": true,
      "CronSchedule": "0 9 * * 1",  // 9:00 AM v pondělí
      "BatchSize": 100
    }
  }
}
```

### Logika

```
1. Najdi uživatele kde preferences.emailWeeklySummary = true
2. Pro každého uživatele:
   a) Spočítej statistiky za poslední týden
   b) Pokud je co reportovat, vytvoř email job
```

### Statistiky v emailu

```typescript
interface WeeklySummary {
  user: { name: string; email: string };
  period: { from: string; to: string };

  // Moje aktivita
  myActivity: {
    zaznamyCreated: number;
    zaznamyUpdated: number;
    documentsUploaded: number;
    commentsAdded: number;
  };

  // Aktivita na sdílených
  sharedActivity: {
    newZaznamy: { property: string; count: number }[];
    newComments: { zaznam: string; author: string }[];
    newMembers: { property: string; member: string }[];
  };

  // Upozornění
  alerts: {
    pendingDrafts: number;
    pendingInvitations: number;
    expiringDrafts: number;
  };
}
```

### Email template

```html
Předmět: Váš týdenní souhrn z MujDomecek

Dobrý den {name},

zde je přehled aktivity za poslední týden ({from} - {to}):

📊 VAŠE AKTIVITA
- Vytvořeno záznamů: {zaznamyCreated}
- Nahráno dokumentů: {documentsUploaded}
- Přidáno komentářů: {commentsAdded}

👥 AKTIVITA NA SDÍLENÝCH NEMOVITOSTECH
{#each sharedActivity.newZaznamy}
- {property}: {count} nových záznamů
{/each}

⚠️ UPOZORNĚNÍ
{#if pendingDrafts > 0}
- Máte {pendingDrafts} rozpracovaných záznamů
{/if}
{#if pendingInvitations > 0}
- Čeká na vás {pendingInvitations} pozvánek
{/if}

[Otevřít MujDomecek]

---
Tento email můžete vypnout v Nastavení → Notifikace.
```

---

## SyncRetry

Opakované pokusy o synchronizaci failed položek.

### Konfigurace

```json
{
  "Jobs": {
    "SyncRetry": {
      "Enabled": true,
      "CronSchedule": "*/5 * * * *",  // Každých 5 minut
      "MaxRetries": 5,
      "RetryDelayMinutes": [5, 15, 60, 240, 1440],  // Exponential backoff
      "BatchSize": 50
    }
  }
}
```

### Logika

```
1. Najdi failed sync operace kde:
   - RetryCount < MaxRetries
   - NextRetryAt <= now
2. Pro každou:
   a) Pokus se znovu synchronizovat
   b) Při úspěchu: smaž z fronty
   c) Při neúspěchu: increment RetryCount, nastav NextRetryAt
```

### Implementace

```csharp
public class SyncRetryJob : IRecurringJob
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var failedItems = await _db.SyncQueue
            .Where(s => s.Status == SyncStatus.Failed)
            .Where(s => s.RetryCount < _options.MaxRetries)
            .Where(s => s.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(s => s.NextRetryAt)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        foreach (var item in failedItems)
        {
            try
            {
                await _syncService.ProcessAsync(item, ct);

                // Úspěch - smaž z fronty
                _db.SyncQueue.Remove(item);
            }
            catch (Exception ex)
            {
                // Neúspěch - naplánuj další retry
                item.RetryCount++;
                item.LastError = ex.Message;
                item.NextRetryAt = CalculateNextRetry(item.RetryCount);

                if (item.RetryCount >= _options.MaxRetries)
                {
                    item.Status = SyncStatus.PermanentlyFailed;
                    // Notifikace uživateli
                    await NotifyUserAboutSyncFailure(item, ct);
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private DateTime CalculateNextRetry(int retryCount)
    {
        var delayMinutes = _options.RetryDelayMinutes[
            Math.Min(retryCount, _options.RetryDelayMinutes.Length - 1)
        ];
        return DateTime.UtcNow.AddMinutes(delayMinutes);
    }
}
```

---

## ExportDataJob

Generování exportu uživatelských dat (fire-and-forget).

### Trigger

```csharp
// POST /api/users/me/export
await _backgroundJobs.Enqueue<ExportDataJob>(
    job => job.ExecuteAsync(userId, format, cancellationToken));
```

### Konfigurace

```json
{
  "Jobs": {
    "ExportData": {
      "ExpirationHours": 24,
      "MaxSizeBytes": 1073741824,  // 1 GB
      "IncludePhotos": true
    }
  }
}
```

### Logika

```
1. Sbírej data uživatele:
   - Profil
   - Projekty, Properties, Units
   - Záznamy s komentáři
   - Dokumenty (metadata + soubory pokud IncludePhotos)
   - Kontakty
   - Notifikace
2. Vytvoř ZIP archiv
3. Upload na S3 s presigned URL (24h expirace)
4. Pošli email s download linkem
```

### Email

```
Předmět: Váš export dat z MujDomecek je připraven

Dobrý den,

váš export dat je připraven ke stažení.

Velikost: {size}
Platnost odkazu: 24 hodin

[Stáhnout export]

S pozdravem,
Tým MujDomecek
```

---

## SendEmailJob

Odeslání emailu (fire-and-forget).

### Trigger

```csharp
await _backgroundJobs.Enqueue<SendEmailJob>(
    job => job.ExecuteAsync(emailRequest, cancellationToken));
```

### Konfigurace

```json
{
  "Email": {
    "Provider": "SMTP",  // nebo "SendGrid", "Mailgun"
    "From": "noreply@mujdomecek.cz",
    "FromName": "MujDomecek",
    "RetryCount": 3,
    "RetryDelaySeconds": 30
  }
}
```

### Typy emailů

| Template | Trigger | Popis |
|----------|---------|-------|
| `registration_confirmation` | Registrace | Potvrzení emailu |
| `password_reset` | Forgot password | Reset hesla |
| `invitation` | Přidání člena | Pozvánka ke sdílení |
| `invitation_accepted` | Accept invitation | Pozvánka přijata |
| `weekly_summary` | WeeklySummaryEmail job | Týdenní souhrn |
| `export_ready` | ExportDataJob | Export připraven |
| `draft_expiring` | DraftReminder job | Varování před smazáním |

---

## Monitoring

### Health Checks

```csharp
// Hangfire dashboard na /hangfire (pouze admin)
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AdminAuthorizationFilter() }
});

// Health check endpoint
app.MapHealthChecks("/health/jobs", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("jobs")
});
```

### Metriky

| Metrika | Popis |
|---------|-------|
| `job_execution_duration_seconds` | Doba běhu jobu |
| `job_execution_total` | Počet spuštění |
| `job_failures_total` | Počet selhání |
| `job_queue_length` | Délka fronty |

### Alerting

| Podmínka | Akce |
|----------|------|
| Job selže 3x za sebou | Email adminovi |
| Fronta > 1000 položek | Slack notifikace |
| Job běží > 10 minut | Warning log |

---

## Cron Schedule Reference

| Výraz | Význam |
|-------|--------|
| `0 3 * * *` | Každý den ve 3:00 |
| `0 2 * * 0` | Každou neděli ve 2:00 |
| `0 9 * * 1` | Každé pondělí v 9:00 |
| `*/5 * * * *` | Každých 5 minut |
| `0 */4 * * *` | Každé 4 hodiny |

---

## Deployment

### Docker

```yaml
# docker-compose.yml
services:
  api:
    # ... hlavní API

  worker:
    image: mujdomecek-api:latest
    command: ["dotnet", "MujDomecek.API.dll", "--worker"]
    environment:
      - HANGFIRE_WORKER=true
    deploy:
      replicas: 2
```

### Scaling

- **Recurring jobs:** Běží pouze na jednom workeru (distributed lock)
- **Fire-and-forget:** Distribuováno mezi všechny workery
- **Doporučení:** 2-3 worker instance pro redundanci
