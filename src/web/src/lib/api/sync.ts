import { db, type SyncQueueItem, getPendingChanges } from '$lib/db';
import { api } from './client';
import { auth } from '$lib/stores/auth.svelte';
import { sync } from '$lib/stores/sync.svelte';

const MAX_RETRY_ATTEMPTS = 3;
const RETRY_DELAY_MS = 5000;

/**
 * Sync Engine - handles synchronization between IndexedDB and server
 */
export const syncEngine = {
  /**
   * Sync all pending changes for a scope to the server
   */
  async syncScope(scopeType: SyncQueueItem['scopeType'], scopeId: string): Promise<void> {
    if (scopeType === 'project') {
      const project = await db.projects.get(scopeId);
      if (!project || project.syncMode !== 'synced') return;
    }
    if (scopeType === 'property') {
      const property = await db.properties.get(scopeId);
      if (!property || property.syncMode !== 'synced') return;
    }
    if (scopeType === 'zaznam') {
      const zaznam = await db.zaznamy.get(scopeId);
      if (!zaznam || zaznam.syncMode !== 'synced') return;
    }

    // Check if user is authenticated
    if (!auth.isAuthenticated) {
      console.warn('Cannot sync: user not authenticated');
      return;
    }

    const pendingChanges = await getPendingChanges(scopeType, scopeId);

    // Sort by createdAt to maintain order
    pendingChanges.sort((a, b) => a.createdAt - b.createdAt);

    for (const change of pendingChanges) {
      // Skip if too many failed attempts
      if (change.attempts >= MAX_RETRY_ATTEMPTS) {
        console.warn(`Skipping change ${change.id}: max attempts reached`);
        continue;
      }

      // Mark as syncing
      await db.syncQueue.update(change.id, {
        status: 'syncing',
        lastAttemptAt: Date.now()
      });

      try {
        await this.pushChange(change);

        // Success - remove from queue
        await db.syncQueue.delete(change.id);

        // Update entity sync status
        await this.updateEntitySyncStatus(change.entityType, change.entityId, 'synced');

      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Unknown error';

        // Check if it's a conflict
        if (this.isConflictError(err)) {
          await this.handleConflict(change);
        } else {
          // Regular error - schedule retry
          await db.syncQueue.update(change.id, {
            status: 'failed',
            attempts: change.attempts + 1,
            lastError: errorMessage,
            nextRetryAt: Date.now() + RETRY_DELAY_MS * Math.pow(2, change.attempts)
          });
        }

        console.error(`Failed to sync change ${change.id}:`, err);
      }
    }

    const now = Date.now();
    if (scopeType === 'project') {
      await db.projects.update(scopeId, { lastSyncAt: now, syncStatus: 'synced' });
    }
    if (scopeType === 'property') {
      await db.properties.update(scopeId, { lastSyncAt: now, syncStatus: 'synced' });
    }
    if (scopeType === 'zaznam') {
      await db.zaznamy.update(scopeId, { lastSyncAt: now, syncStatus: 'synced' });
    }
  },

  /**
   * Backwards-compatible project sync wrapper
   */
  async syncProject(projectId: string): Promise<void> {
    return this.syncScope('project', projectId);
  },

  /**
   * Push a single change to the server
   */
  async pushChange(change: SyncQueueItem): Promise<void> {
    const { entityType, entityId, action, payload } = change;

    switch (entityType) {
      case 'projects':
        await this.pushProjectChange(entityId, action, payload);
        break;
      case 'properties':
        await this.pushPropertyChange(entityId, action, payload);
        break;
      case 'units':
        await this.pushUnitChange(entityId, action, payload);
        break;
      case 'zaznamy':
        await this.pushZaznamChange(entityId, action, payload);
        break;
      case 'media':
        await this.pushMediaChange(entityId, action, payload);
        break;
      default:
        throw new Error(`Unknown entity type: ${entityType}`);
    }
  },

  async pushProjectChange(
    id: string,
    action: SyncQueueItem['action'],
    payload: object | null
  ): Promise<void> {
    switch (action) {
      case 'create':
        // Get local project to send full data
        const project = await db.projects.get(id);
        if (project) {
          const response = await api.post<{ id: string }>('/projects', {
            name: project.name,
            description: project.description
          });
          // Store remote ID if different
          if (response?.id && response.id !== id) {
            await db.projects.update(id, { remoteId: response.id });
          }
        }
        break;
      case 'update':
        await api.put(`/projects/${id}`, payload);
        break;
      case 'delete':
        await api.delete(`/projects/${id}`);
        break;
    }
  },

  async pushPropertyChange(
    id: string,
    action: SyncQueueItem['action'],
    payload: object | null
  ): Promise<void> {
    switch (action) {
      case 'create':
        const property = await db.properties.get(id);
        if (property) {
          await api.post('/properties', {
            projectId: property.projectId,
            name: property.name,
            address: property.address,
            description: property.description
          });
        }
        break;
      case 'update':
        await api.put(`/properties/${id}`, payload);
        break;
      case 'delete':
        await api.delete(`/properties/${id}`);
        break;
    }
  },

  async pushUnitChange(
    id: string,
    action: SyncQueueItem['action'],
    payload: object | null
  ): Promise<void> {
    switch (action) {
      case 'create':
        const unit = await db.units.get(id);
        if (unit) {
          await api.post('/units', {
            propertyId: unit.propertyId,
            parentUnitId: unit.parentUnitId,
            name: unit.name,
            unitType: unit.unitType,
            description: unit.description
          });
        }
        break;
      case 'update':
        await api.put(`/units/${id}`, payload);
        break;
      case 'delete':
        await api.delete(`/units/${id}`);
        break;
    }
  },

  async pushZaznamChange(
    id: string,
    action: SyncQueueItem['action'],
    payload: object | null
  ): Promise<void> {
    switch (action) {
      case 'create':
        const zaznam = await db.zaznamy.get(id);
        if (zaznam) {
          await api.post('/zaznamy', {
            propertyId: zaznam.propertyId,
            unitId: zaznam.unitId,
            title: zaznam.title,
            description: zaznam.description,
            date: zaznam.date,
            cost: zaznam.cost,
            status: zaznam.status
          });
        }
        break;
      case 'update':
        await api.put(`/zaznamy/${id}`, payload);
        break;
      case 'delete':
        await api.delete(`/zaznamy/${id}`);
        break;
    }
  },

  async pushMediaChange(
    id: string,
    action: SyncQueueItem['action'],
    _payload: object | null
  ): Promise<void> {
    // Media records require upload + confirm before creating metadata
    switch (action) {
      case 'create':
        const media = await db.media.get(id);
        if (media?.storageKey) {
          await api.post('/media', {
            ownerType: media.ownerType,
            ownerId: media.ownerId,
            storageKey: media.storageKey,
            mediaType: media.mediaType,
            originalFileName: media.originalFileName,
            mimeType: media.mimeType,
            sizeBytes: media.sizeBytes,
            caption: media.caption
          });
        }
        break;
      case 'update':
        const updated = await db.media.get(id);
        if (updated) {
          await api.put(`/media/${id}`, { caption: updated.caption });
        }
        break;
      case 'delete':
        await api.delete(`/media/${id}`);
        break;
    }
  },

  /**
   * Pull changes from server for a scope
   */
  async pullChanges(scopeType: SyncQueueItem['scopeType'], scopeId: string, since?: number): Promise<void> {
    if (scopeType === 'project') {
      const project = await db.projects.get(scopeId);
      if (!project || project.syncMode !== 'synced') return;
    }
    if (scopeType === 'property') {
      const property = await db.properties.get(scopeId);
      if (!property || property.syncMode !== 'synced') return;
    }
    if (scopeType === 'zaznam') {
      const zaznam = await db.zaznamy.get(scopeId);
      if (!zaznam || zaznam.syncMode !== 'synced') return;
    }

    if (!auth.isAuthenticated) return;

    try {
      const lastSyncAt =
        scopeType === 'project'
          ? (await db.projects.get(scopeId))?.lastSyncAt
          : scopeType === 'property'
            ? (await db.properties.get(scopeId))?.lastSyncAt
            : (await db.zaznamy.get(scopeId))?.lastSyncAt;

      const sinceTs = since ?? lastSyncAt ?? 0;
      const response = await api.get<{
        changes: Array<{
          entityType: string;
          entityId: string;
          action: string;
          data: Record<string, unknown>;
          updatedAt: number;
        }>;
        serverTime: number;
      }>(`/sync/pull?scopeType=${scopeType}&scopeId=${scopeId}&since=${sinceTs}`);

      const { changes, serverTime } = response;

      for (const change of changes) {
        await this.applyServerChange(change);
      }

      if (scopeType === 'project') {
        await db.projects.update(scopeId, { lastSyncAt: serverTime });
      }
      if (scopeType === 'property') {
        await db.properties.update(scopeId, { lastSyncAt: serverTime });
      }
      if (scopeType === 'zaznam') {
        await db.zaznamy.update(scopeId, { lastSyncAt: serverTime });
      }

    } catch (err) {
      console.error('Failed to pull changes:', err);
      throw err;
    }
  },

  /**
   * Apply a change received from the server
   */
  async applyServerChange(change: {
    entityType: string;
    entityId: string;
    action: string;
    data: Record<string, unknown>;
    updatedAt: number;
  }): Promise<void> {
    const { entityType, entityId, action, data, updatedAt } = change;

    // Get table
    const table = this.getTable(entityType);
    if (!table) return;

    // Check for local conflicts
    const localEntity = await table.get(entityId);
    if (localEntity && localEntity.updatedAt > updatedAt) {
      // Local is newer - potential conflict
      sync.addConflict({
        entityType,
        entityId,
        localVersion: {
          data: localEntity as unknown as Record<string, unknown>,
          updatedAt: localEntity.updatedAt
        },
        serverVersion: {
          data,
          updatedAt
        }
      });
      return;
    }

    // Apply change
    switch (action) {
      case 'create':
      case 'update':
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        await (table as any).put({
          ...data,
          id: entityId,
          updatedAt,
          syncStatus: 'synced'
        });
        break;
      case 'delete':
        await table.delete(entityId);
        break;
    }
  },

  /**
   * Get Dexie table by entity type
   */
  getTable(entityType: string) {
    switch (entityType) {
      case 'projects': return db.projects;
      case 'properties': return db.properties;
      case 'units': return db.units;
      case 'zaznamy': return db.zaznamy;
      case 'media': return db.media;
      default: return null;
    }
  },

  /**
   * Update entity sync status
   */
  async updateEntitySyncStatus(
    entityType: SyncQueueItem['entityType'],
    entityId: string,
    status: 'synced' | 'pending' | 'failed'
  ): Promise<void> {
    const table = this.getTable(entityType);
    if (table) {
      await table.update(entityId, { syncStatus: status });
    }
  },

  /**
   * Check if error is a conflict (409)
   */
  isConflictError(err: unknown): boolean {
    if (err && typeof err === 'object' && 'response' in err) {
      const response = (err as { response?: { status?: number } }).response;
      return response?.status === 409;
    }
    return false;
  },

  /**
   * Handle sync conflict
   */
  async handleConflict(change: SyncQueueItem): Promise<void> {
    // Fetch server version
    try {
      const table = this.getTable(change.entityType);
      const localEntity = table ? await table.get(change.entityId) : null;

      const serverEntity = await api.get<Record<string, unknown>>(`/${change.entityType}/${change.entityId}`);

      sync.addConflict({
        entityType: change.entityType,
        entityId: change.entityId,
        localVersion: {
          data: (localEntity as unknown as Record<string, unknown>) ?? {},
          updatedAt: (localEntity as { updatedAt?: number } | null)?.updatedAt ?? Date.now()
        },
        serverVersion: {
          data: serverEntity,
          updatedAt: (serverEntity.updatedAt as number) ?? Date.now()
        }
      });

      // Keep change in queue but mark as conflict
      await db.syncQueue.update(change.id, {
        status: 'failed',
        lastError: 'Conflict - requires resolution'
      });

    } catch (err) {
      console.error('Failed to handle conflict:', err);
    }
  },

  /**
   * Retry failed changes
   */
  async retryFailed(scope?: { scopeType: SyncQueueItem['scopeType']; scopeId: string }): Promise<void> {
    const now = Date.now();
    let query = db.syncQueue.where('status').equals('failed');

    if (scope) {
      query = query.and(item => item.scopeType === scope.scopeType && item.scopeId === scope.scopeId);
    }

    const failedChanges = await query.and(item =>
      item.attempts < MAX_RETRY_ATTEMPTS &&
      (!item.nextRetryAt || item.nextRetryAt <= now)
    ).toArray();

    for (const change of failedChanges) {
      await db.syncQueue.update(change.id, { status: 'pending' });
    }

    // Trigger sync if there are retryable changes
    if (failedChanges.length > 0) {
      await sync.triggerSync();
    }
  }
};
