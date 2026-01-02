export { api, apiRequest, ApiError } from './client';
export { authApi } from './auth';

// Unified API - automatically switches between local (guest) and remote (authenticated)
import { unifiedApi } from './unified';
export const projectsApi = unifiedApi.projects;
export const propertiesApi = unifiedApi.properties;
export const zaznamyApi = unifiedApi.zaznamy;

// Units API - only remote for now (TODO: add local support if needed)
export { unitsApi } from './units';

// Re-export unified for explicit usage
export { unifiedApi };

export type {
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  ChangePasswordRequest
} from './auth';

export * from './types';
