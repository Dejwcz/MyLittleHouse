import { api } from './client';
import type {
  ZaznamDto,
  ZaznamDetailDto,
  CreateZaznamRequest,
  UpdateZaznamRequest,
  ZaznamQueryParams,
  PaginatedResponse,
  CommentDto
} from './types';

export type ZaznamListResponse = PaginatedResponse<ZaznamDto>;

function buildQueryString(params: ZaznamQueryParams): string {
  const searchParams = new URLSearchParams();
  if (params.propertyId) searchParams.set('propertyId', params.propertyId);
  if (params.unitId) searchParams.set('unitId', params.unitId);
  if (params.status) searchParams.set('status', params.status);
  if (params.from) searchParams.set('from', params.from);
  if (params.to) searchParams.set('to', params.to);
  if (params.tags?.length) params.tags.forEach(t => searchParams.append('tags', t));
  if (params.search) searchParams.set('search', params.search);
  if (params.page) searchParams.set('page', String(params.page));
  if (params.pageSize) searchParams.set('pageSize', String(params.pageSize));
  if (params.sort) searchParams.set('sort', params.sort);
  if (params.order) searchParams.set('order', params.order);
  const query = searchParams.toString();
  return query ? `?${query}` : '';
}

export const zaznamyApi = {
  async list(params: ZaznamQueryParams = {}): Promise<ZaznamListResponse> {
    return api.get<ZaznamListResponse>(`/zaznamy${buildQueryString(params)}`);
  },

  async get(id: string): Promise<ZaznamDetailDto> {
    return api.get<ZaznamDetailDto>(`/zaznamy/${id}`);
  },

  async create(data: CreateZaznamRequest): Promise<ZaznamDto> {
    return api.post<ZaznamDto>('/zaznamy', data);
  },

  async update(id: string, data: UpdateZaznamRequest): Promise<ZaznamDto> {
    return api.put<ZaznamDto>(`/zaznamy/${id}`, data);
  },

  async delete(id: string): Promise<void> {
    return api.delete(`/zaznamy/${id}`);
  },

  async complete(id: string): Promise<ZaznamDto> {
    return api.post<ZaznamDto>(`/zaznamy/${id}/complete`);
  },

  async getDrafts(): Promise<ZaznamListResponse> {
    return api.get<ZaznamListResponse>('/zaznamy/drafts');
  },

  // Comments
  async getComments(zaznamId: string): Promise<CommentDto[]> {
    return api.get<CommentDto[]>(`/zaznamy/${zaznamId}/comments`);
  },

  async addComment(zaznamId: string, content: string): Promise<CommentDto> {
    return api.post<CommentDto>(`/zaznamy/${zaznamId}/comments`, { content });
  },

  async updateComment(commentId: string, content: string): Promise<CommentDto> {
    return api.put<CommentDto>(`/comments/${commentId}`, { content });
  },

  async deleteComment(commentId: string): Promise<void> {
    return api.delete(`/comments/${commentId}`);
  }
};
