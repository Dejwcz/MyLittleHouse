import { api } from './client';
import type {
  ProjectDto,
  ProjectDetailDto,
  CreateProjectRequest,
  UpdateProjectRequest,
  MemberDto,
  AddMemberRequest,
  UpdateMemberRequest
} from './types';

export interface ProjectListResponse {
  items: ProjectDto[];
  total: number;
}

export const projectsApi = {
  async list(): Promise<ProjectListResponse> {
    return api.get<ProjectListResponse>('/projects');
  },

  async get(id: string): Promise<ProjectDetailDto> {
    return api.get<ProjectDetailDto>(`/projects/${id}`);
  },

  async create(data: CreateProjectRequest): Promise<ProjectDto> {
    return api.post<ProjectDto>('/projects', data);
  },

  async update(id: string, data: UpdateProjectRequest): Promise<ProjectDto> {
    return api.put<ProjectDto>(`/projects/${id}`, data);
  },

  async delete(id: string): Promise<void> {
    return api.delete(`/projects/${id}`);
  },

  // Members
  async getMembers(projectId: string): Promise<MemberDto[]> {
    return api.get<MemberDto[]>(`/projects/${projectId}/members`);
  },

  async addMember(projectId: string, data: AddMemberRequest): Promise<{ invitationId: string; inviteUrl: string; expiresAt: string }> {
    return api.post(`/projects/${projectId}/members`, data);
  },

  async updateMember(projectId: string, userId: string, data: UpdateMemberRequest): Promise<void> {
    return api.put(`/projects/${projectId}/members/${userId}`, data);
  },

  async removeMember(projectId: string, userId: string): Promise<void> {
    return api.delete(`/projects/${projectId}/members/${userId}`);
  },

  async leave(projectId: string): Promise<void> {
    return api.post(`/projects/${projectId}/leave`);
  }
};
