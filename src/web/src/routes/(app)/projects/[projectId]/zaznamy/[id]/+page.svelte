<script lang="ts">
  import { PageHeader, Card, Button, Badge, Spinner, Input, Textarea, ConfirmDialog } from '$lib';
  import { zaznamyApi, type ZaznamDetailDto } from '$lib/api';
  import { toast } from '$lib/stores/ui.svelte';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { Calendar, DollarSign, Pencil, Trash2, Check } from 'lucide-svelte';

  const projectId = $derived($page.params.projectId ?? '');
  const zaznamId = $derived($page.params.id ?? '');

  let zaznam = $state<ZaznamDetailDto | null>(null);
  let loading = $state(true);
  let editing = $state(false);
  let saving = $state(false);
  let showDeleteConfirm = $state(false);

  let title = $state('');
  let description = $state('');
  let date = $state('');
  let cost = $state('');

  onMount(async () => {
    await loadZaznam();
  });

  async function loadZaznam() {
    loading = true;
    try {
      zaznam = await zaznamyApi.get(zaznamId);
      title = zaznam.title;
      description = zaznam.description ?? '';
      date = zaznam.date;
      cost = zaznam.cost?.toString() ?? '';
    } catch (err) {
      toast.error('Nepodařilo se načíst záznam');
      goto(`/projects/${projectId}/zaznamy`);
    } finally {
      loading = false;
    }
  }

  async function handleSave() {
    saving = true;
    try {
      await zaznamyApi.update(zaznamId, {
        title: title.trim(),
        description: description.trim() || undefined,
        date,
        cost: cost ? parseFloat(cost) : undefined
      });
      toast.success('Záznam uložen');
      editing = false;
      await loadZaznam();
    } catch (err) {
      toast.error('Nepodařilo se uložit záznam');
    } finally {
      saving = false;
    }
  }

  async function handleComplete() {
    saving = true;
    try {
      await zaznamyApi.complete(zaznamId);
      toast.success('Záznam dokončen');
      await loadZaznam();
    } catch (err) {
      toast.error('Nepodařilo se dokončit záznam');
    } finally {
      saving = false;
    }
  }

  async function handleDelete() {
    saving = true;
    try {
      await zaznamyApi.delete(zaznamId);
      toast.success('Záznam smazán');
      goto(`/projects/${projectId}/zaznamy`);
    } catch (err) {
      toast.error('Nepodařilo se smazat záznam');
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
</script>

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else if zaznam}
  <PageHeader title={zaznam.title || 'Bez názvu'}>
    {#snippet actions()}
      {#if zaznam.status === 'draft'}
        <Button variant="secondary" onclick={handleComplete}>
          {#snippet children()}
            <Check class="h-4 w-4" />
            Dokončit
          {/snippet}
        </Button>
      {/if}
      <Button variant="secondary" onclick={() => editing = !editing}>
        {#snippet children()}
          <Pencil class="h-4 w-4" />
          {editing ? 'Zrušit' : 'Upravit'}
        {/snippet}
      </Button>
    {/snippet}
  </PageHeader>

  <div class="mx-auto max-w-2xl">
    {#if editing}
      <Card>
        <form onsubmit={(e) => { e.preventDefault(); handleSave(); }} class="space-y-4">
          <Input label="Název" bind:value={title} />
          <Textarea label="Popis" bind:value={description} rows={4} />
          <div class="grid gap-4 sm:grid-cols-2">
            <Input type="date" label="Datum" bind:value={date} />
            <Input type="number" label="Náklady (Kč)" bind:value={cost} />
          </div>
          <div class="flex justify-between pt-4">
            <Button variant="danger" onclick={() => showDeleteConfirm = true}>
              {#snippet children()}
                <Trash2 class="h-4 w-4" />
                Smazat
              {/snippet}
            </Button>
            <Button type="submit" loading={saving}>
              {#snippet children()}Uložit{/snippet}
            </Button>
          </div>
        </form>
      </Card>
    {:else}
      <Card>
        <div class="flex items-center gap-2 mb-4">
          {#if zaznam.status === 'draft'}
            <Badge variant="warning">Rozpracováno</Badge>
          {:else}
            <Badge variant="success">Dokončeno</Badge>
          {/if}
        </div>

        <div class="flex items-center gap-4 text-sm text-foreground-muted mb-4">
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
        </div>

        {#if zaznam.propertyName}
          <p class="text-sm text-foreground-muted mb-2">
            Nemovitost: <span class="font-medium text-foreground">{zaznam.propertyName}</span>
          </p>
        {/if}

        {#if zaznam.description}
          <p class="mt-4 whitespace-pre-wrap">{zaznam.description}</p>
        {/if}
      </Card>
    {/if}
  </div>

  <ConfirmDialog
    bind:open={showDeleteConfirm}
    title="Smazat záznam?"
    message="Tato akce je nevratná."
    confirmText="Smazat"
    onconfirm={handleDelete}
  />
{/if}
