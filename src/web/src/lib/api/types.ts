// Common types
export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export type Role = 'owner' | 'editor' | 'viewer';
export type ZaznamStatus = 'draft' | 'complete';
export type UnitType = 'flat' | 'house' | 'garage' | 'garden' | 'room' | 'stairs' | 'other';
export type MediaType = 'photo' | 'document' | 'receipt';
export type MediaOwnerType = 'property' | 'unit' | 'zaznam';
export type SyncStatus = 'local' | 'synced' | 'syncing' | 'pending' | 'failed';
export type SyncMode = 'local-only' | 'synced';

// Project types
export interface ProjectDto {
  id: string;
  name: string;
  description?: string;
  propertyCount: number;
  memberCount: number;
  myRole: Role;
  syncMode?: SyncMode;
  syncStatus?: SyncStatus;
  createdAt: string;
  updatedAt: string;
}

export interface ProjectDetailDto extends ProjectDto {
  properties: PropertyDto[];
  members: MemberDto[];
}

export interface CreateProjectRequest {
  name: string;
  description?: string;
}

export interface UpdateProjectRequest {
  name?: string;
  description?: string;
}

// Property types
export interface PropertyDto {
  id: string;
  projectId: string;
  projectName: string;
  name: string;
  description?: string;
  latitude?: number;
  longitude?: number;
  geoRadius: number;
  unitCount: number;
  zaznamCount: number;
  totalCost: number;
  myRole: Role;
  isShared: boolean;
  syncMode?: SyncMode;
  syncStatus?: SyncStatus;
  coverMediaId?: string;
  coverUrl?: string;
  createdAt: string;
  updatedAt: string;
}

export interface PropertyDetailDto extends PropertyDto {
  units: UnitDto[];
  members: MemberDto[];
}

export interface CreatePropertyRequest {
  projectId: string;
  name: string;
  description?: string;
  latitude?: number;
  longitude?: number;
  geoRadius?: number;
}

export interface UpdatePropertyRequest {
  name?: string;
  description?: string;
  latitude?: number;
  longitude?: number;
  geoRadius?: number;
}

export interface PropertyStatsDto {
  totalCost: number;
  zaznamCount: number;
  draftCount: number;
  documentCount: number;
  costByMonth: { month: string; cost: number }[];
  costByYear: { year: number; cost: number }[];
}

// Unit types
export interface UnitDto {
  id: string;
  propertyId: string;
  parentUnitId?: string;
  name: string;
  description?: string;
  unitType: UnitType;
  childCount: number;
  zaznamCount: number;
  coverMediaId?: string;
  coverUrl?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateUnitRequest {
  propertyId: string;
  parentUnitId?: string;
  name: string;
  description?: string;
  unitType: UnitType;
}

export interface UpdateUnitRequest {
  name?: string;
  description?: string;
  unitType?: UnitType;
  parentUnitId?: string;
}

// Zaznam types
export interface ZaznamDto {
  id: string;
  propertyId: string;
  propertyName: string;
  unitId?: string;
  unitName?: string;
  title?: string;
  description?: string;
  date: string;
  cost?: number;
  status: ZaznamStatus;
  flags: string[];
  tags: string[];
  documentCount: number;
  commentCount: number;
  thumbnailUrl?: string;
  syncMode?: SyncMode;
  syncStatus: SyncStatus;
  createdAt: string;
  updatedAt: string;
  createdBy: { id: string; name: string };
}

export interface ZaznamDetailDto extends ZaznamDto {
  documents: MediaDto[];
  comments: CommentDto[];
}

export interface CreateZaznamRequest {
  propertyId: string;
  unitId?: string;
  title?: string;
  description?: string;
  date?: string;
  cost?: number;
  status?: ZaznamStatus;
  flags?: string[];
  tags?: string[];
}

export interface UpdateZaznamRequest {
  unitId?: string;
  title?: string;
  description?: string;
  date?: string;
  cost?: number;
  flags?: string[];
  tags?: string[];
}

// Media types
export interface MediaDto {
  id: string;
  zaznamId: string;
  ownerType?: MediaOwnerType;
  ownerId?: string;
  type: MediaType;
  storageKey: string;
  originalFileName?: string;
  mimeType: string;
  sizeBytes: number;
  caption?: string;
  thumbnailUrl?: string;
  createdAt: string;
}

// Comment types
export interface CommentDto {
  id: string;
  zaznamId: string;
  content: string;
  mentions: { userId: string; name: string }[];
  author: { id: string; name: string; avatarUrl?: string };
  createdAt: string;
  updatedAt: string;
  isEdited: boolean;
}

// Member types
export interface MemberDto {
  userId: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  role: Role;
  permissions?: Record<string, boolean>;
  status: 'active' | 'pending';
  joinedAt?: string;
}

export interface AddMemberRequest {
  email: string;
  role: 'editor' | 'viewer';
  permissions?: Record<string, boolean>;
}

export interface UpdateMemberRequest {
  role?: 'editor' | 'viewer';
  permissions?: Record<string, boolean>;
}

// Query params
export interface ZaznamQueryParams {
  propertyId?: string;
  unitId?: string;
  status?: ZaznamStatus;
  from?: string;
  to?: string;
  tags?: string[];
  search?: string;
  page?: number;
  pageSize?: number;
  sort?: 'date' | 'createdAt' | 'cost';
  order?: 'asc' | 'desc';
}

export interface PropertyQueryParams {
  projectId?: string;
  shared?: boolean;
}

export interface UnitQueryParams {
  propertyId: string;
  parentUnitId?: string;
}
