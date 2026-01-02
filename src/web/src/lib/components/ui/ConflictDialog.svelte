<script lang="ts">
  import { Monitor, Cloud, ArrowRight } from 'lucide-svelte';
  import Modal from './Modal.svelte';
  import Button from './Button.svelte';
  import Card from './Card.svelte';
  import type { ConflictData } from '$lib/stores/sync.svelte';

  interface Props {
    open: boolean;
    conflict: ConflictData | null;
    onResolve: (choice: 'local' | 'server') => void;
    onClose: () => void;
  }

  let { open = $bindable(), conflict, onResolve, onClose }: Props = $props();

  function formatDate(timestamp: number): string {
    return new Date(timestamp).toLocaleString('cs-CZ', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  function getEntityTitle(data: Record<string, unknown>): string {
    return (data.name as string) ?? (data.title as string) ?? 'Neznámý';
  }

  function handleChoice(choice: 'local' | 'server') {
    onResolve(choice);
    open = false;
  }
</script>

<Modal bind:open title="Konflikt synchronizace" size="lg" onclose={onClose}>
  {#if conflict}
    <p class="mb-4 text-sm text-foreground-muted">
      Tento záznam byl změněn jak lokálně, tak na serveru.
      Vyberte verzi, kterou chcete ponechat.
    </p>

    <div class="grid gap-4 sm:grid-cols-2">
      <!-- Local version -->
      <button
        class="group text-left"
        onclick={() => handleChoice('local')}
      >
        <Card class="h-full cursor-pointer transition-all hover:ring-2 hover:ring-primary group-focus:ring-2 group-focus:ring-primary">
          <div class="mb-3 flex items-center gap-2">
            <div class="rounded-full bg-blue-100 p-2 dark:bg-blue-900">
              <Monitor class="h-4 w-4 text-blue-600 dark:text-blue-300" />
            </div>
            <div>
              <h3 class="font-medium text-foreground">Lokální verze</h3>
              <p class="text-xs text-foreground-muted">
                {formatDate(conflict.localVersion.updatedAt)}
              </p>
            </div>
          </div>

          <div class="space-y-2 text-sm">
            <div class="font-medium text-foreground">
              {getEntityTitle(conflict.localVersion.data)}
            </div>
            {#if conflict.localVersion.data.description}
              <p class="text-foreground-muted line-clamp-2">
                {conflict.localVersion.data.description}
              </p>
            {/if}
          </div>

          <div class="mt-4 flex items-center justify-center gap-2 text-sm text-primary opacity-0 transition-opacity group-hover:opacity-100">
            Použít tuto verzi <ArrowRight class="h-4 w-4" />
          </div>
        </Card>
      </button>

      <!-- Server version -->
      <button
        class="group text-left"
        onclick={() => handleChoice('server')}
      >
        <Card class="h-full cursor-pointer transition-all hover:ring-2 hover:ring-primary group-focus:ring-2 group-focus:ring-primary">
          <div class="mb-3 flex items-center gap-2">
            <div class="rounded-full bg-green-100 p-2 dark:bg-green-900">
              <Cloud class="h-4 w-4 text-green-600 dark:text-green-300" />
            </div>
            <div>
              <h3 class="font-medium text-foreground">Serverová verze</h3>
              <p class="text-xs text-foreground-muted">
                {formatDate(conflict.serverVersion.updatedAt)}
              </p>
            </div>
          </div>

          <div class="space-y-2 text-sm">
            <div class="font-medium text-foreground">
              {getEntityTitle(conflict.serverVersion.data)}
            </div>
            {#if conflict.serverVersion.data.description}
              <p class="text-foreground-muted line-clamp-2">
                {conflict.serverVersion.data.description}
              </p>
            {/if}
          </div>

          <div class="mt-4 flex items-center justify-center gap-2 text-sm text-primary opacity-0 transition-opacity group-hover:opacity-100">
            Použít tuto verzi <ArrowRight class="h-4 w-4" />
          </div>
        </Card>
      </button>
    </div>

    <p class="mt-4 text-xs text-foreground-muted">
      Vybraná verze bude zachována, druhá bude ztracena.
    </p>
  {/if}
</Modal>
