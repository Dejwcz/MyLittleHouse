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
      await queueChange(id, 'projects', id, 'update', data);
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
      await queueChange(id, 'projects', id, 'delete');
    }

    // Delete all related data
    const properties = await db.properties.where('projectId').equals(id).toArray();
    const propertyIds = properties.map(p => p.id);

    // Delete zaznamy for all properties
    for (const propertyId of propertyIds) {
      const zaznamy = await db.zaznamy.where('propertyId').equals(propertyId).toArray();
      const zaznamIds = zaznamy.map(z => z.id);

      // Delete documents
      for (const zaznamId of zaznamIds) {
        await db.dokumenty.where('zaznamId').equals(zaznamId).delete();
        await db.zaznamTags.where('zaznamId').equals(zaznamId).delete();
      }

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

    await db.projects.update(id, {
      syncMode: mode,
      syncStatus: mode === 'synced' ? 'pending' : 'local',
      updatedAt: Date.now()
    });

    if (mode === 'synced') {
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
