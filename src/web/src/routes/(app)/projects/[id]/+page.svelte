<script lang="ts">
  import { PageHeader, Card, Button, EmptyState, Badge, Modal, Input, Textarea, Spinner, ConfirmDialog, Avatar, Toggle, SyncBadge, DisableSyncDialog } from '$lib';
  import { projectsApi, propertiesApi, type ProjectDetailDto, type PropertyDto } from '$lib/api';
  import { localProjectsApi, type ProjectDtoWithSync } from '$lib/api/local/projects';
  import { toast } from '$lib/stores/ui.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { sync } from '$lib/stores/sync.svelte';
  import type { SyncMode, SyncStatus } from '$lib/db';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import {
    Plus, Building2, Home, MapPin, FileText, Users, ArrowLeft, Pencil, Trash2, UserPlus, Cloud, CloudOff
  } from 'lucide-svelte';

  const projectId = $derived($page.params.id ?? '');

  let project = $state<ProjectDetailDto | null>(null);
  let loading = $state(true);
  let showPropertyModal = $state(false);
  let showEditModal = $state(false);
  let showDeleteConfirm = $state(false);
  let showMemberModal = $state(false);
  let showDisableSyncDialog = $state(false);
  let saving = $state(false);
  let syncModeChanging = $state(false);
  let selectedProperty = $state<PropertyDto | null>(null);

  // Sync mode state - fetch from local DB
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

  onMount(async () => {
    await loadProject();
  });

  async function loadProject() {
    loading = true;
    try {
      project = await projectsApi.get(projectId);
      // Load sync mode from local DB
      await loadSyncMode();
    } catch (err) {
      toast.error('Nepodařilo se načíst projekt');
      goto('/projects');
    } finally {
      loading = false;
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
      // Turning off sync - show confirmation dialog
      showDisableSyncDialog = true;
    } else {
      // Turning on sync
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
      // Trigger sync immediately
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

      if (deleteFromServer) {
        // TODO: Queue delete from server
        toast.success('Synchronizace vypnuta, data budou smazána ze serveru');
      } else {
        toast.success('Synchronizace vypnuta, data zůstala na serveru');
      }
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
        const updated = await propertiesApi.update(selectedProperty.id, {
          name: propName.trim(),
          description: propDescription.trim() || undefined
        });
        if (project) {
          project.properties = project.properties.map(p =>
            p.id === updated.id ? { ...p, ...updated } : p
          );
        }
        toast.success('Nemovitost upravena');
      } else {
        const newProp = await propertiesApi.create({
          projectId,
          name: propName.trim(),
          description: propDescription.trim() || undefined
        });
        if (project) {
          project.properties = [...project.properties, newProp];
          project.propertyCount++;
        }
        toast.success('Nemovitost vytvořena');
      }
      showPropertyModal = false;
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
      if (project) {
        project.properties = project.properties.filter(p => p.id !== selectedProperty!.id);
        project.propertyCount--;
      }
      showDeleteConfirm = false;
      toast.success('Nemovitost smazána');
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
      await projectsApi.addMember(projectId, {
        email: memberEmail.trim(),
        role: memberRole
      });
      showMemberModal = false;
      toast.success('Pozvánka odeslána');
      await loadProject();
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
</script>

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else if project}
  <PageHeader title={project.name} subtitle={project.description}>
    {#snippet breadcrumb()}
      <a href="/projects" class="flex items-center gap-1 text-sm text-foreground-muted hover:text-foreground">
        <ArrowLeft class="h-4 w-4" />
        Projekty
      </a>
    {/snippet}
    {#snippet actions()}
      {#if project?.myRole === 'owner' || project?.myRole === 'editor'}
        <Button onclick={() => openPropertyModal()}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Nová nemovitost
          {/snippet}
        </Button>
      {/if}
    {/snippet}
  </PageHeader>

  <div class="grid gap-6 lg:grid-cols-3">
    <!-- Properties -->
    <div class="lg:col-span-2">
      <h2 class="mb-4 text-lg font-semibold">Nemovitosti</h2>
      {#if project.properties.length === 0}
        <EmptyState
          icon={Building2}
          title="Žádné nemovitosti"
          description="Přidejte první nemovitost do tohoto projektu"
        >
          {#snippet action()}
            {#if project?.myRole === 'owner' || project?.myRole === 'editor'}
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
            <Card hover class="group cursor-pointer" onclick={() => goto(`/properties/${property.id}`)}>
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
                    {#if project?.myRole === 'owner' || project?.myRole === 'editor'}
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
                  {#if syncMode === 'synced'}
                    Data se zálohují na server
                  {:else}
                    Data pouze v tomto zařízení
                  {/if}
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
        {#if selectedProperty && (project?.myRole === 'owner')}
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
