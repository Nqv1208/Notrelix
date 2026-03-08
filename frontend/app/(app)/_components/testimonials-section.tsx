import { Star } from "lucide-react"

import { Card, CardContent } from "@/registry/new-york-v4/ui/card"
import { Badge } from "@/registry/new-york-v4/ui/badge"
import {
  Avatar,
  AvatarFallback,
  AvatarGroup,
} from "@/registry/new-york-v4/ui/avatar"

interface Testimonial {
  name: string
  role: string
  avatar: string
  content: string
}

const testimonials: Testimonial[] = [
  {
    name: "Minh Anh",
    role: "Product Manager",
    avatar: "MA",
    content:
      "Ứng dụng đã giúp team tôi tăng năng suất 40%. Giao diện đẹp và dễ sử dụng!",
  },
  {
    name: "Hoàng Long",
    role: "Software Engineer",
    avatar: "HL",
    content:
      "Tính năng tự động hóa workflow thực sự ấn tượng. Tiết kiệm rất nhiều thời gian.",
  },
  {
    name: "Thu Hà",
    role: "Designer",
    avatar: "TH",
    content:
      "UI/UX tuyệt vời! Đây là ứng dụng todo duy nhất mà tôi thực sự muốn dùng mỗi ngày.",
  },
]

function TestimonialCard({ testimonial }: { testimonial: Testimonial }) {
  return (
    <Card className="border-border/50 bg-card/50 backdrop-blur-sm hover:shadow-lg transition-shadow">
      <CardContent className="pt-6">
        <div className="flex gap-1 mb-4">
          {[...Array(5)].map((_, i) => (
            <Star key={i} className="w-4 h-4 fill-amber-400 text-amber-400" />
          ))}
        </div>
        <p className="text-foreground mb-6 leading-relaxed">
          &ldquo;{testimonial.content}&rdquo;
        </p>
        <div className="flex items-center gap-3">
          <Avatar>
            <AvatarFallback className="bg-gradient-to-br from-violet-500 to-fuchsia-500 text-white text-xs">
              {testimonial.avatar}
            </AvatarFallback>
          </Avatar>
          <div>
            <div className="font-medium text-sm">{testimonial.name}</div>
            <div className="text-xs text-muted-foreground">
              {testimonial.role}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function UserStats() {
  return (
    <div className="mt-12 text-center">
      <div className="inline-flex items-center gap-4 p-4 rounded-2xl bg-card border border-border/50">
        <AvatarGroup>
          <Avatar size="sm">
            <AvatarFallback className="bg-violet-500 text-white text-xs">
              A
            </AvatarFallback>
          </Avatar>
          <Avatar size="sm">
            <AvatarFallback className="bg-fuchsia-500 text-white text-xs">
              B
            </AvatarFallback>
          </Avatar>
          <Avatar size="sm">
            <AvatarFallback className="bg-pink-500 text-white text-xs">
              C
            </AvatarFallback>
          </Avatar>
          <Avatar size="sm">
            <AvatarFallback className="bg-cyan-500 text-white text-xs">
              D
            </AvatarFallback>
          </Avatar>
        </AvatarGroup>
        <div className="text-left">
          <div className="font-semibold">+10,000 người dùng</div>
          <div className="text-xs text-muted-foreground">
            đã tin tưởng TodoApp
          </div>
        </div>
      </div>
    </div>
  )
}

export function TestimonialsSection() {
  return (
    <section id="testimonials" className="py-24 bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center mb-16">
          <Badge variant="outline" className="mb-4">
            Đánh giá
          </Badge>
          <h2 className="text-3xl sm:text-4xl font-bold tracking-tight mb-4">
            Được yêu thích bởi hàng nghìn người
          </h2>
          <p className="text-muted-foreground max-w-2xl mx-auto">
            Xem những gì khách hàng nói về trải nghiệm của họ với TodoApp.
          </p>
        </div>

        <div className="grid md:grid-cols-3 gap-6">
          {testimonials.map((testimonial, index) => (
            <TestimonialCard key={index} testimonial={testimonial} />
          ))}
        </div>

        <UserStats />
      </div>
    </section>
  )
}
