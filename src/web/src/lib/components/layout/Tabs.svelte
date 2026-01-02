<script lang="ts">
  import { cn } from '$lib/utils/cn';

  interface Tab {
    id: string;
    label: string;
    badge?: number;
  }

  interface Props {
    items?: Tab[];
    tabs?: Tab[];  // deprecated, use items
    active: string;
    onchange?: (id: string) => void;
    class?: string;
  }

  let { items, tabs, active = $bindable(), onchange, class: className }: Props = $props();

  // Support both 'items' and legacy 'tabs' prop
  const tabItems = $derived(items ?? tabs ?? []);

  function handleClick(id: string) {
    active = id;
    onchange?.(id);
  }
</script>

<div class={cn('border-b border-border', className)}>
  <nav class="-mb-px flex gap-4" aria-label="Tabs">
    {#each tabItems as tab}
      <button
        type="button"
        onclick={() => handleClick(tab.id)}
        class={cn(
          'relative flex items-center gap-2 border-b-2 px-1 py-3 text-sm font-medium transition-colors',
          active === tab.id
            ? 'border-primary text-primary'
            : 'border-transparent text-foreground-muted hover:border-border hover:text-foreground'
        )}
        aria-current={active === tab.id ? 'page' : undefined}
      >
        {tab.label}
        {#if tab.badge !== undefined && tab.badge > 0}
          <span
            class={cn(
              'flex h-5 min-w-5 items-center justify-center rounded-full px-1.5 text-xs',
              active === tab.id
                ? 'bg-primary text-foreground-inverse'
                : 'bg-bg-secondary text-foreground-secondary'
            )}
          >
            {tab.badge > 99 ? '99+' : tab.badge}
          </span>
        {/if}
      </button>
    {/each}
  </nav>
</div>
