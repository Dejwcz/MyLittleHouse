<script lang="ts">
  import { PageHeader, Card, Button } from '$lib';
  import { sync } from '$lib/stores/sync.svelte';
  import { Database, Cloud, Download, Trash2, RefreshCw } from 'lucide-svelte';

  let exporting = $state(false);
  let clearing = $state(false);

  async function handleExport() {
    exporting = true;
    // TODO: Export data
    await new Promise(r => setTimeout(r, 1000));
    exporting = false;
  }

  async function handleClearLocal() {
    if (!confirm('Opravdu chcete smazat všechna lokální data? Tato akce je nevratná.')) return;
    clearing = true;
    // TODO: Clear IndexedDB
    await new Promise(r => setTimeout(r, 500));
    clearing = false;
  }
</script>

<PageHeader
  title="Data a synchronizace"
  subtitle="Správa dat a zálohy"
  backHref="/settings"
/>

<div class="mx-auto max-w-2xl space-y-6">
  <Card>
    <div class="mb-4 flex items-center gap-3">
      <Cloud class="h-5 w-5 text-foreground-muted" />
      <h2 class="font-medium">Synchronizace</h2>
    </div>
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <div>
          <p class="font-medium">Stav</p>
          <p class="text-sm text-foreground-muted">
            {#if !sync.isOnline}
              Offline
            {:else if sync.isSyncing}
              Synchronizuji...
            {:else if sync.pendingCount > 0}
              {sync.pendingCount} změn čeká
            {:else}
              Synchronizováno
            {/if}
          </p>
        </div>
        <Button
          variant="secondary"
          size="sm"
          onclick={() => sync.triggerSync()}
          disabled={!sync.isOnline || sync.isSyncing}
        >
          {#snippet children()}
            <RefreshCw class="mr-2 h-4 w-4 {sync.isSyncing ? 'animate-spin' : ''}" />
            Synchronizovat
          {/snippet}
        </Button>
      </div>
      {#if sync.error}
        <div class="rounded-xl bg-red-50 p-3 text-sm text-red-600 dark:bg-red-950 dark:text-red-400">
          {sync.error}
        </div>
      {/if}
    </div>
  </Card>

  <Card>
    <div class="mb-4 flex items-center gap-3">
      <Database class="h-5 w-5 text-foreground-muted" />
      <h2 class="font-medium">Lokální data</h2>
    </div>
    <p class="mb-4 text-sm text-foreground-muted">
      Vaše data jsou primárně uložena v tomto zařízení. Můžete je exportovat jako zálohu.
    </p>
    <div class="flex flex-wrap gap-3">
      <Button variant="secondary" onclick={handleExport} disabled={exporting}>
        {#snippet children()}
          <Download class="mr-2 h-4 w-4" />
          {exporting ? 'Exportuji...' : 'Exportovat data'}
        {/snippet}
      </Button>
    </div>
  </Card>

  <Card class="border-red-200 dark:border-red-900">
    <div class="mb-4 flex items-center gap-3">
      <Trash2 class="h-5 w-5 text-red-500" />
      <h2 class="font-medium text-red-600 dark:text-red-400">Smazat lokální data</h2>
    </div>
    <p class="mb-4 text-sm text-foreground-muted">
      Toto smaže všechna data z tohoto zařízení. Pokud máte zapnutou synchronizaci, data zůstanou na serveru.
    </p>
    <Button variant="danger" size="sm" onclick={handleClearLocal} disabled={clearing}>
      {#snippet children()}
        {clearing ? 'Mažu...' : 'Smazat lokální data'}
      {/snippet}
    </Button>
  </Card>
</div>
