<script lang="ts">
  import { PageHeader, Card, Input, Button, Avatar, RegistrationRequired } from '$lib';
  import { auth } from '$lib/stores/auth.svelte';
  import { Camera } from 'lucide-svelte';

  const isGuest = $derived(auth.isGuest);

  let firstName = $state(auth.user?.firstName ?? '');
  let lastName = $state(auth.user?.lastName ?? '');
  let email = $state(auth.user?.email ?? '');
  let phone = $state(auth.user?.phone ?? '');
  let saving = $state(false);

  async function handleSave() {
    saving = true;
    // TODO: API call to update profile
    await new Promise(r => setTimeout(r, 500));
    saving = false;
  }
</script>

<PageHeader
  title="Profil"
  subtitle="Správa vašich osobních údajů"
  backHref="/settings"
/>

<div class="mx-auto max-w-2xl space-y-6">
  {#if isGuest}
    <RegistrationRequired
      title="Jste přihlášeni jako host"
      description="Vytvořte si účet pro uložení profilu a synchronizaci dat mezi zařízeními."
    />
  {/if}

  <Card>
    <div class="flex flex-col items-center gap-4 sm:flex-row">
      <div class="relative">
        <Avatar name={auth.fullName} size="xl" />
        <button
          class="absolute -bottom-1 -right-1 rounded-full bg-primary p-2 text-foreground-inverse shadow-md hover:bg-primary-600 transition-colors"
          aria-label="Změnit fotku"
        >
          <Camera class="h-4 w-4" />
        </button>
      </div>
      <div class="text-center sm:text-left">
        <p class="text-lg font-medium">{auth.fullName || 'Host'}</p>
        <p class="text-sm text-foreground-muted">{email || 'Bez emailu'}</p>
      </div>
    </div>
  </Card>

  <Card>
    <h2 class="mb-4 font-medium">Osobní údaje</h2>
    <div class="grid gap-4 sm:grid-cols-2">
      <Input label="Jméno" bind:value={firstName} placeholder="Jan" />
      <Input label="Příjmení" bind:value={lastName} placeholder="Novák" />
    </div>
    <div class="mt-4 grid gap-4 sm:grid-cols-2">
      <Input label="Email" type="email" bind:value={email} placeholder="jan@example.cz" />
      <Input label="Telefon" type="tel" bind:value={phone} placeholder="+420 123 456 789" />
    </div>
  </Card>

  <div class="flex justify-end">
    <Button onclick={handleSave} disabled={saving}>
      {#snippet children()}
        {saving ? 'Ukládám...' : 'Uložit změny'}
      {/snippet}
    </Button>
  </div>
</div>
