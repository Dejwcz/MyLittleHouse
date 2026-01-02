<script lang="ts">
  import { Card, Button, Input } from '$lib';
  import { toast } from '$lib/stores/ui.svelte';
  import { authApi, ApiError } from '$lib/api';
  import { goto } from '$app/navigation';

  let email = $state('');
  let password = $state('');
  let loading = $state(false);
  let errors = $state<Record<string, string>>({});

  async function handleSubmit(e: Event) {
    e.preventDefault();
    errors = {};

    if (!email) {
      errors.email = 'Email je povinný';
    }
    if (!password) {
      errors.password = 'Heslo je povinné';
    }

    if (Object.keys(errors).length > 0) {
      return;
    }

    loading = true;

    try {
      await authApi.login({ email, password });
      toast.success('Přihlášení úspěšné!');
      goto('/dashboard');
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 401) {
          errors.password = 'Neplatný email nebo heslo';
        } else if (err.details) {
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
  <title>Přihlášení | MůjDomeček</title>
</svelte:head>

<Card padding="lg">
  <div class="text-center">
    <h1 class="text-2xl font-semibold">Vítejte zpět</h1>
    <p class="mt-2 text-sm text-foreground-muted">
      Přihlaste se do svého účtu
    </p>
  </div>

  <form onsubmit={handleSubmit} class="mt-8 space-y-4">
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
      placeholder="Vaše heslo"
      bind:value={password}
      error={errors.password}
      autocomplete="current-password"
    />

    <div class="flex items-center justify-between text-sm">
      <label class="flex items-center gap-2">
        <input type="checkbox" class="rounded border-border" />
        <span class="text-foreground-secondary">Zapamatovat si mě</span>
      </label>
      <a href="/forgot-password" class="text-primary hover:underline">
        Zapomenuté heslo?
      </a>
    </div>

    <Button type="submit" fullWidth {loading}>
      {#snippet children()}
        Přihlásit se
      {/snippet}
    </Button>
  </form>

  <div class="relative my-6">
    <div class="absolute inset-0 flex items-center">
      <div class="w-full border-t border-border"></div>
    </div>
    <div class="relative flex justify-center text-xs">
      <span class="bg-surface px-2 text-foreground-muted">nebo</span>
    </div>
  </div>

  <div class="space-y-3">
    <Button variant="outline" fullWidth>
      {#snippet children()}
        <svg class="h-5 w-5" viewBox="0 0 24 24">
          <path
            fill="currentColor"
            d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
          />
          <path
            fill="currentColor"
            d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
          />
          <path
            fill="currentColor"
            d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
          />
          <path
            fill="currentColor"
            d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
          />
        </svg>
        Pokračovat s Google
      {/snippet}
    </Button>

    <Button variant="outline" fullWidth>
      {#snippet children()}
        <svg class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor">
          <path
            d="M17.05 20.28c-.98.95-2.05.8-3.08.35-1.09-.46-2.09-.48-3.24 0-1.44.62-2.2.44-3.06-.35C2.79 15.25 3.51 7.59 9.05 7.31c1.35.07 2.29.74 3.08.8 1.18-.24 2.31-.93 3.57-.84 1.51.12 2.65.72 3.4 1.8-3.12 1.87-2.38 5.98.48 7.13-.57 1.5-1.31 2.99-2.54 4.09l.01-.01zM12.03 7.25c-.15-2.23 1.66-4.07 3.74-4.25.29 2.58-2.34 4.5-3.74 4.25z"
          />
        </svg>
        Pokračovat s Apple
      {/snippet}
    </Button>
  </div>

  <p class="mt-6 text-center text-sm text-foreground-muted">
    Nemáte účet?
    <a href="/register" class="text-primary hover:underline">Zaregistrujte se</a>
  </p>
</Card>
