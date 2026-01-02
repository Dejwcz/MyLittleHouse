<script lang="ts">
  import { Cloud, CloudOff, RefreshCw, AlertCircle, Check } from 'lucide-svelte';
  import Badge from './Badge.svelte';
  import type { SyncMode, SyncStatus } from '$lib/db';

  interface Props {
    syncMode: SyncMode;
    syncStatus: SyncStatus;
    size?: 'sm' | 'md';
    showLabel?: boolean;
  }

  let { syncMode, syncStatus, size = 'sm', showLabel = true }: Props = $props();
</script>

{#if syncMode === 'local-only'}
  <Badge variant="secondary" {size}>
    <CloudOff class="h-3 w-3 {showLabel ? 'mr-1' : ''}" />
    {#if showLabel}Lokální{/if}
  </Badge>
{:else if syncStatus === 'synced'}
  <Badge variant="success" {size}>
    <Check class="h-3 w-3 {showLabel ? 'mr-1' : ''}" />
    {#if showLabel}Synchronizováno{/if}
  </Badge>
{:else if syncStatus === 'syncing'}
  <Badge variant="info" {size}>
    <RefreshCw class="h-3 w-3 animate-spin {showLabel ? 'mr-1' : ''}" />
    {#if showLabel}Synchronizuji...{/if}
  </Badge>
{:else if syncStatus === 'pending'}
  <Badge variant="warning" {size}>
    <Cloud class="h-3 w-3 {showLabel ? 'mr-1' : ''}" />
    {#if showLabel}Čeká na sync{/if}
  </Badge>
{:else if syncStatus === 'failed'}
  <Badge variant="error" {size}>
    <AlertCircle class="h-3 w-3 {showLabel ? 'mr-1' : ''}" />
    {#if showLabel}Chyba sync{/if}
  </Badge>
{:else}
  <Badge variant="secondary" {size}>
    <Cloud class="h-3 w-3 {showLabel ? 'mr-1' : ''}" />
    {#if showLabel}Lokální{/if}
  </Badge>
{/if}
