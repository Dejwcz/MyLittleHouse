<script lang="ts">
  import { Sidebar, BottomNav, ToastContainer, Button, Avatar, Dropdown, Spinner } from '$lib';
  import { theme } from '$lib/stores/theme.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { projectsApi, type ProjectDetailDto } from '$lib/api';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { setContext } from 'svelte';
  import { cn } from '$lib/utils/cn';
  import {
    Home,
    Warehouse,
    Layers,
    FileText,
    Settings,
    Sun,
    Moon,
    LogOut,
    User,
    ChevronLeft,
    FolderOpen
  } from 'lucide-svelte';
  import type { Snippet } from 'svelte';

  interface Props {
    children: Snippet;
  }

  let { children }: Props = $props();

  const projectId = $derived($page.params.projectId ?? '');

  let project = $state<ProjectDetailDto | null>(null);
  let loading = $state(true);
  let error = $state<string | null>(null);

  $effect(() => {
    if (projectId) {
      loadProject();
    }
  });

  async function loadProject() {
    loading = true;
    error = null;
    try {
      project = await projectsApi.get(projectId);
    } catch (err) {
      error = 'Projekt nenalezen';
      goto('/projects');
    } finally {
      loading = false;
    }
  }

  // Provide project context to child pages
  setContext('project', {
    get project() { return project; },
    get projectId() { return projectId; },
    reload: loadProject
  });

  const navItems = $derived([
    { href: `/projects/${projectId}`, label: 'Dashboard', icon: Home },
    { href: `/projects/${projectId}/properties`, label: 'Nemovitosti', icon: Warehouse },
    { href: `/projects/${projectId}/units`, label: 'Jednotky', icon: Layers },
    { href: `/projects/${projectId}/zaznamy`, label: 'Záznamy', icon: FileText },
    { href: `/projects/${projectId}/settings`, label: 'Nastavení', icon: Settings }
  ]);

  const userMenuItems = $derived(
    auth.isGuest
      ? [
          { id: 'register', label: 'Vytvořit účet', icon: User },
          { id: 'login', label: 'Přihlásit se', icon: LogOut }
        ]
      : [
          { id: 'profile', label: 'Profil', icon: User },
          { id: 'settings', label: 'Nastavení', icon: Settings },
          { id: 'logout', label: 'Odhlásit', icon: LogOut }
        ]
  );

  async function handleUserMenuAction(id: string) {
    switch (id) {
      case 'logout':
        auth.logout();
        goto('/');
        break;
      case 'profile':
        goto('/settings/profile');
        break;
      case 'settings':
        goto('/settings');
        break;
      case 'register':
        goto('/register');
        break;
      case 'login':
        goto('/login');
        break;
    }
  }

  function toggleTheme() {
    theme.toggle();
  }

  const isActiveRoute = (href: string) => {
    const currentPath = $page.url.pathname;
    if (href === `/projects/${projectId}`) {
      return currentPath === href;
    }
    return currentPath.startsWith(href);
  };
</script>

<div class="flex min-h-screen bg-bg">
  <!-- Desktop Sidebar -->
  <aside class="fixed inset-y-0 left-0 z-40 hidden w-64 border-r border-border bg-surface lg:block">
    <div class="flex h-full flex-col">
      <!-- Project Header -->
      <div class="border-b border-border p-4">
        <a
          href="/projects"
          class="flex items-center gap-2 text-sm text-foreground-muted hover:text-foreground transition-colors"
        >
          <ChevronLeft class="h-4 w-4" />
          Všechny projekty
        </a>
        <div class="mt-3 flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-50 dark:bg-primary-950">
            <FolderOpen class="h-5 w-5 text-primary" />
          </div>
          <div class="min-w-0 flex-1">
            <p class="truncate font-semibold">{project?.name ?? 'Načítání...'}</p>
            {#if project?.description}
              <p class="truncate text-sm text-foreground-muted">{project.description}</p>
            {/if}
          </div>
        </div>
      </div>

      <!-- Navigation -->
      <nav class="flex-1 space-y-1 p-4">
        {#each navItems as item}
          <a
            href={item.href}
            class={cn(
              'flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors',
              isActiveRoute(item.href)
                ? 'bg-primary-50 text-primary dark:bg-primary-950'
                : 'text-foreground-muted hover:bg-bg-secondary hover:text-foreground'
            )}
          >
            <item.icon class="h-5 w-5" />
            {item.label}
          </a>
        {/each}
      </nav>

      <!-- User Section -->
      <div class="border-t border-border p-4">
        <div class="flex items-center justify-between">
          <Dropdown items={userMenuItems} onSelect={handleUserMenuAction}>
            {#snippet trigger()}
              <button class="flex items-center gap-3 rounded-xl p-2 hover:bg-bg-secondary">
                <Avatar name={auth.fullName || 'Guest'} size="sm" />
                <span class="text-sm font-medium">{auth.fullName || 'Host'}</span>
              </button>
            {/snippet}
          </Dropdown>
          <button
            onclick={toggleTheme}
            class="rounded-xl p-2 text-foreground-muted hover:bg-bg-secondary hover:text-foreground"
            aria-label="Přepnout téma"
          >
            {#if theme.current === 'dark'}
              <Sun class="h-5 w-5" />
            {:else}
              <Moon class="h-5 w-5" />
            {/if}
          </button>
        </div>
      </div>
    </div>
  </aside>

  <!-- Mobile Header -->
  <header class="fixed inset-x-0 top-0 z-30 border-b border-border bg-surface lg:hidden">
    <div class="flex h-14 items-center justify-between px-4">
      <a href="/projects" class="flex items-center gap-2">
        <ChevronLeft class="h-5 w-5 text-foreground-muted" />
        <span class="font-semibold">{project?.name ?? '...'}</span>
      </a>
      <div class="flex items-center gap-2">
        <button
          onclick={toggleTheme}
          class="rounded-xl p-2 text-foreground-muted hover:bg-bg-secondary"
        >
          {#if theme.current === 'dark'}
            <Sun class="h-5 w-5" />
          {:else}
            <Moon class="h-5 w-5" />
          {/if}
        </button>
        <Dropdown items={userMenuItems} onSelect={handleUserMenuAction} align="right">
          {#snippet trigger()}
            <button class="rounded-xl p-1">
              <Avatar name={auth.fullName || 'Guest'} size="sm" />
            </button>
          {/snippet}
        </Dropdown>
      </div>
    </div>
  </header>

  <!-- Main Content -->
  <main class="flex-1 lg:ml-64">
    <div class="min-h-screen pt-14 pb-20 lg:pt-0 lg:pb-0">
      <div class="p-4 lg:p-6">
        {#if loading}
          <div class="flex items-center justify-center py-12">
            <Spinner size="lg" />
          </div>
        {:else if error}
          <div class="py-12 text-center text-foreground-muted">{error}</div>
        {:else}
          {@render children()}
        {/if}
      </div>
    </div>
  </main>

  <!-- Mobile Bottom Nav -->
  <BottomNav items={navItems} class="lg:hidden" />
</div>

<ToastContainer />
