<script lang="ts">
  import { ToastContainer } from '$lib';
  import { theme } from '$lib/stores/theme.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { onMount } from 'svelte';
  import { Sun, Moon } from 'lucide-svelte';
  import type { Snippet } from 'svelte';

  interface Props {
    children: Snippet;
  }

  let { children }: Props = $props();

  // Redirect authenticated users to dashboard (except landing page)
  onMount(() => {
    const currentPath = $page.url.pathname;
    const isAuthPage = ['/login', '/register', '/forgot-password', '/reset-password'].some(
      (p) => currentPath.startsWith(p)
    );

    if (auth.isAuthenticated && isAuthPage) {
      goto('/dashboard');
    }
  });
</script>

<div class="flex min-h-screen flex-col bg-bg">
  <!-- Minimal header -->
  <header class="flex items-center justify-between p-4">
    <a href="/" class="flex items-center gap-2">
      <div
        class="flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-foreground-inverse"
      >
        <span class="text-sm font-semibold">MD</span>
      </div>
      <span class="font-semibold">MujDomecek</span>
    </a>

    <button
      onclick={() => theme.toggle()}
      class="rounded-full p-2 text-foreground-muted hover:bg-bg-secondary hover:text-foreground transition-colors"
      aria-label={theme.isDark ? 'Svetly rezim' : 'Tmavy rezim'}
    >
      {#if theme.isDark}
        <Sun class="h-5 w-5" />
      {:else}
        <Moon class="h-5 w-5" />
      {/if}
    </button>
  </header>

  <!-- Centered content -->
  <main class="flex flex-1 items-center justify-center p-4">
    <div class="w-full max-w-md">
      {@render children()}
    </div>
  </main>

  <!-- Footer -->
  <footer class="p-4 text-center text-xs text-foreground-muted">
    Local-first property logbook. Data stays on your devices by default.
  </footer>
</div>

<ToastContainer />
