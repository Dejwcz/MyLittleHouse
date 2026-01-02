<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import type { Snippet } from 'svelte';

  type Variant = 'default' | 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  type Size = 'sm' | 'md';

  interface Props {
    variant?: Variant;
    size?: Size;
    children: Snippet;
    class?: string;
  }

  let { variant = 'default', size = 'md', children, class: className }: Props = $props();

  const variants: Record<Variant, string> = {
    default: 'bg-bg-secondary text-foreground-secondary',
    primary: 'bg-primary-50 text-primary-700 dark:bg-primary-950 dark:text-primary-300',
    secondary: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
    success: 'bg-green-50 text-green-700 dark:bg-green-950 dark:text-green-300',
    warning: 'bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-300',
    error: 'bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-300',
    info: 'bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300'
  };

  const sizes: Record<Size, string> = {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-3 py-1 text-xs'
  };
</script>

<span class={cn('inline-flex items-center rounded-full font-semibold', variants[variant], sizes[size], className)}>
  {@render children()}
</span>
