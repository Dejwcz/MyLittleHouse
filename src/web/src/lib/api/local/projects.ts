import { db, queueChange, queueProjectForSync, type Project, type SyncMode } from '$lib/db';
import type {
  ProjectDto,
  ProjectDetailDto,
  CreateProjectRequest,
  UpdateProjectRequest
} from '../types';

const LOCAL_USER_ID = 'local-user';

// Extended DTO with sync fields
export interface ProjectDtoWithSync extends ProjectDto {
  syncMode: SyncMode;
  syncStatus: string;
  lastSyncAt?: string;
}

function projectToDto(project: Project): ProjectDtoWithSync {
  return {
    id: project.id,
    name: project.name,
    description: project.description,
    propertyCount: project.propertyCount,
    memberCount: project.memberCount,
    myRole: project.myRole,
    createdAt: new Date(project.updatedAt).toISOString(),
    updatedAt: new Date(project.updatedAt).toISOString(),
    // Sync fields
    syncMode: project.syncMode,
    syncStatus: project.syncStatus,
    lastSyncAt: project.lastSyncAt ? new Date(project.lastSyncAt).toISOString() : undefined
  };
}

export const localProjectsApi = {
  async list(): Promise<{ items: ProjectDto[]; total: number }> {
    const projects = await db.projects.toArray();
    return {
      items: projects.map(projectToDto),
      total: projects.length
    };
  },

  async get(id: string): Promise<ProjectDetailDto> {
    const project = await db.projects.get(id);
    if (!project) {
      throw new Error('Projekt nenalezen');
    }

    const properties = await db.properties.where('projectId').equals(id).toArray();

    return {
      ...projectToDto(project),
      properties: properties.map(p => ({
        id: p.id,
        projectId: p.projectId,
        projectName: project.name,
        name: p.name,
        description: p.description,
        geoRadius: 0,
        unitCount: p.unitCount,
        zaznamCount: p.zaznamCount,
        totalCost: p.totalCost,
        myRole: 'owner' as const,
        isShared: false,
        syncMode: p.syncMode,
        syncStatus: p.syncStatus,
        coverMediaId: p.coverMediaId,
        coverUrl: p.coverUrl,
        createdAt: new Date(p.updatedAt).toISOString(),
        updatedAt: new Date(p.updatedAt).toISOString()
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

  async create(data: CreateProjectRequest): Promise<ProjectDto> {
    const id = crypto.randomUUID();
    const now = Date.now();

    const project: Project = {
      id,
      name: data.name,
      description: data.description,
      ownerId: LOCAL_USER_ID,
      memberCount: 1,
      propertyCount: 0,
      myRole: 'owner',
      updatedAt: now,
      syncStatus: 'local',
      syncMode: 'local-only'  // New projects are local-only by default
    };

    await db.projects.add(project);
    return projectToDto(project);
  },

  async update(id: string, data: UpdateProjectRequest): Promise<ProjectDto> {
    const project = await db.projects.get(id);
    if (!project) {
      throw new Error('Projekt nenalezen');
    }

    const updated: Partial<Project> = {
      updatedAt: Date.now()
    };

    if (data.name !== undefined) updated.name = data.name;
    if (data.description !== undefined) updated.description = data.description;

    // If project is synced, queue the change
    if (project.syncMode === 'synced') {
      updated.syncStatus = 'pending';
      await queueChange('project', id, id, 'projects', id, 'update', data);
    } else {
      updated.syncStatus = 'local';
    }

    await db.projects.update(id, updated);
    const result = await db.projects.get(id);
    return projectToDto(result!);
  },

  async delete(id: string): Promise<void> {
    const project = await db.projects.get(id);

    // If project was synced, queue delete
    if (project?.syncMode === 'synced') {
      await queueChange('project', id, id, 'projects', id, 'delete');
    }

    // Delete all related data
    const properties = await db.properties.where('projectId').equals(id).toArray();
    const propertyIds = properties.map(p => p.id);

    // Delete zaznamy for all properties
    for (const propertyId of propertyIds) {
      const units = await db.units.where('propertyId').equals(propertyId).toArray();
      const unitIds = units.map(u => u.id);
      const zaznamy = await db.zaznamy.where('propertyId').equals(propertyId).toArray();
      const zaznamIds = zaznamy.map(z => z.id);

      // Delete media
      for (const zaznamId of zaznamIds) {
        await db.media
          .where('ownerType')
          .equals('zaznam')
          .and(media => media.ownerId === zaznamId)
          .delete();
        await db.zaznamTags.where('zaznamId').equals(zaznamId).delete();
      }

      for (const unitId of unitIds) {
        await db.media
          .where('ownerType')
          .equals('unit')
          .and(media => media.ownerId === unitId)
          .delete();
      }

      await db.media
        .where('ownerType')
        .equals('property')
        .and(media => media.ownerId === propertyId)
        .delete();

      await db.zaznamy.where('propertyId').equals(propertyId).delete();
      await db.units.where('propertyId').equals(propertyId).delete();
    }

    await db.properties.where('projectId').equals(id).delete();

    // Delete pending sync queue items for this project
    await db.syncQueue.where('projectId').equals(id).delete();

    await db.projects.delete(id);
  },

  /**
   * Change sync mode for a project
   */
  async setSyncMode(id: string, mode: SyncMode): Promise<void> {
    const project = await db.projects.get(id);
    if (!project) {
      throw new Error('Projekt nenalezen');
    }

    const now = Date.now();
    await db.projects.update(id, {
      syncMode: mode,
      syncStatus: mode === 'synced' ? 'pending' : 'local',
      updatedAt: now
    });

    if (mode === 'synced') {
      // Ensure child scopes follow project sync
      await db.properties.where('projectId').equals(id).modify({
        syncMode: 'synced',
        syncStatus: 'pending',
        updatedAt: now
      });

      const projectProperties = await db.properties.where('projectId').equals(id).toArray();
      const propertyIds = projectProperties.map(p => p.id);
      if (propertyIds.length > 0) {
        await db.zaznamy.where('propertyId').anyOf(propertyIds).modify({
          syncMode: 'synced',
          syncStatus: 'pending',
          updatedAt: now
        });
      }

      // Queue entire project for initial sync
      await queueProjectForSync(id);
    } else {
      // Remove pending sync items for this project
      await db.syncQueue.where('projectId').equals(id).delete();
    }
  },

  /**
   * Get sync mode for a project
   */
  async getSyncMode(id: string): Promise<SyncMode> {
    const project = await db.projects.get(id);
    if (!project) {
      throw new Error('Projekt nenalezen');
    }
    return project.syncMode;
  }
};
