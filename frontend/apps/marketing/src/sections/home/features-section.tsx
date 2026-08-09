import {
  FileText,
  LayoutGrid,
  Users,
  Blocks,
  Zap,
  Bell,
  Shield,
  Search,
  Globe,
  type LucideIcon,
} from "lucide-react";

import { Badge } from "@notrelix/ui-web/components/ui/badge";

type Feature = {
  icon: LucideIcon;
  title: string;
  description: string;
  color: string;
};

const features: Feature[] = [
  {
    icon: FileText,
    title: "Block-based Editor",
    description:
      "Rich document editor with headings, lists, code blocks, callouts, toggles, and 13+ block types. Slash commands for rapid content creation.",
    color: "from-violet-500 to-indigo-500",
  },
  {
    icon: LayoutGrid,
    title: "Kanban Boards",
    description:
      "Visual project boards with drag-and-drop cards, labels, checklists, and due dates. Track work across lists with ease.",
    color: "from-emerald-500 to-teal-500",
  },
  {
    icon: Users,
    title: "Team Workspaces",
    description:
      "Invite members, assign roles, and collaborate in shared workspaces. Granular permission controls keep content secure.",
    color: "from-amber-500 to-orange-500",
  },
  {
    icon: Blocks,
    title: "Nested Pages",
    description:
      "Organize knowledge with unlimited page nesting. Build wikis, runbooks, and documentation hierarchies effortlessly.",
    color: "from-pink-500 to-rose-500",
  },
  {
    icon: Zap,
    title: "Automations",
    description:
      "Set up rules that trigger actions automatically — move cards, send notifications, update statuses, and more.",
    color: "from-cyan-500 to-blue-500",
  },
  {
    icon: Bell,
    title: "Smart Notifications",
    description:
      "Stay in the loop with real-time notifications for comments, mentions, due dates, and workspace activity.",
    color: "from-purple-500 to-fuchsia-500",
  },
  {
    icon: Shield,
    title: "Roles & Permissions",
    description:
      "Fine-grained RBAC with workspace-level and resource-level access control. Share pages and boards selectively.",
    color: "from-slate-500 to-zinc-600",
  },
  {
    icon: Search,
    title: "Universal Search",
    description:
      "Find anything instantly across pages, blocks, cards, and comments. Full-text search across your entire workspace.",
    color: "from-indigo-500 to-violet-500",
  },
  {
    icon: Globe,
    title: "Integrations",
    description:
      "Connect with Slack, Google Drive, GitHub, and more. Webhooks let you build custom workflows with any service.",
    color: "from-teal-500 to-emerald-500",
  },
];

export function FeaturesSection() {
  return (
    <section id="features" className="py-28 bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center mb-16">
          <Badge
            variant="outline"
            className="mb-4 text-xs font-semibold tracking-wider uppercase"
          >
            Features
          </Badge>
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-bold tracking-tight mb-5">
            Everything your team needs
          </h2>
          <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
            From rich document editing to visual project boards — Notrelix
            replaces a dozen tools with one cohesive workspace.
          </p>
        </div>

        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-5">
          {features.map((feature) => {
            const Icon = feature.icon;
            return (
              <div
                key={feature.title}
                className="group relative rounded-2xl border bg-card/50 backdrop-blur-sm p-6 hover:shadow-xl hover:shadow-violet-500/5 hover:-translate-y-0.5 transition-all duration-300"
              >
                <div
                  className={`flex items-center justify-center size-11 rounded-xl bg-gradient-to-br ${feature.color} mb-4 shadow-lg opacity-90 group-hover:opacity-100 group-hover:scale-105 transition-all`}
                >
                  <Icon className="size-5 text-white" />
                </div>
                <h3 className="text-lg font-semibold mb-2">{feature.title}</h3>
                <p className="text-sm text-muted-foreground leading-relaxed">
                  {feature.description}
                </p>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
