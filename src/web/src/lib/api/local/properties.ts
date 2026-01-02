import { db, queueChange, type Property } from '$lib/db';
import type {
  PropertyDto,
  PropertyDetailDto,
  CreatePropertyRequest,
  UpdatePropertyRequest,
  PropertyStatsDto,
  PropertyQueryParams
} from '../types';

const LOCAL_USER_ID = 'local-user';

async function isProjectSynced(projectId: string): Promise<boolean> {
  const project = await db.projects.get(projectId);
  return project?.syncMode === 'synced';
}

async function propertyToDto(property: Property): Promise<PropertyDto> {
  const project = await db.projects.get(property.projectId);

  return {
    id: property.id,
    projectId: property.projectId,
    projectName: project?.name ?? '',
    name: property.name,
    description: property.description,
    geoRadius: 0,
    unitCount: property.unitCount,
    zaznamCount: property.zaznamCount,
    totalCost: property.totalCost,
    myRole: 'owner',
    isShared: false,
    createdAt: new Date(property.updatedAt).toISOString(),
    updatedAt: new Date(property.updatedAt).toISOString()
  };
}

export const localPropertiesApi = {
  async list(params: PropertyQueryParams = {}): Promise<{ items: PropertyDto[]; total: number }> {
    let properties: Property[];

    if (params.projectId) {
      properties = await db.properties.where('projectId').equals(params.projectId).toArray();
    } else {
      properties = await db.properties.toArray();
    }

    const items = await Promise.all(properties.map(propertyToDto));

    return {
      items,
      total: properties.length
    };
  },

  async get(id: string): Promise<PropertyDetailDto> {
    const property = await db.properties.get(id);
    if (!property) {
      throw new Error('Nemovitost nenalezena');
    }

    const project = await db.projects.get(property.projectId);
    const units = await db.units.where('propertyId').equals(id).toArray();

    return {
      ...(await propertyToDto(property)),
      units: units.map(u => ({
        id: u.id,
        propertyId: u.propertyId,
        parentUnitId: u.parentUnitId,
        name: u.name,
        description: u.description,
        unitType: u.unitType as 'flat' | 'house' | 'garage' | 'garden' | 'room' | 'stairs' | 'other',
        childCount: u.childUnitCount,
        zaznamCount: u.zaznamCount,
        createdAt: new Date(u.updatedAt).toISOString(),
        updatedAt: new Date(u.updatedAt).toISOString()
      })),
      members: [{
        userId: LOCAL_USER_ID,
        email: '',
        displayName: 'Host',
        role: 'owner' as const,
        status: 'active' as const
      }]
    };
  },

  async create(data: CreatePropertyRequest): Promise<PropertyDto> {
    const id = crypto.randomUUID();
    const now = Date.now();
    const synced = await isProjectSynced(data.projectId);

    const property: Property = {
      id,
      projectId: data.projectId,
      name: data.name,
      description: data.description,
      unitCount: 0,
      zaznamCount: 0,
      totalCost: 0,
      updatedAt: now,
      syncStatus: synced ? 'pending' : 'local'
    };

    await db.properties.add(property);

    // Queue change if project is synced
    if (synced) {
      await queueChange(data.projectId, 'properties', id, 'create', {
        name: data.name,
        description: data.description
      });
    }

    // Update project property count
    const project = await db.projects.get(data.projectId);
    if (project) {
      await db.projects.update(data.projectId, {
        propertyCount: project.propertyCount + 1,
        updatedAt: now
      });
    }

    return propertyToDto(property);
  },

  async update(id: string, data: UpdatePropertyRequest): Promise<PropertyDto> {
    const property = await db.properties.get(id);
    if (!property) {
      throw new Error('Nemovitost nenalezena');
    }

    const synced = await isProjectSynced(property.projectId);
    const updated: Partial<Property> = {
      updatedAt: Date.now(),
      syncStatus: synced ? 'pending' : 'local'
    };

    if (data.name !== undefined) updated.name = data.name;
    if (data.description !== undefined) updated.description = data.description;

    // Queue change if project is synced
    if (synced) {
      await queueChange(property.projectId, 'properties', id, 'update', data);
    }

    await db.properties.update(id, updated);
    const result = await db.properties.get(id);
    return propertyToDto(result!);
  },

  async delete(id: string): Promise<void> {
    const property = await db.properties.get(id);
    if (!property) return;

    const synced = await isProjectSynced(property.projectId);

    // Queue delete if project is synced
    if (synced) {
      await queueChange(property.projectId, 'properties', id, 'delete');
    }

    // Delete all related data
    const zaznamy = await db.zaznamy.where('propertyId').equals(id).toArray();
    const zaznamIds = zaznamy.map(z => z.id);

    // Delete documents and tags
    for (const zaznamId of zaznamIds) {
      await db.dokumenty.where('zaznamId').equals(zaznamId).delete();
      await db.zaznamTags.where('zaznamId').equals(zaznamId).delete();
    }

    await db.zaznamy.where('propertyId').equals(id).delete();
    await db.units.where('propertyId').equals(id).delete();
    await db.properties.delete(id);

    // Update project property count
    const project = await db.projects.get(property.projectId);
    if (project) {
      await db.projects.update(property.projectId, {
        propertyCount: Math.max(0, project.propertyCount - 1),
        updatedAt: Date.now()
      });
    }
  },

  async getStats(id: string): Promise<PropertyStatsDto> {
    const zaznamy = await db.zaznamy.where('propertyId').equals(id).toArray();
    const documents = await Promise.all(
      zaznamy.map(z => db.dokumenty.where('zaznamId').equals(z.id).count())
    );

    const totalCost = zaznamy.reduce((sum, z) => sum + (z.cost ?? 0), 0);
    const draftCount = zaznamy.filter(z => z.status === 'draft').length;
    const documentCount = documents.reduce((sum, c) => sum + c, 0);

    // Group by month and year
    const costByMonth: Record<string, number> = {};
    const costByYear: Record<number, number> = {};

    zaznamy.forEach(z => {
      if (z.cost) {
        const date = new Date(z.date);
        const monthKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
        const year = date.getFullYear();

        costByMonth[monthKey] = (costByMonth[monthKey] ?? 0) + z.cost;
        costByYear[year] = (costByYear[year] ?? 0) + z.cost;
      }
    });

    return {
      totalCost,
      zaznamCount: zaznamy.length,
      draftCount,
      documentCount,
      costByMonth: Object.entries(costByMonth).map(([month, cost]) => ({ month, cost })),
      costByYear: Object.entries(costByYear).map(([year, cost]) => ({ year: parseInt(year), cost }))
    };
  }
};
