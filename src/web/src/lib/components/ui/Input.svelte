<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import type { HTMLInputAttributes } from 'svelte/elements';

  interface Props extends Omit<HTMLInputAttributes, 'value'> {
    value?: string;
    label?: string;
    error?: string;
    class?: string;
  }

  let { label, error, id, value = $bindable(''), class: className, ...rest }: Props = $props();

  const generatedId = crypto.randomUUID();
  const inputId = $derived(id ?? generatedId);
</script>

<div class="space-y-1.5">
  {#if label}
    <label for={inputId} class="block text-sm font-medium text-foreground">
      {label}
    </label>
  {/if}

  <input
    id={inputId}
    bind:value
    class={cn(
      'block w-full rounded-2xl border bg-surface px-4 py-3 text-sm text-foreground placeholder:text-foreground-muted transition-colors',
      'focus:border-border-focus focus:outline-none focus:ring-1 focus:ring-border-focus',
      'disabled:cursor-not-allowed disabled:opacity-50',
      error ? 'border-error' : 'border-border',
      className
    )}
    aria-invalid={error ? 'true' : undefined}
    aria-describedby={error ? `${inputId}-error` : undefined}
    {...rest}
  />

  {#if error}
    <p id="{inputId}-error" class="text-xs text-error">
      {error}
    </p>
  {/if}
</div>
