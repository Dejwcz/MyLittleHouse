<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import { X } from 'lucide-svelte';
  import type { Snippet } from 'svelte';

  type Size = 'sm' | 'md' | 'lg' | 'xl';

  interface Props {
    open: boolean;
    title?: string;
    size?: Size;
    onclose?: () => void;
    children: Snippet;
    footer?: Snippet;
    class?: string;
  }

  let {
    open = $bindable(),
    title,
    size = 'md',
    onclose,
    children,
    footer,
    class: className
  }: Props = $props();

  const sizes: Record<Size, string> = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
    xl: 'max-w-xl'
  };

  function handleClose() {
    open = false;
    onclose?.();
  }

  function handleBackdropClick(e: MouseEvent) {
    if (e.target === e.currentTarget) {
      handleClose();
    }
  }

  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') {
      handleClose();
    }
  }
</script>

<svelte:window onkeydown={handleKeydown} />

{#if open}
  <!-- svelte-ignore a11y_click_events_have_key_events -->
  <div
    class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm"
    onclick={handleBackdropClick}
    role="presentation"
  >
    <div
      class={cn(
        'w-full rounded-3xl bg-surface p-6 shadow-lg animate-in fade-in zoom-in-95 duration-200',
        sizes[size],
        className
      )}
      role="dialog"
      aria-modal="true"
      aria-labelledby={title ? 'modal-title' : undefined}
    >
      {#if title}
        <div class="mb-4 flex items-center justify-between">
          <h2 id="modal-title" class="text-lg font-semibold text-foreground">
            {title}
          </h2>
          <button
            onclick={handleClose}
            class="rounded-full p-1 text-foreground-muted hover:bg-bg-secondary hover:text-foreground"
            aria-label="Zavrit"
          >
            <X class="h-5 w-5" />
          </button>
        </div>
      {/if}

      <div class="text-foreground-secondary">
        {@render children()}
      </div>

      {#if footer}
        <div class="mt-6 flex justify-end gap-3">
          {@render footer()}
        </div>
      {/if}
    </div>
  </div>
{/if}

<style>
  @keyframes fade-in {
    from {
      opacity: 0;
    }
    to {
      opacity: 1;
    }
  }

  @keyframes zoom-in-95 {
    from {
      transform: scale(0.95);
    }
    to {
      transform: scale(1);
    }
  }

  .animate-in {
    animation: fade-in 200ms ease-out, zoom-in-95 200ms ease-out;
  }
</style>
