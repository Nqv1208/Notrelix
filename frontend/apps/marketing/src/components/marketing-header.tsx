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

import { MarketingButton } from "./marketing-button";
import Image from "next/image";
import { env } from "../config/env";

const productLinkIcons = [LayoutGrid, BookOpen, Workflow, BarChart3];

export function MarketingHeader() {
  const t = useTranslations("header");
  const navLinks = t.raw("navLinks") as Messages["header"]["navLinks"];
  const productLinks = t.raw(
    "productLinks",
  ) as Messages["header"]["productLinks"];

  const [productOpen, setProductOpen] = React.useState(false);
  const [mobileOpen, setMobileOpen] = React.useState(false);
  const [scrolled, setScrolled] = React.useState(false);

  React.useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setProductOpen(false);
        setMobileOpen(false);
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  React.useEffect(() => {
    const sentinel = document.getElementById("header-scroll-sentinel");

    if (!sentinel) {
      return;
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry) {
          setScrolled(!entry.isIntersecting);
        }
      },
      { threshold: 0 },
    );

    observer.observe(sentinel);

    return () => {
      observer.disconnect();
    };
  }, []);

  return (
    <header
      className={`header marketing-header ${scrolled ? "is-scrolled" : ""}`}
      data-scrolled={scrolled}
    >
      <div className="header-visual-shell" aria-hidden="true" />
      <div className="header-content">
        <Link href="/" aria-label={t("logoAria")} className="header-logo">
          <Image
            src="/logo.svg"
            alt=""
            width={36}
            height={28}
            aria-hidden="true"
            className="header-logo-image"
            priority
          />
        </Link>

        <nav className="header-nav" aria-label={t("navAria")}>
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

        <div className="header-actions">
          <MarketingButton
            variant="ghost"
            size="sm"
            href={`${env.webAppUrl}/sign-in`}
            className="header-action-link"
          >
            {t("signIn")}
          </MarketingButton>
          <MarketingButton
            variant="primary"
            size="sm"
            href={`${env.webAppUrl}/sign-up`}
            className="header-action-link"
          >
            {t("tryFree")}
            <ArrowUpRight className="size-3.5" />
          </MarketingButton>
          <button
            type="button"
            className="mobile-menu-toggle"
            aria-expanded={mobileOpen}
            aria-controls="mobile-menu"
            aria-label={mobileOpen ? t("closeMenu") : t("openMenu")}
            onClick={() => setMobileOpen((open) => !open)}
          >
            {mobileOpen ? (
              <X className="size-5" aria-hidden="true" />
            ) : (
              <Menu className="size-5" aria-hidden="true" />
            )}
          </button>
        </div>
      </div>

      <div
        id="mobile-menu"
        role="dialog"
        aria-modal={mobileOpen ? true : undefined}
        aria-label={t("menuAria")}
        className={`mobile-menu ${mobileOpen ? "is-open" : ""}`}
        hidden={!mobileOpen}
      >
        <nav aria-label={t("mobileNavAria")} className="mobile-menu-nav">
          {navLinks.map((link) => (
            <a
              key={link.label}
              href={link.href}
              className="mobile-menu-link"
              onClick={() => setMobileOpen(false)}
            >
              {link.label}
            </a>
          ))}
        </nav>
        <div className="mobile-menu-actions">
          <MarketingButton
            variant="ghost"
            size="md"
            href={`${env.webAppUrl}/sign-in`}
            onClick={() => setMobileOpen(false)}
          >
            {t("signIn")}
          </MarketingButton>
          <MarketingButton
            variant="primary"
            size="md"
            href={`${env.webAppUrl}/sign-up`}
            onClick={() => setMobileOpen(false)}
          >
            {t("tryFree")}
            <ArrowUpRight className="size-3.5" />
          </MarketingButton>
        </div>
      </div>
    </header>
  );
}
