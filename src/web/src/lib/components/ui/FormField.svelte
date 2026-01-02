<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import type { Snippet } from 'svelte';

  interface Props {
    label?: string;
    error?: string;
    hint?: string;
    required?: boolean;
    children: Snippet;
    class?: string;
  }

  let { label, error, hint, required = false, children, class: className }: Props = $props();

  const id = crypto.randomUUID();
</script>

<div class={cn('space-y-1.5', className)}>
  {#if label}
    <label for={id} class="block text-sm font-medium text-foreground">
      {label}
      {#if required}
        <span class="text-error">*</span>
      {/if}
    </label>
  {/if}

  <div data-field-id={id}>
    {@render children()}
  </div>

  {#if error}
    <p class="text-xs text-error">{error}</p>
  {:else if hint}
    <p class="text-xs text-foreground-muted">{hint}</p>
  {/if}
</div>
