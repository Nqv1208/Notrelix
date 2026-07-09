import Link from 'next/link';
import { ArrowRight } from 'lucide-react';
import { Button } from '@notrelix/ui-web/components/ui/button';

export function CTASection() {
  return (
    <section className="py-24 bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 text-center">
        <h2 className="text-3xl sm:text-4xl font-bold tracking-tight mb-4">
          Ready to get started?
        </h2>
        <p className="text-lg text-muted-foreground mb-8 max-w-2xl mx-auto">
          Join thousands of teams using Notrelix to write, plan, and ship faster.
        </p>
        <Link href="/sign-up">
          <Button
            size="lg"
            className="bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-xl shadow-violet-500/25 px-8 h-12 text-base"
          >
            Start free trial
            <ArrowRight className="size-4 ml-2" />
          </Button>
        </Link>
      </div>
    </section>
  );
}
