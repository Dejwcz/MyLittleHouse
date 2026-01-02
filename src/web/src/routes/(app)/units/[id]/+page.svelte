<script lang="ts">
  import {
    PageHeader, Card, Button, EmptyState, Badge, Spinner
  } from '$lib';
  import { unitsApi, zaznamyApi, type UnitDto, type ZaznamDto } from '$lib/api';
  import { toast } from '$lib/stores/ui.svelte';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import {
    Plus, ArrowLeft, Calendar, DollarSign, FileText, Layers, ChevronRight
  } from 'lucide-svelte';

  const unitId = $derived($page.params.id ?? '');

  let unit = $state<UnitDto | null>(null);
  let childUnits = $state<UnitDto[]>([]);
  let zaznamy = $state<ZaznamDto[]>([]);
  let loading = $state(true);

  const unitTypeLabels: Record<string, string> = {
    room: 'Místnost',
    flat: 'Byt',
    house: 'Dům',
    garage: 'Garáž',
    garden: 'Zahrada',
    stairs: 'Schody',
    other: 'Jiné'
  };

  onMount(async () => {
    await loadData();
  });

  async function loadData() {
    loading = true;
    try {
      const [unitData, zaznamList] = await Promise.all([
        unitsApi.get(unitId),
        zaznamyApi.list({ unitId, pageSize: 20 })
      ]);
      unit = unitData;
      zaznamy = zaznamList.items;

      // Load child units if any
      if (unit) {
        const children = await unitsApi.list({ propertyId: unit.propertyId, parentUnitId: unitId });
        childUnits = children.items;
      }
    } catch (err) {
      toast.error('Nepodařilo se načíst data');
      goto('/properties');
    } finally {
      loading = false;
    }
  }

  function formatCost(cost: number): string {
    return new Intl.NumberFormat('cs-CZ', { style: 'currency', currency: 'CZK', maximumFractionDigits: 0 }).format(cost);
  }

  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('cs-CZ');
  }
</script>

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else if unit}
  <PageHeader title={unit.name} subtitle={unit.description}>
    {#snippet breadcrumb()}
      <a href={`/properties/${unit?.propertyId}`} class="flex items-center gap-1 text-sm text-foreground-muted hover:text-foreground">
        <ArrowLeft class="h-4 w-4" />
        Zpět na nemovitost
      </a>
    {/snippet}
    {#snippet actions()}
      <Button onclick={() => goto(`/zaznamy/new?unitId=${unitId}&propertyId=${unit?.propertyId}`)}>
        {#snippet children()}
          <Plus class="h-4 w-4" />
          Nový záznam
        {/snippet}
      </Button>
    {/snippet}
  </PageHeader>

  <div class="mb-4">
    <Badge>{unitTypeLabels[unit.unitType] ?? unit.unitType}</Badge>
  </div>

  <!-- Child Units -->
  {#if childUnits.length > 0}
    <div class="mb-6">
      <h2 class="mb-3 text-lg font-semibold">Podjednotky</h2>
      <div class="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
        {#each childUnits as child (child.id)}
          <Card hover class="cursor-pointer" onclick={() => goto(`/units/${child.id}`)}>
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-bg-secondary">
                <Layers class="h-5 w-5 text-foreground-muted" />
              </div>
              <div class="min-w-0 flex-1">
                <h3 class="font-medium">{child.name}</h3>
                <p class="text-sm text-foreground-muted">{child.zaznamCount} záznamů</p>
              </div>
              <ChevronRight class="h-5 w-5 text-foreground-muted" />
            </div>
          </Card>
        {/each}
      </div>
    </div>
  {/if}

  <!-- Zaznamy -->
  <h2 class="mb-3 text-lg font-semibold">Záznamy ({zaznamy.length})</h2>
  {#if zaznamy.length === 0}
    <EmptyState
      icon={FileText}
      title="Žádné záznamy"
      description="Zatím nejsou žádné záznamy pro tuto jednotku"
    >
      {#snippet action()}
        <Button onclick={() => goto(`/zaznamy/new?unitId=${unitId}&propertyId=${unit?.propertyId}`)}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Vytvořit záznam
          {/snippet}
        </Button>
      {/snippet}
    </EmptyState>
  {:else}
    <div class="space-y-2">
      {#each zaznamy as zaznam (zaznam.id)}
        <Card hover class="cursor-pointer" onclick={() => goto(`/zaznamy/${zaznam.id}`)}>
          <div class="flex items-start justify-between gap-4">
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2">
                <h3 class="font-medium">{zaznam.title || 'Bez názvu'}</h3>
                {#if zaznam.status === 'draft'}
                  <Badge size="sm" variant="warning">Draft</Badge>
                {/if}
              </div>
              {#if zaznam.description}
                <p class="mt-1 line-clamp-2 text-sm text-foreground-muted">{zaznam.description}</p>
              {/if}
              <div class="mt-2 flex flex-wrap items-center gap-3 text-sm text-foreground-muted">
                <span class="flex items-center gap-1">
                  <Calendar class="h-4 w-4" />
                  {formatDate(zaznam.date)}
                </span>
                {#if zaznam.cost}
                  <span class="flex items-center gap-1">
                    <DollarSign class="h-4 w-4" />
                    {formatCost(zaznam.cost)}
                  </span>
                {/if}
                {#if zaznam.documentCount > 0}
                  <span>{zaznam.documentCount} dok.</span>
                {/if}
              </div>
              {#if zaznam.tags.length > 0}
                <div class="mt-2 flex flex-wrap gap-1">
                  {#each zaznam.tags as tag}
                    <Badge size="sm" variant="secondary">{tag}</Badge>
                  {/each}
                </div>
              {/if}
            </div>
            {#if zaznam.thumbnailUrl}
              <img
                src={zaznam.thumbnailUrl}
                alt=""
                class="h-16 w-16 shrink-0 rounded-lg object-cover"
              />
            {/if}
          </div>
        </Card>
      {/each}
    </div>
  {/if}
{/if}
