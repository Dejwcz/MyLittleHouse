<script lang="ts">
  import { PageHeader, Card, Button, Badge, EmptyState, cn } from '$lib';
  import { Check, Bell, UserPlus, MessageSquare, FileText, Trash2 } from 'lucide-svelte';

  // TODO: Load notifications from API when implemented
  const notifications: { id: string; type: string; title: string; message: string; time: string; read: boolean }[] = [];

  const icons: Record<string, typeof Bell> = {
    invitation: UserPlus,
    comment: MessageSquare,
    reminder: FileText
  };

  function markAllRead() {
    // TODO: implement
  }
</script>

<PageHeader title="Notifikace" subtitle="Všechna vaše upozornění">
  {#snippet actions()}
    <Button variant="ghost" onclick={markAllRead}>
      {#snippet children()}
        <Check class="h-4 w-4" />
        Označit vše jako přečtené
      {/snippet}
    </Button>
  {/snippet}
</PageHeader>

{#if notifications.length === 0}
  <EmptyState
    title="Žádné notifikace"
    description="Zatím nemáte žádná upozornění"
    icon={Bell}
  />
{:else}
  <div class="space-y-2">
    {#each notifications as notification}
      {@const Icon = icons[notification.type] || Bell}
      <Card
        class={notification.read ? 'opacity-60' : ''}
        hover
      >
        <div class="flex items-start gap-4">
          <div
            class={cn(
              'flex h-10 w-10 items-center justify-center rounded-full',
              notification.read
                ? 'bg-bg-secondary'
                : 'bg-primary-50 dark:bg-primary-950'
            )}
          >
            <Icon
              class={cn(
                'h-5 w-5',
                notification.read ? 'text-foreground-muted' : 'text-primary'
              )}
            />
          </div>
          <div class="flex-1">
            <div class="flex items-center gap-2">
              <p class="font-medium">{notification.title}</p>
              {#if !notification.read}
                <span class="h-2 w-2 rounded-full bg-primary"></span>
              {/if}
            </div>
            <p class="mt-1 text-sm text-foreground-muted">{notification.message}</p>
            <p class="mt-2 text-xs text-foreground-muted">{notification.time}</p>
          </div>
          <button
            class="rounded-full p-2 text-foreground-muted hover:bg-bg-secondary hover:text-foreground"
          >
            <Trash2 class="h-4 w-4" />
          </button>
        </div>
      </Card>
    {/each}
  </div>
{/if}
