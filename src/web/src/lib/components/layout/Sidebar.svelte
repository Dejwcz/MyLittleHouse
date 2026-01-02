<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import { page } from '$app/stores';
  import type { Component } from 'svelte';
  import type { Icon as LucideIcon } from 'lucide-svelte';

  interface NavItem {
    href: string;
    label: string;
    icon: typeof LucideIcon;
    badge?: number;
  }

  interface Props {
    items: NavItem[];
    class?: string;
  }

  let { items, class: className }: Props = $props();

  function isActive(href: string): boolean {
    if (href === '/') {
      return $page.url.pathname === '/';
    }
    return $page.url.pathname.startsWith(href);
  }
</script>

<aside class={cn('hidden md:flex md:w-64 md:flex-col', className)}>
  <!-- Logo -->
  <a href="/" class="flex items-center gap-3 px-4 py-5 border-b border-border">
    <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-primary text-foreground-inverse">
      <span class="text-sm font-bold">MD</span>
    </div>
    <span class="text-lg font-semibold">MůjDomeček</span>
  </a>

  <nav class="flex flex-1 flex-col gap-1 p-4">
    {#each items as item}
      {@const active = isActive(item.href)}
      <a
        href={item.href}
        class={cn(
          'flex items-center gap-3 rounded-2xl px-4 py-3 text-sm font-medium transition-colors',
          active
            ? 'bg-primary text-foreground-inverse'
            : 'text-foreground-secondary hover:bg-bg-secondary hover:text-foreground'
        )}
      >
        <item.icon class="h-5 w-5" />
        <span class="flex-1">{item.label}</span>
        {#if item.badge && item.badge > 0}
          <span
            class={cn(
              'flex h-5 min-w-5 items-center justify-center rounded-full px-1.5 text-xs font-semibold',
              active ? 'bg-white/20 text-foreground-inverse' : 'bg-primary text-foreground-inverse'
            )}
          >
            {item.badge > 99 ? '99+' : item.badge}
          </span>
        {/if}
      </a>
    {/each}
  </nav>
</aside>
