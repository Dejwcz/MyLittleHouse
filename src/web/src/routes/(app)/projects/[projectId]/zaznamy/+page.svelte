<script lang="ts">
  import { PageHeader, Card, Button, Badge, Tabs, EmptyState, Spinner } from '$lib';
  import { zaznamyApi, type ProjectDetailDto } from '$lib/api';
  import type { ZaznamDto, ZaznamQueryParams } from '$lib/api/types';
  import { getContext, onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { Plus, Search, FileText, Calendar } from 'lucide-svelte';

  const projectId = $derived($page.params.projectId ?? '');
  const propertyIdFromUrl = $derived($page.url.searchParams.get('propertyId'));

  const projectContext = getContext<{ project: ProjectDetailDto | null }>('project');
  const project = $derived(projectContext.project);
  const propertyIds = $derived(project?.properties.map(p => p.id) ?? []);

  let activeTab = $state('all');
  let searchQuery = $state('');
  let zaznamy = $state<ZaznamDto[]>([]);
  let loading = $state(true);

  const tabs = [
    { id: 'all', label: 'Všechny' },
    { id: 'drafts', label: 'Rozpracované' },
    { id: 'completed', label: 'Dokončené' }
  ];

  onMount(async () => {
    await loadData();
  });

  async function loadData() {
    loading = true;
    try {
      // Load zaznamy for all properties in project
      const allZaznamy: ZaznamDto[] = [];

      for (const propId of propertyIds) {
        if (propertyIdFromUrl && propId !== propertyIdFromUrl) continue;

        const params: ZaznamQueryParams = {
          propertyId: propId,
          status: activeTab === 'drafts' ? 'draft' : activeTab === 'completed' ? 'complete' : undefined
        };

        const response = await zaznamyApi.list(params);
        allZaznamy.push(...response.items);
      }

      zaznamy = allZaznamy.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
    } catch (err) {
      console.error('Failed to load zaznamy:', err);
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

  $effect(() => {
    if (activeTab && propertyIds.length > 0) {
      loadData();
    }
  });

  const canEdit = $derived(project?.myRole === 'owner' || project?.myRole === 'editor');
</script>

<PageHeader title="Záznamy" subtitle={`${zaznamy.length} záznamů v projektu`}>
  {#snippet actions()}
    {#if canEdit}
      <Button onclick={() => goto(`/projects/${projectId}/zaznamy/new`)}>
        {#snippet children()}
          <Plus class="h-4 w-4" />
          Nový záznam
        {/snippet}
      </Button>
    {/if}
  {/snippet}
</PageHeader>

<div class="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
  <form class="relative flex-1 sm:max-w-xs" onsubmit={(e) => { e.preventDefault(); loadData(); }}>
    <Search class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-foreground-muted" />
    <input
      type="search"
      placeholder="Hledat záznamy..."
      bind:value={searchQuery}
      class="w-full rounded-2xl border border-border bg-surface py-2.5 pl-10 pr-4 text-sm placeholder:text-foreground-muted focus:border-border-focus focus:outline-none focus:ring-1 focus:ring-border-focus"
    />
  </form>
</div>

<Tabs items={tabs} bind:active={activeTab} class="mb-6" />

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else if zaznamy.length === 0}
  <EmptyState
    icon={FileText}
    title="Žádné záznamy"
    description="Vytvořte svůj první záznam"
  >
    {#snippet action()}
      {#if canEdit}
        <Button onclick={() => goto(`/projects/${projectId}/zaznamy/new`)}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Vytvořit záznam
          {/snippet}
        </Button>
      {/if}
    {/snippet}
  </EmptyState>
{:else}
  <div class="space-y-3">
    {#each zaznamy as zaznam (zaznam.id)}
      <Card hover class="cursor-pointer" onclick={() => goto(`/projects/${projectId}/zaznamy/${zaznam.id}`)}>
        <div class="flex items-start gap-4">
          <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-bg-secondary">
            <FileText class="h-6 w-6 text-foreground-muted" />
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
              {#if zaznam.propertyName}
                <span>{zaznam.propertyName}</span>
              {/if}
              {#if zaznam.cost}
                <span class="font-medium text-foreground">{formatCost(zaznam.cost)}</span>
              {/if}
            </div>
          </div>
        </div>
      </Card>
    {/each}
  </div>
{/if}
