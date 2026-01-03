import Dexie, { type Table } from 'dexie';

export type SyncStatus = 'local' | 'synced' | 'syncing' | 'pending' | 'failed';
export type SyncMode = 'local-only' | 'synced';

export interface Project {
  id: string;
  name: string;
  description?: string;
  ownerId: string;
  memberCount: number;
  propertyCount: number;
  myRole: 'owner' | 'editor' | 'viewer';
  updatedAt: number;
  syncStatus: SyncStatus;
  // Hybrid sync fields
  syncMode: SyncMode;           // 'local-only' = never sync, 'synced' = sync to server
  lastSyncAt?: number;          // timestamp of last successful sync
  remoteId?: string;            // ID on server (may differ from local ID)
}

export interface Property {
  id: string;
  projectId: string;
  name: string;
  address?: string;
  description?: string;
  propertyType?: string;
  unitCount: number;
  zaznamCount: number;
  totalCost: number;
  coverMediaId?: string;
  coverUrl?: string;
  updatedAt: number;
  syncStatus: SyncStatus;
  syncMode: SyncMode;
  lastSyncAt?: number;
}

export interface Unit {
  id: string;
  propertyId: string;
  parentUnitId?: string;
  name: string;
  unitType: string;
  description?: string;
  childUnitCount: number;
  zaznamCount: number;
  coverMediaId?: string;
  coverUrl?: string;
  updatedAt: number;
  syncStatus: SyncStatus;
}

export interface Zaznam {
  id: string;
  propertyId: string;
  unitId?: string;
  title: string;
  description?: string;
  date: string;
  cost?: number;
  status: 'draft' | 'complete';
  propertyName?: string;
  unitName?: string;
  updatedAt: number;
  flags?: number;
  syncStatus: SyncStatus;
  syncMode: SyncMode;
  lastSyncAt?: number;
}

export interface Media {
  id: string;
  ownerType: 'property' | 'unit' | 'zaznam';
  ownerId: string;
  mediaType: 'photo' | 'document' | 'receipt';
  storageKey?: string;
  originalFileName?: string;
  mimeType: string;
  sizeBytes: number;
  caption?: string;
  thumbnailUrl?: string;
  url?: string;
  data?: Blob;
  updatedAt: number;
  syncStatus: SyncStatus;
}

export interface Tag {
  id: string;
  name: string;
  color?: string;
}

export interface ZaznamTag {
  zaznamId: string;
  tagId: string;
}

export interface SyncQueueItem {
  id: string;
  scopeType: 'project' | 'property' | 'zaznam';
  scopeId: string;
  projectId?: string;           // Optional for UI filtering
  entityType: 'projects' | 'properties' | 'units' | 'zaznamy' | 'media';
  entityId: string;
  action: 'create' | 'update' | 'delete';
  payload: object | null;
  status: 'pending' | 'syncing' | 'failed';
  attempts: number;
  lastError?: string;
  nextRetryAt?: number;
  createdAt: number;
  lastAttemptAt?: number;
}

class MujDomecekDb extends Dexie {
  projects!: Table<Project>;
  properties!: Table<Property>;
  units!: Table<Unit>;
  zaznamy!: Table<Zaznam>;
  media!: Table<Media>;
  tags!: Table<Tag>;
  zaznamTags!: Table<ZaznamTag>;
  syncQueue!: Table<SyncQueueItem>;

  constructor() {
    super('mujdomecek');

    // v2 - original schema
    this.version(2).stores({
      projects: 'id, name, ownerId, updatedAt, syncStatus',
      properties: 'id, projectId, name, updatedAt, syncStatus',
      units: 'id, propertyId, parentUnitId, updatedAt, syncStatus',
      zaznamy: 'id, propertyId, unitId, date, updatedAt, status, syncStatus',
      dokumenty: 'id, zaznamId, updatedAt, syncStatus',
      tags: 'id, name',
      zaznamTags: '[zaznamId+tagId], zaznamId, tagId',
      syncQueue: 'id, entityType, entityId, action, status, createdAt, attempts, nextRetryAt'
    });

    // v3 - hybrid sync support
    this.version(3).stores({
      projects: 'id, name, ownerId, updatedAt, syncStatus, syncMode',
      properties: 'id, projectId, name, updatedAt, syncStatus',
      units: 'id, propertyId, parentUnitId, updatedAt, syncStatus',
      zaznamy: 'id, propertyId, unitId, date, updatedAt, status, syncStatus',
      dokumenty: 'id, zaznamId, updatedAt, syncStatus',
      tags: 'id, name',
      zaznamTags: '[zaznamId+tagId], zaznamId, tagId',
      syncQueue: 'id, projectId, entityType, entityId, action, status, createdAt, attempts'
    }).upgrade(tx => {
      // Add syncMode to existing projects
      return tx.table('projects').toCollection().modify(project => {
        if (!project.syncMode) {
          project.syncMode = 'local-only';
        }
      });
    });

    // v4 - per-scope sync support
    this.version(4).stores({
      projects: 'id, name, ownerId, updatedAt, syncStatus, syncMode',
      properties: 'id, projectId, name, updatedAt, syncStatus, syncMode',
      units: 'id, propertyId, parentUnitId, updatedAt, syncStatus',
      zaznamy: 'id, propertyId, unitId, date, updatedAt, status, syncStatus, syncMode',
      dokumenty: 'id, zaznamId, updatedAt, syncStatus',
      tags: 'id, name',
      zaznamTags: '[zaznamId+tagId], zaznamId, tagId',
      syncQueue: 'id, scopeType, scopeId, projectId, entityType, entityId, action, status, createdAt, attempts'
    }).upgrade(async tx => {
      await tx.table('properties').toCollection().modify(property => {
        if (!property.syncMode) {
          property.syncMode = 'local-only';
        }
      });

      await tx.table('zaznamy').toCollection().modify(zaznam => {
        if (!zaznam.syncMode) {
          zaznam.syncMode = 'local-only';
        }
      });

      await tx.table('syncQueue').toCollection().modify(item => {
        if (!item.scopeType) {
          item.scopeType = 'project';
        }
        if (!item.scopeId) {
          item.scopeId = item.projectId ?? item.entityId;
        }
      });
    });

    // v5 - media + cover support
    this.version(5).stores({
      projects: 'id, name, ownerId, updatedAt, syncStatus, syncMode',
      properties: 'id, projectId, name, updatedAt, syncStatus, syncMode',
      units: 'id, propertyId, parentUnitId, updatedAt, syncStatus',
      zaznamy: 'id, propertyId, unitId, date, updatedAt, status, syncStatus, syncMode',
      media: 'id, ownerType, ownerId, updatedAt, syncStatus, mediaType',
      dokumenty: 'id, zaznamId, updatedAt, syncStatus',
      tags: 'id, name',
      zaznamTags: '[zaznamId+tagId], zaznamId, tagId',
      syncQueue: 'id, scopeType, scopeId, projectId, entityType, entityId, action, status, createdAt, attempts'
    }).upgrade(async tx => {
      const dokumentyTable = tx.table('dokumenty');
      const mediaTable = tx.table('media');

      if (dokumentyTable) {
        const legacyDocuments = await dokumentyTable.toArray();
        for (const dokument of legacyDocuments) {
          await mediaTable.put({
            id: dokument.id,
            ownerType: 'zaznam',
            ownerId: dokument.zaznamId,
            mediaType: 'document',
            originalFileName: dokument.fileName,
            mimeType: dokument.mimeType,
            sizeBytes: dokument.size,
            data: dokument.data,
            updatedAt: dokument.updatedAt,
            syncStatus: dokument.syncStatus
          });
        }
      }

      await tx.table('properties').toCollection().modify(property => {
        if (!('coverMediaId' in property)) {
          property.coverMediaId = undefined;
        }
        if (!('coverUrl' in property)) {
          property.coverUrl = undefined;
        }
      });

      await tx.table('units').toCollection().modify(unit => {
        if (!('coverMediaId' in unit)) {
          unit.coverMediaId = undefined;
        }
        if (!('coverUrl' in unit)) {
          unit.coverUrl = undefined;
        }
      });

      await tx.table('syncQueue').toCollection().modify(item => {
        if (item.entityType === 'dokumenty') {
          item.entityType = 'media';
        }
      });
    });

    // v6 - propertyType + simplified unitType
    this.version(6).stores({
      projects: 'id, name, ownerId, updatedAt, syncStatus, syncMode',
      properties: 'id, projectId, name, updatedAt, syncStatus, syncMode',
      units: 'id, propertyId, parentUnitId, updatedAt, syncStatus',
      zaznamy: 'id, propertyId, unitId, date, updatedAt, status, syncStatus, syncMode',
      media: 'id, ownerType, ownerId, updatedAt, syncStatus, mediaType',
      dokumenty: 'id, zaznamId, updatedAt, syncStatus',
      tags: 'id, name',
      zaznamTags: '[zaznamId+tagId], zaznamId, tagId',
      syncQueue: 'id, scopeType, scopeId, projectId, entityType, entityId, action, status, createdAt, attempts'
    }).upgrade(async tx => {
      await tx.table('properties').toCollection().modify(property => {
        if (!property.propertyType) {
          property.propertyType = 'other';
        }
      });

      await tx.table('units').toCollection().modify(unit => {
        switch (unit.unitType) {
          case 'room':
            unit.unitType = 'room';
            break;
          case 'stairs':
            unit.unitType = 'floor';
            break;
          case 'garage':
            unit.unitType = 'parking';
            break;
          default:
            unit.unitType = 'other';
            break;
        }
      });
    });
  }
}

export const db = new MujDomecekDb();

// =============================================================================
// Sync Queue Helpers
// =============================================================================

/**
 * Add a change to the sync queue for later synchronization
 */
export async function queueChange(
  scopeType: SyncQueueItem['scopeType'],
  scopeId: string,
  projectId: string | undefined,
  entityType: SyncQueueItem['entityType'],
  entityId: string,
  action: SyncQueueItem['action'],
  payload?: object
): Promise<void> {
  await db.syncQueue.add({
    id: crypto.randomUUID(),
    scopeType,
    scopeId,
    projectId,
    entityType,
    entityId,
    action,
    payload: payload ?? null,
    status: 'pending',
    attempts: 0,
    createdAt: Date.now()
  });
}

/**
 * Queue all entities of a project for initial sync
 */
export async function queueProjectForSync(projectId: string): Promise<void> {
  const project = await db.projects.get(projectId);
  if (!project) return;

  // Queue the project itself
  await queueChange('project', projectId, projectId, 'projects', projectId, 'create', {
    name: project.name,
    description: project.description
  });

  // Queue all properties
  const properties = await db.properties.where('projectId').equals(projectId).toArray();
  for (const property of properties) {
    await queueChange('project', projectId, projectId, 'properties', property.id, 'create', {
      name: property.name,
      address: property.address,
      description: property.description,
      propertyType: property.propertyType ?? 'other'
    });

    // Queue all units for this property
    const units = await db.units.where('propertyId').equals(property.id).toArray();
    for (const unit of units) {
      await queueChange('project', projectId, projectId, 'units', unit.id, 'create', {
        propertyId: unit.propertyId,
        parentUnitId: unit.parentUnitId,
        name: unit.name,
        unitType: unit.unitType,
        description: unit.description
      });
    }

    // Queue all zaznamy for this property
    const zaznamy = await db.zaznamy.where('propertyId').equals(property.id).toArray();
    for (const zaznam of zaznamy) {
      await queueChange('project', projectId, projectId, 'zaznamy', zaznam.id, 'create', {
        propertyId: zaznam.propertyId,
        unitId: zaznam.unitId,
        title: zaznam.title,
        description: zaznam.description,
        date: zaznam.date,
        cost: zaznam.cost,
        status: zaznam.status
      });
    }
  }
}

/**
 * Queue all entities of a property for initial sync
 */
export async function queuePropertyForSync(propertyId: string): Promise<void> {
  const property = await db.properties.get(propertyId);
  if (!property) return;

  const projectId = property.projectId;

  await queueChange('property', propertyId, projectId, 'properties', propertyId, 'create', {
    name: property.name,
    address: property.address,
    description: property.description,
    propertyType: property.propertyType ?? 'other'
  });

  const units = await db.units.where('propertyId').equals(propertyId).toArray();
  for (const unit of units) {
    await queueChange('property', propertyId, projectId, 'units', unit.id, 'create', {
      propertyId: unit.propertyId,
      parentUnitId: unit.parentUnitId,
      name: unit.name,
      unitType: unit.unitType,
      description: unit.description
    });
  }

  const zaznamy = await db.zaznamy.where('propertyId').equals(propertyId).toArray();
  for (const zaznam of zaznamy) {
    await queueChange('property', propertyId, projectId, 'zaznamy', zaznam.id, 'create', {
      propertyId: zaznam.propertyId,
      unitId: zaznam.unitId,
      title: zaznam.title,
      description: zaznam.description,
      date: zaznam.date,
      cost: zaznam.cost,
      status: zaznam.status
    });
  }
}

/**
 * Queue a zaznam and its media for initial sync
 */
export async function queueZaznamForSync(zaznamId: string): Promise<void> {
  const zaznam = await db.zaznamy.get(zaznamId);
  if (!zaznam) return;

  const property = await db.properties.get(zaznam.propertyId);
  const projectId = property?.projectId;

  await queueChange('zaznam', zaznamId, projectId, 'zaznamy', zaznamId, 'create', {
    propertyId: zaznam.propertyId,
    unitId: zaznam.unitId,
    title: zaznam.title,
    description: zaznam.description,
    date: zaznam.date,
    cost: zaznam.cost,
    status: zaznam.status
  });

  const mediaItems = await db.media
    .where('ownerType')
    .equals('zaznam')
    .and(item => item.ownerId === zaznamId)
    .toArray();
  for (const media of mediaItems) {
    await queueChange('zaznam', zaznamId, projectId, 'media', media.id, 'create', {
      zaznamId: media.ownerId,
      type: media.mediaType,
      storageKey: media.storageKey,
      originalFileName: media.originalFileName,
      mimeType: media.mimeType,
      sizeBytes: media.sizeBytes,
      description: media.caption
    });
  }
}

/**
 * Get pending changes count for a project (or all if no projectId)
 */
export async function getPendingChangesCount(
  scope?: { scopeType: SyncQueueItem['scopeType']; scopeId: string }
): Promise<number> {
  if (scope) {
    return db.syncQueue
      .where('scopeType')
      .equals(scope.scopeType)
      .and(item => item.scopeId === scope.scopeId && item.status === 'pending')
      .count();
  }
  return db.syncQueue.where('status').equals('pending').count();
}

/**
 * Get all pending changes for a project
 */
export async function getPendingChanges(
  scopeType: SyncQueueItem['scopeType'],
  scopeId: string
): Promise<SyncQueueItem[]> {
  return db.syncQueue
    .where('scopeType')
    .equals(scopeType)
    .and(item => item.scopeId === scopeId && item.status === 'pending')
    .toArray();
}
