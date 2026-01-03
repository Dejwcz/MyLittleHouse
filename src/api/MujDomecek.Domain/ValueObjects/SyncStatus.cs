namespace MujDomecek.Domain.ValueObjects;

public enum SyncStatus
{
    Local = 0,
    Pending = 1,
    Syncing = 2,
    Synced = 3,
    Failed = 4
}
