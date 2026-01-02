import { db, queueChange, type Zaznam } from '$lib/db';
import type {
  ZaznamDto,
  ZaznamDetailDto,
  CreateZaznamRequest,
  UpdateZaznamRequest,
  ZaznamQueryParams,
  PaginatedResponse
} from '../types';

const LOCAL_USER_ID = 'local-user';

async function getProjectIdForProperty(propertyId: string): Promise<string | null> {
  const property = await db.properties.get(propertyId);
  return property?.projectId ?? null;
}

async function isPropertySynced(propertyId: string): Promise<boolean> {
  const property = await db.properties.get(propertyId);
  if (!property) return false;
  const project = await db.projects.get(property.projectId);
  return project?.syncMode === 'synced';
}

async function zaznamToDto(zaznam: Zaznam): Promise<ZaznamDto> {
  const property = await db.properties.get(zaznam.propertyId);
  const unit = zaznam.unitId ? await db.units.get(zaznam.unitId) : null;
  const documentCount = await db.dokumenty.where('zaznamId').equals(zaznam.id).count();
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

    const documents = await db.dokumenty.where('zaznamId').equals(id).toArray();

    return {
      ...(await zaznamToDto(zaznam)),
      documents: documents.map(d => ({
        id: d.id,
        zaznamId: d.zaznamId,
        type: 'document' as const,
        storageKey: '',
        originalFileName: d.fileName,
        mimeType: d.mimeType,
        sizeBytes: d.size,
        createdAt: new Date(d.updatedAt).toISOString()
      })),
      comments: []
    };
  },

  async create(data: CreateZaznamRequest): Promise<ZaznamDto> {
    const id = crypto.randomUUID();
    const now = Date.now();
    const property = await db.properties.get(data.propertyId);
    const unit = data.unitId ? await db.units.get(data.unitId) : null;
    const synced = await isPropertySynced(data.propertyId);
    const projectId = await getProjectIdForProperty(data.propertyId);

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
      syncStatus: synced ? 'pending' : 'local'
    };

    await db.zaznamy.add(zaznam);

    // Queue change if project is synced
    if (synced && projectId) {
      await queueChange(projectId, 'zaznamy', id, 'create', {
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
    const synced = await isPropertySynced(zaznam.propertyId);
    const projectId = await getProjectIdForProperty(zaznam.propertyId);

    const updated: Partial<Zaznam> = {
      updatedAt: now,
      syncStatus: synced ? 'pending' : 'local'
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

    // Queue change if project is synced
    if (synced && projectId) {
      await queueChange(projectId, 'zaznamy', id, 'update', data);
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
    const synced = await isPropertySynced(zaznam.propertyId);
    const projectId = await getProjectIdForProperty(zaznam.propertyId);

    // Queue delete if project is synced
    if (synced && projectId) {
      await queueChange(projectId, 'zaznamy', id, 'delete');
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
    await db.dokumenty.where('zaznamId').equals(id).delete();
    await db.zaznamTags.where('zaznamId').equals(id).delete();
    await db.zaznamy.delete(id);
  },

  async complete(id: string): Promise<ZaznamDto> {
    const zaznam = await db.zaznamy.get(id);
    if (!zaznam) {
      throw new Error('Zaznam nenalezen');
    }

    const synced = await isPropertySynced(zaznam.propertyId);
    const projectId = await getProjectIdForProperty(zaznam.propertyId);

    await db.zaznamy.update(id, {
      status: 'complete',
      updatedAt: Date.now(),
      syncStatus: synced ? 'pending' : 'local'
    });

    // Queue change if project is synced
    if (synced && projectId) {
      await queueChange(projectId, 'zaznamy', id, 'update', { status: 'complete' });
    }

    const result = await db.zaznamy.get(id);
    return zaznamToDto(result!);
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
