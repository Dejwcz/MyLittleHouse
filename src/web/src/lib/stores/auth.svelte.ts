import { browser } from '$app/environment';
import { goto } from '$app/navigation';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  avatarUrl?: string;
  preferredLanguage: string;
  themePreference: string;
  hasPassword: boolean;
  linkedAccounts: {
    google?: string;
    apple?: string;
  };
  createdAt: string;
}

export interface AuthTokens {
  accessToken: string;
  expiresAt: number;
  // Note: refreshToken is stored in HTTP-only cookie, not accessible from JS
}

const STORAGE_KEY = 'mujdomecek-auth';
const GUEST_KEY = 'mujdomecek-guest';

function loadFromStorage(): { user: User | null; tokens: AuthTokens | null; isGuest: boolean } {
  if (!browser) return { user: null, tokens: null, isGuest: false };

  try {
    // Check if guest mode is active
    const guestMode = localStorage.getItem(GUEST_KEY);
    if (guestMode === 'true') {
      return { user: null, tokens: null, isGuest: true };
    }

    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      const data = JSON.parse(stored);
      // Check if token is expired
      if (data.tokens && data.tokens.expiresAt < Date.now()) {
        localStorage.removeItem(STORAGE_KEY);
        return { user: null, tokens: null, isGuest: false };
      }
      return { ...data, isGuest: false };
    }
  } catch {
    localStorage.removeItem(STORAGE_KEY);
  }
  return { user: null, tokens: null, isGuest: false };
}

class AuthStore {
  #initial = loadFromStorage();

  user = $state<User | null>(this.#initial.user);
  tokens = $state<AuthTokens | null>(this.#initial.tokens);
  isGuest = $state(this.#initial.isGuest);
  loading = $state(false);

  // Authenticated = has account and is logged in
  readonly isAuthenticated = $derived(this.user !== null && this.tokens !== null);

  // Can use app = guest OR authenticated
  readonly canUseApp = $derived(this.isGuest || this.isAuthenticated);

  readonly fullName = $derived(
    this.user ? `${this.user.firstName} ${this.user.lastName}` : (this.isGuest ? 'Host' : '')
  );

  setAuth(user: User, tokens: AuthTokens) {
    this.user = user;
    this.tokens = tokens;
    this.isGuest = false;
    this.#persist();
  }

  setUser(user: User) {
    this.user = user;
    this.#persist();
  }

  setTokens(tokens: AuthTokens) {
    this.tokens = tokens;
    this.#persist();
  }

  startAsGuest() {
    this.user = null;
    this.tokens = null;
    this.isGuest = true;
    if (browser) {
      localStorage.removeItem(STORAGE_KEY);
      localStorage.setItem(GUEST_KEY, 'true');
    }
  }

  logout() {
    this.user = null;
    this.tokens = null;
    this.isGuest = false;
    if (browser) {
      localStorage.removeItem(STORAGE_KEY);
      localStorage.removeItem(GUEST_KEY);
      goto('/');
    }
  }

  #persist() {
    if (browser) {
      if (this.user && this.tokens) {
        localStorage.setItem(
          STORAGE_KEY,
          JSON.stringify({ user: this.user, tokens: this.tokens })
        );
        localStorage.removeItem(GUEST_KEY);
      }
    }
  }

  // Check if we need to refresh token (5 min before expiry)
  shouldRefreshToken(): boolean {
    if (!this.tokens) return false;
    const fiveMinutes = 5 * 60 * 1000;
    return this.tokens.expiresAt - Date.now() < fiveMinutes;
  }

  getAccessToken(): string | null {
    return this.tokens?.accessToken ?? null;
  }
}

export const auth = new AuthStore();
