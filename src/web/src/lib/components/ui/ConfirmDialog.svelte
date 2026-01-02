<script lang="ts">
  import Modal from './Modal.svelte';
  import Button from './Button.svelte';
  import { AlertTriangle } from 'lucide-svelte';

  type Variant = 'danger' | 'warning' | 'info';

  interface Props {
    open: boolean;
    title: string;
    message: string;
    confirmText?: string;
    cancelText?: string;
    variant?: Variant;
    loading?: boolean;
    onconfirm: () => void | Promise<void>;
    oncancel?: () => void;
  }

  let {
    open = $bindable(),
    title,
    message,
    confirmText = 'Potvrdit',
    cancelText = 'Zrušit',
    variant = 'danger',
    loading = false,
    onconfirm,
    oncancel
  }: Props = $props();

  const iconColors: Record<Variant, string> = {
    danger: 'text-error',
    warning: 'text-warning',
    info: 'text-info'
  };

  const buttonVariants: Record<Variant, 'danger' | 'primary'> = {
    danger: 'danger',
    warning: 'primary',
    info: 'primary'
  };

  function handleCancel() {
    open = false;
    oncancel?.();
  }

  async function handleConfirm() {
    await onconfirm();
    if (!loading) {
      open = false;
    }
  }
</script>

<Modal bind:open {title} size="sm" onclose={handleCancel}>
  <div class="flex gap-4">
    <div class="flex-shrink-0">
      <div class="flex h-10 w-10 items-center justify-center rounded-full bg-bg-secondary">
        <AlertTriangle class="h-5 w-5 {iconColors[variant]}" />
      </div>
    </div>
    <div class="flex-1">
      <p class="text-sm text-foreground-secondary">
        {message}
      </p>
    </div>
  </div>

  {#snippet footer()}
    <Button variant="ghost" onclick={handleCancel} disabled={loading}>
      {#snippet children()}
        {cancelText}
      {/snippet}
    </Button>
    <Button variant={buttonVariants[variant]} onclick={handleConfirm} {loading}>
      {#snippet children()}
        {confirmText}
      {/snippet}
    </Button>
  {/snippet}
</Modal>
