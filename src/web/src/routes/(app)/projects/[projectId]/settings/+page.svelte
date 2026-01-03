<script lang="ts">
  import { PageHeader, Card, Button, Input, Textarea, ConfirmDialog, Toggle, SyncBadge, DisableSyncDialog } from '$lib';
  import { projectsApi, type ProjectDetailDto } from '$lib/api';
  import { localProjectsApi, type ProjectDtoWithSync } from '$lib/api/local/projects';
  import { toast } from '$lib/stores/ui.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { sync } from '$lib/stores/sync.svelte';
  import type { SyncMode, SyncStatus } from '$lib/db';
  import { getContext, onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { Trash2, Cloud, CloudOff } from 'lucide-svelte';

  const projectId = $derived($page.params.projectId ?? '');

  const projectContext = getContext<{ project: ProjectDetailDto | null; reload: () => Promise<void> }>('project');
  const project = $derived(projectContext.project);

  let saving = $state(false);
  let showDeleteConfirm = $state(false);
  let showDisableSyncDialog = $state(false);
  let syncModeChanging = $state(false);

  let name = $state('');
  let description = $state('');
  let syncMode = $state<SyncMode>('local-only');
  let syncStatus = $state<SyncStatus>('local');

  $effect(() => {
    if (project) {
      name = project.name;
      description = project.description ?? '';
    }
  });

  onMount(async () => {
    await loadSyncMode();
  });

  async function loadSyncMode() {
    try {
      const localProject = await localProjectsApi.list();
      const thisProject = localProject.items.find(p => p.id === projectId) as ProjectDtoWithSync | undefined;
      if (thisProject) {
        syncMode = thisProject.syncMode ?? 'local-only';
        syncStatus = (thisProject.syncStatus as SyncStatus) ?? 'local';
      }
    } catch (err) {
      console.error('Failed to load sync mode:', err);
    }
  }

  async function handleSave() {
    saving = true;
    try {
      await projectsApi.update(projectId, {
        name: name.trim(),
        description: description.trim() || undefined
      });
      toast.success('Projekt uložen');
      await projectContext.reload();
    } catch (err) {
      toast.error('Nepodařilo se uložit projekt');
    } finally {
      saving = false;
    }
  }

  async function handleDelete() {
    saving = true;
    try {
      await projectsApi.delete(projectId);
      toast.success('Projekt smazán');
      goto('/projects');
    } catch (err) {
      toast.error('Nepodařilo se smazat projekt');
    } finally {
      saving = false;
    }
  }

  function handleSyncToggle() {
    if (syncMode === 'synced') {
      showDisableSyncDialog = true;
    } else {
      enableSync();
    }
  }

  async function enableSync() {
    if (!auth.isAuthenticated) {
      toast.error('Pro synchronizaci je potřeba se přihlásit');
      return;
    }

    syncModeChanging = true;
    try {
      await localProjectsApi.setSyncMode(projectId, 'synced');
      syncMode = 'synced';
      syncStatus = 'pending';
      toast.success('Synchronizace zapnuta');
      sync.triggerSync();
    } catch (err) {
      toast.error('Nepodařilo se zapnout synchronizaci');
    } finally {
      syncModeChanging = false;
    }
  }

  async function handleDisableSync(deleteFromServer: boolean) {
    syncModeChanging = true;
    try {
      await localProjectsApi.setSyncMode(projectId, 'local-only');
      syncMode = 'local-only';
      syncStatus = 'local';
      toast.success(deleteFromServer
        ? 'Synchronizace vypnuta, data budou smazána ze serveru'
        : 'Synchronizace vypnuta, data zůstala na serveru'
      );
    } catch (err) {
      toast.error('Nepodařilo se vypnout synchronizaci');
    } finally {
      syncModeChanging = false;
    }
  }

  const isOwner = $derived(project?.myRole === 'owner');
</script>

<PageHeader title="Nastavení projektu" />

<div class="mx-auto max-w-2xl space-y-6">
  <Card>
    <h2 class="mb-4 font-medium">Základní informace</h2>
    <form onsubmit={(e) => { e.preventDefault(); handleSave(); }} class="space-y-4">
      <Input label="Název projektu" bind:value={name} />
      <Textarea label="Popis (volitelné)" bind:value={description} rows={3} />
      <div class="flex justify-end">
        <Button type="submit" loading={saving}>
          {#snippet children()}Uložit{/snippet}
        </Button>
      </div>
    </form>
  </Card>

  <Card>
    <h2 class="mb-4 font-medium">Synchronizace</h2>
    <div class="flex items-center justify-between gap-4">
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 dark:bg-blue-950">
          {#if syncMode === 'synced'}
            <Cloud class="h-5 w-5 text-blue-500" />
          {:else}
            <CloudOff class="h-5 w-5 text-foreground-muted" />
          {/if}
        </div>
        <div>
          <p class="font-medium">{syncMode === 'synced' ? 'Synchronizováno' : 'Pouze lokálně'}</p>
          <p class="text-sm text-foreground-muted">
            {syncMode === 'synced' ? 'Data se zálohují na server' : 'Data pouze v tomto zařízení'}
          </p>
        </div>
      </div>
      <button
        class="flex items-center gap-2"
        onclick={handleSyncToggle}
        disabled={syncModeChanging || (syncMode === 'local-only' && !auth.isAuthenticated)}
      >
        <SyncBadge {syncMode} {syncStatus} showLabel={false} />
        <Toggle
          checked={syncMode === 'synced'}
          disabled={syncModeChanging || (syncMode === 'local-only' && !auth.isAuthenticated)}
        />
      </button>
    </div>
    {#if !auth.isAuthenticated && syncMode === 'local-only'}
      <p class="mt-3 text-sm text-amber-600 dark:text-amber-400">
        Pro synchronizaci je potřeba se přihlásit.
      </p>
    {/if}
  </Card>

  {#if isOwner}
    <Card class="border-red-200 dark:border-red-900">
      <h2 class="mb-4 font-medium text-red-600 dark:text-red-400">Nebezpečná zóna</h2>
      <p class="mb-4 text-sm text-foreground-muted">
        Smazání projektu je nevratné. Všechny nemovitosti a záznamy budou smazány.
      </p>
      <Button variant="danger" onclick={() => showDeleteConfirm = true}>
        {#snippet children()}
          <Trash2 class="h-4 w-4" />
          Smazat projekt
        {/snippet}
      </Button>
    </Card>
  {/if}
</div>

<ConfirmDialog
  bind:open={showDeleteConfirm}
  title="Smazat projekt?"
  message="Tato akce je nevratná. Všechny nemovitosti a záznamy budou trvale smazány."
  confirmText="Smazat"
  onconfirm={handleDelete}
/>

<DisableSyncDialog
  bind:open={showDisableSyncDialog}
  projectName={project?.name ?? ''}
  onConfirm={handleDisableSync}
  onCancel={() => showDisableSyncDialog = false}
/>
