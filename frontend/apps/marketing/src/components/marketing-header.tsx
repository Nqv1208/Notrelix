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
import { useTranslations } from "next-intl";
import type { Messages } from "../messages/en";

import { Button } from "@notrelix/ui-web/components/ui/button";
import { ThemeToggle } from "./theme-toggle";
import Image from "next/image";
import { env } from "../config/env";

const productLinkIcons = [LayoutGrid, BookOpen, Workflow, BarChart3];

export function MarketingHeader() {
  const t = useTranslations("header");
  const navLinks = t.raw("navLinks") as Messages["header"]["navLinks"];
  const productLinks = t.raw(
    "productLinks",
  ) as Messages["header"]["productLinks"];

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
    <header className={`header ${scrolled ? "is-scrolled" : ""}`}>
      <div className="container flex h-[4.5rem] items-center justify-between">
        <Link href="/" aria-label={t("logoAria")} onClick={closeMobile}>
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
          aria-label={t("navAria")}
        >
          {navLinks.map((link) => (
            <div key={link.label} className="relative">
              {link.href === "#showcase" ? (
                <button
                  type="button"
                  aria-expanded={productOpen}
                  aria-controls="product-menu"
                  onClick={() => setProductOpen((open) => !open)}
                  className="nav-link"
                >
                  {link.label}
                  <ChevronDown
                    className={`size-3.5 transition-transform ${productOpen ? "rotate-180" : ""}`}
                  />
                </button>
              ) : (
                <a href={link.href} className="nav-link">
                  {link.label}
                </a>
              )}

              {link.href === "#showcase" && productOpen ? (
                <div id="product-menu" className="product-menu" role="menu">
                  <div className="mb-3 px-3 text-[0.68rem] font-semibold uppercase tracking-[0.18em] text-[var(--muted-text)]">
                    {t("exploreLabel")}
                  </div>
                  {productLinks.map((item, index) => {
                    const Icon = productLinkIcons[index] ?? LayoutGrid;
                    return (
                      <a
                        key={item.label}
                        href={item.href}
                        role="menuitem"
                        onClick={() => setProductOpen(false)}
                        className="product-link"
                      >
                        <span className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-[var(--lilac)] text-[var(--cobalt)]">
                          <Icon className="size-4" />
                        </span>
                        <span>
                          <span className="block text-sm font-semibold text-[var(--ink)]">
                            {item.label}
                          </span>
                          <span className="mt-0.5 block text-xs text-[var(--muted-text)]">
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
              className="px-4 text-[var(--ink)] hover:bg-[var(--lilac)]"
            >
              {t("signIn")}
            </Button>
          </a>
          <a href={`${env.webAppUrl}/sign-up`} className="hidden sm:block">
            <Button size="sm" className="px-4 primary-button">
              {t("tryFree")}
              <ArrowUpRight className="ml-1.5 size-3.5" />
            </Button>
          </a>
          <button
            type="button"
            aria-label={mobileOpen ? t("closeMenu") : t("openMenu")}
            aria-expanded={mobileOpen}
            onClick={() => setMobileOpen((open) => !open)}
            className="flex size-10 items-center justify-center rounded-xl border border-[var(--line)] text-[var(--ink)] transition-colors hover:bg-[var(--lilac)] lg:hidden"
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
          className="mobile-menu lg:hidden"
          role="dialog"
          aria-modal="true"
          aria-label={t("menuAria")}
        >
          <nav
            className="container flex flex-col gap-1 py-5"
            aria-label={t("mobileNavAria")}
          >
            {navLinks.map((link) => (
              <a
                key={link.label}
                href={link.href}
                onClick={closeMobile}
                className="mobile-link"
              >
                {link.label}
              </a>
            ))}
            <div className="mt-3 grid grid-cols-2 gap-2 border-t border-[var(--line)] pt-4">
              <a
                href={`${env.webAppUrl}/sign-in`}
                onClick={closeMobile}
                className="mobile-link justify-center border border-[var(--line)]"
              >
                {t("signIn")}
              </a>
              <a
                href={`${env.webAppUrl}/sign-up`}
                onClick={closeMobile}
                className="mobile-link justify-center bg-[var(--ink)] text-white"
              >
                {t("tryFree")}
              </a>
            </div>
          </nav>
        </div>
      ) : null}
    </header>
  );
}
