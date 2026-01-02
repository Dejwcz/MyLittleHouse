import { api } from './client';
import type {
  UnitDto,
  CreateUnitRequest,
  UpdateUnitRequest,
  UnitQueryParams
} from './types';

export interface UnitListResponse {
  items: UnitDto[];
  total: number;
}

function buildQueryString(params: UnitQueryParams): string {
  const searchParams = new URLSearchParams();
  searchParams.set('propertyId', params.propertyId);
  if (params.parentUnitId) searchParams.set('parentUnitId', params.parentUnitId);
  return `?${searchParams.toString()}`;
}

export const unitsApi = {
  async list(params: UnitQueryParams): Promise<UnitListResponse> {
    return api.get<UnitListResponse>(`/units${buildQueryString(params)}`);
  },

  async get(id: string): Promise<UnitDto> {
    return api.get<UnitDto>(`/units/${id}`);
  },

  async create(data: CreateUnitRequest): Promise<UnitDto> {
    return api.post<UnitDto>('/units', data);
  },

  async update(id: string, data: UpdateUnitRequest): Promise<UnitDto> {
    return api.put<UnitDto>(`/units/${id}`, data);
  },

  async delete(id: string): Promise<void> {
    return api.delete(`/units/${id}`);
  }
};
