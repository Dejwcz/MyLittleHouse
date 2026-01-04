<script lang="ts">
  import { Card, Button, Input } from '$lib';
  import { toast } from '$lib/stores/ui.svelte';
  import { authApi, ApiError } from '$lib/api';
  import { ArrowLeft, Mail } from 'lucide-svelte';

  let email = $state('');
  let loading = $state(false);
  let sent = $state(false);
  let error = $state('');

  async function handleSubmit(e: Event) {
    e.preventDefault();
    error = '';

    if (!email) {
      error = 'Email je povinný';
      return;
    }

    loading = true;

    try {
      await authApi.forgotPassword({ email });
      sent = true;
      toast.success('Odkaz pro obnovu hesla byl odeslán');
    } catch (err) {
      if (err instanceof ApiError) {
        error = err.message;
      } else {
        error = 'Nastala chyba. Zkuste to znovu.';
      }
    } finally {
      loading = false;
    }
  }
</script>

<svelte:head>
  <title>Obnova hesla | MůjDomeček</title>
  <meta name="description" content="Obnovení hesla účtu MůjDomeček." />
  <meta name="robots" content="noindex, nofollow" />
</svelte:head>

<Card padding="lg">
  {#if sent}
    <div class="text-center">
      <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-primary-50 dark:bg-primary-950">
        <Mail class="h-8 w-8 text-primary" />
      </div>
      <h1 class="text-2xl font-semibold">Zkontrolujte email</h1>
      <p class="mt-2 text-sm text-foreground-muted">
        Pokud účet s emailem <strong>{email}</strong> existuje, poslali jsme vám odkaz pro obnovu hesla.
      </p>
      <p class="mt-4 text-sm text-foreground-muted">
        Odkaz je platný 1 hodinu.
      </p>
      <div class="mt-6">
        <a href="/login">
          <Button variant="secondary" fullWidth>
            {#snippet children()}
              <ArrowLeft class="h-4 w-4" />
              Zpět na přihlášení
            {/snippet}
          </Button>
        </a>
      </div>
    </div>
  {:else}
    <div class="text-center">
      <h1 class="text-2xl font-semibold">Zapomenuté heslo</h1>
      <p class="mt-2 text-sm text-foreground-muted">
        Zadejte svůj email a pošleme vám odkaz pro obnovu hesla.
      </p>
    </div>

    <form onsubmit={handleSubmit} class="mt-8 space-y-4">
      <Input
        type="email"
        label="Email"
        placeholder="vas@email.cz"
        bind:value={email}
        error={error}
        autocomplete="email"
      />

      <Button type="submit" fullWidth {loading}>
        {#snippet children()}
          Odeslat odkaz
        {/snippet}
      </Button>
    </form>

    <p class="mt-6 text-center text-sm text-foreground-muted">
      <a href="/login" class="text-primary hover:underline">
        <ArrowLeft class="mr-1 inline h-4 w-4" />
        Zpět na přihlášení
      </a>
    </p>
  {/if}
</Card>
