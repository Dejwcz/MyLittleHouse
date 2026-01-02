<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import type { Snippet } from 'svelte';
  import type { HTMLAttributes } from 'svelte/elements';

  type Padding = 'none' | 'sm' | 'md' | 'lg';

  interface Props extends HTMLAttributes<HTMLDivElement> {
    padding?: Padding;
    hover?: boolean;
    children: Snippet;
    class?: string;
  }

  let { padding = 'md', hover = false, children, class: className, ...rest }: Props = $props();

  const paddings: Record<Padding, string> = {
    none: '',
    sm: 'p-4',
    md: 'p-6',
    lg: 'p-8'
  };
</script>

<div
  class={cn(
    'rounded-3xl border border-border bg-surface',
    paddings[padding],
    hover && 'transition-shadow hover:shadow-soft cursor-pointer',
    className
  )}
  {...rest}
>
  {@render children()}
</div>
