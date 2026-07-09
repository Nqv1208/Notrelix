import { FileText, LayoutGrid, MessageSquare, Zap, Shield, Globe } from 'lucide-react';

const features = [
  {
    icon: <FileText className="size-6" />,
    title: 'Document Editor',
    description: 'Rich block-based editor with slash commands, comments, and real-time collaboration.',
  },
  {
    icon: <LayoutGrid className="size-6" />,
    title: 'Project Boards',
    description: 'Kanban, table, calendar, and timeline views over the same work data.',
  },
  {
    icon: <MessageSquare className="size-6" />,
    title: 'Team Collaboration',
    description: 'Comments, mentions, and notifications keep everyone in the loop.',
  },
  {
    icon: <Zap className="size-6" />,
    title: 'Automation',
    description: 'Trigger-action workflows reduce repetitive tasks automatically.',
  },
  {
    icon: <Shield className="size-6" />,
    title: 'Enterprise Security',
    description: 'Role-based access control, audit logs, and SSO support.',
  },
  {
    icon: <Globe className="size-6" />,
    title: 'Integrations',
    description: 'Connect with Slack, GitHub, Jira, and your favorite tools.',
  },
];

export function FeaturesSection() {
  return (
    <section id="features" className="py-24 bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center mb-16">
          <h2 className="text-3xl sm:text-4xl font-bold tracking-tight mb-4">
            Everything you need to ship
          </h2>
          <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
            One workspace for documents, projects, and team collaboration.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {features.map((feature) => (
            <div
              key={feature.title}
              className="p-6 rounded-2xl border bg-card hover:shadow-lg transition-shadow"
            >
              <div className="size-12 flex items-center justify-center rounded-xl bg-primary/10 text-primary mb-4">
                {feature.icon}
              </div>
              <h3 className="text-lg font-semibold mb-2">{feature.title}</h3>
              <p className="text-muted-foreground">{feature.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
