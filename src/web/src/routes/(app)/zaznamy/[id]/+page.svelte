<script lang="ts">
  import {
    PageHeader, Card, Button, Badge, Spinner, Modal, Input, Textarea, ConfirmDialog, Avatar
  } from '$lib';
  import { zaznamyApi, type ZaznamDetailDto, type CommentDto } from '$lib/api';
  import { auth } from '$lib/stores/auth.svelte';
  import { toast } from '$lib/stores/ui.svelte';
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import {
    ArrowLeft, Calendar, DollarSign, FileText, MessageSquare, Pencil, Trash2,
    Image, Send, CheckCircle
  } from 'lucide-svelte';

  const zaznamId = $derived($page.params.id ?? '');

  let zaznam = $state<ZaznamDetailDto | null>(null);
  let loading = $state(true);
  let showEditModal = $state(false);
  let showDeleteConfirm = $state(false);
  let saving = $state(false);

  // Edit form
  let editTitle = $state('');
  let editDescription = $state('');
  let editCost = $state('');
  let editDate = $state('');
  let editErrors = $state<Record<string, string>>({});

  // Comments
  let newComment = $state('');
  let sendingComment = $state(false);

  onMount(async () => {
    await loadZaznam();
  });

  async function loadZaznam() {
    loading = true;
    try {
      zaznam = await zaznamyApi.get(zaznamId);
    } catch (err) {
      toast.error('Nepodařilo se načíst záznam');
      goto('/zaznamy');
    } finally {
      loading = false;
    }
  }

  function openEditModal() {
    if (!zaznam) return;
    editTitle = zaznam.title ?? '';
    editDescription = zaznam.description ?? '';
    editCost = zaznam.cost?.toString() ?? '';
    editDate = zaznam.date;
    editErrors = {};
    showEditModal = true;
  }

  async function handleEdit() {
    if (!zaznam) return;
    editErrors = {};

    saving = true;
    try {
      const updated = await zaznamyApi.update(zaznamId, {
        title: editTitle.trim() || undefined,
        description: editDescription.trim() || undefined,
        cost: editCost ? parseInt(editCost) : undefined,
        date: editDate
      });
      zaznam = { ...zaznam, ...updated };
      showEditModal = false;
      toast.success('Záznam upraven');
    } catch (err) {
      toast.error('Nepodařilo se upravit záznam');
    } finally {
      saving = false;
    }
  }

  async function handleDelete() {
    saving = true;
    try {
      await zaznamyApi.delete(zaznamId);
      toast.success('Záznam smazán');
      goto('/zaznamy');
    } catch (err) {
      toast.error('Nepodařilo se smazat záznam');
    } finally {
      saving = false;
    }
  }

  async function handleComplete() {
    if (!zaznam) return;
    saving = true;
    try {
      const updated = await zaznamyApi.complete(zaznamId);
      zaznam = { ...zaznam, ...updated };
      toast.success('Záznam dokončen');
    } catch (err) {
      toast.error('Nepodařilo se dokončit záznam');
    } finally {
      saving = false;
    }
  }

  async function handleSendComment() {
    if (!newComment.trim()) return;
    sendingComment = true;
    try {
      const comment = await zaznamyApi.addComment(zaznamId, newComment.trim());
      if (zaznam) {
        zaznam.comments = [...zaznam.comments, comment];
        zaznam.commentCount++;
      }
      newComment = '';
      toast.success('Komentář přidán');
    } catch (err) {
      toast.error('Nepodařilo se přidat komentář');
    } finally {
      sendingComment = false;
    }
  }

  async function handleDeleteComment(commentId: string) {
    try {
      await zaznamyApi.deleteComment(commentId);
      if (zaznam) {
        zaznam.comments = zaznam.comments.filter(c => c.id !== commentId);
        zaznam.commentCount--;
      }
      toast.success('Komentář smazán');
    } catch (err) {
      toast.error('Nepodařilo se smazat komentář');
    }
  }

  function formatCost(cost: number): string {
    return new Intl.NumberFormat('cs-CZ', { style: 'currency', currency: 'CZK', maximumFractionDigits: 0 }).format(cost);
  }

  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('cs-CZ');
  }

  function formatDateTime(dateStr: string): string {
    return new Date(dateStr).toLocaleString('cs-CZ');
  }
</script>

{#if loading}
  <div class="flex items-center justify-center py-12">
    <Spinner size="lg" />
  </div>
{:else if zaznam}
  <PageHeader title={zaznam.title || 'Bez názvu'}>
    {#snippet breadcrumb()}
      <a href="/zaznamy" class="flex items-center gap-1 text-sm text-foreground-muted hover:text-foreground">
        <ArrowLeft class="h-4 w-4" />
        Záznamy
      </a>
    {/snippet}
    {#snippet actions()}
      {#if zaznam?.status === 'draft'}
        <Button variant="secondary" onclick={handleComplete} loading={saving}>
          {#snippet children()}
            <CheckCircle class="h-4 w-4" />
            Dokončit
          {/snippet}
        </Button>
      {/if}
      <Button onclick={openEditModal}>
        {#snippet children()}
          <Pencil class="h-4 w-4" />
          Upravit
        {/snippet}
      </Button>
    {/snippet}
  </PageHeader>

  <div class="mb-4 flex flex-wrap items-center gap-2">
    {#if zaznam.status === 'draft'}
      <Badge variant="warning">Draft</Badge>
    {:else}
      <Badge variant="success">Dokončeno</Badge>
    {/if}
    {#each zaznam.tags as tag}
      <Badge variant="secondary">{tag}</Badge>
    {/each}
  </div>

  <div class="grid gap-6 lg:grid-cols-3">
    <!-- Main content -->
    <div class="lg:col-span-2 space-y-6">
      <!-- Details card -->
      <Card>
        <div class="grid gap-4 sm:grid-cols-2">
          <div>
            <p class="text-sm text-foreground-muted">Nemovitost</p>
            <p class="font-medium">{zaznam.propertyName}</p>
          </div>
          {#if zaznam.unitName}
            <div>
              <p class="text-sm text-foreground-muted">Jednotka</p>
              <p class="font-medium">{zaznam.unitName}</p>
            </div>
          {/if}
          <div>
            <p class="text-sm text-foreground-muted">Datum</p>
            <p class="font-medium">{formatDate(zaznam.date)}</p>
          </div>
          {#if zaznam.cost}
            <div>
              <p class="text-sm text-foreground-muted">Náklady</p>
              <p class="font-semibold text-lg">{formatCost(zaznam.cost)}</p>
            </div>
          {/if}
        </div>

        {#if zaznam.description}
          <div class="mt-4 border-t border-border pt-4">
            <p class="text-sm text-foreground-muted mb-2">Popis</p>
            <p class="whitespace-pre-wrap">{zaznam.description}</p>
          </div>
        {/if}
      </Card>

      <!-- Documents -->
      {#if zaznam.documents.length > 0}
        <div>
          <h2 class="mb-3 text-lg font-semibold">Dokumenty ({zaznam.documents.length})</h2>
          <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {#each zaznam.documents as doc (doc.id)}
              <Card hover class="cursor-pointer">
                {#if doc.thumbnailUrl}
                  <img src={doc.thumbnailUrl} alt="" class="mb-3 h-32 w-full rounded-lg object-cover" />
                {:else}
                  <div class="mb-3 flex h-32 items-center justify-center rounded-lg bg-bg-secondary">
                    <FileText class="h-12 w-12 text-foreground-muted" />
                  </div>
                {/if}
                <p class="truncate text-sm font-medium">{doc.originalFileName || 'Dokument'}</p>
                {#if doc.description}
                  <p class="truncate text-xs text-foreground-muted">{doc.description}</p>
                {/if}
              </Card>
            {/each}
          </div>
        </div>
      {/if}

      <!-- Comments -->
      <div>
        <h2 class="mb-3 text-lg font-semibold">Komentáře ({zaznam.comments.length})</h2>
        <Card>
          {#if zaznam.comments.length > 0}
            <div class="space-y-4 mb-4">
              {#each zaznam.comments as comment (comment.id)}
                <div class="flex gap-3">
                  <Avatar name={comment.author.name} src={comment.author.avatarUrl} size="sm" />
                  <div class="flex-1">
                    <div class="flex items-center gap-2">
                      <span class="font-medium">{comment.author.name}</span>
                      <span class="text-xs text-foreground-muted">{formatDateTime(comment.createdAt)}</span>
                      {#if comment.isEdited}
                        <span class="text-xs text-foreground-muted">(upraveno)</span>
                      {/if}
                      {#if comment.author.id === auth.user?.id}
                        <button
                          class="ml-auto text-foreground-muted hover:text-error"
                          onclick={() => handleDeleteComment(comment.id)}
                        >
                          <Trash2 class="h-4 w-4" />
                        </button>
                      {/if}
                    </div>
                    <p class="mt-1 text-sm">{comment.content}</p>
                  </div>
                </div>
              {/each}
            </div>
          {/if}

          <!-- New comment -->
          <form
            class="flex gap-2"
            onsubmit={(e) => { e.preventDefault(); handleSendComment(); }}
          >
            <input
              type="text"
              placeholder="Napište komentář..."
              bind:value={newComment}
              class="flex-1 rounded-xl border border-border bg-bg px-4 py-2 text-sm focus:border-border-focus focus:outline-none focus:ring-1 focus:ring-border-focus"
            />
            <Button type="submit" loading={sendingComment} disabled={!newComment.trim()}>
              {#snippet children()}
                <Send class="h-4 w-4" />
              {/snippet}
            </Button>
          </form>
        </Card>
      </div>
    </div>

    <!-- Sidebar -->
    <div>
      <Card>
        <h3 class="font-semibold mb-3">Informace</h3>
        <div class="space-y-3 text-sm">
          <div class="flex justify-between">
            <span class="text-foreground-muted">Vytvořil</span>
            <span>{zaznam.createdBy.name}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-foreground-muted">Vytvořeno</span>
            <span>{formatDateTime(zaznam.createdAt)}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-foreground-muted">Upraveno</span>
            <span>{formatDateTime(zaznam.updatedAt)}</span>
          </div>
        </div>
        <div class="mt-4 pt-4 border-t border-border">
          <Button variant="danger" fullWidth onclick={() => showDeleteConfirm = true}>
            {#snippet children()}
              <Trash2 class="h-4 w-4" />
              Smazat záznam
            {/snippet}
          </Button>
        </div>
      </Card>
    </div>
  </div>

  <!-- Edit Modal -->
  <Modal bind:open={showEditModal} title="Upravit záznam">
    <form onsubmit={(e) => { e.preventDefault(); handleEdit(); }} class="space-y-4">
      <Input
        label="Název"
        placeholder="Název záznamu"
        bind:value={editTitle}
        error={editErrors.title}
      />
      <Input
        type="date"
        label="Datum"
        bind:value={editDate}
      />
      <Input
        type="number"
        label="Náklady (Kč)"
        placeholder="0"
        bind:value={editCost}
      />
      <Textarea
        label="Popis"
        placeholder="Podrobný popis..."
        bind:value={editDescription}
        rows={4}
      />
      <div class="flex justify-end gap-3 pt-2">
        <Button variant="secondary" onclick={() => showEditModal = false}>
          {#snippet children()}Zrušit{/snippet}
        </Button>
        <Button type="submit" loading={saving}>
          {#snippet children()}Uložit{/snippet}
        </Button>
      </div>
    </form>
  </Modal>

  <!-- Delete Confirmation -->
  <ConfirmDialog
    bind:open={showDeleteConfirm}
    title="Smazat záznam?"
    message="Tato akce je nevratná. Všechny dokumenty a komentáře budou také smazány."
    confirmText="Smazat"
    onconfirm={handleDelete}
  />
{/if}
