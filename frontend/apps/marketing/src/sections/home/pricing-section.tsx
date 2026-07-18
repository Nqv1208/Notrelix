import Link from 'next/link'
import { Check } from 'lucide-react'

import { Button } from '@notrelix/ui-web/components/ui/button'
import { Badge } from '@notrelix/ui-web/components/ui/badge'
import { cn } from '@notrelix/ui-web/lib/cn'

type Plan = {
  name: string
  price: string
  period: string
  description: string
  features: string[]
  cta: string
  popular?: boolean
}

const plans: Plan[] = [
  {
    name: 'Free',
    price: '$0',
    period: 'forever',
    description: 'For individuals and small side projects.',
    features: [
      'Up to 3 members',
      'Unlimited pages',
      '5 boards',
      '1 GB storage',
      '7-day page history',
      'Community support',
    ],
    cta: 'Get started',
  },
  {
    name: 'Pro',
    price: '$8',
    period: 'per member / month',
    description: 'For growing teams that need more power.',
    features: [
      'Unlimited members',
      'Unlimited pages & boards',
      '50 GB storage',
      '30-day page history',
      'Automations & webhooks',
      'Advanced permissions',
      'Priority support',
      'Custom integrations',
    ],
    cta: 'Start free trial',
    popular: true,
  },
  {
    name: 'Enterprise',
    price: 'Custom',
    period: 'contact sales',
    description: 'For organizations with advanced needs.',
    features: [
      'Everything in Pro',
      'Unlimited storage',
      'Unlimited history',
      'SAML SSO',
      'Audit logs',
      'Dedicated support',
      'Custom contracts',
      'SLA guarantee',
    ],
    cta: 'Contact sales',
  },
]

export function PricingSection() {
  return (
    <section id="pricing" className="py-28">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center mb-16">
          <Badge
            variant="outline"
            className="mb-4 text-xs font-semibold tracking-wider uppercase"
          >
            Pricing
          </Badge>
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-bold tracking-tight mb-5">
            Simple, transparent pricing
          </h2>
          <p className="text-lg text-muted-foreground max-w-xl mx-auto">
            Start free and scale as you grow. No hidden fees, no surprises.
          </p>
        </div>

        <div className="grid md:grid-cols-3 gap-6 max-w-5xl mx-auto items-start">
          {plans.map((plan) => (
            <div
              key={plan.name}
              className={cn(
                'relative rounded-2xl border bg-card p-8 transition-shadow',
                plan.popular
                  ? 'border-violet-500/50 shadow-xl shadow-violet-500/10 scale-[1.02]'
                  : 'hover:shadow-lg'
              )}
            >
              {plan.popular && (
                <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                  <Badge className="bg-gradient-to-r from-violet-600 to-indigo-600 text-white border-0 shadow-md px-3">
                    Most Popular
                  </Badge>
                </div>
              )}

              <div className="mb-6">
                <h3 className="text-lg font-semibold mb-1">{plan.name}</h3>
                <div className="flex items-baseline gap-1 mb-2">
                  <span className="text-4xl font-bold tracking-tight">
                    {plan.price}
                  </span>
                  <span className="text-sm text-muted-foreground">
                    / {plan.period}
                  </span>
                </div>
                <p className="text-sm text-muted-foreground">
                  {plan.description}
                </p>
              </div>

              <Link
                href={
                  plan.name === 'Enterprise'
                    ? '/contact'
                    : '/sign-up'
                }
              >
                <Button
                  className={cn(
                    'w-full mb-6',
                    plan.popular
                      ? 'bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white shadow-lg shadow-violet-500/20'
                      : ''
                  )}
                  variant={plan.popular ? 'default' : 'outline'}
                >
                  {plan.cta}
                </Button>
              </Link>

              <ul className="space-y-3">
                {plan.features.map((feature) => (
                  <li
                    key={feature}
                    className="flex items-start gap-2.5 text-sm"
                  >
                    <div className="flex items-center justify-center size-4 rounded-full bg-emerald-500/15 shrink-0 mt-0.5">
                      <Check className="size-2.5 text-emerald-600" />
                    </div>
                    {feature}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
