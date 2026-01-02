<script lang="ts">
  import { cn } from '$lib/utils/cn';
  import { Loader2 } from 'lucide-svelte';

  type Size = 'sm' | 'md' | 'lg';

  interface Props {
    size?: Size;
    class?: string;
  }

  let { size = 'md', class: className }: Props = $props();

  const sizes: Record<Size, string> = {
    sm: 'h-4 w-4',
    md: 'h-6 w-6',
    lg: 'h-8 w-8'
  };
</script>

<Loader2 class={cn('animate-spin text-primary', sizes[size], className)} />
