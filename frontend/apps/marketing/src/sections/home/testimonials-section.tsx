import { Star } from 'lucide-react'

import { Badge } from '@notrelix/ui-web/components/ui/badge'
import {
  Avatar,
  AvatarFallback,
} from '@notrelix/ui-web/components/ui/avatar'

type Testimonial = {
  name: string
  role: string
  company: string
  avatar: string
  content: string
  gradient: string
}

const testimonials: Testimonial[] = [
  {
    name: 'Sarah Chen',
    role: 'Engineering Manager',
    company: 'Stripe',
    avatar: 'SC',
    content:
      'We replaced Notion AND Trello with Notrelix. Having docs and boards in the same workspace is a game changer for sprint planning.',
    gradient: 'from-violet-500 to-indigo-500',
  },
  {
    name: 'Marcus Rivera',
    role: 'Product Lead',
    company: 'Vercel',
    avatar: 'MR',
    content:
      'The block editor is incredibly smooth. Slash commands, drag-and-drop, nested pages — it feels like the future of team wikis.',
    gradient: 'from-emerald-500 to-teal-500',
  },
  {
    name: 'Yuki Tanaka',
    role: 'Head of Design',
    company: 'Linear',
    avatar: 'YT',
    content:
      "Cleanest UI I've seen in a productivity tool. Our design team adopted it overnight. The kanban boards are chef's kiss.",
    gradient: 'from-pink-500 to-rose-500',
  },
  {
    name: 'Alex Kim',
    role: 'CTO',
    company: 'Railway',
    avatar: 'AK',
    content:
      'Permissions and workspaces are done right. We can share specific boards with clients while keeping internal docs private.',
    gradient: 'from-amber-500 to-orange-500',
  },
  {
    name: 'Priya Patel',
    role: 'Founder',
    company: 'Dub.co',
    avatar: 'PP',
    content:
      'Switched from 4 different tools to just Notrelix. The automations alone save us hours every week. Best decision this year.',
    gradient: 'from-cyan-500 to-blue-500',
  },
  {
    name: 'Tom Wright',
    role: 'DevRel',
    company: 'Supabase',
    avatar: 'TW',
    content:
      'Writing our public docs in Notrelix was effortless. Nested pages, code blocks with syntax highlighting, and easy exports.',
    gradient: 'from-purple-500 to-fuchsia-500',
  },
]

const stats = [
  { value: '10,000+', label: 'Active teams' },
  { value: '2M+', label: 'Pages created' },
  { value: '99.9%', label: 'Uptime SLA' },
  { value: '4.9/5', label: 'Avg. rating' },
]

export function TestimonialsSection() {
  return (
    <section id="testimonials" className="py-28 bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center mb-16">
          <Badge
            variant="outline"
            className="mb-4 text-xs font-semibold tracking-wider uppercase"
          >
            Customers
          </Badge>
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-bold tracking-tight mb-5">
            Loved by teams everywhere
          </h2>
          <p className="text-lg text-muted-foreground max-w-xl mx-auto">
            From startups to enterprises, teams trust Notrelix to organize
            their work and ship faster.
          </p>
        </div>

        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-5 mb-16">
          {testimonials.map((t) => (
            <div
              key={t.name}
              className="rounded-2xl border bg-card/50 backdrop-blur-sm p-6 hover:shadow-lg transition-shadow"
            >
              <div className="flex gap-0.5 mb-4">
                {[...Array(5)].map((_, i) => (
                  <Star
                    key={i}
                    className="size-4 fill-amber-400 text-amber-400"
                  />
                ))}
              </div>
              <p className="text-sm leading-relaxed mb-5">
                &ldquo;{t.content}&rdquo;
              </p>
              <div className="flex items-center gap-3">
                <Avatar>
                  <AvatarFallback
                    className={`bg-gradient-to-br ${t.gradient} text-white text-xs font-medium`}
                  >
                    {t.avatar}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <div className="text-sm font-medium">{t.name}</div>
                  <div className="text-xs text-muted-foreground">
                    {t.role} at {t.company}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>

        <div className="grid grid-cols-2 md:grid-cols-4 gap-6 max-w-3xl mx-auto">
          {stats.map((stat) => (
            <div key={stat.label} className="text-center">
              <div className="text-3xl font-bold tracking-tight bg-gradient-to-r from-violet-600 to-indigo-600 bg-clip-text text-transparent">
                {stat.value}
              </div>
              <div className="text-sm text-muted-foreground mt-1">
                {stat.label}
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
