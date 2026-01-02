<script lang="ts">
  import { PageHeader, Card, Button, EmptyState, Badge, Spinner, Tabs } from '$lib';
  import { unifiedApi } from '$lib/api/unified';
  import type { PropertyDto } from '$lib/api/types';
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';
  import { Home, Building2, FileText, Plus } from 'lucide-svelte';

  let properties = $state<PropertyDto[]>([]);
  let sharedProperties = $state<PropertyDto[]>([]);
  let loading = $state(true);
  let error = $state<string | null>(null);
  let activeTab = $state('mine');

  const tabs = [
    { id: 'mine', label: 'Moje nemovitosti' },
    { id: 'shared', label: 'Sdílené se mnou' }
  ];

  function getRoleBadge(role: string) {
    switch (role) {
      case 'owner': return { variant: 'primary' as const, label: 'Vlastník' };
      case 'editor': return { variant: 'default' as const, label: 'Editor' };
      case 'viewer': return { variant: 'secondary' as const, label: 'Čtenář' };
      default: return { variant: 'secondary' as const, label: role };
    }
  }

  onMount(async () => {
    await loadProperties();
  });

  async function loadProperties() {
    loading = true;
    error = null;
    try {
      const [mine, shared] = await Promise.all([
        unifiedApi.properties.list({ shared: false }),
        unifiedApi.properties.list({ shared: true })
      ]);
      properties = mine.items;
      sharedProperties = shared.items;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Nepodařilo se načíst nemovitosti';
      throw err;
    } finally {
      loading = false;
    }
  }

  function formatCost(cost: number): string {
    return new Intl.NumberFormat('cs-CZ', { style: 'currency', currency: 'CZK', maximumFractionDigits: 0 }).format(cost);
  }

  const displayedProperties = $derived(activeTab === 'mine' ? properties : sharedProperties);
</script>

<PageHeader title="Nemovitosti" subtitle="Přehled všech vašich nemovitostí">
  {#snippet actions()}
    <Button onclick={() => goto('/projects')}>
      {#snippet children()}
        <Plus class="h-4 w-4" />
        Nová nemovitost
      {/snippet}
    </Button>
  {/snippet}
</PageHeader>

<Tabs items={tabs} bind:active={activeTab} class="mb-6" />

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else if error}
  <Card>
    <p class="text-red-600">{error}</p>
  </Card>
{:else if displayedProperties.length === 0}
  <EmptyState
    icon={Home}
    title={activeTab === 'mine' ? 'Žádné nemovitosti' : 'Žádné sdílené nemovitosti'}
    description={activeTab === 'mine'
      ? 'Vytvořte projekt a přidejte do něj nemovitosti'
      : 'Zatím vám nikdo nesdílel žádnou nemovitost'}
  >
    {#snippet action()}
      {#if activeTab === 'mine'}
        <Button onclick={() => goto('/projects')}>
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Vytvořit projekt
          {/snippet}
        </Button>
      {/if}
    {/snippet}
  </EmptyState>
{:else}
  <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
    {#each displayedProperties as property (property.id)}
      {@const roleBadge = getRoleBadge(property.myRole)}
      <Card hover class="cursor-pointer" onclick={() => goto(`/properties/${property.id}`)}>
        <div class="flex items-start justify-between">
          <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-50 dark:bg-blue-950">
            <Home class="h-6 w-6 text-blue-500" />
          </div>
          <Badge variant={roleBadge.variant}>{roleBadge.label}</Badge>
        </div>
        <h3 class="mt-4 font-semibold">{property.name}</h3>
        <p class="mt-1 text-sm text-foreground-muted">{property.projectName}</p>
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
