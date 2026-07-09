import Link from 'next/link';
import { Check } from 'lucide-react';
import { pricingPlans } from '../../content/pricing-copy';

export function PricingSection() {
  return (
    <section id="pricing" className="py-24">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center mb-16">
          <h2 className="text-3xl sm:text-4xl font-bold tracking-tight mb-4">
            Simple, transparent pricing
          </h2>
          <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
            Start free, upgrade when you need more.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8 max-w-5xl mx-auto">
          {pricingPlans.map((plan) => {
            const isPopular = 'popular' in plan && plan.popular;
            return (
              <div
                key={plan.name}
                className={`p-8 rounded-2xl border ${
                  isPopular
                    ? 'border-primary shadow-lg ring-2 ring-primary/20'
                    : 'bg-card'
                }`}
              >
                {isPopular && (
                  <div className="inline-flex items-center px-3 py-1 text-xs font-medium bg-primary text-primary-foreground rounded-full mb-4">
                    Most popular
                  </div>
                )}
                <h3 className="text-xl font-bold mb-2">{plan.name}</h3>
                <div className="mb-4">
                  <span className="text-4xl font-bold">{plan.price}</span>
                  {'period' in plan && plan.period && (
                    <span className="text-muted-foreground">{plan.period}</span>
                  )}
                </div>
                <p className="text-muted-foreground mb-6">{plan.description}</p>
                <ul className="space-y-3 mb-8">
                  {plan.features.map((feature) => (
                    <li key={feature} className="flex items-center gap-2 text-sm">
                      <Check className="size-4 text-primary shrink-0" />
                      {feature}
                    </li>
                  ))}
                </ul>
                <Link href={plan.href}>
                  <button
                    className={`w-full h-10 rounded-lg font-medium transition-colors ${
                      isPopular
                        ? 'bg-primary text-primary-foreground hover:bg-primary/90'
                        : 'border border-border hover:bg-accent'
                    }`}
                  >
                    {plan.cta}
                  </button>
                </Link>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
