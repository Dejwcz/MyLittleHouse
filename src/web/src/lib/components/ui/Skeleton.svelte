<script lang="ts">
  import { cn } from '$lib/utils/cn';

  type Variant = 'text' | 'circular' | 'rectangular' | 'card';

  interface Props {
    variant?: Variant;
    width?: string;
    height?: string;
    lines?: number;
    class?: string;
  }

  let { variant = 'text', width, height, lines = 1, class: className }: Props = $props();

  const baseStyles = 'animate-pulse bg-bg-secondary';

  const variants: Record<Variant, string> = {
    text: 'h-4 rounded',
    circular: 'rounded-full',
    rectangular: 'rounded-2xl',
    card: 'rounded-3xl h-32'
  };
</script>

{#if variant === 'text' && lines > 1}
  <div class={cn('space-y-2', className)}>
    {#each Array(lines) as _, i}
      <div
        class={cn(baseStyles, variants.text, i === lines - 1 && 'w-3/4')}
        style:width={i !== lines - 1 ? width : undefined}
        style:height={height}
      ></div>
    {/each}
  </div>
{:else}
  <div
    class={cn(baseStyles, variants[variant], className)}
    style:width={width}
    style:height={height}
  ></div>
{/if}
