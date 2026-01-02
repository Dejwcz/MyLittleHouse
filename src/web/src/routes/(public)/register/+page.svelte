<script lang="ts">
  import { Card, Button, Input } from '$lib';
  import { toast } from '$lib/stores/ui.svelte';
  import { authApi, ApiError } from '$lib/api';
  import { goto } from '$app/navigation';
  import { CheckCircle } from 'lucide-svelte';

  let firstName = $state('');
  let lastName = $state('');
  let email = $state('');
  let password = $state('');
  let confirmPassword = $state('');
  let loading = $state(false);
  let success = $state(false);
  let errors = $state<Record<string, string>>({});

  async function handleSubmit(e: Event) {
    e.preventDefault();
    errors = {};

    if (!firstName) errors.firstName = 'Jméno je povinné';
    if (!lastName) errors.lastName = 'Příjmení je povinné';
    if (!email) errors.email = 'Email je povinný';
    if (!password) errors.password = 'Heslo je povinné';
    else if (password.length < 6) errors.password = 'Heslo musí mít alespoň 6 znaků';
    if (password !== confirmPassword) errors.confirmPassword = 'Hesla se neshodují';

    if (Object.keys(errors).length > 0) return;

    loading = true;

    try {
      await authApi.register({ firstName, lastName, email, password });
      success = true;
      toast.success('Registrace úspěšná! Zkontrolujte svůj email.');
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.details) {
          errors = Object.fromEntries(
            Object.entries(err.details).map(([key, msgs]) => [key, msgs[0]])
          );
        } else {
          errors.email = err.message;
        }
      } else {
        errors.email = 'Registrace se nezdařila. Zkuste to znovu.';
      }
    } finally {
      loading = false;
    }
  }
</script>

<svelte:head>
  <title>Registrace | MůjDomeček</title>
</svelte:head>

<Card padding="lg">
  {#if success}
    <div class="text-center">
      <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-green-50 dark:bg-green-950">
        <CheckCircle class="h-8 w-8 text-success" />
      </div>
      <h1 class="text-2xl font-semibold">Účet vytvořen</h1>
      <p class="mt-2 text-sm text-foreground-muted">
        Poslali jsme vám email na <strong>{email}</strong> s odkazem pro potvrzení účtu.
      </p>
      <p class="mt-4 text-sm text-foreground-muted">
        Po potvrzení se můžete přihlásit.
      </p>
      <div class="mt-6">
        <a href="/login">
          <Button fullWidth>
            {#snippet children()}
              Přejít na přihlášení
            {/snippet}
          </Button>
        </a>
      </div>
    </div>
  {:else}
    <div class="text-center">
      <h1 class="text-2xl font-semibold">Vytvořit účet</h1>
      <p class="mt-2 text-sm text-foreground-muted">
        Začněte spravovat své nemovitosti
      </p>
    </div>

    <form onsubmit={handleSubmit} class="mt-8 space-y-4">
    <div class="grid gap-4 sm:grid-cols-2">
      <Input
        label="Jméno"
        placeholder="Jan"
        bind:value={firstName}
        error={errors.firstName}
        autocomplete="given-name"
      />
      <Input
        label="Příjmení"
        placeholder="Novák"
        bind:value={lastName}
        error={errors.lastName}
        autocomplete="family-name"
      />
    </div>

    <Input
      type="email"
      label="Email"
      placeholder="vas@email.cz"
      bind:value={email}
      error={errors.email}
      autocomplete="email"
    />

    <Input
      type="password"
      label="Heslo"
      placeholder="Min. 6 znaků"
      bind:value={password}
      error={errors.password}
      autocomplete="new-password"
    />

    <Input
      type="password"
      label="Potvrzení hesla"
      placeholder="Zopakujte heslo"
      bind:value={confirmPassword}
      error={errors.confirmPassword}
      autocomplete="new-password"
    />

    <p class="text-xs text-foreground-muted">
      Registrací souhlasíte s
      <a href="/terms" class="text-primary hover:underline">podmínkami použití</a>
      a
      <a href="/privacy" class="text-primary hover:underline">ochranou soukromí</a>.
    </p>

    <Button type="submit" fullWidth {loading}>
      {#snippet children()}
        Vytvořit účet
      {/snippet}
    </Button>
  </form>

    <p class="mt-6 text-center text-sm text-foreground-muted">
      Již máte účet?
      <a href="/login" class="text-primary hover:underline">Přihlaste se</a>
    </p>
  {/if}
</Card>
