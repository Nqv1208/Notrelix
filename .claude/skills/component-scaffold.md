---
skill: component-scaffold
description: Generate React components following Notrelix conventions and feature-sliced design
version: 1.0.0
---

# Component Scaffolding

Generate React components following Notrelix's feature-sliced design and naming conventions.

## When to Use

- Creating new UI components
- Need to follow established component patterns
- Want proper file location and naming automatically
- Creating components with shadcn/ui integration

## What This Skill Does

1. Determines correct component location (app/_components vs components/)
2. Generates component with TypeScript
3. Follows naming conventions (kebab-case files, PascalCase components)
4. Includes proper imports and exports
5. Integrates with shadcn/ui when needed
6. Sets up proper 'use client' directive when required

## Component Location Strategy

### app/_components/ (Route-Specific, Private)

Use for components that are:
- Only used in a specific route or route group
- Tightly coupled to a specific page
- Not reusable across the application

**Examples:**
- `app/(auth)/_components/sign-in-form.tsx` — Only used in sign-in page
- `app/(dashboard)/_components/app-sidebar.tsx` — Only used in dashboard layout
- `app/(workspace)/[workspaceId]/boards/[boardId]/_components/board-toolbar.tsx` — Only used in board page

### components/ (Shared, Public)

Use for components that are:
- Used in 2 or more different routes
- Generic and reusable
- Part of the design system

**Examples:**
- `components/ui/button.tsx` — shadcn/ui primitive
- `components/shared/notification-list.tsx` — Used in multiple layouts
- `components/boards/kanban-card.tsx` — Used in board views and dashboard

## Naming Conventions

### Files

- **Format:** kebab-case
- **Examples:** `sign-in-form.tsx`, `board-toolbar.tsx`, `notification-bell.tsx`

### Components

- **Format:** PascalCase
- **Examples:** `SignInForm`, `BoardToolbar`, `NotificationBell`

### Props Interface

- **Format:** `{ComponentName}Props` or just `Props` (if defined inline)
- **Examples:** `SignInFormProps`, `BoardToolbarProps`

## Template: Client Component

```typescript
// File: app/(dashboard)/_components/notification-bell.tsx
'use client';

import { useState } from 'react';
import { Bell } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { useNotifications } from '@/features/notifications/hooks/use-notifications';
import { useUnreadCount } from '@/features/notifications/hooks/use-unread-count';

interface NotificationBellProps {
  userId: string;
}

export function NotificationBell({ userId }: NotificationBellProps) {
  const [open, setOpen] = useState(false);
  const { data: unreadCount } = useUnreadCount(userId);
  const { data: notifications } = useNotifications(userId, { enabled: open });

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button variant="ghost" size="icon" className="relative">
          <Bell className="h-5 w-5" />
          {unreadCount > 0 && (
            <span className="absolute -right-1 -top-1 flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-xs text-white">
              {unreadCount > 9 ? '9+' : unreadCount}
            </span>
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-80" align="end">
        <div className="space-y-2">
          <h3 className="font-semibold">Notifications</h3>
          {notifications?.length === 0 ? (
            <p className="text-sm text-muted-foreground">No notifications</p>
          ) : (
            <div className="space-y-2">
              {notifications?.map((notification) => (
                <div key={notification.id} className="text-sm">
                  {notification.message}
                </div>
              ))}
            </div>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}
```

## Template: Server Component

```typescript
// File: app/(workspace)/[workspaceId]/_components/workspace-overview.tsx
import { Suspense } from 'react';
import { getQueryClient } from '@/lib/query/server-query-client';
import { queryKeys } from '@/lib/query/query-keys';
import { workspacesApi } from '@/features/workspace/api/workspaces-api';
import { RecentPages } from './recent-pages';
import { ActiveBoards } from './active-boards';
import { WorkspaceStats } from './workspace-stats';

interface WorkspaceOverviewProps {
  workspaceId: string;
}

export async function WorkspaceOverview({ workspaceId }: WorkspaceOverviewProps) {
  // Prefetch data on server
  const queryClient = getQueryClient();
  
  await queryClient.prefetchQuery({
    queryKey: queryKeys.workspaces.detail(workspaceId),
    queryFn: () => workspacesApi.getWorkspace(workspaceId),
  });

  return (
    <div className="space-y-8">
      <Suspense fallback={<div>Loading stats...</div>}>
        <WorkspaceStats workspaceId={workspaceId} />
      </Suspense>

      <div className="grid gap-8 md:grid-cols-2">
        <Suspense fallback={<div>Loading pages...</div>}>
          <RecentPages workspaceId={workspaceId} />
        </Suspense>

        <Suspense fallback={<div>Loading boards...</div>}>
          <ActiveBoards workspaceId={workspaceId} />
        </Suspense>
      </div>
    </div>
  );
}
```

## Template: Form Component

```typescript
// File: app/(auth)/_components/sign-in-form.tsx
'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { useLogin } from '@/features/auth/hooks/use-login';
import { signInSchema, type SignInInput } from '@/features/auth/schemas/sign-in.schema';

export function SignInForm() {
  const router = useRouter();
  const { mutate: login, isPending } = useLogin();

  const form = useForm<SignInInput>({
    resolver: zodResolver(signInSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  const onSubmit = (data: SignInInput) => {
    login(data, {
      onSuccess: () => {
        router.push('/home');
      },
    });
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input
                  type="email"
                  placeholder="you@example.com"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="password"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Password</FormLabel>
              <FormControl>
                <Input type="password" placeholder="••••••••" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" className="w-full" disabled={isPending}>
          {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Sign In
        </Button>
      </form>
    </Form>
  );
}
```

## Template: Shared Component

```typescript
// File: components/shared/empty-state.tsx
import { LucideIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface EmptyStateProps {
  icon: LucideIcon;
  title: string;
  description: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}

export function EmptyState({ icon: Icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-12 text-center">
      <div className="rounded-full bg-muted p-4">
        <Icon className="h-8 w-8 text-muted-foreground" />
      </div>
      <h3 className="mt-4 text-lg font-semibold">{title}</h3>
      <p className="mt-2 text-sm text-muted-foreground">{description}</p>
      {action && (
        <Button onClick={action.onClick} className="mt-4">
          {action.label}
        </Button>
      )}
    </div>
  );
}
```

## Important Rules

### DO

- ✅ Use Server Components by default (no 'use client')
- ✅ Add 'use client' only when needed (useState, useEffect, event handlers, browser APIs)
- ✅ Use kebab-case for file names
- ✅ Use PascalCase for component names
- ✅ Define Props interface before component
- ✅ Use shadcn/ui components when available
- ✅ Use Lucide React for icons
- ✅ Use Tailwind CSS for styling
- ✅ Export component as named export
- ✅ Use async/await for Server Components that fetch data
- ✅ Use Suspense for loading states in Server Components

### DON'T

- ❌ Don't add 'use client' to Server Components unnecessarily
- ❌ Don't use HTML `<form>` tag (use button onClick instead)
- ❌ Don't import from features/ in components/ (only in app/)
- ❌ Don't create components in features/ (features = logic only)
- ❌ Don't use inline styles (use Tailwind classes)
- ❌ Don't use default exports (use named exports)
- ❌ Don't forget to handle loading and error states
- ❌ Don't use `any` type

## When to Use 'use client'

Add 'use client' directive when component uses:

- **React hooks:** useState, useEffect, useContext, useReducer, etc.
- **Event handlers:** onClick, onChange, onSubmit, etc.
- **Browser APIs:** window, document, localStorage, etc.
- **Third-party libraries:** Most client-side libraries
- **TanStack Query hooks:** useQuery, useMutation (client-side data fetching)

**Don't add 'use client' for:**
- Components that only render JSX
- Components that fetch data on server
- Components that use Server Components features (async/await)

## shadcn/ui Integration

### Available Components

Notrelix has 54 shadcn/ui components installed:

```
accordion, alert, alert-dialog, aspect-ratio, avatar, badge, breadcrumb,
button, calendar, card, carousel, chart, checkbox, collapsible, command,
context-menu, dialog, drawer, dropdown-menu, form, hover-card, input,
input-otp, label, menubar, navigation-menu, pagination, popover, progress,
radio-group, resizable, scroll-area, select, separator, sheet, skeleton,
slider, sonner, switch, table, tabs, textarea, toast, toggle, toggle-group,
tooltip
```

### Using shadcn/ui Components

```typescript
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
```

### Adding New shadcn/ui Components

```bash
cd frontend
npx shadcn@latest add {component-name}

# Examples:
npx shadcn@latest add button
npx shadcn@latest add dialog
npx shadcn@latest add form
```

## Common Patterns

### Loading State

```typescript
'use client';

import { Loader2 } from 'lucide-react';
import { useBoard } from '@/features/boards/hooks/use-board';

export function BoardDetail({ boardId }: { boardId: string }) {
  const { data: board, isLoading, error } = useBoard(boardId);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error) {
    return <div>Error: {error.message}</div>;
  }

  return <div>{board.title}</div>;
}
```

### Error Boundary

```typescript
'use client';

import { useEffect } from 'react';
import { Button } from '@/components/ui/button';

interface ErrorBoundaryProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function Error({ error, reset }: ErrorBoundaryProps) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="flex flex-col items-center justify-center py-12">
      <h2 className="text-lg font-semibold">Something went wrong!</h2>
      <p className="mt-2 text-sm text-muted-foreground">{error.message}</p>
      <Button onClick={reset} className="mt-4">
        Try again
      </Button>
    </div>
  );
}
```

### Skeleton Loading

```typescript
import { Skeleton } from '@/components/ui/skeleton';

export function BoardSkeleton() {
  return (
    <div className="space-y-4">
      <Skeleton className="h-8 w-64" />
      <div className="grid gap-4 md:grid-cols-3">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-32" />
        ))}
      </div>
    </div>
  );
}
```

### Conditional Rendering

```typescript
export function BoardToolbar({ board }: { board: Board }) {
  const { data: user } = useMe();
  const isOwner = board.createdBy === user?.id;

  return (
    <div className="flex items-center gap-2">
      <h1>{board.title}</h1>
      {isOwner && (
        <Button variant="ghost" size="sm">
          Settings
        </Button>
      )}
    </div>
  );
}
```

## Styling Guidelines

### Tailwind CSS Classes

Use Tailwind utility classes following the design system:

```typescript
// Spacing (from DESIGN.md)
className="space-y-8"        // 32px vertical gap (section spacing)
className="space-y-4"        // 16px vertical gap (element spacing)
className="p-6"              // 24px padding (card padding)

// Colors (from DESIGN.md)
className="bg-brand-violet"  // Primary brand color
className="text-muted-foreground"  // Secondary text
className="border-silver"    // Border color

// Typography
className="text-lg font-semibold"  // Subheading
className="text-sm text-muted-foreground"  // Caption

// Radius
className="rounded-card"     // 16px (cards)
className="rounded-button"   // 8px (buttons)
```

### Design Tokens

Reference design tokens from DESIGN.md:

- **Colors:** `--color-brand-violet`, `--color-paper`, `--color-fog`
- **Spacing:** `--spacing-24`, `--spacing-32`, `--spacing-48`
- **Radius:** `--radius-card`, `--radius-button`, `--radius-pill`
- **Shadows:** `--shadow-sm`, `--shadow-md`, `--shadow-lg`

## Checklist

When creating a component, ensure:

- [ ] File name is kebab-case
- [ ] Component name is PascalCase
- [ ] Props interface defined
- [ ] 'use client' added only if needed
- [ ] Proper imports from @/ alias
- [ ] shadcn/ui components used when available
- [ ] Tailwind CSS for styling
- [ ] Loading states handled
- [ ] Error states handled
- [ ] TypeScript types defined (no `any`)
- [ ] Component exported as named export
- [ ] Placed in correct location (app/_components vs components/)

## Examples

### Example 1: Route-Specific Component

**User Request:** "Create a board toolbar component for the board page"

**Generated File:** `app/(workspace)/[workspaceId]/boards/[boardId]/_components/board-toolbar.tsx`

**Reason:** Only used in board page, tightly coupled to board route

### Example 2: Shared Component

**User Request:** "Create a reusable avatar group component"

**Generated File:** `components/shared/avatar-group.tsx`

**Reason:** Will be used in multiple places (board members, workspace members, etc.)

### Example 3: Form Component

**User Request:** "Create a create board dialog"

**Generated File:** `app/(workspace)/[workspaceId]/boards/_components/create-board-dialog.tsx`

**Reason:** Specific to boards list page, uses form with validation

## Related Skills

- `frontend-feature` — Create hooks and API clients that components use
- `backend-cqrs` — Create backend endpoints that components call

## References

- [AGENTS.md](../../AGENTS.md) — Section 4: Frontend Rules
- [DESIGN.md](../../DESIGN.md) — Design system and styling guidelines
- [notrelix-frontend-structure.md](../../notrelix-frontend-structure.md) — Detailed architecture
- [shadcn/ui Docs](https://ui.shadcn.com/)
