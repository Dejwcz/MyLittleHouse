import { browser } from '$app/environment';

export type Theme = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'mujdomecek-theme';

function getInitialTheme(): Theme {
  if (!browser) return 'system';

  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored === 'light' || stored === 'dark' || stored === 'system') {
    return stored;
  }
  return 'system';
}

function getSystemPreference(): 'light' | 'dark' {
  if (!browser) return 'light';
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

class ThemeStore {
  preference = $state<Theme>(getInitialTheme());

  readonly resolved = $derived<'light' | 'dark'>(
    this.preference === 'system' ? getSystemPreference() : this.preference
  );

  readonly isDark = $derived(this.resolved === 'dark');

  constructor() {
    if (browser) {
      // Listen for system preference changes
      const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      mediaQuery.addEventListener('change', () => {
        // Force reactivity update when system preference changes
        if (this.preference === 'system') {
          this.applyTheme();
        }
      });

      // Apply initial theme
      this.applyTheme();
    }
  }

  setTheme(theme: Theme) {
    this.preference = theme;
    if (browser) {
      localStorage.setItem(STORAGE_KEY, theme);
      this.applyTheme();
    }
  }

  toggle() {
    const next = this.isDark ? 'light' : 'dark';
    this.setTheme(next);
  }

  private applyTheme() {
    const resolved = this.preference === 'system' ? getSystemPreference() : this.preference;

    if (resolved === 'dark') {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }
}

export const theme = new ThemeStore();
