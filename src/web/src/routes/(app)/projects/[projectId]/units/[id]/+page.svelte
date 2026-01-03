<script lang="ts">
  import { PageHeader, Card, Button, Badge, Spinner, EmptyState } from '$lib';
  import { unitsApi, zaznamyApi, type UnitDto, type ZaznamDto } from '$lib/api';
  import { toast } from '$lib/stores/ui.svelte';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { FileText, Calendar, Plus, Layers } from 'lucide-svelte';

  const projectId = $derived($page.params.projectId ?? '');
  const unitId = $derived($page.params.id ?? '');

  let unit = $state<UnitDto | null>(null);
  let zaznamy = $state<ZaznamDto[]>([]);
  let loading = $state(true);

  onMount(async () => {
    await loadData();
  });

  async function loadData() {
    loading = true;
    try {
      const [unitData, zaznamList] = await Promise.all([
        unitsApi.get(unitId),
        zaznamyApi.list({ unitId })
      ]);
      unit = unitData;
      zaznamy = zaznamList.items;
    } catch (err) {
      toast.error('Nepodařilo se načíst jednotku');
      goto(`/projects/${projectId}`);
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
    {#snippet actions()}
      <Button onclick={() => goto(`/projects/${projectId}/zaznamy/new?unitId=${unitId}`)}>
        {#snippet children()}
          <Plus class="h-4 w-4" />
          Nový záznam
        {/snippet}
      </Button>
    {/snippet}
  </PageHeader>

  <div class="mb-6 grid gap-4 sm:grid-cols-2">
    <Card>
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 dark:bg-blue-950">
          <Layers class="h-5 w-5 text-blue-500" />
        </div>
        <div>
          <p class="text-2xl font-semibold">{unit.childCount}</p>
          <p class="text-sm text-foreground-muted">Podjednotek</p>
        </div>
      </div>
    </Card>
    <Card>
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-green-50 dark:bg-green-950">
          <FileText class="h-5 w-5 text-green-500" />
        </div>
        <div>
          <p class="text-2xl font-semibold">{zaznamy.length}</p>
          <p class="text-sm text-foreground-muted">Záznamů</p>
        </div>
      </div>
    </Card>
  </div>

  <h2 class="mb-4 text-lg font-semibold">Záznamy</h2>
  {#if zaznamy.length === 0}
    <EmptyState
      icon={FileText}
      title="Žádné záznamy"
      description="Přidejte první záznam k této jednotce"
    >
      {#snippet action()}
        <Button onclick={() => goto(`/projects/${projectId}/zaznamy/new?unitId=${unitId}`)}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Vytvořit záznam
          {/snippet}
        </Button>
      {/snippet}
    </EmptyState>
  {:else}
    <div class="space-y-3">
      {#each zaznamy as zaznam (zaznam.id)}
        <Card hover class="cursor-pointer" onclick={() => goto(`/projects/${projectId}/zaznamy/${zaznam.id}`)}>
          <div class="flex items-center gap-4">
            <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-bg-secondary">
              <FileText class="h-5 w-5 text-foreground-muted" />
            </div>
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2">
                <p class="font-medium">{zaznam.title || 'Bez názvu'}</p>
                {#if zaznam.status === 'draft'}
                  <Badge variant="warning">Draft</Badge>
                {/if}
              </div>
              <div class="mt-1 flex items-center gap-3 text-sm text-foreground-muted">
                <span class="flex items-center gap-1">
                  <Calendar class="h-3 w-3" />
                  {formatDate(zaznam.date)}
                </span>
                {#if zaznam.cost}
                  <span>{formatCost(zaznam.cost)}</span>
                {/if}
              </div>
            </div>
          </div>
        </Card>
      {/each}
    </div>
  {/if}
{/if}
