import { Layers, FileText, LayoutGrid, Users, Zap } from "lucide-react";

const features = [
  { icon: FileText, text: "Block-based docs with slash commands" },
  { icon: LayoutGrid, text: "Kanban boards with drag & drop" },
  { icon: Users, text: "Real-time team collaboration" },
  { icon: Zap, text: "Automations & integrations" },
];

export function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen grid lg:grid-cols-[1fr_1.1fr]">
      {/* Left — Branding Panel */}
      <div className="relative hidden lg:flex flex-col justify-between overflow-hidden bg-gradient-to-br from-violet-600 via-indigo-600 to-purple-700 p-10 text-white">
        {/* Decorative Orbs */}
        <div className="pointer-events-none absolute inset-0">
          <div className="absolute -top-32 -left-32 size-96 rounded-full bg-white/[0.07] blur-3xl" />
          <div className="absolute -bottom-40 -right-20 size-[28rem] rounded-full bg-white/[0.05] blur-3xl" />
          <div className="absolute top-1/2 left-1/3 size-64 rounded-full bg-indigo-400/10 blur-2xl" />
        </div>
        {/* Subtle pattern */}
        <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNjAiIGhlaWdodD0iNjAiIHZpZXdCb3g9IjAgMCA2MCA2MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48ZyBmaWxsPSJub25lIiBmaWxsLXJ1bGU9ImV2ZW5vZGQiPjxwYXRoIGQ9Ik0zNiAxOGMtOS45NDEgMC0xOCA4LjA1OS0xOCAxOHM4LjA1OSAxOCAxOCAxOCAxOC04LjA1OSAxOC0xOC04LjA1OS0xOC0xOC0xOHptMCAzMmMtNy43MzIgMC0xNC02LjI2OC0xNC0xNHM2LjI2OC0xNCAxNC0xNCAxNCA2LjI2OCAxNCAxNC02LjI2OCAxNC0xNCAxNHoiIGZpbGw9IiNmZmYiIGZpbGwtb3BhY2l0eT0iLjAzIi8+PC9nPjwvc3ZnPg==')] opacity-60" />

        {/* Logo */}
        <a href="/" className="relative z-10 flex items-center gap-2.5">
          <div className="flex items-center justify-center size-10 rounded-xl bg-white/15 backdrop-blur-sm">
            <Layers className="size-5 text-white" />
          </div>
          <span className="text-2xl font-bold tracking-tight">Notrelix</span>
        </a>

        {/* Center content */}
        <div className="relative z-10 flex-1 flex flex-col justify-center max-w-md">
          <h2 className="text-3xl font-bold leading-tight mb-3">
            Docs, boards & wikis — unified.
          </h2>
          <p className="text-white/70 leading-relaxed mb-8">
            Join 10,000+ teams writing, planning, and shipping together in one
            beautiful workspace.
          </p>

          <div className="space-y-4">
            {features.map((f) => (
              <div key={f.text} className="flex items-center gap-3">
                <div className="flex items-center justify-center size-9 rounded-lg bg-white/10 backdrop-blur-sm shrink-0">
                  <f.icon className="size-[18px] text-white/90" />
                </div>
                <span className="text-[15px] text-white/80">{f.text}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Bottom testimonial */}
        <div className="relative z-10 pt-8 border-t border-white/15">
          <blockquote>
            <p className="text-[15px] leading-relaxed text-white/80 mb-4">
              &ldquo;We replaced Notion and Trello with Notrelix. Having docs
              and boards in the same workspace is a game changer.&rdquo;
            </p>
            <footer className="flex items-center gap-3">
              <div className="size-10 rounded-full bg-white/15 flex items-center justify-center text-sm font-semibold">
                SC
              </div>
              <div>
                <div className="font-medium text-sm">Sarah Chen</div>
                <div className="text-white/50 text-xs">
                  Engineering Manager @ Stripe
                </div>
              </div>
            </footer>
          </blockquote>
        </div>
      </div>

      {/* Right — Form Panel */}
      <div className="flex flex-col min-h-screen">
        {/* Mobile header */}
        <div className="lg:hidden flex items-center justify-between p-5 border-b">
          <a href="/" className="flex items-center gap-2">
            <div className="flex items-center justify-center size-8 rounded-xl bg-gradient-to-br from-violet-600 to-indigo-600 shadow-md shadow-violet-500/20">
              <Layers className="size-4 text-white" />
            </div>
            <span className="text-lg font-bold tracking-tight">
              Notre
              <span className="bg-gradient-to-r from-violet-600 to-indigo-600 bg-clip-text text-transparent">
                lix
              </span>
            </span>
          </a>
        </div>

        {/* Form centered */}
        <div className="flex-1 flex items-center justify-center px-5 py-10 sm:px-8 lg:px-16">
          <div className="w-full max-w-[420px]">{children}</div>
        </div>
      </div>
    </div>
  );
}
