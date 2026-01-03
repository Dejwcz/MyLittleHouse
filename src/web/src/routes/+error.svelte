<script lang="ts">
  import { page } from '$app/stores';
  import { Button } from '$lib';
  import { Home, ArrowLeft } from 'lucide-svelte';
</script>

<svelte:head>
  <title>{$page.status} | MůjDomeček</title>
</svelte:head>

<div class="flex min-h-screen flex-col items-center justify-center bg-bg px-4">
  <div class="text-center">
    <p class="text-8xl font-bold text-primary">{$page.status}</p>
    <h1 class="mt-4 text-2xl font-semibold">
      {#if $page.status === 404}
        Stránka nenalezena
      {:else if $page.status === 500}
        Něco se pokazilo
      {:else}
        Nastala chyba
      {/if}
    </h1>
    <p class="mt-2 text-foreground-muted">
      {#if $page.status === 404}
        Stránka kterou hledáte neexistuje nebo byla přesunuta.
      {:else}
        {$page.error?.message ?? 'Zkuste to prosím znovu.'}
      {/if}
    </p>
    <div class="mt-8 flex flex-wrap justify-center gap-3">
      <Button variant="secondary" onclick={() => history.back()}>
        {#snippet children()}
          <ArrowLeft class="mr-2 h-4 w-4" />
          Zpět
        {/snippet}
      </Button>
      <a href="/dashboard">
        <Button>
          {#snippet children()}
            <Home class="mr-2 h-4 w-4" />
            Dashboard
          {/snippet}
        </Button>
      </a>
    </div>
  </div>
</div>
