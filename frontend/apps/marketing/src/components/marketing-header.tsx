"use client";

import * as React from "react";
import Link from "next/link";
import {
  ArrowUpRight,
  BarChart3,
  BookOpen,
  ChevronDown,
  LayoutGrid,
  Menu,
  Workflow,
  X,
} from "lucide-react";

import { Button } from "@notrelix/ui-web/components/ui/button";
import { ThemeToggle } from "./theme-toggle";
import Image from "next/image";
import { env } from "../config/env";

const productLinks = [
  {
    label: "Tổng quan",
    description: "Một không gian cho mọi công việc",
    icon: LayoutGrid,
    href: "#showcase",
  },
  {
    label: "Tài liệu",
    description: "Biến kiến thức thành hành động",
    icon: BookOpen,
    href: "#features",
  },
  {
    label: "Tự động hóa",
    description: "Để quy trình chạy thay bạn",
    icon: Workflow,
    href: "#features",
  },
  {
    label: "Báo cáo",
    description: "Luôn biết đội ngũ đang ở đâu",
    icon: BarChart3,
    href: "#features",
  },
];

const navLinks: { label: string; href: string; menu?: boolean }[] = [
  { label: "Sản phẩm", href: "#showcase", menu: true },
  { label: "Giải pháp", href: "#use-cases" },
  { label: "Tính năng", href: "#features" },
  { label: "Bảng giá", href: "#pricing" },
  { label: "Tài nguyên", href: "#resources" },
] as const;

export function MarketingHeader() {
  const [mobileOpen, setMobileOpen] = React.useState(false);
  const [productOpen, setProductOpen] = React.useState(false);
  const [scrolled, setScrolled] = React.useState(false);

  React.useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 18);
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setProductOpen(false);
        setMobileOpen(false);
      }
    };

    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("keydown", onKeyDown);
    document.body.style.overflow = mobileOpen ? "hidden" : "";

    return () => {
      window.removeEventListener("scroll", onScroll);
      window.removeEventListener("keydown", onKeyDown);
      document.body.style.overflow = "";
    };
  }, [mobileOpen]);

  const closeMobile = () => setMobileOpen(false);

  return (
    <header className={`v2-header ${scrolled ? "is-scrolled" : ""}`}>
      <div className="v2-container flex h-[4.5rem] items-center justify-between">
        <Link href="/" aria-label="Notrelix - Trang chủ" onClick={closeMobile}>
          <Image
            src="/logo.svg"
            alt=""
            width={36}
            height={28}
            aria-hidden="true"
            className="h-9 w-auto"
            priority
          />
        </Link>

        <nav
          className="hidden items-center gap-1 lg:flex"
          aria-label="Điều hướng chính"
        >
          {navLinks.map((link) => (
            <div key={link.label} className="relative">
              {link.menu ? (
                <button
                  type="button"
                  aria-expanded={productOpen}
                  aria-controls="product-menu"
                  onClick={() => setProductOpen((open) => !open)}
                  className="v2-nav-link"
                >
                  {link.label}
                  <ChevronDown
                    className={`size-3.5 transition-transform ${productOpen ? "rotate-180" : ""}`}
                  />
                </button>
              ) : (
                <a href={link.href} className="v2-nav-link">
                  {link.label}
                </a>
              )}

              {link.menu && productOpen ? (
                <div id="product-menu" className="v2-product-menu" role="menu">
                  <div className="mb-3 px-3 text-[0.68rem] font-semibold uppercase tracking-[0.18em] text-[var(--v2-muted)]">
                    Khám phá Notrelix
                  </div>
                  {productLinks.map((item) => {
                    const Icon = item.icon;
                    return (
                      <a
                        key={item.label}
                        href={item.href}
                        role="menuitem"
                        onClick={() => setProductOpen(false)}
                        className="v2-product-link"
                      >
                        <span className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-[var(--v2-lilac)] text-[var(--v2-cobalt)]">
                          <Icon className="size-4" />
                        </span>
                        <span>
                          <span className="block text-sm font-semibold text-[var(--v2-ink)]">
                            {item.label}
                          </span>
                          <span className="mt-0.5 block text-xs text-[var(--v2-muted)]">
                            {item.description}
                          </span>
                        </span>
                      </a>
                    );
                  })}
                </div>
              ) : null}
            </div>
          ))}
        </nav>

        <div className="flex items-center gap-2">
          <ThemeToggle />
          <a href={`${env.webAppUrl}/sign-in`} className="hidden sm:block">
            <Button
              variant="ghost"
              size="sm"
              className="px-4 text-[var(--v2-ink)] hover:bg-[var(--v2-lilac)]"
            >
              Đăng nhập
            </Button>
          </a>
          <a href={`${env.webAppUrl}/sign-up`} className="hidden sm:block">
            <Button size="sm" className="px-4 v2-primary-button">
              Dùng thử miễn phí
              <ArrowUpRight className="ml-1.5 size-3.5" />
            </Button>
          </a>
          <button
            type="button"
            aria-label={mobileOpen ? "Đóng menu" : "Mở menu"}
            aria-expanded={mobileOpen}
            onClick={() => setMobileOpen((open) => !open)}
            className="flex size-10 items-center justify-center rounded-xl border border-[var(--v2-line)] text-[var(--v2-ink)] transition-colors hover:bg-[var(--v2-lilac)] lg:hidden"
          >
            {mobileOpen ? (
              <X className="size-5" />
            ) : (
              <Menu className="size-5" />
            )}
          </button>
        </div>
      </div>

      {mobileOpen ? (
        <div
          className="v2-mobile-menu lg:hidden"
          role="dialog"
          aria-modal="true"
          aria-label="Menu điều hướng"
        >
          <nav
            className="v2-container flex flex-col gap-1 py-5"
            aria-label="Điều hướng mobile"
          >
            {navLinks.map((link) => (
              <a
                key={link.label}
                href={link.href}
                onClick={closeMobile}
                className="v2-mobile-link"
              >
                {link.label}
              </a>
            ))}
            <div className="mt-3 grid grid-cols-2 gap-2 border-t border-[var(--v2-line)] pt-4">
              <a
                href={`${env.webAppUrl}/sign-in`}
                onClick={closeMobile}
                className="v2-mobile-link justify-center border border-[var(--v2-line)]"
              >
                Đăng nhập
              </a>
              <a
                href={`${env.webAppUrl}/sign-up`}
                onClick={closeMobile}
                className="v2-mobile-link justify-center bg-[var(--v2-ink)] text-white"
              >
                Dùng thử miễn phí
              </a>
            </div>
          </nav>
        </div>
      ) : null}
    </header>
  );
}
