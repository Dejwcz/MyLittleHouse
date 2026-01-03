<script lang="ts">
  import { PageHeader, Card, Button, EmptyState, Badge, Modal, Input, Textarea, ConfirmDialog } from '$lib';
  import { propertiesApi, type ProjectDetailDto, type PropertyDto } from '$lib/api';
  import { toast } from '$lib/stores/ui.svelte';
  import { getContext } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { Home, Building2, FileText, Plus, Pencil, Trash2 } from 'lucide-svelte';

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
  let propErrors = $state<Record<string, string>>({});

  function openModal(prop?: PropertyDto) {
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
    showModal = true;
  }

  async function handleSave() {
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
        <h3 class="mt-4 font-semibold">{property.name}</h3>
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
