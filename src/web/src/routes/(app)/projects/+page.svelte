<script lang="ts">
  import { PageHeader, Card, Button, EmptyState, Badge, Modal, Input, Textarea, Spinner, ConfirmDialog } from '$lib';
  import { unifiedApi } from '$lib/api/unified';
  import type { ProjectDto } from '$lib/api/types';
  import { onMount } from 'svelte';
  import { Plus, Users, Building2, FolderOpen, Pencil, Trash2 } from 'lucide-svelte';
  import { goto } from '$app/navigation';

  let projects = $state<ProjectDto[]>([]);
  let loading = $state(true);
  let error = $state<string | null>(null);
  let showCreateModal = $state(false);
  let showEditModal = $state(false);
  let showDeleteConfirm = $state(false);
  let selectedProject = $state<ProjectDto | null>(null);
  let saving = $state(false);

  // Form state
  let formName = $state('');
  let formDescription = $state('');
  let formErrors = $state<Record<string, string>>({});

  onMount(async () => {
    await loadProjects();
  });

  async function loadProjects() {
    loading = true;
    error = null;
    try {
      const response = await unifiedApi.projects.list();
      projects = response.items;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Nepodařilo se načíst projekty';
      throw err;
    } finally {
      loading = false;
    }
  }

  function openCreateModal() {
    formName = '';
    formDescription = '';
    formErrors = {};
    showCreateModal = true;
  }

  function openEditModal(project: ProjectDto) {
    selectedProject = project;
    formName = project.name;
    formDescription = project.description ?? '';
    formErrors = {};
    showEditModal = true;
  }

  function openDeleteConfirm(project: ProjectDto) {
    selectedProject = project;
    showDeleteConfirm = true;
  }

  async function handleCreate() {
    formErrors = {};
    if (!formName.trim()) {
      formErrors.name = 'Název je povinný';
      return;
    }

    saving = true;
    try {
      const newProject = await unifiedApi.projects.create({
        name: formName.trim(),
        description: formDescription.trim() || undefined
      });
      projects = [...projects, newProject];
      showCreateModal = false;
    } catch (err) {
      formErrors.name = err instanceof Error ? err.message : 'Nepodařilo se vytvořit projekt';
    } finally {
      saving = false;
    }
  }

  async function handleEdit() {
    if (!selectedProject) return;
    formErrors = {};
    if (!formName.trim()) {
      formErrors.name = 'Název je povinný';
      return;
    }

    saving = true;
    try {
      const updated = await unifiedApi.projects.update(selectedProject.id, {
        name: formName.trim(),
        description: formDescription.trim() || undefined
      });
      projects = projects.map(p => p.id === updated.id ? updated : p);
      showEditModal = false;
    } catch (err) {
      formErrors.name = err instanceof Error ? err.message : 'Nepodařilo se upravit projekt';
    } finally {
      saving = false;
    }
  }

  async function handleDelete() {
    if (!selectedProject) return;
    saving = true;
    try {
      await unifiedApi.projects.delete(selectedProject.id);
      projects = projects.filter(p => p.id !== selectedProject!.id);
      showDeleteConfirm = false;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Nepodařilo se smazat projekt';
    } finally {
      saving = false;
    }
  }

  function getRoleBadge(role: string) {
    switch (role) {
      case 'owner': return { variant: 'primary' as const, label: 'Vlastník' };
      case 'editor': return { variant: 'default' as const, label: 'Editor' };
      case 'viewer': return { variant: 'secondary' as const, label: 'Čtenář' };
      default: return { variant: 'secondary' as const, label: role };
    }
  }
</script>

<PageHeader title="Projekty" subtitle="Správa vašich projektů a nemovitostí">
  {#snippet actions()}
    <Button onclick={openCreateModal}>
      {#snippet children()}
        <Plus class="h-4 w-4" />
        Nový projekt
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
{:else if projects.length === 0}
  <EmptyState
    icon={FolderOpen}
    title="Žádné projekty"
    description="Vytvořte svůj první projekt pro správu nemovitostí"
  >
    {#snippet action()}
      <Button onclick={openCreateModal}>
        {#snippet children()}
          <Plus class="h-4 w-4" />
          Vytvořit projekt
        {/snippet}
      </Button>
    {/snippet}
  </EmptyState>
{:else}
  <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
    {#each projects as project (project.id)}
      {@const roleBadge = getRoleBadge(project.myRole)}
      <Card hover class="group cursor-pointer" onclick={() => goto(`/projects/${project.id}`)}>
        <div class="flex items-start justify-between">
          <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-primary-50 dark:bg-primary-950">
            <FolderOpen class="h-6 w-6 text-primary" />
          </div>
          <div class="flex items-center gap-2">
            <Badge variant={roleBadge.variant}>{roleBadge.label}</Badge>
            {#if project.myRole === 'owner'}
              <div class="relative">
                <button
                  class="rounded p-1 opacity-0 transition-opacity hover:bg-bg-secondary group-hover:opacity-100"
                  onclick={(e) => { e.stopPropagation(); openEditModal(project); }}
                  aria-label="Upravit projekt"
                >
                  <Pencil class="h-4 w-4 text-foreground-muted" />
                </button>
              </div>
            {/if}
          </div>
        </div>
        <h3 class="mt-4 font-semibold">{project.name}</h3>
        {#if project.description}
          <p class="mt-1 line-clamp-2 text-sm text-foreground-muted">
            {project.description}
          </p>
        {/if}
        <div class="mt-4 flex items-center gap-4 text-sm text-foreground-muted">
          <span class="flex items-center gap-1">
            <Building2 class="h-4 w-4" />
            {project.propertyCount} {project.propertyCount === 1 ? 'nemovitost' : 'nemovitostí'}
          </span>
          <span class="flex items-center gap-1">
            <Users class="h-4 w-4" />
            {project.memberCount} {project.memberCount === 1 ? 'člen' : 'členů'}
          </span>
        </div>
      </Card>
    {/each}

    <!-- Add new project card -->
    <Card
      class="flex cursor-pointer flex-col items-center justify-center border-dashed text-center hover:border-primary"
      hover
      onclick={openCreateModal}
    >
      <div class="flex h-12 w-12 items-center justify-center rounded-full bg-bg-secondary">
        <Plus class="h-6 w-6 text-foreground-muted" />
      </div>
      <p class="mt-3 font-medium">Vytvořit nový projekt</p>
      <p class="mt-1 text-sm text-foreground-muted">
        Začněte sdílet nemovitosti
      </p>
    </Card>
  </div>
{/if}

<!-- Create Modal -->
<Modal bind:open={showCreateModal} title="Nový projekt">
  <form onsubmit={(e) => { e.preventDefault(); handleCreate(); }} class="space-y-4">
    <Input
      label="Název projektu"
      placeholder="Např. Rodinné nemovitosti"
      bind:value={formName}
      error={formErrors.name}
    />
    <Textarea
      label="Popis (volitelné)"
      placeholder="Krátký popis projektu..."
      bind:value={formDescription}
      rows={3}
    />
    <div class="flex justify-end gap-3 pt-2">
      <Button variant="secondary" onclick={() => showCreateModal = false}>
        {#snippet children()}Zrušit{/snippet}
      </Button>
      <Button type="submit" loading={saving}>
        {#snippet children()}Vytvořit{/snippet}
      </Button>
    </div>
  </form>
</Modal>

<!-- Edit Modal -->
<Modal bind:open={showEditModal} title="Upravit projekt">
  <form onsubmit={(e) => { e.preventDefault(); handleEdit(); }} class="space-y-4">
    <Input
      label="Název projektu"
      placeholder="Např. Rodinné nemovitosti"
      bind:value={formName}
      error={formErrors.name}
    />
    <Textarea
      label="Popis (volitelné)"
      placeholder="Krátký popis projektu..."
      bind:value={formDescription}
      rows={3}
    />
    <div class="flex justify-between pt-2">
      <Button variant="danger" onclick={() => { showEditModal = false; openDeleteConfirm(selectedProject!); }}>
        {#snippet children()}
          <Trash2 class="h-4 w-4" />
          Smazat
        {/snippet}
      </Button>
      <div class="flex gap-3">
        <Button variant="secondary" onclick={() => showEditModal = false}>
          {#snippet children()}Zrušit{/snippet}
        </Button>
        <Button type="submit" loading={saving}>
          {#snippet children()}Uložit{/snippet}
        </Button>
      </div>
    </div>
  </form>
</Modal>

<!-- Delete Confirmation -->
<ConfirmDialog
  bind:open={showDeleteConfirm}
  title="Smazat projekt?"
  message="Tato akce je nevratná. Všechny nemovitosti a záznamy v tomto projektu budou smazány."
  confirmText="Smazat"
  onconfirm={handleDelete}
/>
