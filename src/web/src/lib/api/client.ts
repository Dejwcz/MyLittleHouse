import { auth } from '$lib/stores/auth.svelte';
import { browser } from '$app/environment';

const API_BASE = import.meta.env.VITE_API_BASE ?? '/api';

export class ApiError extends Error {
  constructor(
    public status: number,
    public code: string,
    message: string,
    public details?: Record<string, string[]>
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

interface ApiErrorResponse {
  code: string;
  message: string;
  details?: Record<string, string[]>;
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T;
  }

  if (!response.ok) {
    let errorData: ApiErrorResponse;
    try {
      errorData = await response.json();
    } catch {
      errorData = {
        code: 'UNKNOWN_ERROR',
        message: response.statusText || 'An error occurred'
      };
    }
    throw new ApiError(
      response.status,
      errorData.code,
      errorData.message,
      errorData.details
    );
  }

  return response.json() as Promise<T>;
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>)
  };

  // Add auth token if available
  const token = auth.getAccessToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
    credentials: 'include'
  });

  // Handle 401 - try to refresh token (refresh token is in HTTP-only cookie)
  if (response.status === 401 && browser && auth.isAuthenticated) {
    const refreshed = await tryRefreshToken();
    if (refreshed) {
      // Retry the request with new token
      headers['Authorization'] = `Bearer ${auth.getAccessToken()}`;
      const retryResponse = await fetch(`${API_BASE}${path}`, {
        ...options,
        headers,
        credentials: 'include'
      });
      return handleResponse<T>(retryResponse);
    } else {
      // Refresh failed, logout
      auth.logout();
      throw new ApiError(401, 'UNAUTHORIZED', 'Session expired');
    }
  }

  return handleResponse<T>(response);
}

async function tryRefreshToken(): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE}/auth/refresh`, {
      method: 'POST',
      credentials: 'include' // Include HTTP-only cookie with refresh token
    });

    if (response.ok) {
      const data = await response.json();
      auth.setTokens({
        accessToken: data.accessToken,
        expiresAt: Date.now() + data.expiresIn * 1000
      });
      return true;
    }
  } catch {
    // Refresh failed
  }
  return false;
}

// Convenience methods
export const api = {
  get: <T>(path: string) => apiRequest<T>(path, { method: 'GET' }),

  post: <T>(path: string, body?: unknown) =>
    apiRequest<T>(path, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined
    }),

  put: <T>(path: string, body?: unknown) =>
    apiRequest<T>(path, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined
    }),

  patch: <T>(path: string, body?: unknown) =>
    apiRequest<T>(path, {
      method: 'PATCH',
      body: body ? JSON.stringify(body) : undefined
    }),

  delete: <T>(path: string) => apiRequest<T>(path, { method: 'DELETE' })
};
