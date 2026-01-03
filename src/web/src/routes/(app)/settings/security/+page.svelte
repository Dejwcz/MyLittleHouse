<script lang="ts">
  import { PageHeader, Card, Input, Button, RegistrationRequired } from '$lib';
  import { auth } from '$lib/stores/auth.svelte';
  import { Key, Link2, Trash2 } from 'lucide-svelte';

  const isGuest = $derived(auth.isGuest);

  let currentPassword = $state('');
  let newPassword = $state('');
  let confirmPassword = $state('');
  let saving = $state(false);

  const linkedAccounts = [
    { id: 'google', name: 'Google', connected: !!auth.user?.linkedAccounts?.google },
    { id: 'apple', name: 'Apple', connected: !!auth.user?.linkedAccounts?.apple }
  ];

  async function handlePasswordChange() {
    if (newPassword !== confirmPassword) return;
    saving = true;
    // TODO: API call
    await new Promise(r => setTimeout(r, 500));
    saving = false;
    currentPassword = '';
    newPassword = '';
    confirmPassword = '';
  }
</script>

<PageHeader
  title="Zabezpečení"
  subtitle="Heslo a propojené účty"
  backHref="/settings"
/>

<div class="mx-auto max-w-2xl space-y-6">
  {#if isGuest}
    <RegistrationRequired
      title="Zabezpečení vyžaduje účet"
      description="Pro nastavení hesla a propojených účtů je potřeba se registrovat. Vaše lokální data zůstanou zachována."
    />
  {:else if auth.user?.hasPassword}
    <Card>
      <div class="mb-4 flex items-center gap-3">
        <Key class="h-5 w-5 text-foreground-muted" />
        <h2 class="font-medium">Změna hesla</h2>
      </div>
      <div class="space-y-4">
        <Input
          label="Současné heslo"
          type="password"
          bind:value={currentPassword}
        />
        <Input
          label="Nové heslo"
          type="password"
          bind:value={newPassword}
        />
        <Input
          label="Potvrzení hesla"
          type="password"
          bind:value={confirmPassword}
          error={confirmPassword && newPassword !== confirmPassword ? 'Hesla se neshodují' : undefined}
        />
        <Button onclick={handlePasswordChange} disabled={saving || !newPassword || newPassword !== confirmPassword}>
          {#snippet children()}
            {saving ? 'Ukládám...' : 'Změnit heslo'}
          {/snippet}
        </Button>
      </div>
    </Card>
  {/if}

  <Card>
    <div class="mb-4 flex items-center gap-3">
      <Link2 class="h-5 w-5 text-foreground-muted" />
      <h2 class="font-medium">Propojené účty</h2>
    </div>
    <div class="divide-y divide-border">
      {#each linkedAccounts as account}
        <div class="flex items-center justify-between py-3">
          <div>
            <p class="font-medium">{account.name}</p>
            <p class="text-sm text-foreground-muted">
              {account.connected ? 'Propojeno' : 'Nepropojeno'}
            </p>
          </div>
          <Button variant={account.connected ? 'secondary' : 'primary'} size="sm">
            {#snippet children()}
              {account.connected ? 'Odpojit' : 'Propojit'}
            {/snippet}
          </Button>
        </div>
      {/each}
    </div>
  </Card>

  <Card class="border-red-200 dark:border-red-900">
    <div class="mb-4 flex items-center gap-3">
      <Trash2 class="h-5 w-5 text-red-500" />
      <h2 class="font-medium text-red-600 dark:text-red-400">Nebezpečná zóna</h2>
    </div>
    <p class="mb-4 text-sm text-foreground-muted">
      Smazání účtu je nevratné. Všechna vaše data budou trvale odstraněna.
    </p>
    <Button variant="danger" size="sm">
      {#snippet children()}
        Smazat účet
      {/snippet}
    </Button>
  </Card>
</div>
