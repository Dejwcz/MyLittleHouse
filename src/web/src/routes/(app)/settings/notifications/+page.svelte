<script lang="ts">
  import { PageHeader, Card, Toggle, RegistrationRequired } from '$lib';
  import { auth } from '$lib/stores/auth.svelte';
  import { Bell, Mail, Smartphone } from 'lucide-svelte';

  const isGuest = $derived(auth.isGuest);

  let pushEnabled = $state(true);
  let emailEnabled = $state(true);
  let newRecords = $state(true);
  let comments = $state(true);
  let shares = $state(true);
  let reminders = $state(true);
  let weeklySummary = $state(false);
</script>

<PageHeader
  title="Notifikace"
  subtitle="Nastavení upozornění"
  backHref="/settings"
/>

<div class="mx-auto max-w-2xl space-y-6">
  {#if isGuest}
    <RegistrationRequired
      title="Notifikace vyžadují účet"
      description="Pro příjem push a email notifikací je potřeba vytvořit účet."
    />
  {/if}

  <Card class={isGuest ? 'opacity-50 pointer-events-none' : ''}>
    <div class="mb-4 flex items-center gap-3">
      <Smartphone class="h-5 w-5 text-foreground-muted" />
      <h2 class="font-medium">Způsob doručení</h2>
    </div>
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <div>
          <p class="font-medium">Push notifikace</p>
          <p class="text-sm text-foreground-muted">Upozornění v prohlížeči/aplikaci</p>
        </div>
        <Toggle bind:checked={pushEnabled} />
      </div>
      <div class="flex items-center justify-between">
        <div>
          <p class="font-medium">Email notifikace</p>
          <p class="text-sm text-foreground-muted">Důležitá upozornění na email</p>
        </div>
        <Toggle bind:checked={emailEnabled} />
      </div>
    </div>
  </Card>

  <Card class={isGuest ? 'opacity-50 pointer-events-none' : ''}>
    <div class="mb-4 flex items-center gap-3">
      <Bell class="h-5 w-5 text-foreground-muted" />
      <h2 class="font-medium">Typy upozornění</h2>
    </div>
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <div>
          <p class="font-medium">Nové záznamy</p>
          <p class="text-sm text-foreground-muted">Když někdo přidá záznam</p>
        </div>
        <Toggle bind:checked={newRecords} />
      </div>
      <div class="flex items-center justify-between">
        <div>
          <p class="font-medium">Komentáře</p>
          <p class="text-sm text-foreground-muted">Nové komentáře a zmínky</p>
        </div>
        <Toggle bind:checked={comments} />
      </div>
      <div class="flex items-center justify-between">
        <div>
          <p class="font-medium">Sdílení</p>
          <p class="text-sm text-foreground-muted">Pozvánky a změny přístupu</p>
        </div>
        <Toggle bind:checked={shares} />
      </div>
      <div class="flex items-center justify-between">
        <div>
          <p class="font-medium">Připomínky</p>
          <p class="text-sm text-foreground-muted">Záruky, revize, termíny</p>
        </div>
        <Toggle bind:checked={reminders} />
      </div>
    </div>
  </Card>

  <Card class={isGuest ? 'opacity-50 pointer-events-none' : ''}>
    <div class="mb-4 flex items-center gap-3">
      <Mail class="h-5 w-5 text-foreground-muted" />
      <h2 class="font-medium">Email souhrny</h2>
    </div>
    <div class="flex items-center justify-between">
      <div>
        <p class="font-medium">Týdenní souhrn</p>
        <p class="text-sm text-foreground-muted">Přehled aktivity každý týden</p>
      </div>
      <Toggle bind:checked={weeklySummary} />
    </div>
  </Card>
</div>
