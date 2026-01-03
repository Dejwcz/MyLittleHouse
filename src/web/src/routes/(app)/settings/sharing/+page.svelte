<script lang="ts">
  import { PageHeader, Card, Button, RegistrationRequired } from '$lib';
  import { auth } from '$lib/stores/auth.svelte';
  import { Share2, Users, Building2, ChevronRight } from 'lucide-svelte';

  const isGuest = $derived(auth.isGuest);

  // TODO: Load from API/IndexedDB
  const sharedByMe = [
    { id: '1', name: 'Chalupa', type: 'property', members: 3 },
    { id: '2', name: 'Rodina Novákovi', type: 'project', members: 5 }
  ];

  const sharedWithMe = [
    { id: '3', name: 'Byt Praha', type: 'property', owner: 'Eva Nováková', role: 'editor' }
  ];
</script>

<PageHeader
  title="Sdílení"
  subtitle="Přehled sdílených položek"
  backHref="/settings"
/>

<div class="mx-auto max-w-2xl space-y-6">
  {#if isGuest}
    <RegistrationRequired
      title="Sdílení vyžaduje účet"
      description="Pro sdílení nemovitostí s rodinou nebo kolegy je potřeba vytvořit účet. Data se synchronizují pouze když to povolíte."
    />
  {/if}

  <Card class={isGuest ? 'opacity-50 pointer-events-none' : ''}>
    <div class="mb-4 flex items-center gap-3">
      <Share2 class="h-5 w-5 text-foreground-muted" />
      <h2 class="font-medium">Sdílím</h2>
    </div>
    {#if sharedByMe.length === 0}
      <p class="text-sm text-foreground-muted">Zatím nic nesdílíte.</p>
    {:else}
      <div class="divide-y divide-border">
        {#each sharedByMe as item}
          <a href="/settings/sharing/{item.id}" class="flex items-center justify-between py-3 hover:bg-bg-secondary -mx-4 px-4 transition-colors">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-50 dark:bg-primary-950">
                <Building2 class="h-5 w-5 text-primary" />
              </div>
              <div>
                <p class="font-medium">{item.name}</p>
                <p class="text-sm text-foreground-muted">{item.members} členů</p>
              </div>
            </div>
            <ChevronRight class="h-5 w-5 text-foreground-muted" />
          </a>
        {/each}
      </div>
    {/if}
  </Card>

  <Card class={isGuest ? 'opacity-50 pointer-events-none' : ''}>
    <div class="mb-4 flex items-center gap-3">
      <Users class="h-5 w-5 text-foreground-muted" />
      <h2 class="font-medium">Sdíleno se mnou</h2>
    </div>
    {#if sharedWithMe.length === 0}
      <p class="text-sm text-foreground-muted">Nikdo s vámi zatím nic nesdílí.</p>
    {:else}
      <div class="divide-y divide-border">
        {#each sharedWithMe as item}
          <div class="flex items-center justify-between py-3">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 dark:bg-blue-950">
                <Building2 class="h-5 w-5 text-blue-500" />
              </div>
              <div>
                <p class="font-medium">{item.name}</p>
                <p class="text-sm text-foreground-muted">Od: {item.owner} · {item.role}</p>
              </div>
            </div>
            <Button variant="secondary" size="sm">
              {#snippet children()}
                Opustit
              {/snippet}
            </Button>
          </div>
        {/each}
      </div>
    {/if}
  </Card>
</div>
