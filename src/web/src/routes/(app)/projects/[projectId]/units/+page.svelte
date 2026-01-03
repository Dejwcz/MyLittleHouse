<script lang="ts">
  import { PageHeader, Card, Button, EmptyState, Spinner } from '$lib';
  import { unitsApi, type ProjectDetailDto, type UnitDto } from '$lib/api';
  import { toast } from '$lib/stores/ui.svelte';
  import { getContext } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { Layers, ChevronRight } from 'lucide-svelte';

  const projectId = $derived($page.params.projectId ?? '');

  const projectContext = getContext<{ project: ProjectDetailDto | null }>('project');
  const project = $derived(projectContext.project);
  const properties = $derived(project?.properties ?? []);

  let units = $state<(UnitDto & { propertyName: string })[]>([]);
  let loading = $state(true);
  let loadedForProperties = $state<string[]>([]);

  const canEdit = $derived(project?.myRole === 'owner' || project?.myRole === 'editor');

  async function loadUnits() {
    if (properties.length === 0) return;

    // Check if we already loaded for these properties
    const propIds = properties.map(p => p.id).sort().join(',');
    if (loadedForProperties.join(',') === propIds) return;

    loading = true;
    try {
      const allUnits: (UnitDto & { propertyName: string })[] = [];

      for (const prop of properties) {
        const response = await unitsApi.list({ propertyId: prop.id });
        const unitsWithProperty = response.items.map(u => ({
          ...u,
          propertyName: prop.name
        }));
        allUnits.push(...unitsWithProperty);
      }

      units = allUnits;
      loadedForProperties = properties.map(p => p.id);
    } catch (err) {
      console.error('Failed to load units:', err);
      toast.error('Nepodařilo se načíst jednotky');
    } finally {
      loading = false;
    }
  }

  // Load when properties are available, or stop loading if project has no properties
  $effect(() => {
    if (project && properties.length === 0) {
      loading = false;
    } else if (properties.length > 0) {
      loadUnits();
    }
  });

  const unitsByProperty = $derived(
    properties.map(property => ({
      property,
      units: units.filter(unit => unit.propertyId === property.id)
    }))
  );
</script>

<PageHeader title="Jednotky" subtitle={`${units.length} jednotek v projektu`} />

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else if properties.length === 0}
  <EmptyState
    icon={Layers}
    title="Žádné jednotky"
    description="Jednotky se vytváří v rámci nemovitostí"
  >
    {#snippet action()}
      <Button onclick={() => goto(`/projects/${projectId}/properties`)}>
        {#snippet children()}
          Přejít na nemovitosti
          <ChevronRight class="h-4 w-4" />
        {/snippet}
      </Button>
    {/snippet}
  </EmptyState>
{:else}
  <div class="space-y-6">
    {#each unitsByProperty as section (section.property.id)}
      <div class="space-y-3">
        <div class="flex items-center justify-between">
          <div>
            <h3 class="text-sm font-semibold">{section.property.name}</h3>
            <p class="text-sm text-foreground-muted">{section.units.length} jednotek</p>
          </div>
          {#if canEdit}
            <Button size="sm" variant="ghost" onclick={() => goto(`/projects/${projectId}/properties/${section.property.id}`)}>
              {#snippet children()}
                Přidat jednotku
                <ChevronRight class="h-4 w-4" />
              {/snippet}
            </Button>
          {/if}
        </div>
        {#if section.units.length === 0}
          <Card>
            <p class="text-sm text-foreground-muted">Zatím žádné jednotky.</p>
          </Card>
        {:else}
          <div class="space-y-3">
            {#each section.units as unit (unit.id)}
              <Card hover class="cursor-pointer" onclick={() => goto(`/projects/${projectId}/units/${unit.id}`)}>
                <div class="flex items-center gap-4">
                  <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-purple-50 dark:bg-purple-950">
                    <Layers class="h-6 w-6 text-purple-500" />
                  </div>
                  <div class="min-w-0 flex-1">
                    <h3 class="font-semibold">{unit.name}</h3>
                    <div class="mt-1 flex items-center gap-3 text-sm text-foreground-muted">
                      {#if unit.childCount > 0}
                        <span class="flex items-center gap-1">
                          <Layers class="h-3 w-3" />
                          {unit.childCount} podjednotek
                        </span>
                      {/if}
                    </div>
                  </div>
                  <ChevronRight class="h-5 w-5 text-foreground-muted" />
                </div>
              </Card>
            {/each}
          </div>
        {/if}
      </div>
    {/each}
  </div>
{/if}
