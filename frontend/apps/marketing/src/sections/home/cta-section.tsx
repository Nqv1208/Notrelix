import Link from "next/link";
import { ArrowRight, Layers } from "lucide-react";

import { Button } from "@notrelix/ui-web/components/ui/button";

export function CTASection() {
  return (
    <section className="py-28">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-violet-600 via-indigo-600 to-purple-700 p-12 lg:p-20">
          <div className="absolute inset-0 opacity-20">
            <div className="absolute top-0 left-0 size-96 bg-white/10 rounded-full -translate-x-1/2 -translate-y-1/2 blur-3xl" />
            <div className="absolute bottom-0 right-0 size-96 bg-white/10 rounded-full translate-x-1/2 translate-y-1/2 blur-3xl" />
          </div>
          <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNjAiIGhlaWdodD0iNjAiIHZpZXdCb3g9IjAgMCA2MCA2MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48ZyBmaWxsPSJub25lIiBmaWxsLXJ1bGU9ImV2ZW5vZGQiPjxwYXRoIGQ9Ik0zNiAxOGMtOS45NDEgMC0xOCA4LjA1OS0xOCAxOHM4LjA1OSAxOCAxOCAxOCAxOC04LjA1OSAxOC0xOC04LjA1OS0xOC0xOC0xOHptMCAzMmMtNy43MzIgMC0xNC02LjI2OC0xNC0xNHM2LjI2OC0xNCAxNC0xNCAxNCA2LjI2OCAxNCAxNC02LjI2OCAxNC0xNCAxNHoiIGZpbGw9IiNmZmYiIGZpbGwtb3BhY2l0eT0iLjA1Ii8+PC9nPjwvc3ZnPg==')] opacity-30" />

          <div className="relative z-10 max-w-2xl mx-auto text-center">
            <div className="inline-flex items-center justify-center size-14 rounded-2xl bg-white/10 backdrop-blur-sm mb-6">
              <Layers className="size-7 text-white" />
            </div>
            <h2 className="text-3xl sm:text-4xl lg:text-5xl font-bold text-white mb-6 tracking-tight">
              Ready to unify your workflow?
            </h2>
            <p className="text-lg text-white/80 mb-10 leading-relaxed">
              Join 10,000+ teams using Notrelix to write, plan, and ship — all
              in one place. Free forever for small teams.
            </p>
            <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
              <Link href="/sign-up">
                <Button
                  size="lg"
                  className="w-full sm:w-auto bg-white text-violet-700 hover:bg-white/90 px-8 h-12 text-base shadow-xl font-semibold"
                >
                  Start for free
                  <ArrowRight className="size-4 ml-2" />
                </Button>
              </Link>
              <Link href="/contact">
                <Button
                  variant="outline"
                  size="lg"
                  className="w-full sm:w-auto border-white/30 text-white bg-white/5 hover:bg-white/10 px-8 h-12 text-base"
                >
                  Book a demo
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
