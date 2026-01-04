import { db, queueChange, queueZaznamForSync, type Zaznam, type SyncMode } from '$lib/db';
import type {
  ZaznamDto,
  ZaznamDetailDto,
  CreateZaznamRequest,
  UpdateZaznamRequest,
  ZaznamQueryParams,
  PaginatedResponse
} from '../types';

const LOCAL_USER_ID = 'local-user';

async function getDefaultZaznamSyncMode(propertyId: string): Promise<SyncMode> {
  const property = await db.properties.get(propertyId);
  if (!property) return 'local-only';
  return property.syncMode === 'synced' ? 'synced' : 'local-only';
}

async function getZaznamSyncScope(zaznam: Zaznam): Promise<{
  scopeType: 'project' | 'property' | 'zaznam';
  scopeId: string;
  projectId?: string;
} | null> {
  if (zaznam.syncMode === 'synced') {
    const property = await db.properties.get(zaznam.propertyId);
    return {
      scopeType: 'zaznam',
      scopeId: zaznam.id,
      projectId: property?.projectId
    };
  }

  const property = await db.properties.get(zaznam.propertyId);
  if (property?.syncMode === 'synced') {
    return {
      scopeType: 'property',
      scopeId: property.id,
      projectId: property.projectId
    };
  }

  if (property) {
    const project = await db.projects.get(property.projectId);
    if (project?.syncMode === 'synced') {
      return {
        scopeType: 'project',
        scopeId: project.id,
        projectId: project.id
      };
    }
  }

  return null;
}

async function zaznamToDto(zaznam: Zaznam): Promise<ZaznamDto> {
  const property = await db.properties.get(zaznam.propertyId);
  const unit = zaznam.unitId ? await db.units.get(zaznam.unitId) : null;
  const documentCount = await db.media
    .where('ownerType')
    .equals('zaznam')
    .and(media => media.ownerId === zaznam.id)
    .count();
  const thumbnailMedia = await db.media
    .where('ownerType')
    .equals('zaznam')
    .and(media => media.ownerId === zaznam.id)
    .first();
  let thumbnailUrl: string | undefined;
  if (thumbnailMedia?.data && typeof URL !== 'undefined') {
    thumbnailUrl = URL.createObjectURL(thumbnailMedia.data);
  } else {
    thumbnailUrl = thumbnailMedia?.thumbnailUrl ?? thumbnailMedia?.url;
  }
  const tags = await db.zaznamTags.where('zaznamId').equals(zaznam.id).toArray();
  const tagNames = await Promise.all(
    tags.map(async t => {
      const tag = await db.tags.get(t.tagId);
      return tag?.name ?? '';
    })
  );

  return {
    id: zaznam.id,
    propertyId: zaznam.propertyId,
    propertyName: property?.name ?? zaznam.propertyName ?? '',
    unitId: zaznam.unitId,
    unitName: unit?.name ?? zaznam.unitName,
    title: zaznam.title,
    description: zaznam.description,
    date: zaznam.date,
    cost: zaznam.cost,
    status: zaznam.status,
    flags: [],
    tags: tagNames.filter(Boolean),
    documentCount,
    commentCount: 0,
    thumbnailUrl,
    syncMode: zaznam.syncMode,
    syncStatus: zaznam.syncStatus,
    createdAt: new Date(zaznam.updatedAt).toISOString(),
    updatedAt: new Date(zaznam.updatedAt).toISOString(),
    createdBy: { id: LOCAL_USER_ID, name: 'Host' }
  };
}

export const localZaznamyApi = {
  async list(params: ZaznamQueryParams = {}): Promise<PaginatedResponse<ZaznamDto>> {
    let query = db.zaznamy.toCollection();

    // Apply filters
    if (params.propertyId) {
      query = db.zaznamy.where('propertyId').equals(params.propertyId);
    }
    if (params.unitId) {
      query = db.zaznamy.where('unitId').equals(params.unitId);
    }
    if (params.status) {
      query = db.zaznamy.where('status').equals(params.status);
    }

    let zaznamy = await query.toArray();

    // Apply date filters
    if (params.from) {
      const from = new Date(params.from);
      zaznamy = zaznamy.filter(z => new Date(z.date) >= from);
    }
    if (params.to) {
      const to = new Date(params.to);
      zaznamy = zaznamy.filter(z => new Date(z.date) <= to);
    }

    // Apply search filter
    if (params.search) {
      const search = params.search.toLowerCase();
      zaznamy = zaznamy.filter(z =>
        z.title?.toLowerCase().includes(search) ||
        z.description?.toLowerCase().includes(search)
      );
    }

    // Sort
    const sortField = params.sort ?? 'date';
    const sortOrder = params.order ?? 'desc';
    zaznamy.sort((a, b) => {
      let aVal: string | number | undefined;
      let bVal: string | number | undefined;

      if (sortField === 'date') {
        aVal = a.date;
        bVal = b.date;
      } else if (sortField === 'cost') {
        aVal = a.cost ?? 0;
        bVal = b.cost ?? 0;
      } else {
        aVal = a.updatedAt;
        bVal = b.updatedAt;
      }

      if (aVal === undefined) aVal = '';
      if (bVal === undefined) bVal = '';

      if (aVal < bVal) return sortOrder === 'asc' ? -1 : 1;
      if (aVal > bVal) return sortOrder === 'asc' ? 1 : -1;
      return 0;
    });

    const total = zaznamy.length;
    const page = params.page ?? 1;
    const pageSize = params.pageSize ?? 20;
    const start = (page - 1) * pageSize;
    const end = start + pageSize;

    const paged = zaznamy.slice(start, end);
    const items = await Promise.all(paged.map(zaznamToDto));

    return { items, total, page, pageSize };
  },

  async get(id: string): Promise<ZaznamDetailDto> {
    const zaznam = await db.zaznamy.get(id);
    if (!zaznam) {
      throw new Error('Zaznam nenalezen');
    }

    const documents = await db.media
      .where('ownerType')
      .equals('zaznam')
      .and(media => media.ownerId === id)
      .toArray();

    const documentDtos = documents.map(d => ({
      id: d.id,
      zaznamId: d.ownerId,
      type: d.mediaType,
      storageKey: d.storageKey ?? '',
      originalFileName: d.originalFileName,
      mimeType: d.mimeType,
      sizeBytes: d.sizeBytes,
      caption: d.caption,
      thumbnailUrl: d.data && typeof URL !== 'undefined'
        ? URL.createObjectURL(d.data)
        : d.thumbnailUrl,
      createdAt: new Date(d.updatedAt).toISOString()
    }));

    return {
      ...(await zaznamToDto(zaznam)),
      documents: documentDtos,
      comments: []
    };
  },

  async create(data: CreateZaznamRequest): Promise<ZaznamDto> {
    const id = crypto.randomUUID();
    const now = Date.now();
    const property = await db.properties.get(data.propertyId);
    const unit = data.unitId ? await db.units.get(data.unitId) : null;
    const syncMode = await getDefaultZaznamSyncMode(data.propertyId);

    const zaznam: Zaznam = {
      id,
      propertyId: data.propertyId,
      unitId: data.unitId,
      title: data.title ?? '',
      description: data.description,
      date: data.date ?? new Date().toISOString().split('T')[0],
      cost: data.cost,
      status: data.status ?? 'draft',
      propertyName: property?.name,
      unitName: unit?.name,
      updatedAt: now,
      syncStatus: syncMode === 'synced' ? 'pending' : 'local',
      syncMode
    };

    await db.zaznamy.add(zaznam);

    const scope = await getZaznamSyncScope(zaznam);
    if (scope) {
      await queueChange(scope.scopeType, scope.scopeId, scope.projectId, 'zaznamy', id, 'create', {
        propertyId: data.propertyId,
        unitId: data.unitId,
        title: zaznam.title,
        description: zaznam.description,
        date: zaznam.date,
        cost: zaznam.cost,
        status: zaznam.status
      });
    }

    // Update property zaznam count and total cost
    if (property) {
      await db.properties.update(data.propertyId, {
        zaznamCount: property.zaznamCount + 1,
        totalCost: property.totalCost + (data.cost ?? 0),
        updatedAt: now
      });
    }

    // Update unit zaznam count
    if (unit) {
      await db.units.update(data.unitId!, {
        zaznamCount: unit.zaznamCount + 1,
        updatedAt: now
      });
    }

    // Handle tags
    if (data.tags?.length) {
      for (const tagName of data.tags) {
        let tag = await db.tags.where('name').equals(tagName).first();
        if (!tag) {
          const tagId = crypto.randomUUID();
          await db.tags.add({ id: tagId, name: tagName });
          tag = { id: tagId, name: tagName };
        }
        await db.zaznamTags.add({ zaznamId: id, tagId: tag.id });
      }
    }

    return zaznamToDto(zaznam);
  },

  async update(id: string, data: UpdateZaznamRequest): Promise<ZaznamDto> {
    const zaznam = await db.zaznamy.get(id);
    if (!zaznam) {
      throw new Error('Zaznam nenalezen');
    }

    const now = Date.now();
    const oldCost = zaznam.cost ?? 0;
    const newCost = data.cost ?? oldCost;
    const costDiff = newCost - oldCost;
    const updated: Partial<Zaznam> = {
      updatedAt: now
    };

    if (data.title !== undefined) updated.title = data.title;
    if (data.description !== undefined) updated.description = data.description;
    if (data.date !== undefined) updated.date = data.date;
    if (data.cost !== undefined) updated.cost = data.cost;
    if (data.unitId !== undefined) {
      updated.unitId = data.unitId;
      if (data.unitId) {
        const unit = await db.units.get(data.unitId);
        updated.unitName = unit?.name;
      } else {
        updated.unitName = undefined;
      }
    }

    await db.zaznamy.update(id, updated);

    const scope = await getZaznamSyncScope(zaznam);
    if (scope) {
      updated.syncStatus = 'pending';
      await queueChange(scope.scopeType, scope.scopeId, scope.projectId, 'zaznamy', id, 'update', data);
    } else {
      updated.syncStatus = 'local';
    }

    // Update property total cost
    if (costDiff !== 0) {
      const property = await db.properties.get(zaznam.propertyId);
      if (property) {
        await db.properties.update(zaznam.propertyId, {
          totalCost: property.totalCost + costDiff,
          updatedAt: now
        });
      }
    }

    // Handle tags update
    if (data.tags) {
      await db.zaznamTags.where('zaznamId').equals(id).delete();
      for (const tagName of data.tags) {
        let tag = await db.tags.where('name').equals(tagName).first();
        if (!tag) {
          const tagId = crypto.randomUUID();
          await db.tags.add({ id: tagId, name: tagName });
          tag = { id: tagId, name: tagName };
        }
        await db.zaznamTags.add({ zaznamId: id, tagId: tag.id });
      }
    }

    const result = await db.zaznamy.get(id);
    return zaznamToDto(result!);
  },

  async delete(id: string): Promise<void> {
    const zaznam = await db.zaznamy.get(id);
    if (!zaznam) return;

    const now = Date.now();
    const scope = await getZaznamSyncScope(zaznam);
    if (scope) {
      await queueChange(scope.scopeType, scope.scopeId, scope.projectId, 'zaznamy', id, 'delete');
    }

    // Update property stats
    const property = await db.properties.get(zaznam.propertyId);
    if (property) {
      await db.properties.update(zaznam.propertyId, {
        zaznamCount: Math.max(0, property.zaznamCount - 1),
        totalCost: Math.max(0, property.totalCost - (zaznam.cost ?? 0)),
        updatedAt: now
      });
    }

    // Update unit stats
    if (zaznam.unitId) {
      const unit = await db.units.get(zaznam.unitId);
      if (unit) {
        await db.units.update(zaznam.unitId, {
          zaznamCount: Math.max(0, unit.zaznamCount - 1),
          updatedAt: now
        });
      }
    }

    // Delete related data
    await db.media
      .where('ownerType')
      .equals('zaznam')
      .and(media => media.ownerId === id)
      .delete();
    await db.zaznamTags.where('zaznamId').equals(id).delete();
    await db.zaznamy.delete(id);
  },

  async complete(id: string): Promise<ZaznamDto> {
    const zaznam = await db.zaznamy.get(id);
    if (!zaznam) {
      throw new Error('Zaznam nenalezen');
    }

    const scope = await getZaznamSyncScope(zaznam);

    await db.zaznamy.update(id, {
      status: 'complete',
      updatedAt: Date.now(),
      syncStatus: scope ? 'pending' : 'local'
    });

    if (scope) {
      await queueChange(scope.scopeType, scope.scopeId, scope.projectId, 'zaznamy', id, 'update', { status: 'complete' });
    }

    const result = await db.zaznamy.get(id);
    return zaznamToDto(result!);
  },

  async setSyncMode(id: string, mode: SyncMode): Promise<void> {
    const zaznam = await db.zaznamy.get(id);
    if (!zaznam) {
      throw new Error('Zaznam nenalezen');
    }

    await db.zaznamy.update(id, {
      syncMode: mode,
      syncStatus: mode === 'synced' ? 'pending' : 'local',
      updatedAt: Date.now()
    });

    if (mode === 'synced') {
      await queueZaznamForSync(id);
    } else {
      await db.syncQueue.where('scopeType').equals('zaznam').and(item => item.scopeId === id).delete();
    }
  },

  async getDrafts(): Promise<PaginatedResponse<ZaznamDto>> {
    const zaznamy = await db.zaznamy.where('status').equals('draft').toArray();
    const items = await Promise.all(zaznamy.map(zaznamToDto));

    return {
      items,
      total: zaznamy.length,
      page: 1,
      pageSize: zaznamy.length
    };
  }
};
