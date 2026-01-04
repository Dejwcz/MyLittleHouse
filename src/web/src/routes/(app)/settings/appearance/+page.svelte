<script lang="ts">
  import { PageHeader, Card } from '$lib';
  import { theme } from '$lib/stores/theme.svelte';
  import { Sun, Moon, Monitor, Languages } from 'lucide-svelte';

  const themes = [
    { id: 'light', label: 'Světlý', icon: Sun },
    { id: 'dark', label: 'Tmavý', icon: Moon },
    { id: 'system', label: 'Systémový', icon: Monitor }
  ];

  const languages = [
    { id: 'cs', label: 'Čeština', flag: '🇨🇿', disabled: false },
    { id: 'en', label: 'English', flag: '🇬🇧', disabled: true, note: 'AJ verze se připravuje' }
  ];

  let selectedTheme = $state(theme.preference);
  let selectedLanguage = $state('cs');

  function setTheme(id: string) {
    selectedTheme = id;
    theme.setTheme(id === 'light' || id === 'dark' ? id : 'system');
  }
</script>

<PageHeader
  title="Vzhled"
  subtitle="Téma a jazyk aplikace"
  backHref="/settings"
/>

<div class="mx-auto max-w-2xl space-y-6">
  <Card>
    <h2 class="mb-4 font-medium">Téma</h2>
    <div class="grid grid-cols-3 gap-3">
      {#each themes as t}
        <button
          onclick={() => setTheme(t.id)}
          class="flex flex-col items-center gap-2 rounded-2xl border-2 p-4 transition-colors {selectedTheme === t.id ? 'border-primary bg-primary-50 dark:bg-primary-950' : 'border-border hover:border-foreground-muted'}"
        >
          <t.icon class="h-6 w-6 {selectedTheme === t.id ? 'text-primary' : 'text-foreground-muted'}" />
          <span class="text-sm font-medium">{t.label}</span>
        </button>
      {/each}
    </div>
  </Card>

  <Card>
    <div class="mb-4 flex items-center gap-3">
      <Languages class="h-5 w-5 text-foreground-muted" />
      <h2 class="font-medium">Jazyk</h2>
    </div>
    <div class="space-y-2">
      {#each languages as lang}
        <button
          onclick={() => !lang.disabled && (selectedLanguage = lang.id)}
          disabled={lang.disabled}
          class="flex w-full items-center gap-3 rounded-xl border-2 p-3 transition-colors disabled:cursor-not-allowed disabled:opacity-60 {selectedLanguage === lang.id ? 'border-primary bg-primary-50 dark:bg-primary-950' : 'border-border hover:border-foreground-muted'}"
        >
          <span class="text-2xl">{lang.flag}</span>
          <div class="text-left">
            <div class="font-medium">{lang.label}</div>
            {#if lang.note}
              <div class="text-xs text-foreground-muted">{lang.note}</div>
            {/if}
          </div>
        </button>
      {/each}
    </div>
  </Card>
</div>
