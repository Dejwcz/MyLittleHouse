<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import { FileQuestion, type Icon as LucideIcon } from 'lucide-svelte';
  import type { Snippet } from 'svelte';

  interface Props {
    title: string;
    description?: string;
    icon?: typeof LucideIcon;
    action?: Snippet;
    class?: string;
  }

  let { title, description, icon: Icon = FileQuestion as typeof LucideIcon, action, class: className }: Props = $props();
</script>

<div class={cn('flex flex-col items-center justify-center py-12 text-center', className)}>
  <div class="mb-4 rounded-full bg-bg-secondary p-4">
    <Icon class="h-8 w-8 text-foreground-muted" />
  </div>

  <h3 class="text-lg font-semibold text-foreground">
    {title}
  </h3>

  {#if description}
    <p class="mt-1 max-w-sm text-sm text-foreground-muted">
      {description}
    </p>
  {/if}

  {#if action}
    <div class="mt-4">
      {@render action()}
    </div>
  {/if}
</div>
