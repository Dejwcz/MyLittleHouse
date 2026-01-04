<script lang="ts">
  import { PageHeader, Card, Button, Badge, Spinner, Input, Textarea, ConfirmDialog, EmptyState } from '$lib';
  import { mediaApi, zaznamyApi, type ZaznamDetailDto, type MediaDto } from '$lib/api';
  import { api } from '$lib/api/client';
  import { localMediaApi } from '$lib/api/local/media';
  import { toast } from '$lib/stores/ui.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { Calendar, DollarSign, Pencil, Trash2, Check, Image, Plus, ChevronDown, ChevronUp } from 'lucide-svelte';

  const projectId = $derived($page.params.projectId ?? '');
  const zaznamId = $derived($page.params.id ?? '');

  let zaznam = $state<ZaznamDetailDto | null>(null);
  let loading = $state(true);
  let editing = $state(false);
  let saving = $state(false);
  let showDeleteConfirm = $state(false);
  let mediaItems = $state<MediaDto[]>([]);
  let mediaLoading = $state(false);
  let uploading = $state(false);
  let galleryCollapsed = $state(true);
  let photoInput: HTMLInputElement | null = null;
  let showMediaDeleteConfirm = $state(false);
  let selectedMedia = $state<MediaDto | null>(null);

  interface UploadRequestResponse {
    storageKey: string;
    uploadUrl: string;
  }

  interface UploadConfirmResponse {
    storageKey: string;
    thumbnailUrl?: string;
  }

  let title = $state('');
  let description = $state('');
  let date = $state('');
  let cost = $state('');

  onMount(async () => {
    await loadZaznam();
  });

  async function loadZaznam() {
    loading = true;
    mediaLoading = true;
    try {
      zaznam = await zaznamyApi.get(zaznamId);
      title = zaznam.title;
      description = zaznam.description ?? '';
      date = zaznam.date;
      cost = zaznam.cost?.toString() ?? '';
      const mediaList = await mediaApi.list('zaznam', zaznamId);
      mediaItems = mediaList.items;
    } catch (err) {
      toast.error('Nepodařilo se načíst záznam');
      goto(`/projects/${projectId}/zaznamy`);
    } finally {
      loading = false;
      mediaLoading = false;
    }
  }

  async function refreshMedia() {
    const mediaList = await mediaApi.list('zaznam', zaznamId);
    mediaItems = mediaList.items;
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

  function openPhotoPicker() {
    photoInput?.click();
  }

  async function handlePhotoChange(event: Event) {
    const target = event.currentTarget as HTMLInputElement | null;
    const files = target?.files ? Array.from(target.files) : [];
    if (files.length === 0) return;

    const useLocalUpload = auth.isGuest || !auth.isAuthenticated;
    uploading = true;
    try {
      for (const file of files) {
        if (!file.type.startsWith('image/')) {
          toast.error(`Soubor ${file.name} není obrázek.`);
          continue;
        }

        if (useLocalUpload) {
          await localMediaApi.create({
            ownerType: 'zaznam',
            ownerId: zaznamId,
            mediaType: 'photo',
            originalFileName: file.name,
            mimeType: file.type,
            sizeBytes: file.size,
            data: file
          });
        } else {
          const request = await api.post<UploadRequestResponse>('/upload/request', {
            fileName: file.name,
            mimeType: file.type,
            sizeBytes: file.size
          });

          await fetch(request.uploadUrl, {
            method: 'PUT',
            headers: { 'Content-Type': file.type },
            body: file
          });

          const confirm = await api.post<UploadConfirmResponse>('/upload/confirm', {
            storageKey: request.storageKey
          });

          await mediaApi.create({
            ownerType: 'zaznam',
            ownerId: zaznamId,
            storageKey: confirm.storageKey,
            mediaType: 'photo',
            originalFileName: file.name,
            mimeType: file.type,
            sizeBytes: file.size
          });
        }
      }

      await refreshMedia();
      toast.success('Fotky nahrány');
    } catch (err) {
      toast.error('Nepodařilo se nahrát fotky');
    } finally {
      uploading = false;
      if (photoInput) {
        photoInput.value = '';
      }
    }
  }

  function requestMediaDelete(media: MediaDto) {
    selectedMedia = media;
    showMediaDeleteConfirm = true;
  }

  async function handleMediaDelete() {
    if (!selectedMedia) return;
    try {
      await mediaApi.delete(selectedMedia.id);
      await refreshMedia();
      toast.success('Fotka smazána');
    } catch (err) {
      toast.error('Nepodařilo se smazat fotku');
    } finally {
      showMediaDeleteConfirm = false;
      selectedMedia = null;
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

  <div class="mx-auto mb-6 max-w-2xl">
    <div class="mb-3 flex items-center justify-between">
      <button
        type="button"
        class="flex items-center gap-2 text-lg font-semibold text-foreground"
        onclick={() => galleryCollapsed = !galleryCollapsed}
        aria-expanded={!galleryCollapsed}
      >
        Galerie
        {#if galleryCollapsed}
          <ChevronDown class="h-4 w-4 text-foreground-muted" />
        {:else}
          <ChevronUp class="h-4 w-4 text-foreground-muted" />
        {/if}
      </button>
      <div class="flex items-center gap-3">
        {#if mediaItems.length > 0}
          <span class="text-sm text-foreground-muted">{mediaItems.length} položek</span>
        {/if}
        <Button
          size="sm"
          variant="outline"
          class="border-primary text-primary hover:bg-primary-50"
          onclick={openPhotoPicker}
          disabled={uploading}
        >
          {#snippet children()}
            <Plus class="h-4 w-4" />
            Přidat fotky
          {/snippet}
        </Button>
      </div>
    </div>
    <input
      bind:this={photoInput}
      type="file"
      accept="image/*"
      multiple
      class="hidden"
      onchange={handlePhotoChange}
    />
    {#if !galleryCollapsed}
      {#if mediaLoading}
        <div class="flex items-center justify-center py-6">
          <Spinner />
        </div>
      {:else if mediaItems.length === 0}
        <EmptyState
          icon={Image}
          title="Galerie je prázdná"
          description="Přidejte fotky k tomuto záznamu."
        />
      {:else}
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {#each mediaItems as media (media.id)}
            <Card class="group">
              <div class="relative aspect-[4/3] overflow-hidden rounded-lg bg-bg-secondary">
                <button
                  class="absolute right-2 top-2 flex h-8 w-8 items-center justify-center rounded-full border border-dashed border-red-400 bg-white text-red-600 opacity-0 transition-opacity group-hover:opacity-100"
                  onclick={() => requestMediaDelete(media)}
                  aria-label="Smazat fotku"
                >
                  <Trash2 class="h-4 w-4" />
                </button>
                {#if media.thumbnailUrl}
                  <img src={media.thumbnailUrl} alt={media.originalFileName ?? 'Media'} class="h-full w-full object-cover" />
                {:else}
                  <div class="flex h-full items-center justify-center text-foreground-muted">
                    <Image class="h-6 w-6" />
                  </div>
                {/if}
              </div>
              <div class="mt-3">
                <p class="truncate text-sm font-medium">{media.originalFileName ?? 'Media'}</p>
                {#if media.caption}
                  <p class="truncate text-xs text-foreground-muted">{media.caption}</p>
                {/if}
              </div>
            </Card>
          {/each}
        </div>
      {/if}
    {/if}
  </div>

  <ConfirmDialog
    bind:open={showDeleteConfirm}
    title="Smazat záznam?"
    message="Tato akce je nevratná."
    confirmText="Smazat"
    onconfirm={handleDelete}
  />
  <ConfirmDialog
    bind:open={showMediaDeleteConfirm}
    title="Smazat fotku?"
    message="Tato akce je nevratná."
    confirmText="Smazat"
    onconfirm={handleMediaDelete}
  />
{/if}
