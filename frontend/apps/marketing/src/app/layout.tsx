import type { Metadata } from 'next';
import { MarketingHeader } from '../components/marketing-header';
import { MarketingFooter } from '../components/marketing-footer';
import '../styles/globals.css';

export const metadata: Metadata = {
  title: 'Notrelix — Docs, Tasks & Boards in One Workspace',
  description:
    'Notrelix combines the power of a Notion-like document editor with Trello-style project boards. Write, plan, and ship — all in one place.',
  keywords: [
    'project management',
    'notion alternative',
    'trello alternative',
    'wiki',
    'kanban',
    'document editor',
    'collaboration',
    'SaaS',
    'productivity',
  ],
  authors: [{ name: 'Notrelix Team' }],
  openGraph: {
    title: 'Notrelix — Docs, Tasks & Boards in One Workspace',
    description:
      'Write docs like Notion. Manage tasks like Trello. Collaborate like a team that ships.',
    type: 'website',
    siteName: 'Notrelix',
  },
  twitter: {
    card: 'summary_large_image',
    title: 'Notrelix — Docs, Tasks & Boards in One Workspace',
    description:
      'Write docs like Notion. Manage tasks like Trello. Collaborate like a team that ships.',
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <MarketingHeader />
        {children}
        <MarketingFooter />
      </body>
    </html>
  );
}
