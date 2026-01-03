<script lang="ts">
  import { PageHeader, Card, Button, EmptyState, Badge, Modal, Input, Textarea, ConfirmDialog, Avatar, Toggle, SyncBadge, DisableSyncDialog, Select } from '$lib';
  import { propertiesApi, projectsApi, type ProjectDetailDto, type PropertyDto, type ProjectDto } from '$lib/api';
  import { localProjectsApi, type ProjectDtoWithSync } from '$lib/api/local/projects';
  import { toast } from '$lib/stores/ui.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { sync } from '$lib/stores/sync.svelte';
  import type { SyncMode, SyncStatus } from '$lib/db';
  import { getContext, onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import {
    Plus, Building2, Home, FileText, Pencil, Trash2, UserPlus, Cloud, CloudOff, TrendingUp, ChevronDown
  } from 'lucide-svelte';

  const projectId = $derived($page.params.projectId ?? '');

  // Get project from layout context
  const projectContext = getContext<{ project: ProjectDetailDto | null; reload: () => Promise<void> }>('project');
  const project = $derived(projectContext.project);

  let showPropertyModal = $state(false);
  let showDeleteConfirm = $state(false);
  let showMemberModal = $state(false);
  let showDisableSyncDialog = $state(false);
  let showProjectSelector = $state(false);
  let saving = $state(false);
  let syncModeChanging = $state(false);
  let selectedProperty = $state<PropertyDto | null>(null);

  // All projects for selector
  let allProjects = $state<ProjectDto[]>([]);

  // Sync mode state
  let syncMode = $state<SyncMode>('local-only');
  let syncStatus = $state<SyncStatus>('local');

  // Property form state
  let propName = $state('');
  let propDescription = $state('');
  let propErrors = $state<Record<string, string>>({});

  // Member form state
  let memberEmail = $state('');
  let memberRole = $state<'editor' | 'viewer'>('viewer');
  let memberErrors = $state<Record<string, string>>({});

  // Stats
  const totalCost = $derived(project?.properties.reduce((sum, p) => sum + p.totalCost, 0) ?? 0);
  const totalZaznamy = $derived(project?.properties.reduce((sum, p) => sum + p.zaznamCount, 0) ?? 0);

  onMount(async () => {
    await Promise.all([loadSyncMode(), loadAllProjects()]);
  });

  async function loadAllProjects() {
    try {
      const response = await projectsApi.list();
      allProjects = response.items;
    } catch (err) {
      console.error('Failed to load projects:', err);
    }
  }

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

  function openPropertyModal(prop?: PropertyDto) {
    if (prop) {
      selectedProperty = prop;
      propName = prop.name;
      propDescription = prop.description ?? '';
    } else {
      selectedProperty = null;
      propName = '';
      propDescription = '';
    }
    propErrors = {};
    showPropertyModal = true;
  }

  async function handlePropertySave() {
    propErrors = {};
    if (!propName.trim()) {
      propErrors.name = 'Název je povinný';
      return;
    }

    saving = true;
    try {
      if (selectedProperty) {
        await propertiesApi.update(selectedProperty.id, {
          name: propName.trim(),
          description: propDescription.trim() || undefined
        });
        toast.success('Nemovitost upravena');
      } else {
        await propertiesApi.create({
          projectId,
          name: propName.trim(),
          description: propDescription.trim() || undefined
        });
        toast.success('Nemovitost vytvořena');
      }
      showPropertyModal = false;
      await projectContext.reload();
    } catch (err) {
      toast.error(selectedProperty ? 'Nepodařilo se upravit nemovitost' : 'Nepodařilo se vytvořit nemovitost');
    } finally {
      saving = false;
    }
  }

  async function handlePropertyDelete() {
    if (!selectedProperty) return;
    saving = true;
    try {
      await propertiesApi.delete(selectedProperty.id);
      showDeleteConfirm = false;
      toast.success('Nemovitost smazána');
      await projectContext.reload();
    } catch (err) {
      toast.error('Nepodařilo se smazat nemovitost');
    } finally {
      saving = false;
    }
  }

  function openMemberModal() {
    memberEmail = '';
    memberRole = 'viewer';
    memberErrors = {};
    showMemberModal = true;
  }

  async function handleAddMember() {
    memberErrors = {};
    if (!memberEmail.trim()) {
      memberErrors.email = 'Email je povinný';
      return;
    }

    saving = true;
    try {
      // TODO: Fix projectsApi.addMember
      toast.info('Funkce pozvánky bude brzy dostupná');
      showMemberModal = false;
    } catch (err) {
      toast.error('Nepodařilo se odeslat pozvánku');
    } finally {
      saving = false;
    }
  }

  function getRoleBadge(role: string) {
    switch (role) {
      case 'owner': return { variant: 'primary' as const, label: 'Vlastník' };
      case 'editor': return { variant: 'default' as const, label: 'Editor' };
      case 'viewer': return { variant: 'secondary' as const, label: 'Čtenář' };
      default: return { variant: 'secondary' as const, label: role };
    }
  }

  function formatCost(cost: number): string {
    return new Intl.NumberFormat('cs-CZ', { style: 'currency', currency: 'CZK', maximumFractionDigits: 0 }).format(cost);
  }

  const canEdit = $derived(project?.myRole === 'owner' || project?.myRole === 'editor');
</script>

{#if project}
  <div class="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
    <h1 class="text-2xl font-semibold">
      Dashboard — {project.name}
    </h1>
    <div class="flex items-center gap-2">
      {#if canEdit}
        <Button onclick={() => goto(`/projects/${projectId}/zaznamy/new`)}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Nový záznam
          {/snippet}
        </Button>
      {/if}
      {#if allProjects.length > 1}
        <div class="relative">
          <Button variant="secondary" onclick={() => showProjectSelector = !showProjectSelector}>
            {#snippet children()}
              Změnit projekt
              <ChevronDown class="h-4 w-4" />
            {/snippet}
          </Button>
          {#if showProjectSelector}
            <div class="absolute right-0 top-full z-50 mt-1 min-w-48 rounded-xl border border-border bg-surface p-1 shadow-lg">
              {#each allProjects.filter(p => p.id !== projectId) as proj}
                <button
                  class="w-full rounded-lg px-3 py-2 text-left text-sm hover:bg-bg-secondary"
                  onclick={() => { showProjectSelector = false; goto(`/projects/${proj.id}`); }}
                >
                  {proj.name}
                </button>
              {/each}
            </div>
          {/if}
        </div>
      {/if}
    </div>
  </div>

  <!-- Stats -->
  <div class="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
    <a href={`/projects/${projectId}/properties`}>
      <Card hover>
        <div class="flex items-center gap-4">
          <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-primary-50 dark:bg-primary-950">
            <Building2 class="h-6 w-6 text-primary" />
          </div>
          <div>
            <p class="text-2xl font-semibold">{project.properties.length}</p>
            <p class="text-sm text-foreground-muted">Nemovitosti</p>
          </div>
        </div>
      </Card>
    </a>

    <a href={`/projects/${projectId}/zaznamy`}>
      <Card hover>
        <div class="flex items-center gap-4">
          <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-50 dark:bg-blue-950">
            <FileText class="h-6 w-6 text-blue-500" />
          </div>
          <div>
            <p class="text-2xl font-semibold">{totalZaznamy}</p>
            <p class="text-sm text-foreground-muted">Záznamy</p>
          </div>
        </div>
      </Card>
    </a>

    <Card>
      <div class="flex items-center gap-4">
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-50 dark:bg-amber-950">
          <TrendingUp class="h-6 w-6 text-amber-500" />
        </div>
        <div>
          <p class="text-2xl font-semibold">{formatCost(totalCost)}</p>
          <p class="text-sm text-foreground-muted">Celkové náklady</p>
        </div>
      </div>
    </Card>

    <Card>
      <div class="flex items-center gap-4">
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-50 dark:bg-blue-950">
          {#if syncMode === 'synced'}
            <Cloud class="h-6 w-6 text-blue-500" />
          {:else}
            <CloudOff class="h-6 w-6 text-foreground-muted" />
          {/if}
        </div>
        <div>
          <p class="text-2xl font-semibold">{syncMode === 'synced' ? 'Cloud' : 'Lokálně'}</p>
          <p class="text-sm text-foreground-muted">Synchronizace</p>
        </div>
      </div>
    </Card>
  </div>

  <div class="grid gap-6 lg:grid-cols-3">
    <!-- Properties -->
    <div class="lg:col-span-2">
      <div class="mb-4 flex items-center justify-between">
        <h2 class="text-lg font-semibold">Nemovitosti</h2>
        {#if canEdit}
          <Button size="sm" variant="secondary" onclick={() => openPropertyModal()}>
            {#snippet children()}
              <Plus class="h-4 w-4" />
              Přidat
            {/snippet}
          </Button>
        {/if}
      </div>
      {#if project.properties.length === 0}
        <EmptyState
          icon={Building2}
          title="Žádné nemovitosti"
          description="Přidejte první nemovitost do tohoto projektu"
        >
          {#snippet action()}
            {#if canEdit}
              <Button onclick={() => openPropertyModal()}>
                {#snippet children()}
                  <Plus class="h-4 w-4" />
                  Přidat nemovitost
                {/snippet}
              </Button>
            {/if}
          {/snippet}
        </EmptyState>
      {:else}
        <div class="space-y-3">
          {#each project.properties as property (property.id)}
            <Card hover class="group cursor-pointer" onclick={() => goto(`/projects/${projectId}/properties/${property.id}`)}>
              <div class="flex items-start gap-4">
                <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-blue-50 dark:bg-blue-950">
                  <Home class="h-6 w-6 text-blue-500" />
                </div>
                <div class="min-w-0 flex-1">
                  <div class="flex items-start justify-between">
                    <div>
                      <h3 class="font-semibold">{property.name}</h3>
                      {#if property.description}
                        <p class="mt-1 line-clamp-1 text-sm text-foreground-muted">{property.description}</p>
                      {/if}
                    </div>
                    {#if canEdit}
                      <button
                        class="rounded p-1 opacity-0 transition-opacity hover:bg-bg-secondary group-hover:opacity-100"
                        onclick={(e) => { e.stopPropagation(); openPropertyModal(property); }}
                        aria-label="Upravit"
                      >
                        <Pencil class="h-4 w-4 text-foreground-muted" />
                      </button>
                    {/if}
                  </div>
                  <div class="mt-3 flex flex-wrap items-center gap-4 text-sm text-foreground-muted">
                    <span class="flex items-center gap-1">
                      <Building2 class="h-4 w-4" />
                      {property.unitCount} jednotek
                    </span>
                    <span class="flex items-center gap-1">
                      <FileText class="h-4 w-4" />
                      {property.zaznamCount} záznamů
                    </span>
                    {#if property.totalCost > 0}
                      <span class="font-medium text-foreground">
                        {formatCost(property.totalCost)}
                      </span>
                    {/if}
                  </div>
                </div>
              </div>
            </Card>
          {/each}
        </div>
      {/if}
    </div>

    <!-- Sidebar -->
    <div class="space-y-6">
      <!-- Sync Settings -->
      <div>
        <h2 class="mb-4 text-lg font-semibold">Synchronizace</h2>
        <Card>
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
                <p class="font-medium">
                  {syncMode === 'synced' ? 'Synchronizováno' : 'Pouze lokálně'}
                </p>
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
      </div>

      <!-- Members -->
      <div>
        <div class="mb-4 flex items-center justify-between">
          <h2 class="text-lg font-semibold">Členové</h2>
          {#if project?.myRole === 'owner'}
            <Button size="sm" variant="secondary" onclick={openMemberModal}>
              {#snippet children()}
                <UserPlus class="h-4 w-4" />
              {/snippet}
            </Button>
          {/if}
        </div>
        <Card>
          <div class="divide-y divide-border">
            {#each project.members as member (member.userId)}
              {@const roleBadge = getRoleBadge(member.role)}
              <div class="flex items-center gap-3 py-3 first:pt-0 last:pb-0">
                <Avatar name={member.displayName} src={member.avatarUrl} size="sm" />
                <div class="min-w-0 flex-1">
                  <p class="truncate font-medium">{member.displayName}</p>
                  <p class="truncate text-sm text-foreground-muted">{member.email}</p>
                </div>
                <Badge size="sm" variant={roleBadge.variant}>{roleBadge.label}</Badge>
              </div>
            {/each}
          </div>
        </Card>
      </div>
    </div>
  </div>

  <!-- Property Modal -->
  <Modal bind:open={showPropertyModal} title={selectedProperty ? 'Upravit nemovitost' : 'Nová nemovitost'}>
    <form onsubmit={(e) => { e.preventDefault(); handlePropertySave(); }} class="space-y-4">
      <Input
        label="Název"
        placeholder="Např. Byt Praha 5"
        bind:value={propName}
        error={propErrors.name}
      />
      <Textarea
        label="Popis (volitelné)"
        placeholder="Adresa, poznámky..."
        bind:value={propDescription}
        rows={3}
      />
      <div class="flex justify-between pt-2">
        {#if selectedProperty && project?.myRole === 'owner'}
          <Button variant="danger" onclick={() => { showPropertyModal = false; showDeleteConfirm = true; }}>
            {#snippet children()}
              <Trash2 class="h-4 w-4" />
              Smazat
            {/snippet}
          </Button>
        {:else}
          <div></div>
        {/if}
        <div class="flex gap-3">
          <Button variant="secondary" onclick={() => showPropertyModal = false}>
            {#snippet children()}Zrušit{/snippet}
          </Button>
          <Button type="submit" loading={saving}>
            {#snippet children()}{selectedProperty ? 'Uložit' : 'Vytvořit'}{/snippet}
          </Button>
        </div>
      </div>
    </form>
  </Modal>

  <!-- Delete Confirmation -->
  <ConfirmDialog
    bind:open={showDeleteConfirm}
    title="Smazat nemovitost?"
    message="Tato akce je nevratná. Všechny jednotky a záznamy budou smazány."
    confirmText="Smazat"
    onconfirm={handlePropertyDelete}
  />

  <!-- Member Modal -->
  <Modal bind:open={showMemberModal} title="Přidat člena">
    <form onsubmit={(e) => { e.preventDefault(); handleAddMember(); }} class="space-y-4">
      <Input
        type="email"
        label="Email"
        placeholder="email@example.com"
        bind:value={memberEmail}
        error={memberErrors.email}
      />
      <div>
        <label class="mb-2 block text-sm font-medium">Role</label>
        <div class="flex gap-3">
          <label class="flex cursor-pointer items-center gap-2">
            <input type="radio" bind:group={memberRole} value="viewer" class="accent-primary" />
            <span>Čtenář</span>
          </label>
          <label class="flex cursor-pointer items-center gap-2">
            <input type="radio" bind:group={memberRole} value="editor" class="accent-primary" />
            <span>Editor</span>
          </label>
        </div>
      </div>
      <div class="flex justify-end gap-3 pt-2">
        <Button variant="secondary" onclick={() => showMemberModal = false}>
          {#snippet children()}Zrušit{/snippet}
        </Button>
        <Button type="submit" loading={saving}>
          {#snippet children()}Poslat pozvánku{/snippet}
        </Button>
      </div>
    </form>
  </Modal>

  <!-- Disable Sync Dialog -->
  <DisableSyncDialog
    bind:open={showDisableSyncDialog}
    projectName={project?.name ?? ''}
    onConfirm={handleDisableSync}
    onCancel={() => showDisableSyncDialog = false}
  />
{/if}
