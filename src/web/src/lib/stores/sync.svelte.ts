import { browser } from '$app/environment';
import { db, getPendingChangesCount } from '$lib/db';

export interface ConflictData {
  entityType: string;
  entityId: string;
  localVersion: {
    data: Record<string, unknown>;
    updatedAt: number;
  };
  serverVersion: {
    data: Record<string, unknown>;
    updatedAt: number;
  };
}

class SyncStore {
  // Connection state
  isOnline = $state(browser ? navigator.onLine : true);

  // Sync state
  isSyncing = $state(false);
  pendingCount = $state(0);
  lastSyncAt = $state<number | null>(null);
  error = $state<string | null>(null);

  // Conflict resolution
  conflicts = $state<ConflictData[]>([]);
  hasConflicts = $derived(this.conflicts.length > 0);

  // Derived states
  readonly needsSync = $derived(this.pendingCount > 0 && this.isOnline && !this.isSyncing);
  readonly syncStatus = $derived<'idle' | 'syncing' | 'pending' | 'error' | 'offline'>(
    !this.isOnline ? 'offline' :
    this.error ? 'error' :
    this.isSyncing ? 'syncing' :
    this.pendingCount > 0 ? 'pending' :
    'idle'
  );

  constructor() {
    if (browser) {
      // Listen for online/offline events
      window.addEventListener('online', () => {
        this.isOnline = true;
        // Auto-sync when coming back online
        this.triggerSync();
      });
      window.addEventListener('offline', () => {
        this.isOnline = false;
      });

      // Initial pending count
      this.updatePendingCount();
    }
  }

  /**
   * Update the pending changes count from database
   */
  async updatePendingCount(): Promise<void> {
    try {
      this.pendingCount = await getPendingChangesCount();
    } catch (err) {
      console.error('Failed to get pending count:', err);
    }
  }

  /**
   * Trigger synchronization for all synced projects
   */
  async triggerSync(): Promise<void> {
    if (!this.isOnline || this.isSyncing) return;

    this.isSyncing = true;
    this.error = null;

    try {
      const syncedProjects = await db.projects.where('syncMode').equals('synced').toArray();
      const syncedProperties = await db.properties.where('syncMode').equals('synced').toArray();
      const syncedZaznamy = await db.zaznamy.where('syncMode').equals('synced').toArray();

      for (const project of syncedProjects) {
        await this.syncScope('project', project.id);
      }
      for (const property of syncedProperties) {
        await this.syncScope('property', property.id);
      }
      for (const zaznam of syncedZaznamy) {
        await this.syncScope('zaznam', zaznam.id);
      }

      this.lastSyncAt = Date.now();
    } catch (err) {
      this.error = err instanceof Error ? err.message : 'Synchronizace selhala';
      console.error('Sync failed:', err);
    } finally {
      this.isSyncing = false;
      await this.updatePendingCount();
    }
  }

  /**
   * Sync a single scope using the sync engine
   */
  async syncScope(scopeType: 'project' | 'property' | 'zaznam', scopeId: string): Promise<void> {
    const { syncEngine } = await import('$lib/api/sync');
    await syncEngine.syncScope(scopeType, scopeId);
  }

  /**
   * Backwards-compatible project sync wrapper
   */
  async syncProject(projectId: string): Promise<void> {
    return this.syncScope('project', projectId);
  }

  /**
   * Pull changes from server for a project
   */
  async pullProject(projectId: string): Promise<void> {
    const { syncEngine } = await import('$lib/api/sync');
    await syncEngine.pullChanges('project', projectId);
  }

  /**
   * Retry failed sync operations
   */
  async retryFailed(scope?: { scopeType: 'project' | 'property' | 'zaznam'; scopeId: string }): Promise<void> {
    const { syncEngine } = await import('$lib/api/sync');
    await syncEngine.retryFailed(scope);
  }

  /**
   * Add a conflict to be resolved by user
   */
  addConflict(conflict: ConflictData): void {
    this.conflicts = [...this.conflicts, conflict];
  }

  /**
   * Resolve a conflict
   */
  async resolveConflict(
    entityType: string,
    entityId: string,
    choice: 'local' | 'server'
  ): Promise<void> {
    // Remove from conflicts list
    this.conflicts = this.conflicts.filter(
      c => !(c.entityType === entityType && c.entityId === entityId)
    );

    // TODO: Apply the chosen version
    if (choice === 'server') {
      // Update local with server data
    }
    // If 'local', we just push our version (already in queue)

    await this.updatePendingCount();
  }

  /**
   * Clear all errors
   */
  clearError(): void {
    this.error = null;
  }
}

export const sync = new SyncStore();
