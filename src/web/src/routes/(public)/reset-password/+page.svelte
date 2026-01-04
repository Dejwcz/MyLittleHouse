<script lang="ts">
  import { Card, Button, Input } from '$lib';
  import { toast } from '$lib/stores/ui.svelte';
  import { authApi, ApiError } from '$lib/api';
  import { page } from '$app/stores';
  import { goto } from '$app/navigation';
  import { onMount } from 'svelte';
  import { CheckCircle, XCircle } from 'lucide-svelte';

  let password = $state('');
  let confirmPassword = $state('');
  let loading = $state(false);
  let validating = $state(true);
  let tokenValid = $state(false);
  let success = $state(false);
  let errors = $state<Record<string, string>>({});

  const token = $derived($page.url.searchParams.get('token') ?? '');

  onMount(async () => {
    if (!token) {
      tokenValid = false;
      validating = false;
      return;
    }

    try {
      tokenValid = await authApi.validateResetToken(token);
    } catch {
      tokenValid = false;
    }
    validating = false;
  });

  async function handleSubmit(e: Event) {
    e.preventDefault();
    errors = {};

    if (!password) {
      errors.password = 'Heslo je povinné';
    } else if (password.length < 6) {
      errors.password = 'Heslo musí mít alespoň 6 znaků';
    }

    if (password !== confirmPassword) {
      errors.confirmPassword = 'Hesla se neshodují';
    }

    if (Object.keys(errors).length > 0) return;

    loading = true;

    try {
      await authApi.resetPassword({ token, password });
      success = true;
      toast.success('Heslo bylo úspěšně změněno');
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.details) {
          errors = Object.fromEntries(
            Object.entries(err.details).map(([key, msgs]) => [key, msgs[0]])
          );
        } else {
          errors.password = err.message;
        }
      } else {
        errors.password = 'Nastala chyba. Zkuste to znovu.';
      }
    } finally {
      loading = false;
    }
  }
</script>

<svelte:head>
  <title>Obnova hesla | MůjDomeček</title>
  <meta name="description" content="Nastavení nového hesla pro účet MůjDomeček." />
  <meta name="robots" content="noindex, nofollow" />
</svelte:head>

<Card padding="lg">
  {#if validating}
    <div class="flex items-center justify-center py-8">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"></div>
    </div>
  {:else if !tokenValid}
    <div class="text-center">
      <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-red-50 dark:bg-red-950">
        <XCircle class="h-8 w-8 text-error" />
      </div>
      <h1 class="text-2xl font-semibold">Neplatný odkaz</h1>
      <p class="mt-2 text-sm text-foreground-muted">
        Odkaz pro obnovu hesla je neplatný nebo vypršel. Zkuste požádat o nový odkaz.
      </p>
      <div class="mt-6">
        <a href="/forgot-password">
          <Button fullWidth>
            {#snippet children()}
              Požádat o nový odkaz
            {/snippet}
          </Button>
        </a>
      </div>
    </div>
  {:else if success}
    <div class="text-center">
      <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-green-50 dark:bg-green-950">
        <CheckCircle class="h-8 w-8 text-success" />
      </div>
      <h1 class="text-2xl font-semibold">Heslo změněno</h1>
      <p class="mt-2 text-sm text-foreground-muted">
        Vaše heslo bylo úspěšně změněno. Nyní se můžete přihlásit.
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
      <h1 class="text-2xl font-semibold">Nové heslo</h1>
      <p class="mt-2 text-sm text-foreground-muted">
        Zadejte nové heslo pro svůj účet.
      </p>
    </div>

    <form onsubmit={handleSubmit} class="mt-8 space-y-4">
      <Input
        type="password"
        label="Nové heslo"
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

      <Button type="submit" fullWidth {loading}>
        {#snippet children()}
          Nastavit nové heslo
        {/snippet}
      </Button>
    </form>
  {/if}
</Card>
