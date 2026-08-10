import { Badge } from "@notrelix/ui-web/components/ui/badge";
import { UserPlus, PenLine, Rocket } from "lucide-react";

const steps = [
  {
    step: "01",
    icon: UserPlus,
    title: "Create a workspace",
    description:
      "Sign up in seconds, name your workspace, and invite your team. Everyone gets access instantly.",
    color: "from-violet-500 to-indigo-500",
  },
  {
    step: "02",
    icon: PenLine,
    title: "Write & organize",
    description:
      "Create pages with our block editor, build wikis, or spin up kanban boards — whatever fits your workflow.",
    color: "from-indigo-500 to-purple-500",
  },
  {
    step: "03",
    icon: Rocket,
    title: "Ship together",
    description:
      "Collaborate in real time with comments, assignments, and notifications. Keep everyone aligned and shipping.",
    color: "from-purple-500 to-fuchsia-500",
  },
];

export function HowItWorksSection() {
  return (
    <section className="py-28 bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center mb-16">
          <Badge
            variant="outline"
            className="mb-4 text-xs font-semibold tracking-wider uppercase"
          >
            How it works
          </Badge>
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-bold tracking-tight mb-5">
            Up and running in minutes
          </h2>
          <p className="text-lg text-muted-foreground max-w-xl mx-auto">
            Three simple steps to transform how your team works.
          </p>
        </div>

        <div className="grid md:grid-cols-3 gap-8 max-w-4xl mx-auto">
          {steps.map((step, i) => {
            const Icon = step.icon;
            return (
              <div key={step.step} className="relative text-center">
                {i < steps.length - 1 && (
                  <div className="hidden md:block absolute top-12 left-[60%] w-[80%] border-t-2 border-dashed border-border" />
                )}

                <div className="relative inline-flex mb-6">
                  <div
                    className={`flex items-center justify-center size-20 rounded-2xl bg-gradient-to-br ${step.color} shadow-lg`}
                  >
                    <Icon className="size-8 text-white" />
                  </div>
                  <div className="absolute -top-2 -right-2 flex items-center justify-center size-7 rounded-full bg-background border-2 border-border text-xs font-bold">
                    {step.step}
                  </div>
                </div>

                <h3 className="text-lg font-semibold mb-2">{step.title}</h3>
                <p className="text-sm text-muted-foreground leading-relaxed max-w-xs mx-auto">
                  {step.description}
                </p>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
