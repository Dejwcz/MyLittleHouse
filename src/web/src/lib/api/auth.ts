import { api, ApiError } from './client';
import { auth, type User, type AuthTokens } from '$lib/stores/auth.svelte';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  user: User;
  accessToken: string;
  expiresIn: number;
  // refreshToken is set as HTTP-only cookie by backend
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  password: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export const authApi = {
  async login(data: LoginRequest): Promise<User> {
    const response = await api.post<AuthResponse>('/auth/login', data);

    // refreshToken is set as HTTP-only cookie by backend
    const tokens: AuthTokens = {
      accessToken: response.accessToken,
      expiresAt: Date.now() + response.expiresIn * 1000
    };

    auth.setAuth(response.user, tokens);
    return response.user;
  },

  async register(data: RegisterRequest): Promise<void> {
    await api.post('/auth/register', data);
  },

  async logout(): Promise<void> {
    try {
      await api.post('/auth/logout');
    } finally {
      auth.logout();
    }
  },

  async forgotPassword(data: ForgotPasswordRequest): Promise<void> {
    await api.post('/auth/forgot-password', data);
  },

  async validateResetToken(token: string): Promise<boolean> {
    try {
      await api.get(`/auth/validate-reset-token?token=${encodeURIComponent(token)}`);
      return true;
    } catch (error) {
      if (error instanceof ApiError && error.status === 400) {
        return false;
      }
      throw error;
    }
  },

  async resetPassword(data: ResetPasswordRequest): Promise<void> {
    await api.post('/auth/reset-password', data);
  },

  async changePassword(data: ChangePasswordRequest): Promise<void> {
    await api.post('/auth/change-password', data);
  },

  async getMe(): Promise<User> {
    const user = await api.get<User>('/users/me');
    auth.setUser(user);
    return user;
  },

  async resendConfirmation(email: string): Promise<void> {
    await api.post('/auth/resend-confirmation', { email });
  }
};

export { ApiError };
