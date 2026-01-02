<script lang="ts">
  import { cn } from '$lib/utils/cn';

  type Size = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

  interface Props {
    src?: string | null;
    name: string;
    size?: Size;
    class?: string;
  }

  let { src, name, size = 'md', class: className }: Props = $props();

  const sizes: Record<Size, string> = {
    xs: 'h-6 w-6 text-xs',
    sm: 'h-8 w-8 text-xs',
    md: 'h-10 w-10 text-sm',
    lg: 'h-12 w-12 text-base',
    xl: 'h-16 w-16 text-lg'
  };

  // Generate initials from name
  const initials = $derived(
    name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2)
  );

  // Generate consistent color from name
  const colorIndex = $derived(
    name.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0) % 6
  );

  const colors = [
    'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
    'bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300',
    'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
    'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
    'bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300',
    'bg-pink-100 text-pink-700 dark:bg-pink-900 dark:text-pink-300'
  ];
</script>

{#if src}
  <img
    {src}
    alt={name}
    class={cn('rounded-full object-cover', sizes[size], className)}
  />
{:else}
  <div
    class={cn(
      'flex items-center justify-center rounded-full font-semibold',
      sizes[size],
      colors[colorIndex],
      className
    )}
    title={name}
  >
    {initials}
  </div>
{/if}
