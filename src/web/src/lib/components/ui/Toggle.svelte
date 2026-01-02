<script lang="ts">
  import { cn } from '$lib/utils/cn';

  interface Props {
    checked: boolean;
    label?: string;
    description?: string;
    disabled?: boolean;
    class?: string;
  }

  let {
    checked = $bindable(),
    label,
    description,
    disabled = false,
    class: className
  }: Props = $props();

  const id = crypto.randomUUID();

  function handleToggle() {
    if (!disabled) {
      checked = !checked;
    }
  }

  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleToggle();
    }
  }
</script>

<div class={cn('flex items-start gap-3', className)}>
  <button
    type="button"
    role="switch"
    aria-checked={checked}
    aria-labelledby={label ? `${id}-label` : undefined}
    aria-describedby={description ? `${id}-desc` : undefined}
    {disabled}
    onclick={handleToggle}
    onkeydown={handleKeydown}
    class={cn(
      'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out',
      'focus:outline-none focus-visible:ring-2 focus-visible:ring-border-focus focus-visible:ring-offset-2',
      checked ? 'bg-primary' : 'bg-bg-secondary',
      disabled && 'cursor-not-allowed opacity-50'
    )}
  >
    <span
      class={cn(
        'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out',
        checked ? 'translate-x-5' : 'translate-x-0'
      )}
    ></span>
  </button>

  {#if label || description}
    <div class="flex flex-col">
      {#if label}
        <span
          id="{id}-label"
          class={cn('text-sm font-medium text-foreground', disabled && 'opacity-50')}
        >
          {label}
        </span>
      {/if}
      {#if description}
        <span
          id="{id}-desc"
          class={cn('text-xs text-foreground-muted', disabled && 'opacity-50')}
        >
          {description}
        </span>
      {/if}
    </div>
  {/if}
</div>
