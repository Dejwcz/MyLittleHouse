<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import { ArrowLeft } from 'lucide-svelte';
  import type { Snippet } from 'svelte';

  interface Props {
    title: string;
    subtitle?: string;
    backHref?: string;
    breadcrumb?: Snippet;
    actions?: Snippet;
    class?: string;
  }

  let { title, subtitle, backHref, breadcrumb, actions, class: className }: Props = $props();
</script>

<header class={cn('mb-6', className)}>
  {#if breadcrumb}
    <div class="mb-2">
      {@render breadcrumb()}
    </div>
  {/if}

  <div class="flex items-start justify-between gap-4">
    <div class="flex items-start gap-3">
      {#if backHref}
        <a
          href={backHref}
          class="mt-1 rounded-full p-1 text-foreground-muted hover:bg-bg-secondary hover:text-foreground transition-colors"
          aria-label="Zpět"
        >
          <ArrowLeft class="h-5 w-5" />
        </a>
      {/if}

      <div>
        <h1 class="text-2xl font-semibold text-foreground">
          {title}
        </h1>
        {#if subtitle}
          <p class="mt-1 text-sm text-foreground-muted">
            {subtitle}
          </p>
        {/if}
      </div>
    </div>

    {#if actions}
      <div class="flex flex-wrap items-center justify-end gap-2">
        {@render actions()}
      </div>
    {/if}
  </div>
</header>
