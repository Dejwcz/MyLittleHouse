<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import { ui, type ToastType } from '$lib/stores/ui.svelte';
  import { CheckCircle, XCircle, AlertTriangle, Info, X } from 'lucide-svelte';

  interface Props {
    id: string;
    type: ToastType;
    message: string;
  }

  let { id, type, message }: Props = $props();

  const icons = {
    success: CheckCircle,
    error: XCircle,
    warning: AlertTriangle,
    info: Info
  };

  const styles: Record<ToastType, string> = {
    success: 'bg-green-50 text-green-800 border-green-200 dark:bg-green-950 dark:text-green-200 dark:border-green-800',
    error: 'bg-red-50 text-red-800 border-red-200 dark:bg-red-950 dark:text-red-200 dark:border-red-800',
    warning: 'bg-amber-50 text-amber-800 border-amber-200 dark:bg-amber-950 dark:text-amber-200 dark:border-amber-800',
    info: 'bg-blue-50 text-blue-800 border-blue-200 dark:bg-blue-950 dark:text-blue-200 dark:border-blue-800'
  };

  const iconStyles: Record<ToastType, string> = {
    success: 'text-green-500',
    error: 'text-red-500',
    warning: 'text-amber-500',
    info: 'text-blue-500'
  };

  const Icon = $derived(icons[type]);

  function handleDismiss() {
    ui.removeToast(id);
  }
</script>

<div
  class={cn(
    'flex items-start gap-3 rounded-2xl border p-4 shadow-lg animate-in slide-in-from-top-2 duration-300',
    styles[type]
  )}
  role="alert"
>
  <Icon class={cn('h-5 w-5 flex-shrink-0 mt-0.5', iconStyles[type])} />

  <p class="flex-1 text-sm font-medium">
    {message}
  </p>

  <button
    onclick={handleDismiss}
    class="flex-shrink-0 rounded-full p-0.5 opacity-70 hover:opacity-100 transition-opacity"
    aria-label="Zavrít"
  >
    <X class="h-4 w-4" />
  </button>
</div>

<style>
  @keyframes slide-in-from-top-2 {
    from {
      transform: translateY(-0.5rem);
      opacity: 0;
    }
    to {
      transform: translateY(0);
      opacity: 1;
    }
  }

  .animate-in {
    animation: slide-in-from-top-2 300ms ease-out;
  }
</style>
