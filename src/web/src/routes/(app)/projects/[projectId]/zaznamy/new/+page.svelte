<script lang="ts">
  import { PageHeader, Card, Button, Input, Textarea, Select, Spinner } from '$lib';
  import { zaznamyApi, type ProjectDetailDto, type PropertyDto } from '$lib/api';
  import { toast } from '$lib/stores/ui.svelte';
  import { getContext } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';

  const projectId = $derived($page.params.projectId ?? '');
  const propertyIdFromUrl = $derived($page.url.searchParams.get('propertyId'));

  const projectContext = getContext<{ project: ProjectDetailDto | null }>('project');
  const project = $derived(projectContext.project);
  const properties = $derived(project?.properties ?? []);

  let saving = $state(false);
  let title = $state('');
  let description = $state('');
  let propertyId = $state(propertyIdFromUrl ?? '');
  let date = $state(new Date().toISOString().split('T')[0]);
  let cost = $state('');
  let errors = $state<Record<string, string>>({});

  const propertyOptions = $derived([
    { value: '', label: 'Vyberte nemovitost' },
    ...properties.map(p => ({ value: p.id, label: p.name }))
  ]);

  async function handleSubmit() {
    errors = {};

    if (!title.trim()) {
      errors.title = 'Název je povinný';
    }
    if (!propertyId) {
      errors.propertyId = 'Vyberte nemovitost';
    }

    if (Object.keys(errors).length > 0) return;

    saving = true;
    try {
      const zaznam = await zaznamyApi.create({
        propertyId,
        title: title.trim(),
        description: description.trim() || undefined,
        date,
        cost: cost ? parseFloat(cost) : undefined,
        status: 'draft'
      });
      toast.success('Záznam vytvořen');
      goto(`/projects/${projectId}/zaznamy/${zaznam.id}`);
    } catch (err) {
      toast.error('Nepodařilo se vytvořit záznam');
    } finally {
      saving = false;
    }
  }
</script>

<PageHeader title="Nový záznam" />

<div class="mx-auto max-w-2xl">
  <Card>
    <form onsubmit={(e) => { e.preventDefault(); handleSubmit(); }} class="space-y-4">
      <Input
        label="Název"
        placeholder="Co jste dělali?"
        bind:value={title}
        error={errors.title}
      />

      <Select
        label="Nemovitost"
        options={propertyOptions}
        bind:value={propertyId}
        error={errors.propertyId}
      />

      <Textarea
        label="Popis (volitelné)"
        placeholder="Podrobnosti..."
        bind:value={description}
        rows={4}
      />

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

      <div class="flex justify-end gap-3 pt-4">
        <Button variant="secondary" onclick={() => goto(`/projects/${projectId}/zaznamy`)}>
          {#snippet children()}Zrušit{/snippet}
        </Button>
        <Button type="submit" loading={saving}>
          {#snippet children()}Vytvořit záznam{/snippet}
        </Button>
      </div>
    </form>
  </Card>
</div>
