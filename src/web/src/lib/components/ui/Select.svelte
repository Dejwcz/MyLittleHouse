<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import { ChevronDown } from 'lucide-svelte';
  import type { HTMLSelectAttributes } from 'svelte/elements';

  interface Option {
    value: string;
    label: string;
    disabled?: boolean;
  }

  interface Props extends HTMLSelectAttributes {
    options: Option[];
    label?: string;
    error?: string;
    placeholder?: string;
    class?: string;
  }

  let {
    options,
    label,
    error,
    placeholder,
    id,
    value = $bindable(),
    class: className,
    ...rest
  }: Props = $props();

  const generatedId = crypto.randomUUID();
  const selectId = $derived(id ?? generatedId);
</script>

<div class="space-y-1.5">
  {#if label}
    <label for={selectId} class="block text-sm font-medium text-foreground">
      {label}
    </label>
  {/if}

  <div class="relative">
    <select
      id={selectId}
      bind:value
      class={cn(
        'block w-full appearance-none rounded-2xl border bg-surface px-4 py-3 pr-10 text-sm text-foreground transition-colors',
        'focus:border-border-focus focus:outline-none focus:ring-1 focus:ring-border-focus',
        'disabled:cursor-not-allowed disabled:opacity-50',
        error ? 'border-error' : 'border-border',
        className
      )}
      aria-invalid={error ? 'true' : undefined}
      {...rest}
    >
      {#if placeholder}
        <option value="" disabled selected>{placeholder}</option>
      {/if}
      {#each options as option}
        <option value={option.value} disabled={option.disabled}>
          {option.label}
        </option>
      {/each}
    </select>

    <div class="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3">
      <ChevronDown class="h-4 w-4 text-foreground-muted" />
    </div>
  </div>

  {#if error}
    <p class="text-xs text-error">{error}</p>
  {/if}
</div>
