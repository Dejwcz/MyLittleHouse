<script lang="ts">
  import { Archive, Trash2, X } from 'lucide-svelte';
  import Modal from './Modal.svelte';
  import Button from './Button.svelte';

  interface Props {
    open: boolean;
    projectName: string;
    onConfirm: (deleteFromServer: boolean) => void;
    onCancel: () => void;
  }

  let { open = $bindable(), projectName, onConfirm, onCancel }: Props = $props();

  function handleConfirm(deleteFromServer: boolean) {
    onConfirm(deleteFromServer);
    open = false;
  }

  function handleCancel() {
    onCancel();
    open = false;
  }
</script>

<Modal bind:open title="Vypnout synchronizaci?" size="sm" onclose={handleCancel}>
  <p class="mb-6 text-sm text-foreground-muted">
    Co chcete udělat s daty projektu <strong class="text-foreground">"{projectName}"</strong> na serveru?
  </p>

  <div class="space-y-3">
    <button
      class="flex w-full items-center gap-3 rounded-xl border border-border bg-surface p-4 text-left transition-colors hover:bg-bg-secondary focus:outline-none focus:ring-2 focus:ring-primary"
      onclick={() => handleConfirm(false)}
    >
      <div class="rounded-full bg-blue-100 p-2 dark:bg-blue-900">
        <Archive class="h-5 w-5 text-blue-600 dark:text-blue-300" />
      </div>
      <div>
        <div class="font-medium text-foreground">Ponechat jako archiv</div>
        <div class="text-sm text-foreground-muted">
          Data zůstanou na serveru, ale nebudou se synchronizovat
        </div>
      </div>
    </button>

    <button
      class="flex w-full items-center gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-left transition-colors hover:bg-red-100 focus:outline-none focus:ring-2 focus:ring-red-500 dark:border-red-900 dark:bg-red-950 dark:hover:bg-red-900"
      onclick={() => handleConfirm(true)}
    >
      <div class="rounded-full bg-red-100 p-2 dark:bg-red-900">
        <Trash2 class="h-5 w-5 text-red-600 dark:text-red-300" />
      </div>
      <div>
        <div class="font-medium text-red-700 dark:text-red-300">Smazat ze serveru</div>
        <div class="text-sm text-red-600 dark:text-red-400">
          Data budou trvale odstraněna ze serveru
        </div>
      </div>
    </button>
  </div>

  <div class="mt-6">
    <Button variant="ghost" fullWidth onclick={handleCancel}>
      Zrušit
    </Button>
  </div>
</Modal>
