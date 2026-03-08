import Link from "next/link"
import { ArrowRight, CheckSquare, Sparkles } from "lucide-react"

import { Button } from "@/registry/new-york-v4/ui/button"
import { Badge } from "@/registry/new-york-v4/ui/badge"

export function HeroSection() {
  return (
    <section className="relative pt-20 pb-32 overflow-hidden">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="max-w-4xl mx-auto text-center">
          <Badge
            variant="secondary"
            className="mb-6 px-4 py-1.5 text-sm font-medium bg-violet-100 dark:bg-violet-950/50 text-violet-700 dark:text-violet-300 border-violet-200 dark:border-violet-800"
          >
            <Sparkles className="w-3.5 h-3.5 mr-1.5" />
            Phiên bản mới ra mắt
          </Badge>

          <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold tracking-tight mb-6">
            <span className="block text-foreground">Quản lý công việc</span>
            <span className="inline-block mt-2 py-1 bg-gradient-to-r from-violet-600 via-fuchsia-600 to-pink-600 bg-clip-text text-transparent">
              thông minh & hiệu quả
            </span>
          </h1>

          <p className="text-lg sm:text-xl text-muted-foreground mb-10 max-w-2xl mx-auto leading-relaxed">
            Biến ý tưởng thành hành động, task thành thành tựu. Tăng năng suất
            làm việc với công cụ quản lý todo đơn giản nhưng mạnh mẽ.
          </p>

          <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mb-12">
            <Link href="/register">
              <Button
                size="lg"
                className="w-full sm:w-auto bg-gradient-to-r from-violet-600 to-fuchsia-600 hover:from-violet-700 hover:to-fuchsia-700 text-white border-0 shadow-xl shadow-violet-500/30 px-8"
              >
                Dùng thử miễn phí
                <ArrowRight className="w-4 h-4 ml-2" />
              </Button>
            </Link>
            <Button variant="outline" size="lg" className="w-full sm:w-auto px-8">
              Xem demo
            </Button>
          </div>

          <div className="flex items-center justify-center gap-6 text-sm text-muted-foreground">
            <div className="flex items-center gap-2">
              <div className="flex items-center justify-center w-5 h-5 rounded-full bg-emerald-500/20">
                <CheckSquare className="w-3 h-3 text-emerald-600" />
              </div>
              Không cần thẻ tín dụng
            </div>
            <div className="flex items-center gap-2">
              <div className="flex items-center justify-center w-5 h-5 rounded-full bg-emerald-500/20">
                <CheckSquare className="w-3 h-3 text-emerald-600" />
              </div>
              Miễn phí 14 ngày
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
