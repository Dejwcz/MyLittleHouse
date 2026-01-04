<script lang="ts">
  import { ToastContainer, Button, Modal } from '$lib';
  import { theme } from '$lib/stores/theme.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { db, type Zaznam } from '$lib/db';
  import { goto } from '$app/navigation';
  import { onMount } from 'svelte';
  import { browser } from '$app/environment';
  import { Sun, Moon, Building2, FileText, Users, Shield, ArrowRight, AlertTriangle, Clock } from 'lucide-svelte';

  interface ActivityItem {
    id: string;
    title: string;
    propertyName?: string;
    date: string;
    type: 'zaznam';
  }

  let hasLocalData = $state(false);
  let projectCount = $state(0);
  let checking = $state(true);
  let showWarningModal = $state(false);
  let recentActivity = $state<ActivityItem[]>([]);

  function formatRelativeTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'právě teď';
    if (diffMins < 60) return `před ${diffMins} min`;
    if (diffHours < 24) return `před ${diffHours}h`;
    if (diffDays < 7) return `před ${diffDays}d`;
    return date.toLocaleDateString('cs-CZ');
  }

  onMount(async () => {
    if (browser) {
      try {
        const projects = await db.projects.count();
        projectCount = projects;
        hasLocalData = projects > 0;

        // Load recent activity
        if (hasLocalData) {
          const zaznamy = await db.zaznamy
            .orderBy('updatedAt')
            .reverse()
            .limit(5)
            .toArray();

          recentActivity = await Promise.all(zaznamy.map(async (z) => {
            const property = await db.properties.get(z.propertyId);
            return {
              id: z.id,
              title: z.title || 'Bez názvu',
              propertyName: property?.name,
              date: new Date(z.updatedAt).toISOString(),
              type: 'zaznam' as const
            };
          }));
        }
      } catch {
        // DB not initialized yet, that's fine
      }
      checking = false;
    }
  });

  function handleContinueClick() {
    if (hasLocalData) {
      showWarningModal = true;
    } else {
      startWithoutAccount();
    }
  }

  function startWithoutAccount() {
    showWarningModal = false;
    auth.startAsGuest();
    goto('/projects');
  }
</script>

<svelte:head>
  <title>MůjDomeček - Správa nemovitostí</title>
  <meta
    name="description"
    content="Local-first deník nemovitostí. Záznamy, fotky, dokumenty a náklady offline se synchronizací jen když chcete."
  />
  <meta property="og:title" content="MůjDomeček - Správa nemovitostí" />
  <meta
    property="og:description"
    content="Local-first deník nemovitostí. Záznamy, fotky, dokumenty a náklady offline se synchronizací jen když chcete."
  />
  <meta property="og:type" content="website" />
  <meta name="twitter:card" content="summary" />
  <meta name="twitter:title" content="MůjDomeček - Správa nemovitostí" />
  <meta
    name="twitter:description"
    content="Local-first deník nemovitostí. Záznamy, fotky, dokumenty a náklady offline se synchronizací jen když chcete."
  />
</svelte:head>

<div class="min-h-screen bg-bg">
  <!-- Header -->
  <header class="mx-auto flex w-full max-w-6xl items-center justify-between px-6 py-6">
    <div class="flex items-center gap-3">
      <div
        class="flex h-11 w-11 items-center justify-center rounded-2xl bg-primary text-foreground-inverse shadow-soft"
      >
        <span class="text-lg font-semibold">MD</span>
      </div>
      <div>
        <p class="text-sm uppercase tracking-[0.3em] text-foreground-muted">MujDomecek</p>
        <p class="text-lg font-semibold">My Little House</p>
      </div>
    </div>

    <div class="flex items-center gap-3">
      <button
        onclick={() => theme.toggle()}
        class="rounded-full p-2 text-foreground-muted hover:bg-bg-secondary hover:text-foreground transition-colors"
        aria-label={theme.isDark ? 'Světlý režim' : 'Tmavý režim'}
      >
        {#if theme.isDark}
          <Sun class="h-5 w-5" />
        {:else}
          <Moon class="h-5 w-5" />
        {/if}
      </button>

      <a href="/login">
        <Button variant="secondary" size="sm">
          {#snippet children()}
            Přihlásit se
          {/snippet}
        </Button>
      </a>
      <a href="/register" class="hidden sm:block">
        <Button size="sm">
          {#snippet children()}
            Začít zdarma
          {/snippet}
        </Button>
      </a>
    </div>
  </header>

  <!-- Hero -->
  <main class="mx-auto w-full max-w-6xl px-6 pb-16">
    <section class="grid gap-10 py-12 md:grid-cols-[1.2fr_0.8fr] md:py-20">
      <div class="space-y-6">
        <p class="text-xs font-semibold uppercase tracking-[0.4em] text-primary-600">
          Local-first property journal
        </p>
        <h1 class="text-4xl font-semibold leading-tight md:text-5xl">
          Každá oprava, vylepšení a záruka přehledně i offline.
        </h1>
        <p class="text-lg text-foreground-muted">
          Zachyťte záznamy v momentě kdy se dějí. Synchronizujte jen když chcete, sdílejte jen když potřebujete.
        </p>
        <div class="flex flex-wrap gap-3">
          <Button size="lg" onclick={handleContinueClick}>
            {#snippet children()}
              {#if checking}
                Načítám...
              {:else if hasLocalData}
                <span class="flex items-center gap-2">
                  Pokračovat
                  <ArrowRight class="h-4 w-4" />
                </span>
              {:else}
                Začít bez účtu
              {/if}
            {/snippet}
          </Button>
          <a href="/register">
            <Button variant="secondary" size="lg">
              {#snippet children()}
                Vytvořit účet
              {/snippet}
            </Button>
          </a>
          <a href="/how-it-works">
            <Button variant="ghost" size="lg">
              {#snippet children()}
                Jak to funguje
              {/snippet}
            </Button>
          </a>
        </div>
        {#if hasLocalData && !checking}
          <p class="text-sm text-foreground-muted">
            Máte {projectCount} {projectCount === 1 ? 'projekt uložený' : projectCount < 5 ? 'projekty uložené' : 'projektů uložených'} lokálně
          </p>
        {/if}
      </div>

      <div class="space-y-6">
        <div class="rounded-3xl bg-surface p-6 shadow-soft">
          <div class="flex items-center justify-between">
            <p class="text-sm font-semibold">Rychlý záznam</p>
            <span class="rounded-full bg-primary-50 px-3 py-1 text-xs font-semibold text-primary-700 dark:bg-primary-950 dark:text-primary-300">
              Rozpracovaný
            </span>
          </div>
          <div class="mt-6 space-y-4">
            <div class="rounded-2xl border border-border px-4 py-3 text-sm text-foreground-muted">
              Název
            </div>
            <div class="rounded-2xl border border-border px-4 py-3 text-sm text-foreground-muted">
              Nemovitost
            </div>
            <div class="rounded-2xl border border-border px-4 py-3 text-sm text-foreground-muted">
              Datum
            </div>
          </div>
          <button
            class="mt-6 w-full rounded-2xl bg-foreground px-4 py-3 text-sm font-semibold text-foreground-inverse"
          >
            Uložit lokálně
          </button>
        </div>

        {#if recentActivity.length > 0}
          <div class="rounded-3xl border border-border bg-bg-secondary p-6">
            <div class="flex items-center gap-2">
              <Clock class="h-4 w-4 text-foreground-muted" />
              <p class="text-sm font-semibold">Poslední aktivita</p>
            </div>
            <div class="mt-4 space-y-3 text-sm">
              {#each recentActivity as activity}
                <div class="flex items-center justify-between">
                  <div class="min-w-0 flex-1">
                    <p class="truncate font-medium">{activity.title}</p>
                    {#if activity.propertyName}
                      <p class="truncate text-xs text-foreground-muted">{activity.propertyName}</p>
                    {/if}
                  </div>
                  <span class="ml-3 shrink-0 text-foreground-muted">{formatRelativeTime(activity.date)}</span>
                </div>
              {/each}
            </div>
          </div>
        {/if}
      </div>
    </section>

    <!-- Features -->
    <section class="grid gap-6 md:grid-cols-4">
      <div class="rounded-3xl border border-border bg-surface p-6">
        <div class="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-primary-50 dark:bg-primary-950">
          <Building2 class="h-6 w-6 text-primary" />
        </div>
        <h3 class="font-semibold">Projekty</h3>
        <p class="mt-2 text-sm text-foreground-muted">
          Jeden workspace pro rodinu nebo tým.
        </p>
      </div>

      <div class="rounded-3xl border border-border bg-surface p-6">
        <div class="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-50 dark:bg-blue-950">
          <FileText class="h-6 w-6 text-blue-500" />
        </div>
        <h3 class="font-semibold">Záznamy</h3>
        <p class="mt-2 text-sm text-foreground-muted">
          Fotky, dokumenty, náklady na jednom místě.
        </p>
      </div>

      <div class="rounded-3xl border border-border bg-surface p-6">
        <div class="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-50 dark:bg-amber-950">
          <Users class="h-6 w-6 text-amber-500" />
        </div>
        <h3 class="font-semibold">Sdílení</h3>
        <p class="mt-2 text-sm text-foreground-muted">
          Pozvěte rodinu, nastavte role a práva.
        </p>
      </div>

      <div class="rounded-3xl border border-border bg-surface p-6">
        <div class="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-purple-50 dark:bg-purple-950">
          <Shield class="h-6 w-6 text-purple-500" />
        </div>
        <h3 class="font-semibold">Local-first</h3>
        <p class="mt-2 text-sm text-foreground-muted">
          Data zůstávají ve vašem zařízení.
        </p>
      </div>
    </section>
  </main>

  <!-- Footer -->
  <footer class="mx-auto w-full max-w-6xl px-6 pb-10 text-xs text-foreground-muted">
    Local-first property logbook. Data stays on your devices by default.
  </footer>
</div>

<Modal bind:open={showWarningModal} title="Lokální data">
  <div class="space-y-4">
    <div class="flex items-start gap-3 rounded-xl bg-amber-50 p-4 dark:bg-amber-950">
      <AlertTriangle class="h-5 w-5 shrink-0 text-amber-600 dark:text-amber-400" />
      <div class="text-sm text-amber-800 dark:text-amber-200">
        <p class="font-medium">Vaše data jsou uložena pouze v tomto prohlížeči</p>
        <p class="mt-1">
          Vymazání dat prohlížeče, použití čističe (CCleaner apod.) nebo přeinstalace prohlížeče
          může trvale smazat vaše záznamy.
        </p>
      </div>
    </div>
    <div class="text-sm text-foreground-muted">
      <p class="font-medium text-foreground">Pro ochranu dat doporučujeme:</p>
      <ul class="mt-2 list-inside list-disc space-y-1">
        <li>Vytvořit účet a zapnout synchronizaci</li>
        <li>Pravidelně exportovat data (Nastavení → Data)</li>
      </ul>
    </div>
  </div>
  {#snippet footer()}
    <div class="flex justify-end gap-3">
      <Button variant="secondary" onclick={() => showWarningModal = false}>
        {#snippet children()}Zrušit{/snippet}
      </Button>
      <Button onclick={startWithoutAccount}>
        {#snippet children()}Rozumím, pokračovat{/snippet}
      </Button>
    </div>
  {/snippet}
</Modal>

<ToastContainer />
