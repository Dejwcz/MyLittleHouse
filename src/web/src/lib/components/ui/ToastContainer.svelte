<script lang="ts">
  import { ui } from '$lib/stores/ui.svelte';
  import Toast from './Toast.svelte';
</script>

{#if ui.toasts.length > 0}
  <div class="fixed top-4 right-4 z-[100] flex flex-col gap-2 w-full max-w-sm">
    {#each ui.toasts as toast (toast.id)}
      <Toast id={toast.id} type={toast.type} message={toast.message} />
    {/each}
  </div>
{/if}
