<script lang="ts">
  import { Sidebar, BottomNav, ToastContainer, Button, Avatar, Dropdown, Spinner, ConflictDialog } from '$lib';
  import { theme } from '$lib/stores/theme.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { sync } from '$lib/stores/sync.svelte';
  import { authApi } from '$lib/api';
  import { goto } from '$app/navigation';
  import { browser } from '$app/environment';
  import { page } from '$app/stores';
  import { onMount } from 'svelte';
  import { cn } from '$lib/utils/cn';
  import {
    Building2,
    Bell,
    Settings,
    Sun,
    Moon,
    LogOut,
    User,
    MoreVertical,
    RefreshCw,
    CloudOff,
    AlertCircle
  } from 'lucide-svelte';
  import type { Snippet } from 'svelte';

  // Detekce zda jsme uvnitř projektu - projekt má vlastní layout se sidebarem
  const isInsideProject = $derived(
    /^\/projects\/[^/]+\/.+/.test($page.url.pathname) ||
    /^\/projects\/[^/]+$/.test($page.url.pathname)
  );

  interface Props {
    children: Snippet;
  }

  let { children }: Props = $props();
  let checking = $state(true);
  let conflictDialogOpen = $state(false);
  let currentConflict = $derived(sync.conflicts[0] ?? null);

  // Auto-open conflict dialog when conflicts arise
  $effect(() => {
    if (sync.hasConflicts && !conflictDialogOpen) {
      conflictDialogOpen = true;
    }
  });

  async function handleConflictResolve(choice: 'local' | 'server') {
    if (currentConflict) {
      await sync.resolveConflict(currentConflict.entityType, currentConflict.entityId, choice);
    }
  }

  function handleConflictClose() {
    conflictDialogOpen = false;
  }

  // Auth guard - allow guest users OR authenticated users
  onMount(async () => {
    // Guest users can access the app
    if (auth.isGuest) {
      checking = false;
      return;
    }

    // Authenticated users - verify session
    if (auth.isAuthenticated) {
      if (auth.tokens?.accessToken) {
        try {
          await authApi.getMe();
        } catch {
          auth.logout();
          goto('/login');
          return;
        }
      }
      checking = false;
      return;
    }

    // Not guest and not authenticated - redirect to landing
    goto('/');
  });

  // Hlavní navigace - projekt-specifické položky jsou v project layoutu
  const navItems = [
    { href: '/projects', label: 'Projekty', icon: Building2 },
    { href: '/notifications', label: 'Notifikace', icon: Bell },
    { href: '/settings', label: 'Nastavení', icon: Settings }
  ];

  const userMenuItems = $derived(
    auth.isGuest
      ? [
          { id: 'register', label: 'Vytvořit účet', icon: User },
          { id: 'login', label: 'Přihlásit se', icon: LogOut }
        ]
      : [
          { id: 'profile', label: 'Profil', icon: User },
          { id: 'settings', label: 'Nastavení', icon: Settings },
          { id: 'logout', label: 'Odhlásit se', icon: LogOut, danger: true }
        ]
  );

  const userName = $derived(
    auth.user ? `${auth.user.firstName} ${auth.user.lastName}` : (auth.isGuest ? 'Host' : 'Uživatel')
  );

  async function handleUserMenuSelect(id: string) {
    if (id === 'logout') {
      await authApi.logout();
      goto('/');
    } else if (id === 'login') {
      goto('/login');
    } else if (id === 'register') {
      goto('/register');
    } else if (id === 'profile') {
      goto('/settings/profile');
    } else if (id === 'settings') {
      goto('/settings');
    }
  }
</script>

{#if checking}
  <div class="flex min-h-screen items-center justify-center bg-bg">
    <Spinner size="lg" />
  </div>
{:else}
  <!-- Pokud jsme uvnitř projektu, renderujeme jen children - projekt má vlastní layout -->
  {#if isInsideProject}
    {@render children()}
  {:else}
    <div class="flex min-h-screen bg-bg">
      <!-- Sidebar (desktop) -->
      <Sidebar items={navItems} class="fixed inset-y-0 left-0 border-r border-border bg-surface" />

      <!-- Main content area -->
      <div class="flex flex-1 flex-col md:pl-64">
        <!-- Top header -->
        <header class="sticky top-0 z-30 border-b border-border bg-surface/80 backdrop-blur-sm">
          <div class="flex h-16 items-center justify-between px-4 md:px-6">
            <!-- Logo -->
            <a href="/" class="flex items-center gap-2">
              <div
                class="flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-foreground-inverse"
              >
                <span class="text-sm font-semibold">MD</span>
              </div>
              <span class="font-semibold md:hidden">MůjDomeček</span>
            </a>

            <!-- Right side actions -->
            <div class="flex items-center gap-2">
              <!-- Sync status -->
              {#if sync.pendingCount > 0 || sync.error || !sync.isOnline}
                <button
                  onclick={() => sync.triggerSync()}
                  class={cn(
                    "relative rounded-full p-2 transition-colors",
                    !sync.isOnline
                      ? "text-foreground-muted"
                      : sync.error
                        ? "text-red-500 hover:bg-red-50 dark:hover:bg-red-950"
                        : "text-amber-500 hover:bg-amber-50 dark:hover:bg-amber-950"
                  )}
                  disabled={!sync.isOnline || sync.isSyncing}
                  aria-label={
                    !sync.isOnline
                      ? 'Offline'
                      : sync.error
                        ? 'Chyba synchronizace'
                        : `${sync.pendingCount} změn čeká na synchronizaci`
                  }
                  title={
                    !sync.isOnline
                      ? 'Offline - změny se uloží lokálně'
                      : sync.error
                        ? sync.error
                        : `${sync.pendingCount} změn čeká`
                  }
                >
                  {#if !sync.isOnline}
                    <CloudOff class="h-5 w-5" />
                  {:else if sync.error}
                    <AlertCircle class="h-5 w-5" />
                  {:else}
                    <RefreshCw class={cn("h-5 w-5", sync.isSyncing && "animate-spin")} />
                  {/if}
                  {#if sync.pendingCount > 0 && sync.isOnline}
                    <span class="absolute -right-1 -top-1 flex h-4 w-4 items-center justify-center rounded-full bg-amber-500 text-[10px] font-medium text-white">
                      {sync.pendingCount > 9 ? '9+' : sync.pendingCount}
                    </span>
                  {/if}
                </button>
              {/if}

              <!-- Theme toggle -->
              <button
                onclick={() => theme.toggle()}
                class="rounded-full p-2 text-foreground-muted hover:bg-bg-secondary hover:text-foreground transition-colors"
                aria-label={theme.isDark ? 'Světlý režim' : 'Tmavý režim'}
              >
                {#if theme.isDark}
                  <Sun class="h-5 w-5" />
                {:else}
                  <Moon class="h-5 w-5" />
                {/if}
              </button>

              <!-- Notifications (desktop) -->
              <a
                href="/notifications"
                class="relative hidden rounded-full p-2 text-foreground-muted hover:bg-bg-secondary hover:text-foreground transition-colors md:block"
              >
                <Bell class="h-5 w-5" />
              </a>

              <!-- User menu -->
              <Dropdown items={userMenuItems} onselect={handleUserMenuSelect}>
                {#snippet trigger()}
                  <div class="flex items-center gap-2">
                    <Avatar name={userName} size="sm" />
                    <MoreVertical class="h-4 w-4 text-foreground-muted md:hidden" />
                  </div>
                {/snippet}
              </Dropdown>
            </div>
          </div>
        </header>

        <!-- Page content -->
        <main class="flex-1 p-4 pb-20 md:p-6 md:pb-6">
          {@render children()}
        </main>
      </div>

      <!-- Bottom nav (mobile) -->
      <BottomNav items={navItems.slice(0, 5)} />
    </div>
  {/if}

  <ToastContainer />

  <!-- Conflict resolution dialog -->
  <ConflictDialog
    bind:open={conflictDialogOpen}
    conflict={currentConflict}
    onResolve={handleConflictResolve}
    onClose={handleConflictClose}
  />
{/if}
