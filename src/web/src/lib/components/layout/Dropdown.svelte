<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import type { Snippet } from 'svelte';
  import type { Icon as LucideIcon } from 'lucide-svelte';

  interface DropdownItem {
    id: string;
    label: string;
    icon?: typeof LucideIcon;
    danger?: boolean;
    disabled?: boolean;
  }

  interface Props {
    items: DropdownItem[];
    trigger: Snippet;
    onselect?: (id: string) => void;
    align?: 'left' | 'right';
    class?: string;
  }

  let { items, trigger, onselect, align = 'right', class: className }: Props = $props();

  let open = $state(false);
  let containerRef: HTMLDivElement;

  function handleSelect(item: DropdownItem) {
    if (item.disabled) return;
    open = false;
    onselect?.(item.id);
  }

  function handleClickOutside(e: MouseEvent) {
    if (containerRef && !containerRef.contains(e.target as Node)) {
      open = false;
    }
  }

  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') {
      open = false;
    }
  }
</script>

<svelte:window onclick={handleClickOutside} onkeydown={handleKeydown} />

<div class={cn('relative inline-block', className)} bind:this={containerRef}>
  <button type="button" onclick={() => (open = !open)} aria-expanded={open} aria-haspopup="menu">
    {@render trigger()}
  </button>

  {#if open}
    <div
      class={cn(
        'absolute z-50 mt-2 min-w-48 rounded-2xl border border-border bg-surface p-1 shadow-lg',
        align === 'right' ? 'right-0' : 'left-0'
      )}
      role="menu"
    >
      {#each items as item}
        <button
          type="button"
          onclick={() => handleSelect(item)}
          disabled={item.disabled}
          class={cn(
            'flex w-full items-center gap-2 rounded-xl px-3 py-2 text-sm transition-colors',
            item.disabled && 'cursor-not-allowed opacity-50',
            item.danger
              ? 'text-error hover:bg-red-50 dark:hover:bg-red-950'
              : 'text-foreground hover:bg-bg-secondary'
          )}
          role="menuitem"
        >
          {#if item.icon}
            <item.icon class="h-4 w-4" />
          {/if}
          {item.label}
        </button>
      {/each}
    </div>
  {/if}
</div>
