import Link from "next/link"
import { CheckSquare } from "lucide-react"

import { Separator } from "@/registry/new-york-v4/ui/separator"

const footerLinks = {
  product: {
    title: "Sản phẩm",
    links: [
      { label: "Tính năng", href: "#features" },
      { label: "Bảng giá", href: "#pricing" },
      { label: "Tích hợp", href: "#integrations" },
    ],
  },
  support: {
    title: "Hỗ trợ",
    links: [
      { label: "Tài liệu", href: "#docs" },
      { label: "FAQ", href: "#faq" },
      { label: "Liên hệ", href: "#contact" },
    ],
  },
  company: {
    title: "Công ty",
    links: [
      { label: "Về chúng tôi", href: "#about" },
      { label: "Blog", href: "#blog" },
      { label: "Tuyển dụng", href: "#careers" },
    ],
  },
}

function FooterLinkSection({
  title,
  links,
}: {
  title: string
  links: { label: string; href: string }[]
}) {
  return (
    <div>
      <h4 className="font-semibold mb-4 text-sm">{title}</h4>
      <ul className="space-y-2 text-sm text-muted-foreground">
        {links.map((link) => (
          <li key={link.href}>
            <a
              href={link.href}
              className="hover:text-foreground transition-colors"
            >
              {link.label}
            </a>
          </li>
        ))}
      </ul>
    </div>
  )
}

export function Footer() {
  const currentYear = new Date().getFullYear()

  return (
    <footer className="border-t border-border/50 bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8">
          {/* Brand */}
          <div className="col-span-2 md:col-span-1">
            <Link href="/" className="flex items-center gap-2 mb-4">
              <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-gradient-to-br from-violet-600 to-fuchsia-600">
                <CheckSquare className="w-4 h-4 text-white" />
              </div>
              <span className="font-bold bg-gradient-to-r from-violet-600 to-fuchsia-600 bg-clip-text text-transparent">
                TodoApp
              </span>
            </Link>
            <p className="text-sm text-muted-foreground">
              Quản lý công việc thông minh và hiệu quả.
            </p>
          </div>

          {/* Links */}
          <FooterLinkSection {...footerLinks.product} />
          <FooterLinkSection {...footerLinks.support} />
          <FooterLinkSection {...footerLinks.company} />
        </div>

        <Separator className="my-8" />

        <div className="flex flex-col md:flex-row items-center justify-between gap-4 text-sm text-muted-foreground">
          <p>© {currentYear} TodoApp. Mọi quyền được bảo lưu.</p>
          <div className="flex gap-6">
            <a href="#terms" className="hover:text-foreground transition-colors">
              Điều khoản
            </a>
            <a
              href="#privacy"
              className="hover:text-foreground transition-colors"
            >
              Bảo mật
            </a>
          </div>
        </div>
      </div>
    </footer>
  )
}
