<script lang="ts">
  import {
    PageHeader, Card, Button, EmptyState, Badge, Modal, Input, Textarea,
    Spinner, ConfirmDialog, Select, Toggle, SyncBadge, DisableSyncDialog
  } from '$lib';
  import {
    propertiesApi, unitsApi, zaznamyApi,
    type PropertyDetailDto, type UnitDto, type ZaznamDto
  } from '$lib/api';
  import { localPropertiesApi } from '$lib/api/local/properties';
  import { toast } from '$lib/stores/ui.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { sync } from '$lib/stores/sync.svelte';
  import type { SyncMode, SyncStatus } from '$lib/db';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import {
    Plus, Building2, FileText, Pencil, Trash2,
    Calendar, DollarSign, ChevronRight, Layers
  } from 'lucide-svelte';

  const projectId = $derived($page.params.projectId ?? '');
  const propertyId = $derived($page.params.id ?? '');

  let property = $state<PropertyDetailDto | null>(null);
  let units = $state<UnitDto[]>([]);
  let recentZaznamy = $state<ZaznamDto[]>([]);
  let loading = $state(true);

  let showUnitModal = $state(false);
  let showDeleteConfirm = $state(false);
  let saving = $state(false);
  let selectedUnit = $state<UnitDto | null>(null);
  let showDisableSyncDialog = $state(false);
  let syncModeChanging = $state(false);

  let syncMode = $state<SyncMode>('local-only');
  let syncStatus = $state<SyncStatus>('local');

  let unitName = $state('');
  let unitDescription = $state('');
  let unitType = $state('room');
  let unitParentId = $state('');
  let unitErrors = $state<Record<string, string>>({});

  const unitTypeOptions = [
    { value: 'room', label: 'Místnost' },
    { value: 'flat', label: 'Byt' },
    { value: 'house', label: 'Dům' },
    { value: 'garage', label: 'Garáž' },
    { value: 'garden', label: 'Zahrada' },
    { value: 'stairs', label: 'Schody' },
    { value: 'other', label: 'Jiné' }
  ];

  onMount(async () => {
    await loadData();
  });

  async function loadData() {
    loading = true;
    try {
      const [prop, unitList, zaznamList] = await Promise.all([
        propertiesApi.get(propertyId),
        unitsApi.list({ propertyId }),
        zaznamyApi.list({ propertyId, pageSize: 5 })
      ]);
      property = prop;
      units = unitList.items;
      recentZaznamy = zaznamList.items;
      await loadSyncMode();
    } catch (err) {
      toast.error('Nepodařilo se načíst data');
      goto(`/projects/${projectId}/properties`);
    } finally {
      loading = false;
    }
  }

  async function loadSyncMode() {
    try {
      const localProperty = await localPropertiesApi.get(propertyId);
      syncMode = localProperty.syncMode ?? 'local-only';
      syncStatus = (localProperty.syncStatus as SyncStatus) ?? 'local';
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
      await localPropertiesApi.setSyncMode(propertyId, 'synced');
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
      await localPropertiesApi.setSyncMode(propertyId, 'local-only');
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

  function openUnitModal(unit?: UnitDto) {
    if (unit) {
      selectedUnit = unit;
      unitName = unit.name;
      unitDescription = unit.description ?? '';
      unitType = unit.unitType;
      unitParentId = unit.parentUnitId ?? '';
    } else {
      selectedUnit = null;
      unitName = '';
      unitDescription = '';
      unitType = 'room';
      unitParentId = '';
    }
    unitErrors = {};
    showUnitModal = true;
  }

  async function handleUnitSave() {
    unitErrors = {};
    if (!unitName.trim()) {
      unitErrors.name = 'Název je povinný';
      return;
    }

    saving = true;
    try {
      if (selectedUnit) {
        const updated = await unitsApi.update(selectedUnit.id, {
          name: unitName.trim(),
          description: unitDescription.trim() || undefined,
          unitType: unitType as any,
          parentUnitId: unitParentId || undefined
        });
        units = units.map(u => u.id === updated.id ? updated : u);
        toast.success('Jednotka upravena');
      } else {
        const newUnit = await unitsApi.create({
          propertyId,
          name: unitName.trim(),
          description: unitDescription.trim() || undefined,
          unitType: unitType as any,
          parentUnitId: unitParentId || undefined
        });
        units = [...units, newUnit];
        toast.success('Jednotka vytvořena');
      }
      showUnitModal = false;
    } catch (err) {
      toast.error(selectedUnit ? 'Nepodařilo se upravit jednotku' : 'Nepodařilo se vytvořit jednotku');
    } finally {
      saving = false;
    }
  }

  async function handleUnitDelete() {
    if (!selectedUnit) return;
    saving = true;
    try {
      await unitsApi.delete(selectedUnit.id);
      units = units.filter(u => u.id !== selectedUnit!.id);
      showDeleteConfirm = false;
      toast.success('Jednotka smazána');
    } catch (err) {
      toast.error('Nepodařilo se smazat jednotku');
    } finally {
      saving = false;
    }
  }

  function formatCost(cost: number): string {
    return new Intl.NumberFormat('cs-CZ', { style: 'currency', currency: 'CZK', maximumFractionDigits: 0 }).format(cost);
  }

  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('cs-CZ');
  }

  function getUnitTypeLabel(type: string): string {
    return unitTypeOptions.find(o => o.value === type)?.label ?? type;
  }

  const parentUnitOptions = $derived([
    { value: '', label: 'Žádná (hlavní jednotka)' },
    ...units.filter(u => u.id !== selectedUnit?.id).map(u => ({ value: u.id, label: u.name }))
  ]);

  const canEdit = $derived(property?.myRole === 'owner' || property?.myRole === 'editor');
</script>

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else if property}
  <PageHeader title={property.name} subtitle={property.description}>
    {#snippet actions()}
      {#if canEdit}
        <Button variant="secondary" onclick={() => goto(`/projects/${projectId}/zaznamy/new?propertyId=${propertyId}`)}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Nový záznam
          {/snippet}
        </Button>
        <Button onclick={() => openUnitModal()}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Nová jednotka
          {/snippet}
        </Button>
      {/if}
    {/snippet}
  </PageHeader>

  <!-- Stats -->
  <div class="mb-6 grid gap-4 sm:grid-cols-3">
    <Card>
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 dark:bg-blue-950">
          <Building2 class="h-5 w-5 text-blue-500" />
        </div>
        <div>
          <p class="text-2xl font-semibold">{units.length}</p>
          <p class="text-sm text-foreground-muted">Jednotek</p>
        </div>
      </div>
    </Card>
    <Card>
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-green-50 dark:bg-green-950">
          <FileText class="h-5 w-5 text-green-500" />
        </div>
        <div>
          <p class="text-2xl font-semibold">{property.zaznamCount}</p>
          <p class="text-sm text-foreground-muted">Záznamů</p>
        </div>
      </div>
    </Card>
    <Card>
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-50 dark:bg-amber-950">
          <DollarSign class="h-5 w-5 text-amber-500" />
        </div>
        <div>
          <p class="text-2xl font-semibold">{formatCost(property.totalCost)}</p>
          <p class="text-sm text-foreground-muted">Celkové náklady</p>
        </div>
      </div>
    </Card>
  </div>

  <div class="grid gap-6 lg:grid-cols-3">
    <!-- Units -->
    <div class="lg:col-span-2">
      <h2 class="mb-4 text-lg font-semibold">Jednotky</h2>
      {#if units.length === 0}
        <EmptyState
          icon={Layers}
          title="Žádné jednotky"
          description="Přidejte jednotky pro organizaci záznamů (místnosti, patra...)"
        >
          {#snippet action()}
            {#if canEdit}
              <Button onclick={() => openUnitModal()}>
                {#snippet children()}
                  <Plus class="h-4 w-4" />
                  Přidat jednotku
                {/snippet}
              </Button>
            {/if}
          {/snippet}
        </EmptyState>
      {:else}
        <div class="space-y-2">
          {#each units as unit (unit.id)}
            <Card hover class="group cursor-pointer" onclick={() => goto(`/projects/${projectId}/units/${unit.id}`)}>
              <div class="flex items-center gap-3">
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-bg-secondary">
                  <Layers class="h-5 w-5 text-foreground-muted" />
                </div>
                <div class="min-w-0 flex-1">
                  <div class="flex items-center gap-2">
                    <h3 class="font-medium">{unit.name}</h3>
                    <Badge size="sm" variant="secondary">{getUnitTypeLabel(unit.unitType)}</Badge>
                  </div>
                  <div class="mt-1 flex items-center gap-3 text-sm text-foreground-muted">
                    <span>{unit.zaznamCount} záznamů</span>
                    {#if unit.childCount > 0}
                      <span>{unit.childCount} podjednotek</span>
                    {/if}
                  </div>
                </div>
                {#if canEdit}
                  <button
                    class="rounded p-1 opacity-0 transition-opacity hover:bg-bg-secondary group-hover:opacity-100"
                    onclick={(e) => { e.stopPropagation(); openUnitModal(unit); }}
                    aria-label="Upravit"
                  >
                    <Pencil class="h-4 w-4 text-foreground-muted" />
                  </button>
                {/if}
                <ChevronRight class="h-5 w-5 text-foreground-muted" />
              </div>
            </Card>
          {/each}
        </div>
      {/if}
    </div>

    <!-- Recent Zaznamy -->
    <div>
      <div class="mb-4 flex items-center justify-between">
        <h2 class="text-lg font-semibold">Poslední záznamy</h2>
        <Button size="sm" variant="ghost" onclick={() => goto(`/projects/${projectId}/zaznamy?propertyId=${propertyId}`)}>
          {#snippet children()}Všechny{/snippet}
        </Button>
      </div>
      {#if recentZaznamy.length === 0}
        <Card>
          <p class="text-center text-sm text-foreground-muted">Žádné záznamy</p>
        </Card>
      {:else}
        <div class="space-y-2">
          {#each recentZaznamy as zaznam (zaznam.id)}
            <Card hover class="cursor-pointer" onclick={() => goto(`/projects/${projectId}/zaznamy/${zaznam.id}`)}>
              <h4 class="font-medium">{zaznam.title || 'Bez názvu'}</h4>
              <div class="mt-1 flex items-center gap-3 text-sm text-foreground-muted">
                <span class="flex items-center gap-1">
                  <Calendar class="h-3 w-3" />
                  {formatDate(zaznam.date)}
                </span>
                {#if zaznam.cost}
                  <span>{formatCost(zaznam.cost)}</span>
                {/if}
              </div>
              {#if zaznam.status === 'draft'}
                <Badge size="sm" variant="warning" class="mt-2">Draft</Badge>
              {/if}
            </Card>
          {/each}
        </div>
      {/if}
    </div>
  </div>

  <!-- Unit Modal -->
  <Modal bind:open={showUnitModal} title={selectedUnit ? 'Upravit jednotku' : 'Nová jednotka'}>
    <form onsubmit={(e) => { e.preventDefault(); handleUnitSave(); }} class="space-y-4">
      <Input
        label="Název"
        placeholder="Např. Obývací pokoj"
        bind:value={unitName}
        error={unitErrors.name}
      />
      <Select
        label="Typ"
        options={unitTypeOptions}
        bind:value={unitType}
      />
      <Select
        label="Nadřazená jednotka"
        options={parentUnitOptions}
        bind:value={unitParentId}
      />
      <Textarea
        label="Popis (volitelné)"
        placeholder="Poznámky..."
        bind:value={unitDescription}
        rows={2}
      />
      <div class="flex justify-between pt-2">
        {#if selectedUnit && canEdit}
          <Button variant="danger" onclick={() => { showUnitModal = false; showDeleteConfirm = true; }}>
            {#snippet children()}
              <Trash2 class="h-4 w-4" />
              Smazat
            {/snippet}
          </Button>
        {:else}
          <div></div>
        {/if}
        <div class="flex gap-3">
          <Button variant="secondary" onclick={() => showUnitModal = false}>
            {#snippet children()}Zrušit{/snippet}
          </Button>
          <Button type="submit" loading={saving}>
            {#snippet children()}{selectedUnit ? 'Uložit' : 'Vytvořit'}{/snippet}
          </Button>
        </div>
      </div>
    </form>
  </Modal>

  <ConfirmDialog
    bind:open={showDeleteConfirm}
    title="Smazat jednotku?"
    message="Tato akce je nevratná. Záznamy zůstanou zachovány."
    confirmText="Smazat"
    onconfirm={handleUnitDelete}
  />

  <DisableSyncDialog
    bind:open={showDisableSyncDialog}
    onConfirm={handleDisableSync}
    onCancel={() => showDisableSyncDialog = false}
  />
{/if}
