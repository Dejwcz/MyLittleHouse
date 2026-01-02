<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import type { HTMLTextareaAttributes } from 'svelte/elements';

  interface Props extends Omit<HTMLTextareaAttributes, 'value'> {
    value?: string;
    label?: string;
    error?: string;
    class?: string;
  }

  let { value = $bindable(''), label, error, id, rows = 4, class: className, ...rest }: Props = $props();

  const generatedId = crypto.randomUUID();
  const textareaId = $derived(id ?? generatedId);
</script>

<div class="space-y-1.5">
  {#if label}
    <label for={textareaId} class="block text-sm font-medium text-foreground">
      {label}
    </label>
  {/if}

  <textarea
    id={textareaId}
    {rows}
    bind:value
    class={cn(
      'block w-full rounded-2xl border bg-surface px-4 py-3 text-sm text-foreground placeholder:text-foreground-muted transition-colors resize-none',
      'focus:border-border-focus focus:outline-none focus:ring-1 focus:ring-border-focus',
      'disabled:cursor-not-allowed disabled:opacity-50',
      error ? 'border-error' : 'border-border',
      className
    )}
    aria-invalid={error ? 'true' : undefined}
    aria-describedby={error ? `${textareaId}-error` : undefined}
    {...rest}
  ></textarea>

  {#if error}
    <p id="{textareaId}-error" class="text-xs text-error">
      {error}
    </p>
  {/if}
</div>
