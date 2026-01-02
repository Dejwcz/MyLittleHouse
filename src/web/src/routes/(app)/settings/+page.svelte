<script lang="ts">
  import { PageHeader, Card, Toggle } from '$lib';
  import {
    User,
    Shield,
    Share2,
    Bell,
    Users,
    Palette,
    Database,
    ChevronRight
  } from 'lucide-svelte';

  const settingsGroups = [
    {
      title: 'Účet',
      items: [
        { href: '/settings/profile', icon: User, label: 'Profil', description: 'Jméno, email, fotka' },
        {
          href: '/settings/security',
          icon: Shield,
          label: 'Zabezpečení',
          description: 'Heslo, propojené účty'
        }
      ]
    },
    {
      title: 'Sdílení',
      items: [
        {
          href: '/settings/sharing',
          icon: Share2,
          label: 'Sdílení',
          description: 'Co sdílím, sdíleno se mnou'
        },
        {
          href: '/settings/contacts',
          icon: Users,
          label: 'Kontakty',
          description: 'Správa kontaktů a skupin'
        }
      ]
    },
    {
      title: 'Aplikace',
      items: [
        {
          href: '/settings/notifications',
          icon: Bell,
          label: 'Notifikace',
          description: 'Push a email notifikace'
        },
        {
          href: '/settings/appearance',
          icon: Palette,
          label: 'Vzhled',
          description: 'Téma, jazyk'
        },
        {
          href: '/settings/data',
          icon: Database,
          label: 'Data a sync',
          description: 'Synchronizace, export, smazání'
        }
      ]
    }
  ];
</script>

<PageHeader title="Nastavení" subtitle="Správa vašeho účtu a preferencí" />

<div class="space-y-8">
  {#each settingsGroups as group}
    <div>
      <h2 class="mb-3 text-sm font-medium text-foreground-muted">{group.title}</h2>
      <Card padding="none" class="divide-y divide-border">
        {#each group.items as item}
          <a
            href={item.href}
            class="flex items-center gap-4 p-4 transition-colors hover:bg-bg-secondary first:rounded-t-3xl last:rounded-b-3xl"
          >
            <div
              class="flex h-10 w-10 items-center justify-center rounded-2xl bg-bg-secondary"
            >
              <item.icon class="h-5 w-5 text-foreground-muted" />
            </div>
            <div class="flex-1">
              <p class="font-medium">{item.label}</p>
              <p class="text-sm text-foreground-muted">{item.description}</p>
            </div>
            <ChevronRight class="h-5 w-5 text-foreground-muted" />
          </a>
        {/each}
      </Card>
    </div>
  {/each}
</div>
