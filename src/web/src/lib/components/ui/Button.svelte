<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import { Loader2 } from 'lucide-svelte';
  import type { Snippet } from 'svelte';
  import type { HTMLButtonAttributes } from 'svelte/elements';

  type Variant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'outline';
  type Size = 'sm' | 'md' | 'lg';

  interface Props extends HTMLButtonAttributes {
    variant?: Variant;
    size?: Size;
    loading?: boolean;
    fullWidth?: boolean;
    children: Snippet;
    class?: string;
  }

  let {
    variant = 'primary',
    size = 'md',
    loading = false,
    fullWidth = false,
    disabled,
    children,
    class: className,
    ...rest
  }: Props = $props();

  const baseStyles =
    'inline-flex items-center justify-center gap-2 rounded-full font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-border-focus focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50';

  const variants: Record<Variant, string> = {
    primary: 'bg-primary text-foreground-inverse hover:bg-primary-600 shadow-soft',
    secondary: 'bg-transparent text-foreground border border-foreground-muted hover:bg-bg-secondary',
    ghost: 'text-foreground border border-border hover:bg-bg-secondary',
    danger: 'bg-error text-foreground-inverse hover:bg-red-600',
    outline: 'border border-border text-foreground hover:bg-bg-secondary'
  };

  const sizes: Record<Size, string> = {
    sm: 'h-8 px-3 text-xs',
    md: 'h-10 px-4 text-sm',
    lg: 'h-12 px-6 text-base'
  };
</script>

<button
  class={cn(baseStyles, variants[variant], sizes[size], fullWidth && 'w-full', className)}
  disabled={disabled || loading}
  {...rest}
>
  {#if loading}
    <Loader2 class="h-4 w-4 animate-spin" />
  {/if}
  {@render children()}
</button>
