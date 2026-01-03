<script lang="ts">
  import { PageHeader, Card, Button, EmptyState, Badge, Modal, Input, Textarea, ConfirmDialog, Avatar, Toggle, SyncBadge, DisableSyncDialog } from '$lib';
  import { propertiesApi, unitsApi, projectsApi, type ProjectDetailDto, type PropertyDto, type ProjectDto, type PropertyType, type UnitType } from '$lib/api';
  import { localProjectsApi, type ProjectDtoWithSync } from '$lib/api/local/projects';
  import { toast } from '$lib/stores/ui.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { sync } from '$lib/stores/sync.svelte';
  import type { SyncMode, SyncStatus } from '$lib/db';
  import { getContext, onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import {
    Plus, Building2, Home, FileText, Pencil, Trash2, UserPlus, Cloud, CloudOff, TrendingUp, ChevronDown,
    Warehouse, Leaf, Hammer, Map, MoreHorizontal
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
  let propType = $state<PropertyType | ''>('');
  let addPresetUnits = $state(false);
  let selectedPresetIds = $state<string[]>([]);
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
      propType = prop.propertyType ?? 'other';
    } else {
      selectedProperty = null;
      propName = '';
      propDescription = '';
      propType = '';
      addPresetUnits = false;
      selectedPresetIds = [];
    }
    propErrors = {};
    showPropertyModal = true;
  }

  const propertyTypeOptions = [
    { value: 'house', label: 'Dům', icon: Home },
    { value: 'apartment', label: 'Byt', icon: Building2 },
    { value: 'garage', label: 'Garáž', icon: Warehouse },
    { value: 'garden', label: 'Zahrada', icon: Leaf },
    { value: 'shed', label: 'Kůlna', icon: Hammer },
    { value: 'land', label: 'Pozemek', icon: Map },
    { value: 'other', label: 'Jiné', icon: MoreHorizontal }
  ] as const;

  const propertyTypeLabels = new Map(
    propertyTypeOptions.map(option => [option.value, option.label])
  );

  function setPropertyType(value: PropertyType) {
    propType = value;
    selectedPresetIds = [];
  }

  function getPropertyTypeLabel(value: string): string {
    return propertyTypeLabels.get(value as PropertyType) ?? value;
  }

  const unitTypeLabels = new Map<UnitType, string>([
    ['room', 'Místnost'],
    ['floor', 'Podlaží'],
    ['cellar', 'Sklep'],
    ['parking', 'Parkovací stání'],
    ['other', 'Jiné']
  ]);

  function getUnitTypeLabel(value: UnitType): string {
    return unitTypeLabels.get(value) ?? value;
  }

  type PresetUnit = { id: string; label: string; unitType: UnitType };
  const presetUnitsByType: Record<PropertyType, PresetUnit[]> = {
    house: [
      { id: 'house-ground', label: 'Přízemí', unitType: 'floor' },
      { id: 'house-upper', label: '1. patro', unitType: 'floor' }
    ],
    apartment: [
      { id: 'apt-living', label: 'Obývák', unitType: 'room' },
      { id: 'apt-bedroom', label: 'Ložnice', unitType: 'room' },
      { id: 'apt-kitchen', label: 'Kuchyně', unitType: 'room' },
      { id: 'apt-bath', label: 'Koupelna', unitType: 'room' }
    ],
    garage: [
      { id: 'garage-bay', label: 'Parkovací stání', unitType: 'parking' }
    ],
    garden: [],
    shed: [],
    land: [],
    other: []
  };

  const presetUnits = $derived(propType ? presetUnitsByType[propType as PropertyType] : []);

  function togglePresetUnit(id: string) {
    if (selectedPresetIds.includes(id)) {
      selectedPresetIds = selectedPresetIds.filter(presetId => presetId !== id);
      return;
    }
    selectedPresetIds = [...selectedPresetIds, id];
  }

  async function handlePropertySave() {
    propErrors = {};
    if (!propName.trim()) {
      propErrors.name = 'Název je povinný';
      return;
    }
    if (!propType) {
      propErrors.type = 'Vyberte typ nemovitosti';
      return;
    }

    saving = true;
    try {
      if (selectedProperty) {
        await propertiesApi.update(selectedProperty.id, {
          name: propName.trim(),
          description: propDescription.trim() || undefined,
          propertyType: propType
        });
        toast.success('Nemovitost upravena');
      } else {
        const created = await propertiesApi.create({
          projectId,
          name: propName.trim(),
          description: propDescription.trim() || undefined,
          propertyType: propType
        });
        if (addPresetUnits && selectedPresetIds.length > 0) {
          const presets = presetUnits.filter(preset => selectedPresetIds.includes(preset.id));
          await Promise.all(presets.map(preset => unitsApi.create({
            propertyId: created.id,
            name: preset.label,
            unitType: preset.unitType
          })));
        }
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
  <PageHeader title={`Dashboard — ${project.name}`}>
    {#snippet actions()}
      {#if allProjects.length > 1}
        <div class="relative">
          <Button variant="ghost" onclick={() => showProjectSelector = !showProjectSelector}>
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
      {#if canEdit}
        <Button variant="ghost" onclick={() => openPropertyModal()}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Nová nemovitost
          {/snippet}
        </Button>
        <Button onclick={() => goto(`/projects/${projectId}/zaznamy/new`)}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Nový záznam
          {/snippet}
        </Button>
      {/if}
    {/snippet}
  </PageHeader>

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
                      <div class="flex items-center gap-2">
                        <h3 class="font-semibold">{property.name}</h3>
                        <Badge size="sm" variant="secondary">{getPropertyTypeLabel(property.propertyType)}</Badge>
                      </div>
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
      <div>
        <label class="mb-2 block text-sm font-medium">Typ nemovitosti</label>
        <div class="grid gap-3 sm:grid-cols-2">
          {#each propertyTypeOptions as option}
            <button
              type="button"
              class={`flex items-center gap-3 rounded-xl border px-3 py-3 text-left transition ${
                propType === option.value
                  ? 'border-primary bg-primary-50 text-primary dark:bg-primary-950'
                  : 'border-border bg-bg-secondary/40 hover:border-primary/50'
              }`}
              onclick={() => setPropertyType(option.value)}
            >
              <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-bg-secondary text-foreground">
                <svelte:component this={option.icon} class="h-5 w-5" />
              </div>
              <div>
                <p class="font-medium">{option.label}</p>
              </div>
            </button>
          {/each}
        </div>
        {#if propErrors.type}
          <p class="mt-2 text-sm text-red-500">{propErrors.type}</p>
        {/if}
      </div>
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
      {#if !selectedProperty}
        <div class="rounded-xl border border-border p-4">
          <div class="flex items-center justify-between gap-4">
            <div>
              <p class="font-medium">Chceš přidat jednotky?</p>
              <p class="text-sm text-foreground-muted">Vyber šablony a vytvoř je rovnou s nemovitostí.</p>
            </div>
            <input type="checkbox" bind:checked={addPresetUnits} class="h-4 w-4 accent-primary" />
          </div>
          {#if addPresetUnits}
            <div class="mt-4 grid gap-2">
              {#if !propType}
                <p class="text-sm text-foreground-muted">Nejprve vyber typ nemovitosti.</p>
              {:else if presetUnits.length === 0}
                <p class="text-sm text-foreground-muted">Pro tento typ nejsou šablony.</p>
              {:else}
                {#each presetUnits as preset}
                  <label class="flex items-center justify-between gap-3 rounded-lg border border-border px-3 py-2">
                    <div class="flex items-center gap-3">
                      <input
                        type="checkbox"
                        checked={selectedPresetIds.includes(preset.id)}
                        onclick={() => togglePresetUnit(preset.id)}
                        class="h-4 w-4 accent-primary"
                      />
                      <span class="text-sm font-medium">{preset.label}</span>
                    </div>
                    <Badge size="sm" variant="secondary">{getUnitTypeLabel(preset.unitType)}</Badge>
                  </label>
                {/each}
              {/if}
            </div>
          {/if}
        </div>
      {/if}
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
