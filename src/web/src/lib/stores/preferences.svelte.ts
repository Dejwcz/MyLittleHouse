import { browser } from '$app/environment';
import type { SyncMode } from '$lib/db';

const STORAGE_KEY = 'mujdomecek-preferences';

export interface UserPreferences {
  defaultSyncMode: SyncMode;
}

const DEFAULT_PREFERENCES: UserPreferences = {
  defaultSyncMode: 'local-only'
};

function loadFromStorage(): UserPreferences {
  if (!browser) return DEFAULT_PREFERENCES;

  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      const data = JSON.parse(stored);
      return { ...DEFAULT_PREFERENCES, ...data };
    }
  } catch {
    // Invalid data, use defaults
  }
  return DEFAULT_PREFERENCES;
}

class PreferencesStore {
  #prefs = $state<UserPreferences>(loadFromStorage());

  get defaultSyncMode(): SyncMode {
    return this.#prefs.defaultSyncMode;
  }

  setDefaultSyncMode(mode: SyncMode) {
    this.#prefs.defaultSyncMode = mode;
    this.#persist();
  }

  #persist() {
    if (browser) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.#prefs));
    }
  }

  reset() {
    this.#prefs = { ...DEFAULT_PREFERENCES };
    if (browser) {
      localStorage.removeItem(STORAGE_KEY);
    }
  }
}

export const preferences = new PreferencesStore();
