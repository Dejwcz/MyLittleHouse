/**
 * Unified API Layer
 *
 * This module provides a unified interface for data operations.
 * It automatically switches between local (IndexedDB) and remote (backend API)
 * based on the user's authentication state.
 *
 * - Guest users: All operations go to IndexedDB
 * - Authenticated users: All operations go to backend API
 */

import { auth } from '$lib/stores/auth.svelte';
import { localProjectsApi, localPropertiesApi, localUnitsApi, localZaznamyApi } from './local';
import { projectsApi as remoteProjectsApi, type ProjectListResponse } from './projects';
import { propertiesApi as remotePropertiesApi, type PropertyListResponse } from './properties';
import { unitsApi as remoteUnitsApi, type UnitListResponse } from './units';
import { zaznamyApi as remoteZaznamyApi, type ZaznamListResponse } from './zaznamy';
import type {
  ProjectDto,
  ProjectDetailDto,
  CreateProjectRequest,
  UpdateProjectRequest,
  AddMemberRequest,
  UpdateMemberRequest,
  MemberDto,
  PropertyDto,
  PropertyDetailDto,
  CreatePropertyRequest,
  UpdatePropertyRequest,
  PropertyQueryParams,
  PropertyStatsDto,
  UnitDto,
  CreateUnitRequest,
  UpdateUnitRequest,
  UnitQueryParams,
  ZaznamDto,
  ZaznamDetailDto,
  CreateZaznamRequest,
  UpdateZaznamRequest,
  ZaznamQueryParams,
  CommentDto
} from './types';

function isGuest(): boolean {
  return auth.isGuest && !auth.isAuthenticated;
}

class GuestOnlyError extends Error {
  constructor(feature: string) {
    super(`${feature} vyžaduje registraci. Vytvořte si účet pro přístup k této funkci.`);
    this.name = 'GuestOnlyError';
  }
}

export const unifiedApi = {
  projects: {
    async list(): Promise<ProjectListResponse> {
      if (isGuest()) {
        return localProjectsApi.list();
      }
      return remoteProjectsApi.list();
    },

    async get(id: string): Promise<ProjectDetailDto> {
      if (isGuest()) {
        return localProjectsApi.get(id);
      }
      return remoteProjectsApi.get(id);
    },

    async create(data: CreateProjectRequest): Promise<ProjectDto> {
      if (isGuest()) {
        return localProjectsApi.create(data);
      }
      return remoteProjectsApi.create(data);
    },

    async update(id: string, data: UpdateProjectRequest): Promise<ProjectDto> {
      if (isGuest()) {
        return localProjectsApi.update(id, data);
      }
      return remoteProjectsApi.update(id, data);
    },

    async delete(id: string): Promise<void> {
      if (isGuest()) {
        return localProjectsApi.delete(id);
      }
      return remoteProjectsApi.delete(id);
    },

    // Member operations - only available for authenticated users
    async getMembers(projectId: string): Promise<MemberDto[]> {
      if (isGuest()) {
        throw new GuestOnlyError('Správa členů');
      }
      return remoteProjectsApi.getMembers(projectId);
    },

    async addMember(projectId: string, data: AddMemberRequest): Promise<{ invitationId: string; inviteUrl: string; expiresAt: string }> {
      if (isGuest()) {
        throw new GuestOnlyError('Přidávání členů');
      }
      return remoteProjectsApi.addMember(projectId, data);
    },

    async updateMember(projectId: string, userId: string, data: UpdateMemberRequest): Promise<void> {
      if (isGuest()) {
        throw new GuestOnlyError('Úprava členů');
      }
      return remoteProjectsApi.updateMember(projectId, userId, data);
    },

    async removeMember(projectId: string, userId: string): Promise<void> {
      if (isGuest()) {
        throw new GuestOnlyError('Odebírání členů');
      }
      return remoteProjectsApi.removeMember(projectId, userId);
    },

    async leave(projectId: string): Promise<void> {
      if (isGuest()) {
        throw new GuestOnlyError('Opuštění projektu');
      }
      return remoteProjectsApi.leave(projectId);
    }
  },

  properties: {
    async list(params: PropertyQueryParams = {}): Promise<PropertyListResponse> {
      if (isGuest()) {
        return localPropertiesApi.list(params);
      }
      return remotePropertiesApi.list(params);
    },

    async get(id: string): Promise<PropertyDetailDto> {
      if (isGuest()) {
        return localPropertiesApi.get(id);
      }
      return remotePropertiesApi.get(id);
    },

    async create(data: CreatePropertyRequest): Promise<PropertyDto> {
      if (isGuest()) {
        return localPropertiesApi.create(data);
      }
      return remotePropertiesApi.create(data);
    },

    async update(id: string, data: UpdatePropertyRequest): Promise<PropertyDto> {
      if (isGuest()) {
        return localPropertiesApi.update(id, data);
      }
      return remotePropertiesApi.update(id, data);
    },

    async delete(id: string): Promise<void> {
      if (isGuest()) {
        return localPropertiesApi.delete(id);
      }
      return remotePropertiesApi.delete(id);
    },

    async getStats(id: string): Promise<PropertyStatsDto> {
      if (isGuest()) {
        return localPropertiesApi.getStats(id);
      }
      return remotePropertiesApi.getStats(id);
    }
  },

  units: {
    async list(params: UnitQueryParams): Promise<UnitListResponse> {
      if (isGuest()) {
        return localUnitsApi.list(params);
      }
      return remoteUnitsApi.list(params);
    },

    async get(id: string): Promise<UnitDto> {
      if (isGuest()) {
        return localUnitsApi.get(id);
      }
      return remoteUnitsApi.get(id);
    },

    async create(data: CreateUnitRequest): Promise<UnitDto> {
      if (isGuest()) {
        return localUnitsApi.create(data);
      }
      return remoteUnitsApi.create(data);
    },

    async update(id: string, data: UpdateUnitRequest): Promise<UnitDto> {
      if (isGuest()) {
        return localUnitsApi.update(id, data);
      }
      return remoteUnitsApi.update(id, data);
    },

    async delete(id: string): Promise<void> {
      if (isGuest()) {
        return localUnitsApi.delete(id);
      }
      return remoteUnitsApi.delete(id);
    }
  },

  zaznamy: {
    async list(params: ZaznamQueryParams = {}): Promise<ZaznamListResponse> {
      if (isGuest()) {
        return localZaznamyApi.list(params);
      }
      return remoteZaznamyApi.list(params);
    },

    async get(id: string): Promise<ZaznamDetailDto> {
      if (isGuest()) {
        return localZaznamyApi.get(id);
      }
      return remoteZaznamyApi.get(id);
    },

    async create(data: CreateZaznamRequest): Promise<ZaznamDto> {
      if (isGuest()) {
        return localZaznamyApi.create(data);
      }
      return remoteZaznamyApi.create(data);
    },

    async update(id: string, data: UpdateZaznamRequest): Promise<ZaznamDto> {
      if (isGuest()) {
        return localZaznamyApi.update(id, data);
      }
      return remoteZaznamyApi.update(id, data);
    },

    async delete(id: string): Promise<void> {
      if (isGuest()) {
        return localZaznamyApi.delete(id);
      }
      return remoteZaznamyApi.delete(id);
    },

    async complete(id: string): Promise<ZaznamDto> {
      if (isGuest()) {
        return localZaznamyApi.complete(id);
      }
      return remoteZaznamyApi.complete(id);
    },

    async getDrafts(): Promise<ZaznamListResponse> {
      if (isGuest()) {
        return localZaznamyApi.getDrafts();
      }
      return remoteZaznamyApi.getDrafts();
    },

    // Comments - only available for authenticated users for now
    async getComments(zaznamId: string): Promise<CommentDto[]> {
      if (isGuest()) {
        // Return empty array for guests - comments stored in zaznam.comments
        return [];
      }
      return remoteZaznamyApi.getComments(zaznamId);
    },

    async addComment(zaznamId: string, content: string): Promise<CommentDto> {
      if (isGuest()) {
        throw new GuestOnlyError('Komentáře');
      }
      return remoteZaznamyApi.addComment(zaznamId, content);
    },

    async updateComment(commentId: string, content: string): Promise<CommentDto> {
      if (isGuest()) {
        throw new GuestOnlyError('Komentáře');
      }
      return remoteZaznamyApi.updateComment(commentId, content);
    },

    async deleteComment(commentId: string): Promise<void> {
      if (isGuest()) {
        throw new GuestOnlyError('Komentáře');
      }
      return remoteZaznamyApi.deleteComment(commentId);
    }
  }
};

// Re-export for convenience
export type { ProjectListResponse, PropertyListResponse, UnitListResponse, ZaznamListResponse };
