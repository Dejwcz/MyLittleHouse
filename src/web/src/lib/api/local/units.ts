import { db, queueChange, type Unit, type Property } from '$lib/db';
import type {
  UnitDto,
  CreateUnitRequest,
  UpdateUnitRequest,
  UnitQueryParams
} from '../types';

async function getPropertySyncScope(property: Property): Promise<{
  scopeType: 'project' | 'property';
  scopeId: string;
  projectId?: string;
} | null> {
  if (property.syncMode === 'synced') {
    return { scopeType: 'property', scopeId: property.id, projectId: property.projectId };
  }

  const project = await db.projects.get(property.projectId);
  if (project?.syncMode === 'synced') {
    return { scopeType: 'project', scopeId: project.id, projectId: project.id };
  }

  return null;
}

async function unitToDto(unit: Unit): Promise<UnitDto> {
  let coverUrl = unit.coverUrl;

  if (unit.coverMediaId) {
    const coverMedia = await db.media.get(unit.coverMediaId);
    if (coverMedia?.data && typeof URL !== 'undefined') {
      coverUrl = URL.createObjectURL(coverMedia.data);
    } else if (coverMedia?.thumbnailUrl || coverMedia?.url) {
      coverUrl = coverMedia.thumbnailUrl ?? coverMedia.url;
    }
  }

  return {
    id: unit.id,
    propertyId: unit.propertyId,
    parentUnitId: unit.parentUnitId,
    name: unit.name,
    description: unit.description,
    unitType: unit.unitType as UnitDto['unitType'],
    childCount: unit.childUnitCount,
    zaznamCount: unit.zaznamCount,
    coverMediaId: unit.coverMediaId,
    coverUrl,
    createdAt: new Date(unit.updatedAt).toISOString(),
    updatedAt: new Date(unit.updatedAt).toISOString()
  };
}

export interface UnitListResponse {
  items: UnitDto[];
  total: number;
}

export const localUnitsApi = {
  async list(params: UnitQueryParams): Promise<UnitListResponse> {
    let units: Unit[];

    if (params.parentUnitId) {
      units = await db.units
        .where('propertyId').equals(params.propertyId)
        .and(u => u.parentUnitId === params.parentUnitId)
        .toArray();
    } else {
      units = await db.units
        .where('propertyId').equals(params.propertyId)
        .toArray();
    }

    const items = await Promise.all(units.map(unitToDto));
    return { items, total: units.length };
  },

  async get(id: string): Promise<UnitDto> {
    const unit = await db.units.get(id);
    if (!unit) {
      throw new Error('Jednotka nenalezena');
    }
    return await unitToDto(unit);
  },

  async create(data: CreateUnitRequest): Promise<UnitDto> {
    const id = crypto.randomUUID();
    const now = Date.now();

    const property = await db.properties.get(data.propertyId);
    if (!property) {
      throw new Error('Nemovitost nenalezena');
    }

    const unit: Unit = {
      id,
      propertyId: data.propertyId,
      parentUnitId: data.parentUnitId,
      name: data.name,
      description: data.description,
      unitType: data.unitType,
      childUnitCount: 0,
      zaznamCount: 0,
      updatedAt: now,
      syncStatus: 'local'
    };

    await db.units.add(unit);

    // Update property unit count
    await db.properties.update(data.propertyId, {
      unitCount: property.unitCount + 1,
      updatedAt: now
    });

    // Update parent unit child count
    if (data.parentUnitId) {
      const parent = await db.units.get(data.parentUnitId);
      if (parent) {
        await db.units.update(data.parentUnitId, {
          childUnitCount: parent.childUnitCount + 1,
          updatedAt: now
        });
      }
    }

    // Queue for sync if property is synced
    const scope = await getPropertySyncScope(property);
    if (scope) {
      await db.units.update(id, { syncStatus: 'pending' });
      await queueChange(scope.scopeType, scope.scopeId, scope.projectId, 'units', id, 'create', {
        propertyId: data.propertyId,
        parentUnitId: data.parentUnitId,
        name: data.name,
        description: data.description,
        unitType: data.unitType
      });
    }

    return await unitToDto(unit);
  },

  async update(id: string, data: UpdateUnitRequest): Promise<UnitDto> {
    const unit = await db.units.get(id);
    if (!unit) {
      throw new Error('Jednotka nenalezena');
    }

    const property = await db.properties.get(unit.propertyId);
    if (!property) {
      throw new Error('Nemovitost nenalezena');
    }

    const now = Date.now();
    const oldParentId = unit.parentUnitId;
    const newParentId = data.parentUnitId;

    const updated: Partial<Unit> = {
      updatedAt: now
    };

    if (data.name !== undefined) updated.name = data.name;
    if (data.description !== undefined) updated.description = data.description;
    if (data.unitType !== undefined) updated.unitType = data.unitType;
    if (data.parentUnitId !== undefined) updated.parentUnitId = data.parentUnitId || undefined;

    // Update parent child counts if parent changed
    if (newParentId !== oldParentId) {
      if (oldParentId) {
        const oldParent = await db.units.get(oldParentId);
        if (oldParent) {
          await db.units.update(oldParentId, {
            childUnitCount: Math.max(0, oldParent.childUnitCount - 1),
            updatedAt: now
          });
        }
      }
      if (newParentId) {
        const newParent = await db.units.get(newParentId);
        if (newParent) {
          await db.units.update(newParentId, {
            childUnitCount: newParent.childUnitCount + 1,
            updatedAt: now
          });
        }
      }
    }

    const scope = await getPropertySyncScope(property);
    if (scope) {
      updated.syncStatus = 'pending';
      await queueChange(scope.scopeType, scope.scopeId, scope.projectId, 'units', id, 'update', data);
    }

    await db.units.update(id, updated);
    const result = await db.units.get(id);
    return await unitToDto(result!);
  },

  async delete(id: string): Promise<void> {
    const unit = await db.units.get(id);
    if (!unit) return;

    const property = await db.properties.get(unit.propertyId);

    // Queue for sync if needed
    if (property) {
      const scope = await getPropertySyncScope(property);
      if (scope) {
        await queueChange(scope.scopeType, scope.scopeId, scope.projectId, 'units', id, 'delete');
      }
    }

    const now = Date.now();

    // Update parent child count
    if (unit.parentUnitId) {
      const parent = await db.units.get(unit.parentUnitId);
      if (parent) {
        await db.units.update(unit.parentUnitId, {
          childUnitCount: Math.max(0, parent.childUnitCount - 1),
          updatedAt: now
        });
      }
    }

    // Move child units to parent (or root)
    const children = await db.units.where('parentUnitId').equals(id).toArray();
    for (const child of children) {
      await db.units.update(child.id, {
        parentUnitId: unit.parentUnitId,
        updatedAt: now
      });
    }

    // Update zaznamy - set unitId to undefined
    await db.zaznamy.where('unitId').equals(id).modify({ unitId: undefined, updatedAt: now });

    // Update property unit count
    if (property) {
      await db.properties.update(unit.propertyId, {
        unitCount: Math.max(0, property.unitCount - 1),
        updatedAt: now
      });
    }

    await db.media
      .where('ownerType')
      .equals('unit')
      .and(media => media.ownerId === id)
      .delete();

    await db.units.delete(id);
  },
  async updateCover(id: string, coverMediaId?: string): Promise<UnitDto> {
    const unit = await db.units.get(id);
    if (!unit) {
      throw new Error('Jednotka nenalezena');
    }

    const property = await db.properties.get(unit.propertyId);

    let coverUrl: string | undefined;
    if (coverMediaId) {
      const media = await db.media.get(coverMediaId);
      coverUrl = media?.thumbnailUrl ?? media?.url;
    }

    const updated: Partial<Unit> = {
      coverMediaId: coverMediaId || undefined,
      coverUrl,
      updatedAt: Date.now()
    };

    if (property) {
      const scope = await getPropertySyncScope(property);
      if (scope) {
        updated.syncStatus = 'pending';
        await queueChange(scope.scopeType, scope.scopeId, scope.projectId, 'units', id, 'update', {
          coverMediaId: coverMediaId ?? null
        });
      }
    } else {
      updated.syncStatus = 'local';
    }

    await db.units.update(id, updated);

    const result = await db.units.get(id);
    return await unitToDto(result!);
  }
};
