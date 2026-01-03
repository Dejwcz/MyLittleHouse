<script lang="ts">
  import { PageHeader, Card, Button, EmptyState, Badge, Modal, Input, Textarea, ConfirmDialog } from '$lib';
  import { propertiesApi, unitsApi, type ProjectDetailDto, type PropertyDto, type PropertyType, type UnitType } from '$lib/api';
  import { toast } from '$lib/stores/ui.svelte';
  import { getContext } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { Home, Building2, FileText, Plus, Pencil, Trash2, Warehouse, Leaf, Hammer, Map, MoreHorizontal } from 'lucide-svelte';

  const projectId = $derived($page.params.projectId ?? '');

  const projectContext = getContext<{ project: ProjectDetailDto | null; reload: () => Promise<void> }>('project');
  const project = $derived(projectContext.project);
  const properties = $derived(project?.properties ?? []);

  let showModal = $state(false);
  let showDeleteConfirm = $state(false);
  let saving = $state(false);
  let selectedProperty = $state<PropertyDto | null>(null);

  let propName = $state('');
  let propDescription = $state('');
  let propType = $state<PropertyType | ''>('');
  let addPresetUnits = $state(false);
  let selectedPresetIds = $state<string[]>([]);
  let propErrors = $state<Record<string, string>>({});

  function openModal(prop?: PropertyDto) {
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
    showModal = true;
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

  async function handleSave() {
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
      showModal = false;
      await projectContext.reload();
    } catch (err) {
      toast.error(selectedProperty ? 'Nepodařilo se upravit nemovitost' : 'Nepodařilo se vytvořit nemovitost');
    } finally {
      saving = false;
    }
  }

  async function handleDelete() {
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

  function formatCost(cost: number): string {
    return new Intl.NumberFormat('cs-CZ', { style: 'currency', currency: 'CZK', maximumFractionDigits: 0 }).format(cost);
  }

  const canEdit = $derived(project?.myRole === 'owner' || project?.myRole === 'editor');
</script>

<PageHeader title="Nemovitosti" subtitle={`${properties.length} nemovitostí v projektu`}>
  {#snippet actions()}
    {#if canEdit}
      <Button onclick={() => openModal()}>
        {#snippet children()}
          <Plus class="h-4 w-4" />
          Nová nemovitost
        {/snippet}
      </Button>
    {/if}
  {/snippet}
</PageHeader>

{#if properties.length === 0}
  <EmptyState
    icon={Home}
    title="Žádné nemovitosti"
    description="Přidejte první nemovitost do tohoto projektu"
  >
    {#snippet action()}
      {#if canEdit}
        <Button onclick={() => openModal()}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Přidat nemovitost
          {/snippet}
        </Button>
      {/if}
    {/snippet}
  </EmptyState>
{:else}
  <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
    {#each properties as property (property.id)}
      <Card hover class="group cursor-pointer" onclick={() => goto(`/projects/${projectId}/properties/${property.id}`)}>
        <div class="flex items-start justify-between">
          <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-50 dark:bg-blue-950">
            <Home class="h-6 w-6 text-blue-500" />
          </div>
          {#if canEdit}
            <button
              class="rounded p-1 opacity-0 transition-opacity hover:bg-bg-secondary group-hover:opacity-100"
              onclick={(e) => { e.stopPropagation(); openModal(property); }}
              aria-label="Upravit"
            >
              <Pencil class="h-4 w-4 text-foreground-muted" />
            </button>
          {/if}
        </div>
        <div class="mt-4 flex items-center gap-2">
          <h3 class="font-semibold">{property.name}</h3>
          <Badge size="sm" variant="secondary">{getPropertyTypeLabel(property.propertyType)}</Badge>
        </div>
        {#if property.description}
          <p class="mt-2 line-clamp-2 text-sm text-foreground-muted">{property.description}</p>
        {/if}
        <div class="mt-4 flex flex-wrap items-center gap-3 text-sm text-foreground-muted">
          <span class="flex items-center gap-1">
            <Building2 class="h-4 w-4" />
            {property.unitCount}
          </span>
          <span class="flex items-center gap-1">
            <FileText class="h-4 w-4" />
            {property.zaznamCount}
          </span>
          {#if property.totalCost > 0}
            <span class="ml-auto font-medium text-foreground">
              {formatCost(property.totalCost)}
            </span>
          {/if}
        </div>
      </Card>
    {/each}
  </div>
{/if}

<!-- Property Modal -->
<Modal bind:open={showModal} title={selectedProperty ? 'Upravit nemovitost' : 'Nová nemovitost'}>
  <form onsubmit={(e) => { e.preventDefault(); handleSave(); }} class="space-y-4">
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
        <Button variant="danger" onclick={() => { showModal = false; showDeleteConfirm = true; }}>
          {#snippet children()}
            <Trash2 class="h-4 w-4" />
            Smazat
          {/snippet}
        </Button>
      {:else}
        <div></div>
      {/if}
      <div class="flex gap-3">
        <Button variant="secondary" onclick={() => showModal = false}>
          {#snippet children()}Zrušit{/snippet}
        </Button>
        <Button type="submit" loading={saving}>
          {#snippet children()}{selectedProperty ? 'Uložit' : 'Vytvořit'}{/snippet}
        </Button>
      </div>
    </div>
  </form>
</Modal>

<ConfirmDialog
  bind:open={showDeleteConfirm}
  title="Smazat nemovitost?"
  message="Tato akce je nevratná. Všechny jednotky a záznamy budou smazány."
  confirmText="Smazat"
  onconfirm={handleDelete}
/>
