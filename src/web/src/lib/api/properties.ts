import { api } from './client';
import type {
  PropertyDto,
  PropertyDetailDto,
  CreatePropertyRequest,
  UpdatePropertyRequest,
  PropertyStatsDto,
  PropertyQueryParams,
  MemberDto,
  AddMemberRequest,
  UpdateMemberRequest
} from './types';

export interface PropertyListResponse {
  items: PropertyDto[];
  total: number;
}

function buildQueryString(params: PropertyQueryParams): string {
  const searchParams = new URLSearchParams();
  if (params.projectId) searchParams.set('projectId', params.projectId);
  if (params.shared !== undefined) searchParams.set('shared', String(params.shared));
  const query = searchParams.toString();
  return query ? `?${query}` : '';
}

export const propertiesApi = {
  async list(params: PropertyQueryParams = {}): Promise<PropertyListResponse> {
    return api.get<PropertyListResponse>(`/properties${buildQueryString(params)}`);
  },

  async get(id: string): Promise<PropertyDetailDto> {
    return api.get<PropertyDetailDto>(`/properties/${id}`);
  },

  async create(data: CreatePropertyRequest): Promise<PropertyDto> {
    return api.post<PropertyDto>('/properties', data);
  },

  async update(id: string, data: UpdatePropertyRequest): Promise<PropertyDto> {
    return api.put<PropertyDto>(`/properties/${id}`, data);
  },

  async delete(id: string): Promise<void> {
    return api.delete(`/properties/${id}`);
  },

  async getStats(id: string): Promise<PropertyStatsDto> {
    return api.get<PropertyStatsDto>(`/properties/${id}/stats`);
  },

  async updateCover(id: string, coverMediaId?: string): Promise<PropertyDto> {
    return api.patch<PropertyDto>(`/properties/${id}/cover`, {
      coverMediaId: coverMediaId ?? null
    });
  },

  // Members
  async getMembers(propertyId: string): Promise<MemberDto[]> {
    return api.get<MemberDto[]>(`/properties/${propertyId}/members`);
  },

  async addMember(propertyId: string, data: AddMemberRequest): Promise<{ invitationId: string; inviteUrl: string; expiresAt: string }> {
    return api.post(`/properties/${propertyId}/members`, data);
  },

  async updateMember(propertyId: string, userId: string, data: UpdateMemberRequest): Promise<void> {
    return api.put(`/properties/${propertyId}/members/${userId}`, data);
  },

  async removeMember(propertyId: string, userId: string): Promise<void> {
    return api.delete(`/properties/${propertyId}/members/${userId}`);
  },

  async leave(propertyId: string): Promise<void> {
    return api.post(`/properties/${propertyId}/leave`);
  }
};
