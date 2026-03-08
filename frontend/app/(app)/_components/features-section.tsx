import {
  ListChecks,
  Target,
  Calendar,
  Users,
  BarChart3,
  Zap,
  type LucideIcon,
} from "lucide-react"

import { Card, CardDescription, CardHeader, CardTitle } from "@/registry/new-york-v4/ui/card"
import { Badge } from "@/registry/new-york-v4/ui/badge"

interface Feature {
  icon: LucideIcon
  title: string
  description: string
}

const features: Feature[] = [
  {
    icon: ListChecks,
    title: "Quản lý Task thông minh",
    description:
      "Tạo, sắp xếp và theo dõi các công việc với giao diện trực quan, dễ sử dụng.",
  },
  {
    icon: Target,
    title: "Mục tiêu & Milestones",
    description:
      "Đặt mục tiêu lớn và chia nhỏ thành các milestone có thể đạt được.",
  },
  {
    icon: Calendar,
    title: "Lịch & Nhắc nhở",
    description:
      "Tích hợp lịch và thông báo để không bao giờ bỏ lỡ deadline quan trọng.",
  },
  {
    icon: Users,
    title: "Cộng tác nhóm",
    description:
      "Làm việc cùng team, phân công nhiệm vụ và theo dõi tiến độ chung.",
  },
  {
    icon: BarChart3,
    title: "Thống kê & Báo cáo",
    description:
      "Phân tích năng suất với biểu đồ và báo cáo chi tiết theo thời gian.",
  },
  {
    icon: Zap,
    title: "Tự động hóa",
    description:
      "Thiết lập workflow tự động để tiết kiệm thời gian cho những việc lặp lại.",
  },
]

function FeatureCard({ feature }: { feature: Feature }) {
  const Icon = feature.icon

  return (
    <Card className="group hover:shadow-xl hover:shadow-violet-500/5 transition-all duration-300 border-border/50 bg-card/50 backdrop-blur-sm hover:-translate-y-1">
      <CardHeader>
        <div className="flex items-center justify-center w-12 h-12 rounded-xl bg-gradient-to-br from-violet-500/10 to-fuchsia-500/10 mb-4 group-hover:from-violet-500/20 group-hover:to-fuchsia-500/20 transition-colors">
          <Icon className="w-6 h-6 text-violet-600 dark:text-violet-400" />
        </div>
        <CardTitle className="text-lg">{feature.title}</CardTitle>
        <CardDescription className="leading-relaxed">
          {feature.description}
        </CardDescription>
      </CardHeader>
    </Card>
  )
}

export function FeaturesSection() {
  return (
    <section id="features" className="py-24 bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center mb-16">
          <Badge variant="outline" className="mb-4">
            Tính năng
          </Badge>
          <h2 className="text-3xl sm:text-4xl font-bold tracking-tight mb-4">
            Mọi thứ bạn cần để làm việc hiệu quả
          </h2>
          <p className="text-muted-foreground max-w-2xl mx-auto">
            Từ quản lý task đơn giản đến cộng tác team phức tạp, chúng tôi có
            tất cả.
          </p>
        </div>

        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {features.map((feature, index) => (
            <FeatureCard key={index} feature={feature} />
          ))}
        </div>
      </div>
    </section>
  )
}
