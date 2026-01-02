<script lang="ts">
  import { PageHeader, Card, Button, Badge, Tabs, EmptyState, Spinner } from '$lib';
  import { unifiedApi } from '$lib/api/unified';
  import type { ZaznamDto, ZaznamQueryParams } from '$lib/api/types';
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { Plus, Search, Filter, FileText } from 'lucide-svelte';

  // Query params from URL
  const propertyIdFromUrl = $derived($page.url.searchParams.get('propertyId'));
  const unitIdFromUrl = $derived($page.url.searchParams.get('unitId'));

  let activeTab = $state('all');
  let searchQuery = $state('');
  let zaznamy = $state<ZaznamDto[]>([]);
  let drafts = $state<ZaznamDto[]>([]);
  let loading = $state(true);
  let error = $state<string | null>(null);
  let total = $state(0);
  let currentPage = $state(1);
  const pageSize = 20;

  const tabs = $derived([
    { id: 'all', label: 'Všechny', badge: total },
    { id: 'drafts', label: 'Rozpracované', badge: drafts.length },
    { id: 'completed', label: 'Dokončené' }
  ]);

  onMount(async () => {
    await loadData();
  });

  async function loadData() {
    loading = true;
    error = null;
    try {
      const params: ZaznamQueryParams = {
        page: currentPage,
        pageSize,
        propertyId: propertyIdFromUrl ?? undefined,
        unitId: unitIdFromUrl ?? undefined
      };

      if (activeTab === 'drafts') {
        params.status = 'draft';
      } else if (activeTab === 'completed') {
        params.status = 'complete';
      }

      if (searchQuery.trim()) {
        params.search = searchQuery.trim();
      }

      const [allResponse, draftsResponse] = await Promise.all([
        unifiedApi.zaznamy.list(params),
        unifiedApi.zaznamy.getDrafts()
      ]);

      zaznamy = allResponse.items;
      total = allResponse.total;
      drafts = draftsResponse.items;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Nepodařilo se načíst záznamy';
      throw err;
    } finally {
      loading = false;
    }
  }

  function handleTabChange() {
    currentPage = 1;
    loadData();
  }

  function handleSearch() {
    currentPage = 1;
    loadData();
  }

  function formatCost(cost: number): string {
    return new Intl.NumberFormat('cs-CZ', { style: 'currency', currency: 'CZK', maximumFractionDigits: 0 }).format(cost);
  }

  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('cs-CZ');
  }

  $effect(() => {
    if (activeTab) {
      handleTabChange();
    }
  });
</script>

<PageHeader title="Záznamy" subtitle="Všechny vaše záznamy a opravy">
  {#snippet actions()}
    <Button onclick={() => goto('/zaznamy/new')}>
      {#snippet children()}
        <Plus class="h-4 w-4" />
        Nový záznam
      {/snippet}
    </Button>
  {/snippet}
</PageHeader>

<!-- Search and filters -->
<div class="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
  <form
    class="relative flex-1 sm:max-w-xs"
    onsubmit={(e) => { e.preventDefault(); handleSearch(); }}
  >
    <Search class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-foreground-muted" />
    <input
      type="search"
      placeholder="Hledat záznamy..."
      bind:value={searchQuery}
      class="w-full rounded-2xl border border-border bg-surface py-2.5 pl-10 pr-4 text-sm placeholder:text-foreground-muted focus:border-border-focus focus:outline-none focus:ring-1 focus:ring-border-focus"
    />
  </form>
  <Button variant="secondary">
    {#snippet children()}
      <Filter class="h-4 w-4" />
      Filtry
    {/snippet}
  </Button>
</div>

<!-- Tabs -->
<Tabs items={tabs} bind:active={activeTab} class="mb-6" />

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else if error}
  <Card>
    <p class="text-red-600">{error}</p>
  </Card>
{:else if zaznamy.length === 0}
  <EmptyState
    icon={FileText}
    title="Žádné záznamy"
    description={searchQuery ? 'Zkuste změnit vyhledávání' : 'Vytvořte svůj první záznam'}
  >
    {#snippet action()}
      {#if !searchQuery}
        <Button onclick={() => goto('/zaznamy/new')}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Vytvořit záznam
          {/snippet}
        </Button>
      {/if}
    {/snippet}
  </EmptyState>
{:else}
  <!-- Records list -->
  <div class="space-y-3">
    {#each zaznamy as zaznam (zaznam.id)}
      <Card hover class="cursor-pointer" onclick={() => goto(`/zaznamy/${zaznam.id}`)}>
        <div class="flex items-start justify-between gap-4">
          <div class="flex items-start gap-4">
            {#if zaznam.thumbnailUrl}
              <img
                src={zaznam.thumbnailUrl}
                alt=""
                class="h-12 w-12 shrink-0 rounded-xl object-cover"
              />
            {:else}
              <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-bg-secondary">
                <FileText class="h-6 w-6 text-foreground-muted" />
              </div>
            {/if}
            <div class="min-w-0">
              <div class="flex items-center gap-2">
                <p class="font-medium">{zaznam.title || 'Bez názvu'}</p>
                {#if zaznam.status === 'draft'}
                  <Badge variant="warning">Draft</Badge>
                {/if}
              </div>
              <p class="mt-1 text-sm text-foreground-muted">
                {zaznam.propertyName}
                {#if zaznam.unitName}
                  &bull; {zaznam.unitName}
                {/if}
              </p>
              {#if zaznam.tags.length > 0}
                <div class="mt-2 flex flex-wrap gap-1">
                  {#each zaznam.tags.slice(0, 3) as tag}
                    <Badge size="sm" variant="secondary">{tag}</Badge>
                  {/each}
                  {#if zaznam.tags.length > 3}
                    <Badge size="sm" variant="secondary">+{zaznam.tags.length - 3}</Badge>
                  {/if}
                </div>
              {/if}
            </div>
          </div>
          <div class="shrink-0 text-right">
            {#if zaznam.cost}
              <p class="font-semibold">{formatCost(zaznam.cost)}</p>
            {/if}
            <p class="text-sm text-foreground-muted">
              {formatDate(zaznam.date)}
            </p>
            {#if zaznam.documentCount > 0 || zaznam.commentCount > 0}
              <p class="mt-1 text-xs text-foreground-muted">
                {#if zaznam.documentCount > 0}{zaznam.documentCount} dok.{/if}
                {#if zaznam.documentCount > 0 && zaznam.commentCount > 0}, {/if}
                {#if zaznam.commentCount > 0}{zaznam.commentCount} kom.{/if}
              </p>
            {/if}
          </div>
        </div>
      </Card>
    {/each}
  </div>

  <!-- Pagination could go here -->
  {#if total > pageSize}
    <div class="mt-6 flex justify-center gap-2">
      <Button
        variant="secondary"
        disabled={currentPage === 1}
        onclick={() => { currentPage--; loadData(); }}
      >
        {#snippet children()}Předchozí{/snippet}
      </Button>
      <span class="flex items-center px-4 text-sm text-foreground-muted">
        Strana {currentPage} z {Math.ceil(total / pageSize)}
      </span>
      <Button
        variant="secondary"
        disabled={currentPage >= Math.ceil(total / pageSize)}
        onclick={() => { currentPage++; loadData(); }}
      >
        {#snippet children()}Další{/snippet}
      </Button>
    </div>
  {/if}
{/if}
