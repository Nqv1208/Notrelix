export const pricingPlans = [
  {
    name: 'Free',
    price: '$0',
    description: 'For individuals and small teams',
    features: ['Unlimited pages', '3 boards', 'Basic permissions', 'Community support'],
    cta: 'Get started',
    href: '/sign-up',
  },
  {
    name: 'Pro',
    price: '$12',
    period: '/user/month',
    description: 'For growing teams',
    features: ['Unlimited boards', 'Advanced permissions', 'Automation', 'Priority support'],
    cta: 'Start free trial',
    href: '/sign-up',
    popular: true,
  },
  {
    name: 'Enterprise',
    price: 'Custom',
    description: 'For large organizations',
    features: ['SSO & SAML', 'Audit logs', 'Custom integrations', 'Dedicated support'],
    cta: 'Contact sales',
    href: '/contact',
  },
] as const;
