<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import { page } from '$app/stores';
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

<nav
  class={cn(
    'fixed bottom-0 left-0 right-0 z-40 border-t border-border bg-surface md:hidden',
    className
  )}
>
  <div class="flex items-center justify-around">
    {#each items as item}
      {@const active = isActive(item.href)}
      <a
        href={item.href}
        class={cn(
          'relative flex flex-1 flex-col items-center gap-1 px-2 py-3 text-xs font-medium transition-colors',
          active ? 'text-primary' : 'text-foreground-muted hover:text-foreground'
        )}
      >
        <div class="relative">
          <item.icon class="h-6 w-6" />
          {#if item.badge && item.badge > 0}
            <span
              class="absolute -right-2 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-error px-1 text-[10px] font-semibold text-white"
            >
              {item.badge > 99 ? '99+' : item.badge}
            </span>
          {/if}
        </div>
        <span>{item.label}</span>
      </a>
    {/each}
  </div>
</nav>
