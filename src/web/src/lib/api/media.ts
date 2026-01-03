import { api } from './client';
import type { MediaDto, MediaOwnerType, MediaType } from './types';

export interface MediaListResponse {
  items: MediaDto[];
}

export interface AddMediaRequest {
  ownerType: MediaOwnerType;
  ownerId: string;
  storageKey: string;
  mediaType: MediaType;
  originalFileName?: string;
  mimeType: string;
  sizeBytes: number;
  caption?: string;
}

export interface UpdateMediaRequest {
  caption?: string;
}

export interface MediaUrlResponse {
  url: string;
  expiresAt: string;
}

export const mediaApi = {
  async list(ownerType: MediaOwnerType, ownerId: string): Promise<MediaListResponse> {
    const params = new URLSearchParams({ ownerType, ownerId });
    return api.get<MediaListResponse>(`/media?${params.toString()}`);
  },

  async create(data: AddMediaRequest): Promise<MediaDto> {
    return api.post<MediaDto>('/media', data);
  },

  async update(id: string, data: UpdateMediaRequest): Promise<MediaDto> {
    return api.put<MediaDto>(`/media/${id}`, data);
  },

  async delete(id: string): Promise<void> {
    return api.delete(`/media/${id}`);
  },

  async getUrl(id: string): Promise<MediaUrlResponse> {
    return api.get<MediaUrlResponse>(`/media/${id}/url`);
  }
};
