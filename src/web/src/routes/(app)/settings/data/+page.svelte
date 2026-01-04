<script lang="ts">
  import { PageHeader, Card, Button, Modal } from '$lib';
  import { sync } from '$lib/stores/sync.svelte';
  import { toast } from '$lib/stores/ui.svelte';
  import { db, exportLocalData, downloadExport, importLocalData, readExportFile, type ExportData } from '$lib/db';
  import { Database, Cloud, Download, Upload, Trash2, RefreshCw, AlertTriangle } from 'lucide-svelte';

  let exporting = $state(false);
  let importing = $state(false);
  let clearing = $state(false);
  let showImportModal = $state(false);
  let importFile = $state<File | null>(null);
  let importPreview = $state<ExportData | null>(null);
  let importError = $state('');
  let clearBeforeImport = $state(false);

  let fileInput: HTMLInputElement;

  async function handleExport() {
    exporting = true;
    try {
      const data = await exportLocalData();
      downloadExport(data);
      toast.success(`Exportováno ${data.projects.length} projektů, ${data.properties.length} nemovitostí, ${data.zaznamy.length} záznamů`);
    } catch {
      toast.error('Export se nezdařil');
    } finally {
      exporting = false;
    }
  }

  function handleFileSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    importFile = file;
    importError = '';
    importPreview = null;

    readExportFile(file)
      .then(data => {
        importPreview = data;
        showImportModal = true;
      })
      .catch(err => {
        importError = err.message;
        toast.error(err.message);
      });

    // Reset input so same file can be selected again
    input.value = '';
  }

  async function handleImport() {
    if (!importPreview) return;

    importing = true;
    try {
      // Clone to remove Svelte proxy (IndexedDB can't clone proxies)
      const plainData = JSON.parse(JSON.stringify(importPreview));
      const result = await importLocalData(plainData, { clearExisting: clearBeforeImport });

      if (result.errors.length > 0) {
        toast.error(result.errors.join(', '));
      } else {
        toast.success(`Import dokončen: ${result.imported} položek`);
      }

      showImportModal = false;
      importPreview = null;
      clearBeforeImport = false;
    } catch (err) {
      console.error('Import error:', err);
      const message = err instanceof Error ? err.message : 'Neznámá chyba';
      toast.error(`Import se nezdařil: ${message}`);
    } finally {
      importing = false;
    }
  }

  function closeImportModal() {
    showImportModal = false;
    importPreview = null;
    importFile = null;
    importError = '';
    clearBeforeImport = false;
  }

  async function handleClearLocal() {
    if (!confirm('Opravdu chcete smazat všechna lokální data? Tato akce je nevratná.')) return;
    clearing = true;
    try {
      await Promise.all([
        db.projects.clear(),
        db.properties.clear(),
        db.units.clear(),
        db.zaznamy.clear(),
        db.media.clear(),
        db.tags.clear(),
        db.zaznamTags.clear(),
        db.syncQueue.clear()
      ]);
      toast.success('Lokální data byla smazána');
    } catch {
      toast.error('Smazání se nezdařilo');
    } finally {
      clearing = false;
    }
  }
</script>

<PageHeader
  title="Data a synchronizace"
  subtitle="Správa dat a zálohy"
  backHref="/settings"
/>

<div class="mx-auto max-w-2xl space-y-6">
  <div class="flex items-start gap-3 rounded-2xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-900 dark:bg-amber-950">
    <AlertTriangle class="h-5 w-5 shrink-0 text-amber-600 dark:text-amber-400" />
    <div class="text-sm">
      <p class="font-medium text-amber-800 dark:text-amber-200">Vaše data jsou uložena v prohlížeči</p>
      <p class="mt-1 text-amber-700 dark:text-amber-300">
        Vymazání dat prohlížeče, použití systémových čističů (CCleaner, BleachBit apod.)
        nebo přeinstalace prohlížeče může trvale smazat vaše záznamy.
        Pro ochranu dat doporučujeme zapnout synchronizaci nebo pravidelně exportovat.
      </p>
    </div>
  </div>

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
      Vaše data jsou primárně uložena v tomto zařízení. Exportujte je jako zálohu nebo importujte z předchozího exportu.
    </p>
    <div class="flex flex-wrap gap-3">
      <Button variant="secondary" onclick={handleExport} disabled={exporting}>
        {#snippet children()}
          <Download class="mr-2 h-4 w-4" />
          {exporting ? 'Exportuji...' : 'Exportovat data'}
        {/snippet}
      </Button>
      <Button variant="secondary" onclick={() => fileInput.click()}>
        {#snippet children()}
          <Upload class="mr-2 h-4 w-4" />
          Importovat data
        {/snippet}
      </Button>
      <input
        bind:this={fileInput}
        type="file"
        accept=".json"
        class="hidden"
        onchange={handleFileSelect}
      />
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

<Modal bind:open={showImportModal} title="Import dat">
  {#if importPreview}
    <div class="space-y-4">
      <div class="rounded-xl bg-bg-secondary p-4">
        <p class="mb-2 text-sm font-medium">Náhled importu:</p>
        <ul class="space-y-1 text-sm text-foreground-muted">
          <li>{importPreview.projects?.length ?? 0} projektů</li>
          <li>{importPreview.properties?.length ?? 0} nemovitostí</li>
          <li>{importPreview.units?.length ?? 0} jednotek</li>
          <li>{importPreview.zaznamy?.length ?? 0} záznamů</li>
          <li>{importPreview.media?.length ?? 0} médií</li>
        </ul>
        <p class="mt-2 text-xs text-foreground-muted">
          Exportováno: {new Date(importPreview.exportedAt).toLocaleString('cs-CZ')}
        </p>
      </div>

      <label class="flex items-center gap-3">
        <input
          type="checkbox"
          bind:checked={clearBeforeImport}
          class="h-4 w-4 rounded border-border text-primary focus:ring-primary"
        />
        <span class="text-sm">Smazat stávající data před importem</span>
      </label>

      {#if !clearBeforeImport}
        <p class="text-xs text-foreground-muted">
          Bez této volby se data sloučí. Položky se stejným ID budou přepsány.
        </p>
      {/if}
    </div>
  {/if}

  {#snippet footer()}
    <div class="flex justify-end gap-3">
      <Button variant="secondary" onclick={closeImportModal}>
        {#snippet children()}Zrušit{/snippet}
      </Button>
      <Button onclick={handleImport} disabled={importing || !importPreview}>
        {#snippet children()}
          {importing ? 'Importuji...' : 'Importovat'}
        {/snippet}
      </Button>
    </div>
  {/snippet}
</Modal>
