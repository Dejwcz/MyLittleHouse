<script lang="ts">
  import { onMount } from 'svelte';
  import { PageHeader, Card, Badge, Button, Spinner } from '$lib';
  import { Plus, Building2, FileText, TrendingUp, Clock } from 'lucide-svelte';
  import { unifiedApi } from '$lib/api/unified';
  import type { PropertyDto, ZaznamDto } from '$lib/api/types';
  import { goto } from '$app/navigation';

  let properties = $state<PropertyDto[]>([]);
  let recentZaznamy = $state<ZaznamDto[]>([]);
  let draftCount = $state(0);
  let loading = $state(true);
  let error = $state<string | null>(null);

  const totalCost = $derived(properties.reduce((sum, p) => sum + p.totalCost, 0));
  const totalZaznamCount = $derived(properties.reduce((sum, p) => sum + p.zaznamCount, 0));

  function formatCost(cost: number): string {
    return new Intl.NumberFormat('cs-CZ', { style: 'currency', currency: 'CZK', maximumFractionDigits: 0 }).format(cost);
  }

  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('cs-CZ');
  }

  onMount(async () => {
    try {
      const [propsRes, zaznamyRes, draftsRes] = await Promise.all([
        unifiedApi.properties.list(),
        unifiedApi.zaznamy.list({ pageSize: 5, sort: 'date', order: 'desc' }),
        unifiedApi.zaznamy.getDrafts()
      ]);

      properties = propsRes.items;
      recentZaznamy = zaznamyRes.items;
      draftCount = draftsRes.total;
    } catch (e) {
      error = e instanceof Error ? e.message : 'Nepodařilo se načíst data';
      throw e;
    } finally {
      loading = false;
    }
  });
</script>

<PageHeader title="Dashboard" subtitle="Přehled vašeho majetku">
  {#snippet actions()}
    <Button onclick={() => goto('/zaznamy/new')}>
      {#snippet children()}
        <Plus class="h-4 w-4" />
        Nový záznam
      {/snippet}
    </Button>
  {/snippet}
</PageHeader>

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else if error}
  <Card>
    <p class="text-red-600">{error}</p>
  </Card>
{:else}
  <!-- Stats -->
  <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
    <Card>
      <div class="flex items-center gap-4">
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-primary-50 dark:bg-primary-950">
          <Building2 class="h-6 w-6 text-primary" />
        </div>
        <div>
          <p class="text-2xl font-semibold">{properties.length}</p>
          <p class="text-sm text-foreground-muted">Nemovitosti</p>
        </div>
      </div>
    </Card>

    <Card>
      <div class="flex items-center gap-4">
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-50 dark:bg-blue-950">
          <FileText class="h-6 w-6 text-blue-500" />
        </div>
        <div>
          <p class="text-2xl font-semibold">{totalZaznamCount}</p>
          <p class="text-sm text-foreground-muted">Záznamy</p>
        </div>
      </div>
    </Card>

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
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-purple-50 dark:bg-purple-950">
          <Clock class="h-6 w-6 text-purple-500" />
        </div>
        <div>
          <p class="text-2xl font-semibold">{draftCount}</p>
          <p class="text-sm text-foreground-muted">Rozpracované</p>
        </div>
      </div>
    </Card>
  </div>

  <!-- Recent records -->
  <div class="mt-8">
    <div class="mb-4 flex items-center justify-between">
      <h2 class="text-lg font-semibold">Poslední záznamy</h2>
      <a href="/zaznamy" class="text-sm text-primary hover:underline">Zobrazit vše</a>
    </div>

    {#if recentZaznamy.length === 0}
      <Card>
        <p class="text-foreground-muted">Zatím nemáte žádné záznamy.</p>
      </Card>
    {:else}
      <div class="space-y-3">
        {#each recentZaznamy as zaznam (zaznam.id)}
          <a href="/zaznamy/{zaznam.id}">
            <Card hover>
              <div class="flex items-start justify-between">
                <div>
                  <div class="flex items-center gap-2">
                    <p class="font-medium">{zaznam.title || 'Bez názvu'}</p>
                    <Badge variant={zaznam.status === 'complete' ? 'success' : 'primary'}>
                      {zaznam.status === 'complete' ? 'Dokončeno' : 'Draft'}
                    </Badge>
                  </div>
                  <p class="mt-1 text-sm text-foreground-muted">
                    {zaznam.propertyName}{zaznam.unitName ? ` • ${zaznam.unitName}` : ''}
                  </p>
                </div>
                <div class="text-right">
                  {#if zaznam.cost}
                    <p class="font-semibold">{formatCost(zaznam.cost)}</p>
                  {/if}
                  <p class="text-xs text-foreground-muted">{formatDate(zaznam.date)}</p>
                </div>
              </div>
            </Card>
          </a>
        {/each}
      </div>
    {/if}
  </div>

  <!-- Quick actions -->
  <div class="mt-8">
    <h2 class="mb-4 text-lg font-semibold">Rychlé akce</h2>
    <div class="flex flex-wrap gap-3">
      <Button variant="secondary" onclick={() => goto('/projects')}>
        {#snippet children()}
          <Plus class="h-4 w-4" />
          Přidat nemovitost
        {/snippet}
      </Button>
      <Button variant="secondary" onclick={() => goto('/zaznamy/new')}>
        {#snippet children()}
          <FileText class="h-4 w-4" />
          Nový záznam
        {/snippet}
      </Button>
    </div>
  </div>
{/if}
