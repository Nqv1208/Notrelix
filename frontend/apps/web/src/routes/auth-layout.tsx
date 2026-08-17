import { FileText, LayoutGrid, Users, Zap, CheckCircle2 } from "lucide-react";
import { NotrelixLogo } from "@notrelix/ui-web";

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
      <div className="relative hidden lg:flex flex-col justify-between overflow-hidden bg-slate-950 p-10 text-white">
        {/* Atmospheric Brand Glow Fields */}
        <div className="pointer-events-none absolute inset-0 overflow-hidden">
          {/* Primary Red Glow Top-Left */}
          <div
            className="absolute -top-32 -left-32 size-[30rem] rounded-full opacity-25 blur-3xl"
            style={{ backgroundColor: "#FF1E56" }}
          />
          {/* Primary Blue Glow Bottom-Right */}
          <div
            className="absolute -bottom-40 -right-20 size-[32rem] rounded-full opacity-30 blur-3xl"
            style={{ backgroundColor: "#1E90FF" }}
          />
          {/* Warm Bridge Center Glow */}
          <div
            className="absolute top-1/2 left-1/3 size-72 rounded-full opacity-15 blur-3xl"
            style={{ backgroundColor: "#FC744C" }}
          />
        </div>

        {/* Subtle grid pattern */}
        <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNjAiIGhlaWdodD0iNjAiIHZpZXdCb3g9IjAgMCA2MCA2MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48ZyBmaWxsPSJub25lIiBmaWxsLXJ1bGU9ImV2ZW5vZGQiPjxwYXRoIGQ9Ik0zNiAxOGMtOS45NDEgMC0xOCA4LjA1OS0xOCAxOHM4LjA1OSAxOCAxOCAxOCAxOC04LjA1OSAxOC0xOC04LjA1OS0xOC0xOC0xOHptMCAzMmMtNy43MzIgMC0xNC02LjI2OC0xNC0xNHM2LjI2OC0xNCAxNC0xNCAxNCA2LjI2OCAxNCAxNC02LjI2OCAxNC0xNCAxNHoiIGZpbGw9IiNmZmYiIGZpbGwtb3BhY2l0eT0iLjAzIi8+PC9nPjwvc3ZnPg==')] opacity-40" />

        {/* Brand Logo */}
        <a href="/" className="relative z-10 inline-flex items-center">
          <NotrelixLogo size="lg" className="[&_span]:text-white" />
        </a>

        {/* Center content */}
        <div className="relative z-10 flex-1 flex flex-col justify-center max-w-md my-auto">
          <h2 className="text-3xl font-bold tracking-tight leading-tight mb-3">
            Docs, boards & wikis — unified.
          </h2>
          <p className="text-slate-300/80 leading-relaxed mb-8 text-[15px]">
            Write, plan, and ship together in one cohesive workspace designed
            for modern product and engineering teams.
          </p>

          <div className="space-y-4">
            {features.map((f) => (
              <div key={f.text} className="flex items-center gap-3">
                <div className="flex items-center justify-center size-9 rounded-lg bg-white/10 backdrop-blur-sm shrink-0 border border-white/10">
                  <f.icon className="size-[18px] text-white/90" />
                </div>
                <span className="text-[15px] text-slate-200/90 font-medium">
                  {f.text}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Bottom Proof / Value Block */}
        <div className="relative z-10 pt-6 border-t border-white/10">
          <div className="flex items-start gap-3 rounded-xl bg-white/[0.05] p-4 border border-white/10 backdrop-blur-sm">
            <CheckCircle2
              className="size-5 shrink-0 mt-0.5"
              style={{ color: "#1E90FF" }}
            />
            <div>
              <div className="font-semibold text-sm text-white">
                Connected Workspace Operating System
              </div>
              <div className="text-slate-400 text-xs mt-0.5 leading-relaxed">
                Replace fragmented single-purpose tools with one authoritative
                workspace.
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Right — Form Panel */}
      <div className="flex flex-col min-h-screen">
        {/* Mobile header */}
        <div className="lg:hidden flex items-center justify-between p-5 border-b">
          <a href="/" className="flex items-center gap-2">
            <NotrelixLogo size="md" />
          </a>
        </div>

        {/* Form centered */}
        <div className="flex-1 flex items-center justify-center px-5 py-10 sm:px-8 lg:px-12">
          <div className="w-full max-w-[460px]">{children}</div>
        </div>
      </div>
    </div>
  );
}
