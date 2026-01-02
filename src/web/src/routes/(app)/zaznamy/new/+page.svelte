<script lang="ts">
  import { PageHeader, Card, Button, Input, Textarea, Select, Spinner } from '$lib';
  import { propertiesApi, unitsApi, zaznamyApi, type PropertyDto, type UnitDto } from '$lib/api';
  import { toast } from '$lib/stores/ui.svelte';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { ArrowLeft, Save, FileText } from 'lucide-svelte';

  // Pre-selected from URL params
  const prePropertyId = $derived($page.url.searchParams.get('propertyId'));
  const preUnitId = $derived($page.url.searchParams.get('unitId'));

  let properties = $state<PropertyDto[]>([]);
  let units = $state<UnitDto[]>([]);
  let loading = $state(true);
  let saving = $state(false);

  // Form
  let propertyId = $state('');
  let unitId = $state('');
  let title = $state('');
  let description = $state('');
  let date = $state(new Date().toISOString().split('T')[0]);
  let cost = $state('');
  let status = $state<'draft' | 'complete'>('complete');
  let errors = $state<Record<string, string>>({});

  const propertyOptions = $derived([
    { value: '', label: 'Vyberte nemovitost' },
    ...properties.map(p => ({ value: p.id, label: `${p.name} (${p.projectName})` }))
  ]);

  const unitOptions = $derived([
    { value: '', label: 'Bez jednotky' },
    ...units.map(u => ({ value: u.id, label: u.name }))
  ]);

  const statusOptions = [
    { value: 'complete', label: 'Dokončený' },
    { value: 'draft', label: 'Rozpracovaný (draft)' }
  ];

  onMount(async () => {
    await loadProperties();
    if (prePropertyId) {
      propertyId = prePropertyId;
      await loadUnits();
      if (preUnitId) {
        unitId = preUnitId;
      }
    }
    loading = false;
  });

  async function loadProperties() {
    try {
      const response = await propertiesApi.list();
      properties = response.items;
    } catch (err) {
      toast.error('Nepodařilo se načíst nemovitosti');
    }
  }

  async function loadUnits() {
    if (!propertyId) {
      units = [];
      unitId = '';
      return;
    }
    try {
      const response = await unitsApi.list({ propertyId });
      units = response.items;
    } catch (err) {
      toast.error('Nepodařilo se načíst jednotky');
    }
  }

  $effect(() => {
    if (propertyId) {
      loadUnits();
    }
  });

  async function handleSubmit() {
    errors = {};

    if (!propertyId) {
      errors.propertyId = 'Vyberte nemovitost';
      return;
    }

    if (status === 'complete' && !title.trim()) {
      errors.title = 'Název je povinný pro dokončený záznam';
      return;
    }

    saving = true;
    try {
      const zaznam = await zaznamyApi.create({
        propertyId,
        unitId: unitId || undefined,
        title: title.trim() || undefined,
        description: description.trim() || undefined,
        date,
        cost: cost ? parseInt(cost) : undefined,
        status
      });
      toast.success('Záznam vytvořen');
      goto(`/zaznamy/${zaznam.id}`);
    } catch (err) {
      toast.error('Nepodařilo se vytvořit záznam');
    } finally {
      saving = false;
    }
  }

  function handleSaveAsDraft() {
    status = 'draft';
    handleSubmit();
  }
</script>

<PageHeader title="Nový záznam">
  {#snippet breadcrumb()}
    <a href="/zaznamy" class="flex items-center gap-1 text-sm text-foreground-muted hover:text-foreground">
      <ArrowLeft class="h-4 w-4" />
      Záznamy
    </a>
  {/snippet}
</PageHeader>

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else}
  <div class="mx-auto max-w-2xl">
    <Card>
      <form onsubmit={(e) => { e.preventDefault(); handleSubmit(); }} class="space-y-6">
        <!-- Property & Unit -->
        <div class="grid gap-4 sm:grid-cols-2">
          <Select
            label="Nemovitost"
            options={propertyOptions}
            bind:value={propertyId}
            error={errors.propertyId}
          />
          <Select
            label="Jednotka (volitelné)"
            options={unitOptions}
            bind:value={unitId}
            disabled={!propertyId || units.length === 0}
          />
        </div>

        <!-- Title -->
        <Input
          label="Název"
          placeholder="Např. Výměna kotle"
          bind:value={title}
          error={errors.title}
        />

        <!-- Date & Cost -->
        <div class="grid gap-4 sm:grid-cols-2">
          <Input
            type="date"
            label="Datum"
            bind:value={date}
          />
          <Input
            type="number"
            label="Náklady (Kč)"
            placeholder="0"
            bind:value={cost}
          />
        </div>

        <!-- Description -->
        <Textarea
          label="Popis"
          placeholder="Podrobnější popis záznamu..."
          bind:value={description}
          rows={4}
        />

        <!-- Status -->
        <Select
          label="Stav"
          options={statusOptions}
          bind:value={status}
        />

        <!-- Actions -->
        <div class="flex flex-col gap-3 pt-4 sm:flex-row sm:justify-between">
          <Button variant="secondary" onclick={handleSaveAsDraft}>
            {#snippet children()}
              <FileText class="h-4 w-4" />
              Uložit jako draft
            {/snippet}
          </Button>
          <div class="flex gap-3">
            <Button variant="secondary" onclick={() => goto('/zaznamy')}>
              {#snippet children()}Zrušit{/snippet}
            </Button>
            <Button type="submit" loading={saving}>
              {#snippet children()}
                <Save class="h-4 w-4" />
                Vytvořit záznam
              {/snippet}
            </Button>
          </div>
        </div>
      </form>
    </Card>
  </div>
{/if}
