export const messages = {
  layout: {
    description:
      "Notrelix connects documents, Boards, workflows and data in a single workspace so teams always know what to do next.",
    title: "Notrelix | Workspace for modern teams",
    keywords: [
      "work OS",
      "work management",
      "workspace",
      "board",
      "team docs",
      "workflow automation",
    ],
    ogTitle: "Notrelix | From idea to clear outcomes",
  },
  header: {
    logoAria: "Notrelix - Home",
    navAria: "Main navigation",
    mobileNavAria: "Mobile navigation",
    menuAria: "Navigation menu",
    exploreLabel: "Explore Notrelix",
    signIn: "Sign in",
    tryFree: "Try for free",
    openMenu: "Open menu",
    closeMenu: "Close menu",
    navLinks: [
      { label: "Product", href: "#showcase" },
      { label: "Solutions", href: "#use-cases" },
      { label: "Features", href: "#features" },
      { label: "Pricing", href: "#pricing" },
      { label: "Resources", href: "#resources" },
    ],
    productLinks: [
      {
        label: "Overview",
        description: "One place for everything your team does",
        href: "#showcase",
      },
      {
        label: "Docs",
        description: "Turn knowledge into action",
        href: "#features",
      },
      {
        label: "Automation",
        description: "Let workflows run themselves",
        href: "#features",
      },
      {
        label: "Reporting",
        description: "Always know where the team stands",
        href: "#features",
      },
    ],
  },
  themeToggle: {
    switch: "Switch appearance",
    switchToLight: "Switch to light appearance",
    switchToDark: "Switch to dark appearance",
    light: "Light appearance",
    dark: "Dark appearance",
  },
  hero: {
    badge: "Work OS for modern teams",
    titleLine1: "Work Management Made",
    titleHighlight: "Simple and Powerful",
    subtitle:
      "Notrelix connects documents, Boards, workflows and data in a single workspace so your team always knows what to do next.",
    ctaPrimary: "Get started free",
    ctaSecondary: "See Notrelix in action",
    checks: [
      "No credit card required",
      "Set up in minutes",
      "Free for small teams",
    ],
    preview: {
      tabsLabel: "Product views",
      urlLabel: "app.notrelix.com",
      hint: "Click a view to explore",
      tabs: [
        {
          id: "table",
          label: "Table",
          alt: "A Notrelix Board in Table view, with items grouped by status and their assignees, due dates, priorities and labels",
        },
        {
          id: "kanban",
          label: "Kanban",
          alt: "A Notrelix Board in Kanban view, with items as cards in status lanes",
        },
        {
          id: "calendar",
          label: "Calendar",
          alt: "A Notrelix Board in Calendar view, with items and phases placed on the days of the month",
        },
        {
          id: "timeline",
          label: "Timeline",
          alt: "A Notrelix Board in Timeline view, with item bars on a dated scale and the dependencies between them",
        },
      ],
    },
    floatCards: {
      tasks: { strong: "12 tasks", small: "done today" },
      progress: { strong: "On track", small: "+24% vs last week" },
    },
  },
  metrics: {
    eyebrow: "Visible outcomes",
    title: "Fewer tab switches. More momentum.",
    subtitle:
      "When the whole team works on the same source of truth, small changes compound into big differences.",
    link: "Talk to our team",
    items: [
      { value: "30%", label: "less time hunting for information" },
      { value: "2.4×", label: "faster project completion" },
      { value: "40%", label: "manual work automated" },
    ],
  },
  socialProof: {
    line: "Trusted by modern teams turning plans into progress.",
    trust: ["Workspace-level permissions", "Data always belongs to your team"],
  },
  problemSolution: {
    eyebrow: "From messy to clear",
    title: "Teams don't need more tools.",
    titleHighlight: "They need a rhythm.",
    description:
      "Notrelix brings context, ownership and action into one place so work never falls through the cracks between departments.",
    shifts: [
      {
        before: "Docs scattered everywhere",
        after: "One source of truth",
        text: "Connect briefs, decisions and work into the same context instead of hunting across tabs.",
      },
      {
        before: "Work gets lost",
        after: "Full visibility on the Board",
        text: "Flexible BoardFields mirror how the team actually works, from status to owner and deadline.",
      },
      {
        before: "Tracking by gut feeling",
        after: "Clear progress signals",
        text: "Automation and reporting surface the next step, risks and decision points automatically.",
      },
    ],
  },
  story: {
    title: "Designed so work flows naturally.",
    stories: [
      {
        id: "inbox",
        title: "Know what needs you, then clear it.",
        description:
          "Mentions, assignments and approvals land in one Inbox. Answer in place, mark it done, or ask to be reminded later.",
        benefits: [
          "Needs action, Mentions and Assigned views",
          "Reply to a mention without leaving the Inbox",
          "Mark as done, or remind me later",
        ],
        link: "Start free",
        alt: "The Notrelix Inbox: a mention that needs a reply, with a reply box and the Mark as read, Mark as done and Remind me later actions",
      },
      {
        id: "sharing",
        title: "Share a Board without sharing everything.",
        description:
          "Every Board lists who can open it and at what level. People come in through their department role; a guest sees only that Board.",
        benefits: [
          "Can view, can comment, can edit or Manager, per person or team",
          "Guests outside the company are marked and see one Board",
          "Permission history for every change",
        ],
        link: "How we protect your data",
        alt: "The Share dialog of a Board: access through the department, five permissions with their levels, a guest marked as outside the company, the Board link and the permission history",
      },
      {
        id: "departments",
        title: "Every department's work at a glance.",
        description:
          "A department page shows what needs attention and each person's open work, counted only across the Boards you can see.",
        benefits: [
          "Overdue items, unclaimed requests and items waiting for approval",
          "Open work by person, with what is overdue",
          "Private Boards stay visible only to the people added",
        ],
        link: "See pricing",
        alt: "A department overview: what needs attention in the department and the open work of each person",
      },
    ],
  },
  showcase: {
    title: "How work gets done.",
    description:
      "From one item to the report on it. These are screens from the product, shown with sample data.",
    tabsLabel: "How work gets done",
    tabs: [
      {
        id: "item",
        label: "Item",
        alt: "An item page: the description with a linked doc, sub-items with their progress, links and files, and every field of the item in a side panel",
        notes: {
          properties: "Every field of the item, in one panel",
          subitems: "Sub-items roll their progress up",
          links: "Dependencies and files stay on the item",
        },
      },
      {
        id: "docs",
        label: "Docs",
        alt: "A document page with items mentioned inline, a panel listing the tasks it mentions, and the Approve document action",
        notes: {
          chip: "Mention an item inside the text",
          links: "Linked tasks and backlinks, listed for you",
          approve: "Approve the doc when it is ready",
        },
      },
      {
        id: "automation",
        label: "Automation",
        alt: "The Automations settings of a Board with a rule being written: a trigger, a condition and two actions that run in order",
        notes: {
          trigger: "Start from something that happens on the Board",
          conditions: "Narrow it with conditions",
          actions: "Up to five actions, in the order you choose",
        },
      },
      {
        id: "reports",
        label: "Reports",
        alt: "A report on a Board: items done, completion pace against the go-live date, due soon and workload by person",
        notes: {
          summary: "The headline numbers first",
          pace: "Pace compared with what the date needs",
          workload: "Open work by person",
        },
      },
    ],
  },
  useCases: {
    eyebrow: "For every team",
    title: "One platform.",
    titleHighlight: "Your team's way of working.",
    description:
      "Notrelix adapts to each team's rhythm while keeping everyone in one system.",
    link: "Explore",
    items: [
      {
        title: "Marketing",
        text: "Keep campaign briefs, timelines and feedback in a single launch rhythm.",
        benefits: ["Clear campaign owners", "Shared content calendar"],
      },
      {
        title: "Product",
        text: "Connect insights, roadmap and product decisions to everyday work.",
        benefits: ["Roadmap with context", "Transparent prioritization"],
      },
      {
        title: "Operations",
        text: "Standardize playbooks and processes for consistent execution.",
        benefits: ["Findable SOPs", "Repeatable workflows"],
      },
      {
        title: "Sales",
        text: "Track deals, handoffs and customer docs without dropping a step.",
        benefits: ["Handoffs that never slip", "Pipeline signals"],
      },
      {
        title: "Leadership",
        text: "Get a progress picture deep enough to decide without micromanaging.",
        benefits: ["Goal-based dashboards", "Early risk visibility"],
      },
    ],
  },
  testimonials: {
    eyebrow: "What teams say",
    prevAria: "Previous testimonial",
    nextAria: "Next testimonial",
    viewingAria: "Viewing testimonial {current} of {total}",
    viewAria: "View testimonial by {name}",
    items: [
      {
        quote:
          "We used to lose half a day tying briefs to work. Now the team opens Notrelix and instantly knows the context and the next step.",
        name: "Linh Nguyen",
        role: "Head of Operations",
        company: "Morrow Studio",
        initials: "LN",
      },
      {
        quote:
          "The most valuable part isn't one more Board — it's that the Board sits right next to the docs and the decisions that created it.",
        name: "Minh Tran",
        role: "Product Lead",
        company: "Northstar Labs",
        initials: "MT",
      },
      {
        quote:
          "Notrelix gives leadership the signals they need without five different status updates from five teams.",
        name: "An Pham",
        role: "COO",
        company: "Cedar Collective",
        initials: "AP",
      },
    ],
  },
  pricing: {
    eyebrow: "Simple pricing",
    titleTagline: "Start small.",
    titleHighlight: "Scale naturally.",
    description:
      "Free to try, upgrade when your team needs more power. No hidden fees.",
    billingAria: "Billing cycle",
    monthly: "Monthly",
    yearly: "Yearly",
    yearlySave: "Save 20%",
    contactSales: "Contact sales",
    custom: "Custom",
    freeSuffix: "free",
    perUserSuffix: "user / month",
    popular: "Most popular",
    startFree: "Get started free",
    talkToSales: "Talk to sales",
    plans: [
      {
        name: "Starter",
        description:
          "For individuals and small teams starting a shared rhythm.",
        monthly: "0",
        yearly: "0",
        features: [
          "Unlimited docs and Boards",
          "Up to 5 members",
          "Core views",
        ],
      },
      {
        name: "Growth",
        description: "For teams accelerating and in need of clearer processes.",
        monthly: "12",
        yearly: "9.6",
        features: [
          "Everything in Starter",
          "Automation and reporting",
          "Advanced permissions",
          "Priority support",
        ],
      },
      {
        name: "Enterprise",
        description: "For organizations needing control, security and scale.",
        monthly: "Contact sales",
        yearly: "Contact sales",
        features: [
          "SSO and audit log",
          "Unlimited workspaces",
          "Custom integrations",
          "SLA and dedicated support",
        ],
      },
    ],
  },
  finalCta: {
    title: "Give your team one place to move forward together.",
    description:
      "Create your first workspace in minutes and let Notrelix connect the rest.",
    ctaPrimary: "Get started free",
    ctaSecondary: "Book a demo",
    checks: ["No credit card required", "Cancel anytime"],
  },
  footer: {
    logoAria: "Notrelix - Home",
    tagline:
      "A unified workspace where teams write, plan and get things done together.",
    emailAria: "Contact Notrelix",
    githubAria: "Notrelix on GitHub",
    linkedinAria: "Notrelix on LinkedIn",
    copyright: "© {year} Notrelix. Building how teams work better.",
    status: "All systems operational",
    sections: [
      {
        title: "Product",
        links: [
          { label: "Overview", href: "#showcase" },
          { label: "Features", href: "#features" },
          { label: "Pricing", href: "#pricing" },
          { label: "Integrations", href: "#features" },
        ],
      },
      {
        title: "Solutions",
        links: [
          { label: "Marketing", href: "#use-cases" },
          { label: "Product", href: "#use-cases" },
          { label: "Operations", href: "#use-cases" },
          { label: "Leadership", href: "#use-cases" },
        ],
      },
      {
        title: "Resources",
        links: [
          { label: "Help center", href: "/contact" },
          { label: "Contact the team", href: "/contact" },
          { label: "System status", href: "#status" },
          { label: "Product updates", href: "#resources" },
        ],
      },
      {
        title: "Legal",
        links: [
          { label: "Privacy policy", href: "/legal/privacy" },
          { label: "Terms of service", href: "/legal/terms" },
          { label: "Enterprise security", href: "/legal/privacy" },
        ],
      },
    ],
  },
  contact: {
    backHome: "Back to home",
    title: "Contact Notrelix",
    subtitle:
      "Have a question or need support? The Notrelix team is here to listen.",
    emailTitle: "Email",
    general: "General inquiries:",
    support: "Technical support:",
    responseTitle: "Response time",
    responseText: "We typically respond within 24 hours on business days.",
  },
  legal: {
    backHome: "Back to home",
    updated: "Last updated: {date}",
    privacy: {
      title: "Privacy Policy",
      sections: [
        {
          heading: "1. Information We Collect",
          body: "We collect information you provide directly, for example when you create an account, use our services or contact our support team.",
        },
        {
          heading: "2. How We Use Your Information",
          body: "We use the information we collect to provide, maintain and improve our services, to send technical notices, support messages and to reach out when needed.",
        },
        {
          heading: "3. Data Security",
          body: "We take reasonable measures to protect your personal information from loss, theft, misuse, unauthorized access, disclosure, alteration and destruction.",
        },
        {
          heading: "4. Contact Us",
          body: "If you have questions about this Privacy Policy, contact us at:",
        },
      ],
    },
    terms: {
      title: "Terms of Service",
      sections: [
        {
          heading: "1. Acceptance of Terms",
          body: "By accessing or using Notrelix, you agree to be bound by these Terms of Service. If you do not agree, please do not use our service.",
        },
        {
          heading: "2. Use of Service",
          body: "You may use the service only for lawful purposes and in accordance with these Terms. You are responsible for keeping your account credentials confidential.",
        },
        {
          heading: "3. Intellectual Property",
          body: "The service and its original content, features and functionality are owned by Notrelix and protected by international copyright, trademark, patent, trade secret and other intellectual property laws.",
        },
        {
          heading: "4. User Content",
          body: "You retain ownership of content you create or upload to Notrelix. By using the service, you grant us a limited license to host, store and display your content solely to provide the service.",
        },
        {
          heading: "5. Termination",
          body: "We may terminate or suspend your access to the service immediately, without prior notice, if you breach these Terms.",
        },
        {
          heading: "6. Limitation of Liability",
          body: "Notrelix shall not be liable for any indirect, incidental, special, consequential or punitive damages resulting from your use of or inability to use the service.",
        },
        {
          heading: "7. Changes to Terms",
          body: "We reserve the right to modify or replace these Terms at any time. If a change is material, we will try to provide at least 30 days notice.",
        },
        {
          heading: "8. Contact Us",
          body: "If you have questions about these Terms, contact us at:",
        },
      ],
    },
  },
  problemTransition: {
    heading: "Wave goodbye to",
    accessibleSummary:
      "Wave goodbye to missed deadlines, disorganized workflows, unnecessary complexity, slow progress, wasted time, lack of collaboration, and task overload.",
    problems: {
      missedDeadlines: "missed deadlines",
      disorganizedWorkflows: "disorganized workflows",
      unnecessaryComplexity: "unnecessary complexity",
      slowProgress: "slow progress",
      wastedTime: "wasted time",
      lackOfCollaboration: "lack of collaboration",
      taskOverload: "task overload",
    },
    metrics: [
      { value: "300K+", label: "Team Collaborations" },
      { value: "500K+", label: "Tasks Completed" },
      { value: "15M+", label: "Projects Managed" },
      { value: "150K+", label: "Successful Integrations" },
    ],
  },
} as const;

export type Messages = typeof messages;
