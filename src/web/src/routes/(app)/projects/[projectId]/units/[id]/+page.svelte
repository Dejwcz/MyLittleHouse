<script lang="ts">
  import { PageHeader, Card, Button, Badge, Spinner, EmptyState, ConfirmDialog } from '$lib';
  import { unitsApi, zaznamyApi, mediaApi, type UnitDto, type ZaznamDto, type MediaDto } from '$lib/api';
  import { api } from '$lib/api/client';
  import { localMediaApi } from '$lib/api/local/media';
  import { toast } from '$lib/stores/ui.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { FileText, Calendar, Plus, Layers, Image, Star, ChevronDown, ChevronUp, Trash2 } from 'lucide-svelte';

  const projectId = $derived($page.params.projectId ?? '');
  const unitId = $derived($page.params.id ?? '');

  let unit = $state<UnitDto | null>(null);
  let zaznamy = $state<ZaznamDto[]>([]);
  let mediaItems = $state<MediaDto[]>([]);
  let loading = $state(true);
  let mediaLoading = $state(false);
  let coverUpdating = $state(false);
  let showMediaDeleteConfirm = $state(false);
  let selectedMedia = $state<MediaDto | null>(null);
  let uploading = $state(false);
  let galleryCollapsed = $state(true);
  let photoInput: HTMLInputElement | null = null;
  const canEdit = $derived(auth.isGuest || auth.isAuthenticated);

  interface UploadRequestResponse {
    storageKey: string;
    uploadUrl: string;
  }

  interface UploadConfirmResponse {
    storageKey: string;
    thumbnailUrl?: string;
  }

  onMount(async () => {
    await loadData();
  });

  async function loadData() {
    loading = true;
    mediaLoading = true;
    try {
      const [unitData, zaznamList, mediaList] = await Promise.all([
        unitsApi.get(unitId),
        zaznamyApi.list({ unitId }),
        mediaApi.list('unit', unitId)
      ]);
      unit = unitData;
      zaznamy = zaznamList.items;
      mediaItems = mediaList.items;
    } catch (err) {
      toast.error('Nepodařilo se načíst jednotku');
      goto(`/projects/${projectId}`);
    } finally {
      loading = false;
      mediaLoading = false;
    }
  }

  async function refreshMedia() {
    const mediaList = await mediaApi.list('unit', unitId);
    mediaItems = mediaList.items;
  }

  async function setCover(mediaId?: string) {
    if (!unit) return;
    coverUpdating = true;
    try {
      const updated = await unitsApi.updateCover(unitId, mediaId);
      unit = { ...unit, ...updated };
      toast.success(mediaId ? 'Obálka nastavena' : 'Obálka odebrána');
    } catch (err) {
      toast.error('Nepodařilo se upravit obálku');
    } finally {
      coverUpdating = false;
    }
  }

  function requestMediaDelete(media: MediaDto) {
    selectedMedia = media;
    showMediaDeleteConfirm = true;
  }

  async function handleMediaDelete() {
    if (!selectedMedia) return;
    try {
      if (unit?.coverMediaId === selectedMedia.id) {
        const updated = await unitsApi.updateCover(unitId, undefined);
        unit = { ...unit, ...updated };
      }
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
            ownerType: 'unit',
            ownerId: unitId,
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
            ownerType: 'unit',
            ownerId: unitId,
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

  <div class="mb-6">
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
          description="Přidejte fotky nebo dokumenty k této jednotce."
        />
      {:else}
        <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {#each mediaItems as media (media.id)}
            <Card class="group">
              <div class="relative aspect-[4/3] overflow-hidden rounded-lg bg-bg-secondary">
                {#if canEdit}
                  <button
                    class="absolute right-2 top-2 flex h-8 w-8 items-center justify-center rounded-full border border-dashed border-red-400 bg-white text-red-600 opacity-0 transition-opacity group-hover:opacity-100"
                    onclick={() => requestMediaDelete(media)}
                    aria-label="Smazat fotku"
                  >
                    <Trash2 class="h-4 w-4" />
                  </button>
                {/if}
                {#if media.thumbnailUrl}
                  <img src={media.thumbnailUrl} alt={media.originalFileName ?? 'Media'} class="h-full w-full object-cover" />
                {:else}
                  <div class="flex h-full items-center justify-center text-foreground-muted">
                    <Image class="h-6 w-6" />
                  </div>
                {/if}
              </div>
              <div class="mt-3 flex items-center justify-between gap-3">
                <div class="min-w-0">
                  <p class="truncate text-sm font-medium">{media.originalFileName ?? 'Media'}</p>
                  {#if media.caption}
                    <p class="truncate text-xs text-foreground-muted">{media.caption}</p>
                  {/if}
                </div>
                {#if unit.coverMediaId === media.id}
                  <Button size="sm" variant="secondary" disabled>
                    {#snippet children()}
                      <Star class="h-4 w-4" />
                      Obálka
                    {/snippet}
                  </Button>
                {:else}
                  <Button size="sm" variant="ghost" onclick={() => setCover(media.id)} disabled={coverUpdating}>
                    {#snippet children()}
                      <Star class="h-4 w-4" />
                      Nastavit
                    {/snippet}
                  </Button>
                {/if}
              </div>
            </Card>
          {/each}
        </div>
      {/if}
    {/if}
  </div>

  <ConfirmDialog
    bind:open={showMediaDeleteConfirm}
    title="Smazat fotku?"
    message="Tato akce je nevratná."
    confirmText="Smazat"
    onconfirm={handleMediaDelete}
  />

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
