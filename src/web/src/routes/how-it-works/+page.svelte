<script lang="ts">
  import { Button, ToastContainer } from '$lib';
  import { theme } from '$lib/stores/theme.svelte';
  import { auth } from '$lib/stores/auth.svelte';
  import { goto } from '$app/navigation';
  import { Sun, Moon } from 'lucide-svelte';
  import {
    Building2,
    FileText,
    Users,
    Shield,
    Camera,
    FolderOpen,
    Share2,
    Smartphone,
    Cloud,
    CheckCircle,
    ArrowRight,
    XCircle,
    HardDrive,
    Lock,
    RefreshCw,
    Laptop
  } from 'lucide-svelte';

  function startWithoutAccount() {
    auth.startAsGuest();
    goto('/projects');
  }
</script>

<svelte:head>
  <title>Jak to funguje - MujDomecek</title>
</svelte:head>

<div class="min-h-screen bg-bg">
  <!-- Header -->
  <header class="mx-auto flex w-full max-w-6xl items-center justify-between px-6 py-6">
    <a href="/" class="flex items-center gap-3">
      <div class="flex h-11 w-11 items-center justify-center rounded-2xl bg-primary text-foreground-inverse shadow-soft">
        <span class="text-lg font-semibold">MD</span>
      </div>
      <div>
        <p class="text-sm uppercase tracking-[0.3em] text-foreground-muted">MujDomecek</p>
        <p class="text-lg font-semibold">My Little House</p>
      </div>
    </a>

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
          {#snippet children()}Přihlásit se{/snippet}
        </Button>
      </a>
      <a href="/register" class="hidden sm:block">
        <Button size="sm">
          {#snippet children()}Začít zdarma{/snippet}
        </Button>
      </a>
    </div>
  </header>

  <main class="mx-auto max-w-6xl px-6 py-12">
  <!-- Header -->
  <div class="text-center">
    <h1 class="text-4xl font-semibold">Jak MujDomecek funguje</h1>
    <p class="mt-4 text-lg text-foreground-muted">
      Jednoduchý způsob, jak mít přehled o všech opravách, vylepšeních a zárukách vašich nemovitostí.
    </p>
  </div>

  <!-- Steps -->
  <div class="mt-16 space-y-16">
    <!-- Step 1 -->
    <section class="grid gap-8 md:grid-cols-2 md:items-center">
      <div>
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-primary-50 text-primary dark:bg-primary-950">
          <span class="text-xl font-bold">1</span>
        </div>
        <h2 class="mt-4 text-2xl font-semibold">Vytvořte projekt</h2>
        <p class="mt-3 text-foreground-muted">
          Projekt je váš hlavní workspace. Může to být "Rodinné nemovitosti", "Investiční byty"
          nebo cokoliv, co vám dává smysl. Do projektu pak přidáváte jednotlivé nemovitosti.
        </p>
        <ul class="mt-4 space-y-2 text-sm text-foreground-muted">
          <li class="flex items-center gap-2">
            <CheckCircle class="h-4 w-4 text-green-500" />
            Neomezený počet projektů
          </li>
          <li class="flex items-center gap-2">
            <CheckCircle class="h-4 w-4 text-green-500" />
            Možnost sdílet celý projekt s rodinou
          </li>
        </ul>
      </div>
      <div class="rounded-3xl border border-border bg-surface p-6">
        <div class="flex items-center gap-3">
          <FolderOpen class="h-8 w-8 text-primary" />
          <div>
            <p class="font-semibold">Rodinné nemovitosti</p>
            <p class="text-sm text-foreground-muted">3 nemovitosti · 2 členové</p>
          </div>
        </div>
      </div>
    </section>

    <!-- Step 2 -->
    <section class="grid gap-8 md:grid-cols-2 md:items-center">
      <div class="md:order-2">
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-50 text-blue-600 dark:bg-blue-950">
          <span class="text-xl font-bold">2</span>
        </div>
        <h2 class="mt-4 text-2xl font-semibold">Přidejte nemovitosti a jednotky</h2>
        <p class="mt-3 text-foreground-muted">
          Každá nemovitost může mít jednotky - místnosti, patra, garáž, zahradu.
          Díky tomu pak snadno najdete, že výměna kotle byla v kotelně chalupy,
          ne v bytě v Praze.
        </p>
        <ul class="mt-4 space-y-2 text-sm text-foreground-muted">
          <li class="flex items-center gap-2">
            <CheckCircle class="h-4 w-4 text-green-500" />
            Hierarchická struktura (nemovitost → jednotky)
          </li>
          <li class="flex items-center gap-2">
            <CheckCircle class="h-4 w-4 text-green-500" />
            Volitelná GPS poloha
          </li>
        </ul>
      </div>
      <div class="rounded-3xl border border-border bg-surface p-6 md:order-1">
        <div class="space-y-3">
          <div class="flex items-center gap-3">
            <Building2 class="h-6 w-6 text-blue-500" />
            <span class="font-medium">Chalupa Krkonoše</span>
          </div>
          <div class="ml-6 space-y-2 border-l-2 border-border pl-4">
            <p class="text-sm text-foreground-muted">Obývák</p>
            <p class="text-sm text-foreground-muted">Kuchyň</p>
            <p class="text-sm text-foreground-muted">Kotelna</p>
            <p class="text-sm text-foreground-muted">Zahrada</p>
          </div>
        </div>
      </div>
    </section>

    <!-- Step 3 -->
    <section class="grid gap-8 md:grid-cols-2 md:items-center">
      <div>
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-50 text-amber-600 dark:bg-amber-950">
          <span class="text-xl font-bold">3</span>
        </div>
        <h2 class="mt-4 text-2xl font-semibold">Zaznamenávejte vše důležité</h2>
        <p class="mt-3 text-foreground-muted">
          Oprava, údržba, revize, nákup materiálu, pojistná událost - cokoliv, co se týká
          vaší nemovitosti. Přidejte fotky, dokumenty, účtenky. Vše na jednom místě.
        </p>
        <ul class="mt-4 space-y-2 text-sm text-foreground-muted">
          <li class="flex items-center gap-2">
            <Camera class="h-4 w-4 text-foreground-muted" />
            Fotky přímo z mobilu
          </li>
          <li class="flex items-center gap-2">
            <FileText class="h-4 w-4 text-foreground-muted" />
            PDF dokumenty, faktury, smlouvy
          </li>
          <li class="flex items-center gap-2">
            <CheckCircle class="h-4 w-4 text-green-500" />
            Tagy pro rychlé filtrování
          </li>
        </ul>
      </div>
      <div class="rounded-3xl border border-border bg-surface p-6">
        <div class="flex items-start justify-between">
          <div>
            <p class="font-semibold">Výměna kotle</p>
            <p class="text-sm text-foreground-muted">Chalupa · Kotelna</p>
          </div>
          <span class="rounded-full bg-green-100 px-2 py-1 text-xs font-medium text-green-700 dark:bg-green-900 dark:text-green-300">
            Dokončeno
          </span>
        </div>
        <div class="mt-4 flex gap-2">
          <span class="rounded-full bg-bg-secondary px-2 py-1 text-xs">Oprava</span>
          <span class="rounded-full bg-bg-secondary px-2 py-1 text-xs">Topení</span>
        </div>
        <div class="mt-4 flex items-center justify-between text-sm">
          <span class="text-foreground-muted">15. 12. 2024</span>
          <span class="font-semibold">15 000 Kč</span>
        </div>
      </div>
    </section>

    <!-- Step 4 -->
    <section class="grid gap-8 md:grid-cols-2 md:items-center">
      <div class="md:order-2">
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-purple-50 text-purple-600 dark:bg-purple-950">
          <span class="text-xl font-bold">4</span>
        </div>
        <h2 class="mt-4 text-2xl font-semibold">Sdílejte s rodinou</h2>
        <p class="mt-3 text-foreground-muted">
          Pozvěte partnera, rodiče nebo správce. Nastavte jim práva - někdo může jen
          prohlížet, někdo může přidávat záznamy, někdo má plný přístup.
        </p>
        <ul class="mt-4 space-y-2 text-sm text-foreground-muted">
          <li class="flex items-center gap-2">
            <CheckCircle class="h-4 w-4 text-green-500" />
            Role: Vlastník, Editor, Čtenář
          </li>
          <li class="flex items-center gap-2">
            <CheckCircle class="h-4 w-4 text-green-500" />
            Pozvánky přes email
          </li>
        </ul>
      </div>
      <div class="rounded-3xl border border-border bg-surface p-6 md:order-1">
        <div class="flex items-center gap-3">
          <Users class="h-6 w-6 text-purple-500" />
          <span class="font-medium">Členové projektu</span>
        </div>
        <div class="mt-4 space-y-3">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <div class="h-8 w-8 rounded-full bg-primary text-center text-sm font-medium leading-8 text-white">JN</div>
              <span class="text-sm">Jan Novák</span>
            </div>
            <span class="text-xs text-foreground-muted">Vlastník</span>
          </div>
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <div class="h-8 w-8 rounded-full bg-blue-500 text-center text-sm font-medium leading-8 text-white">EN</div>
              <span class="text-sm">Eva Nováková</span>
            </div>
            <span class="text-xs text-foreground-muted">Editor</span>
          </div>
        </div>
      </div>
    </section>

    <!-- Dva způsoby použití -->
    <section class="mt-8">
      <div class="text-center">
        <h2 class="text-3xl font-semibold">Dva způsoby použití</h2>
        <p class="mt-3 text-foreground-muted">
          Vyberte si, co vám vyhovuje. Můžete kdykoliv přejít z jednoho na druhé.
        </p>
      </div>

      <div class="mt-10 grid gap-6 md:grid-cols-2">
        <!-- Bez účtu -->
        <div class="rounded-3xl border-2 border-border bg-surface p-8">
          <div class="flex items-center gap-3">
            <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-50 dark:bg-amber-950">
              <HardDrive class="h-6 w-6 text-amber-600" />
            </div>
            <div>
              <h3 class="text-xl font-semibold">Bez účtu</h3>
              <p class="text-sm text-foreground-muted">Lokální režim</p>
            </div>
          </div>

          <p class="mt-4 text-foreground-muted">
            Data zůstávají pouze ve vašem prohlížeči. Žádná registrace, žádné emaily, žádná hesla.
          </p>

          <div class="mt-6 space-y-3">
            <div class="flex items-start gap-3">
              <CheckCircle class="mt-0.5 h-5 w-5 shrink-0 text-green-500" />
              <div>
                <p class="font-medium">Okamžitý start</p>
                <p class="text-sm text-foreground-muted">Klikněte a používejte. Žádné formuláře.</p>
              </div>
            </div>
            <div class="flex items-start gap-3">
              <CheckCircle class="mt-0.5 h-5 w-5 shrink-0 text-green-500" />
              <div>
                <p class="font-medium">100% soukromí</p>
                <p class="text-sm text-foreground-muted">Data nikdy neopustí váš počítač.</p>
              </div>
            </div>
            <div class="flex items-start gap-3">
              <CheckCircle class="mt-0.5 h-5 w-5 shrink-0 text-green-500" />
              <div>
                <p class="font-medium">Funguje offline</p>
                <p class="text-sm text-foreground-muted">Nepotřebujete internet.</p>
              </div>
            </div>
          </div>

          <div class="mt-6 space-y-3 border-t border-border pt-6">
            <div class="flex items-start gap-3">
              <XCircle class="mt-0.5 h-5 w-5 shrink-0 text-red-400" />
              <div>
                <p class="font-medium text-foreground-muted">Jeden "účet" na prohlížeč</p>
                <p class="text-sm text-foreground-muted">Pro více oddělených dat použijte profily prohlížeče.</p>
              </div>
            </div>
            <div class="flex items-start gap-3">
              <XCircle class="mt-0.5 h-5 w-5 shrink-0 text-red-400" />
              <div>
                <p class="font-medium text-foreground-muted">Bez synchronizace</p>
                <p class="text-sm text-foreground-muted">Data existují jen na tomto zařízení.</p>
              </div>
            </div>
            <div class="flex items-start gap-3">
              <XCircle class="mt-0.5 h-5 w-5 shrink-0 text-red-400" />
              <div>
                <p class="font-medium text-foreground-muted">Bez sdílení</p>
                <p class="text-sm text-foreground-muted">Nemůžete pozvat rodinu nebo kolegy.</p>
              </div>
            </div>
          </div>

          <div class="mt-6 rounded-2xl bg-amber-50 p-4 dark:bg-amber-950">
            <div class="flex items-start gap-3">
              <Lock class="mt-0.5 h-5 w-5 shrink-0 text-amber-600" />
              <div>
                <p class="font-medium text-amber-800 dark:text-amber-200">Bezpečnost</p>
                <p class="text-sm text-amber-700 dark:text-amber-300">
                  Data jsou chráněná vaším přihlášením do Windows/Mac. Kdo má přístup k vašemu počítači a prohlížeči, má přístup k datům.
                </p>
              </div>
            </div>
          </div>

          <div class="mt-6">
            <Button size="lg" class="w-full" onclick={startWithoutAccount}>
              {#snippet children()}
                Začít bez účtu
                <ArrowRight class="h-4 w-4" />
              {/snippet}
            </Button>
          </div>
        </div>

        <!-- S účtem -->
        <div class="rounded-3xl border-2 border-primary bg-surface p-8">
          <div class="flex items-center gap-3">
            <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-primary-50 dark:bg-primary-950">
              <Cloud class="h-6 w-6 text-primary" />
            </div>
            <div>
              <h3 class="text-xl font-semibold">S účtem</h3>
              <p class="text-sm text-foreground-muted">Cloud + lokální</p>
            </div>
          </div>

          <p class="mt-4 text-foreground-muted">
            Data se ukládají lokálně i v cloudu. Přístup z více zařízení, sdílení s rodinou.
          </p>

          <div class="mt-6 space-y-3">
            <div class="flex items-start gap-3">
              <CheckCircle class="mt-0.5 h-5 w-5 shrink-0 text-green-500" />
              <div>
                <p class="font-medium">Synchronizace</p>
                <p class="text-sm text-foreground-muted">Přístup z mobilu, tabletu, druhého počítače.</p>
              </div>
            </div>
            <div class="flex items-start gap-3">
              <CheckCircle class="mt-0.5 h-5 w-5 shrink-0 text-green-500" />
              <div>
                <p class="font-medium">Sdílení s rodinou</p>
                <p class="text-sm text-foreground-muted">Pozvěte partnera, rodiče, správce nemovitosti.</p>
              </div>
            </div>
            <div class="flex items-start gap-3">
              <CheckCircle class="mt-0.5 h-5 w-5 shrink-0 text-green-500" />
              <div>
                <p class="font-medium">Záloha v cloudu</p>
                <p class="text-sm text-foreground-muted">Když vám shořelá počítač, data máte pořád.</p>
              </div>
            </div>
            <div class="flex items-start gap-3">
              <CheckCircle class="mt-0.5 h-5 w-5 shrink-0 text-green-500" />
              <div>
                <p class="font-medium">Funguje i offline</p>
                <p class="text-sm text-foreground-muted">Změny se synchronizují, až budete online.</p>
              </div>
            </div>
          </div>

          <div class="mt-6 rounded-2xl bg-primary-50 p-4 dark:bg-primary-950">
            <div class="flex items-start gap-3">
              <Lock class="mt-0.5 h-5 w-5 shrink-0 text-primary" />
              <div>
                <p class="font-medium text-primary-800 dark:text-primary-200">Bezpečnost</p>
                <p class="text-sm text-primary-700 dark:text-primary-300">
                  Přihlášení emailem a heslem. Data šifrována při přenosu. Můžete zapnout dvoufaktorové ověření.
                </p>
              </div>
            </div>
          </div>

          <div class="mt-6">
            <a href="/register">
              <Button size="lg" class="w-full">
                {#snippet children()}
                  Vytvořit účet zdarma
                  <ArrowRight class="h-4 w-4" />
                {/snippet}
              </Button>
            </a>
          </div>
        </div>
      </div>

      <!-- Přechod mezi režimy -->
      <div class="mt-10 rounded-3xl border border-border bg-bg-secondary p-6 text-center">
        <RefreshCw class="mx-auto h-8 w-8 text-foreground-muted" />
        <h3 class="mt-3 text-lg font-semibold">Můžete kdykoliv přejít</h3>
        <p class="mt-2 text-foreground-muted">
          Začněte bez účtu a později se zaregistrujte. Vaše lokální data se automaticky nahrají do cloudu.
        </p>
      </div>
    </section>
  </div>
  </main>

  <!-- Footer -->
  <footer class="mx-auto w-full max-w-6xl px-6 pb-10 text-xs text-foreground-muted">
    Local-first property logbook. Data stays on your devices by default.
  </footer>
</div>

<ToastContainer />
