<script lang="ts">
  import { Card, Button } from '$lib';
  import { UserPlus, Lock } from 'lucide-svelte';
  import type { Snippet } from 'svelte';

  interface Props {
    title?: string;
    description?: string;
    children?: Snippet;
  }

  let {
    title = 'Vyžaduje účet',
    description = 'Pro tuto funkci je potřeba vytvořit účet. Vaše data zůstanou lokálně a můžete je kdykoliv synchronizovat.',
    children
  }: Props = $props();
</script>

<Card class="border-amber-200 bg-amber-50/50 dark:border-amber-900 dark:bg-amber-950/30">
  <div class="flex flex-col items-center text-center sm:flex-row sm:text-left gap-4">
    <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-amber-100 dark:bg-amber-900">
      <Lock class="h-6 w-6 text-amber-600 dark:text-amber-400" />
    </div>
    <div class="flex-1">
      <p class="font-medium text-amber-800 dark:text-amber-200">{title}</p>
      <p class="mt-1 text-sm text-amber-700 dark:text-amber-300">{description}</p>
      {#if children}
        <div class="mt-2">
          {@render children()}
        </div>
      {/if}
    </div>
    <a href="/register" class="shrink-0">
      <Button size="sm">
        {#snippet children()}
          <UserPlus class="mr-2 h-4 w-4" />
          Vytvořit účet
        {/snippet}
      </Button>
    </a>
  </div>
</Card>
