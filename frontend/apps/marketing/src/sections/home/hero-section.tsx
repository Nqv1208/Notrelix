import Link from 'next/link';
import { ArrowRight, Check } from 'lucide-react';
import { Button } from '@notrelix/ui-web/components/ui/button';
import { Badge } from '@notrelix/ui-web/components/ui/badge';

export function HeroSection() {
  return (
    <section className="relative pt-20 pb-28 overflow-hidden">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="max-w-4xl mx-auto text-center mb-16">
          <Badge
            variant="secondary"
            className="mb-6 px-4 py-1.5 text-sm font-medium bg-violet-100 dark:bg-violet-950/50 text-violet-700 dark:text-violet-300 border-violet-200/60 dark:border-violet-800/60"
          >
            Now in public beta
          </Badge>

          <h1 className="text-4xl sm:text-5xl lg:text-7xl font-bold tracking-tight mb-6 leading-[1.1]">
            <span className="block">Write like Notion.</span>
            <span className="block">Plan like Trello.</span>
            <span className="block mt-1 bg-gradient-to-r from-violet-600 via-indigo-600 to-purple-600 bg-clip-text text-transparent">
              Ship like a pro.
            </span>
          </h1>

          <p className="text-lg sm:text-xl text-muted-foreground mb-10 max-w-2xl mx-auto leading-relaxed">
            Notrelix unifies documents, wikis, and project boards into one
            workspace. No more tab-switching — just focus and flow.
          </p>

          <div className="flex flex-col sm:flex-row items-center justify-center gap-3 mb-10">
            <Link href="/sign-up">
              <Button
                size="lg"
                className="w-full sm:w-auto bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white border-0 shadow-xl shadow-violet-500/25 px-8 h-12 text-base"
              >
                Start free
                <ArrowRight className="size-4 ml-2" />
              </Button>
            </Link>
            <a href="#features">
              <Button variant="outline" size="lg" className="w-full sm:w-auto px-8 h-12 text-base">
                See it in action
              </Button>
            </a>
          </div>

          <div className="flex items-center justify-center gap-6 text-sm text-muted-foreground">
            {['No credit card required', 'Free for small teams', 'Setup in 30s'].map(
              (text) => (
                <div key={text} className="flex items-center gap-1.5">
                  <div className="flex items-center justify-center size-4 rounded-full bg-emerald-500/15">
                    <Check className="size-2.5 text-emerald-600" />
                  </div>
                  {text}
                </div>
              )
            )}
          </div>
        </div>
      </div>
    </section>
  );
}
