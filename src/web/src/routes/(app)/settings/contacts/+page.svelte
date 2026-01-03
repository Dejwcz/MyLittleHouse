<script lang="ts">
  import { PageHeader, Card, Button, Input, Avatar, RegistrationRequired } from '$lib';
  import { auth } from '$lib/stores/auth.svelte';
  import { Users, Plus, Search, Mail, Phone, MoreVertical } from 'lucide-svelte';

  const isGuest = $derived(auth.isGuest);
  let searchQuery = $state('');

  // TODO: Load from API/IndexedDB
  const contacts = [
    { id: '1', name: 'Eva Nováková', email: 'eva@example.cz', phone: '+420 111 222 333' },
    { id: '2', name: 'Jan Svoboda', email: 'jan@example.cz', phone: '+420 444 555 666' },
    { id: '3', name: 'Petr Řemeslník', email: 'petr@remeslo.cz', phone: '+420 777 888 999' }
  ];

  const filteredContacts = $derived(
    contacts.filter(c =>
      c.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      c.email.toLowerCase().includes(searchQuery.toLowerCase())
    )
  );
</script>

<PageHeader
  title="Kontakty"
  subtitle="Správa kontaktů pro sdílení"
  backHref="/settings"
>
  {#snippet actions()}
    <Button size="sm">
      {#snippet children()}
        <Plus class="mr-2 h-4 w-4" />
        Přidat kontakt
      {/snippet}
    </Button>
  {/snippet}
</PageHeader>

<div class="mx-auto max-w-2xl space-y-6">
  {#if isGuest}
    <RegistrationRequired
      title="Kontakty vyžadují účet"
      description="Pro správu kontaktů a jejich použití při sdílení je potřeba vytvořit účet."
    />
  {/if}

  <div class="relative {isGuest ? 'opacity-50 pointer-events-none' : ''}">
    <Search class="absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-foreground-muted" />
    <input
      type="text"
      placeholder="Hledat kontakty..."
      bind:value={searchQuery}
      class="w-full rounded-2xl border border-border bg-surface py-3 pl-10 pr-4 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
    />
  </div>

  <Card padding="none" class={isGuest ? 'opacity-50 pointer-events-none' : ''}>
    {#if filteredContacts.length === 0}
      <div class="p-8 text-center">
        <Users class="mx-auto h-12 w-12 text-foreground-muted" />
        <p class="mt-4 font-medium">Žádné kontakty</p>
        <p class="mt-1 text-sm text-foreground-muted">
          {searchQuery ? 'Zkuste jiný vyhledávací dotaz.' : 'Přidejte svůj první kontakt.'}
        </p>
      </div>
    {:else}
      <div class="divide-y divide-border">
        {#each filteredContacts as contact}
          <div class="flex items-center justify-between p-4 hover:bg-bg-secondary transition-colors">
            <div class="flex items-center gap-3">
              <Avatar name={contact.name} size="md" />
              <div>
                <p class="font-medium">{contact.name}</p>
                <div class="flex items-center gap-3 text-sm text-foreground-muted">
                  <span class="flex items-center gap-1">
                    <Mail class="h-3 w-3" />
                    {contact.email}
                  </span>
                  {#if contact.phone}
                    <span class="flex items-center gap-1">
                      <Phone class="h-3 w-3" />
                      {contact.phone}
                    </span>
                  {/if}
                </div>
              </div>
            </div>
            <button class="rounded-full p-2 hover:bg-bg-secondary">
              <MoreVertical class="h-5 w-5 text-foreground-muted" />
            </button>
          </div>
        {/each}
      </div>
    {/if}
  </Card>
</div>
